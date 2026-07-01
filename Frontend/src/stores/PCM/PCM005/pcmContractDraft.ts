import { defineStore } from "pinia";
import { usePcm005DetailStore } from "./pcm005";
import { useAuthenticationStore } from "@/stores/authentication";
import type { TAttachmentBase, TBuyerInfo, TContractDraftBody, TGuaranteeInfo, TPenaltyInfo, TRetentionPayment, TVendor } from "@/views/PP/models/PP0010/ContractDraft";
import { computed, ref } from "vue";
import { ReasonDialogType } from "@/enums/dialog";
import { showReasonDialogAsync } from "@/helpers/dialog";
import ToastHelper from "@/helpers/toast";
import contractDraftService from "@/views/PP/services/PP010/ContractDraftService";
import { HttpStatusCode } from "axios";
import type { CardSelectItems, Option } from "@/models/shared/option";
import { TAgreementBaseType, TContractDraftStatus } from "@/views/PP/enums/pp010";
import { AcceptorStatus, AcceptorType } from "@/enums/participants";
import type { Ref } from "vue";
import { EGroupCode } from "@/enums/shared";
import SharedService from "@/services/Shared/dropdown";
import { ContractDraftTemplate } from "@/enums/contractDraftt";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import { SectionProcessType } from "@/enums/operations";
import operationService from "@/services/Shared/operations";
import type { ParticipantsAcceptor } from "@/models/shared/participants";

export const usePcmContractDraftStore = defineStore("pcm-contract-draft-store", () => {
  const pcmStore = usePcm005DetailStore();
  const authStore = useAuthenticationStore();
  const isHasPermission = ref(false);
  const initial = ref(true);

  const body = ref<TContractDraftBody>({
    contractType: "CMRentalType001",
    detail: {
      buyer: {
        province: {},
        district: {},
        subDistrict: {}
      },
      vendor: {},
      attachments: [] as TAttachmentBase[],
    },
  } as TContractDraftBody);

  const cloneBody = ref<TContractDraftBody>({} as TContractDraftBody);

  const onClearBody = () => {
    body.value = {
      contractType: "CMRentalType001",
      detail: {
        buyer: {
          province: {},
          district: {},
          subDistrict: {}
        },
        vendor: {},
        attachments: [] as TAttachmentBase[],
      },
    } as TContractDraftBody;
  };

  const vendorId = ref<string>();

  const vendorList = ref<CardSelectItems[]>([] as CardSelectItems[]);

  const canEdit = computed(() => {
    const status = [TContractDraftStatus.Draft, TContractDraftStatus.Rejected, TContractDraftStatus.Edit].includes(body.value.status) || !body.value.status;

    return status && isHasPermission.value;
  });

  const canRestoreContractDraftDocument = computed(() => {
    return canEdit.value && (body.value.contractDraftDocumentVersions?.length ?? 0) > 1;
  });

  const canRestoreApprovalContractDraftDocument = computed(() => {
    return canEdit.value && (body.value.approvalContractDraftDocumentVersions?.length ?? 0) > 1;
  });

  const canRestoreConfidentialContractDraftDocument = computed(() => {
    return canEdit.value && (body.value.confidentialContractDraftDocumentVersions?.length ?? 0) > 1;
  });

  const canAcceptAndReject = computed(() => {
    const status = body.value.status === TContractDraftStatus.Pending;
    const checkUser = body.value.acceptors?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && checkUser;
  });

  const isLastApproval = computed(() => {
    const approvalUser = body.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending);

    if (!approvalUser) {
      return false;
    }

    const current = approvalUser.find(f => f.isCurrent);

    if (!current) {
      return false;
    }

    return canAcceptAndReject.value && current.sequence === approvalUser[approvalUser.length - 1].sequence;
  });

  const getVendorListAsync = async (initialVendorId?: string) => {
    if (!pcmStore.body.id) return;

    const { data, status } = await contractDraftService.getContractDraftVendorList(pcmStore.body.id);

    if (status == HttpStatusCode.Ok) {
      body.value.id = data.id;
      isHasPermission.value = data.hasEditPermission;

      vendorList.value = data.vendors.map((item: any) => ({
        title: item.contractNumber,
        description: item.name,
        value: item.id,
        status: item.status,
        isCompleted: item.isCompleted,
      } as CardSelectItems));

      const matchedVendor = initialVendorId && vendorList.value.find(v => v.value === initialVendorId);
      const selectedVendor = matchedVendor ? matchedVendor.value : vendorList.value[0]?.value;

      if (selectedVendor) {
        vendorId.value = selectedVendor.toString();

        await getContractDraftByVendorIdAsync();
      }

      const hasAcceptorSign = body.value.acceptors?.some(
        a => a.acceptorType === AcceptorType.AcceptorSign
      );

      if (!hasAcceptorSign) {
        await onGetDefaultAcceptorAsync();
      }

      initial.value = false;

      return;
    }

    isHasPermission.value = false;
  };

  const INVALID_FIELDS = [
    "contractType",
    "contractSignedDate",
    "template",
    "periodConditionType"
  ];

  const isInvalidValue = (key: string, value: any) => {
    return INVALID_FIELDS.includes(key) && (value === "" || value === "0001-01-01T00:00:00+00:00");
  };

  const filterSelectedInvalidFields = (obj: Record<string, any>) => {
    const result: Record<string, any> = {};
    for (const key in obj) {
      const value = obj[key];

      if (value !== null && typeof value === "object" && !Array.isArray(value)) {
        result[key] = filterSelectedInvalidFields(value);
      } else if (!isInvalidValue(key, value)) {
        result[key] = value;
      }
    }
    return result;
  };

  const getContractDraftByVendorIdAsync = async () => {
    const procurementId = pcmStore.body.id;
    const contractDraftId = pcmStore.body.contractDraft?.id;
    const vendor = vendorId.value;

    if (!procurementId || !contractDraftId || !vendor) return;

    const { data, status } = await contractDraftService.getContractDraftByVendorId(procurementId, contractDraftId, vendor);

    if (status === HttpStatusCode.Ok) {
      const cleanedData = filterSelectedInvalidFields(data);
      const originalId = body.value.id;

      body.value = {
        ...body.value,
        ...cleanedData,
        id: originalId,
      };

      if (body.value.detail.payment == null || body.value.detail.agreement == null) {
        switchTemplate();
      }

      cloneBody.value = JSON.parse(JSON.stringify(body.value));

      await getCmRentalTypeAttacement(body.value.template);
    }
  };

  const onUpdateContractDraft = async (isSaveDraft?: boolean, newStatus?: TContractDraftStatus) => {
    const procurementId = pcmStore.body.id;
    const contractDraftId = pcmStore.body.contractDraft?.id;
    const bodyId = vendorId.value;

    if (!procurementId || !contractDraftId || !bodyId) return;

    const payload = {
      ...body.value,
      detail: {
        ...body.value.detail,
        payment: undefined,
      }
    } as TContractDraftBody;

    const { status } = await contractDraftService.updateContractDraft(procurementId, contractDraftId, bodyId, payload, newStatus, isSaveDraft);

    if (status === HttpStatusCode.Ok) {
      newStatus == TContractDraftStatus.Pending && ToastHelper.sendApproveConfirmMessageToast();
      newStatus == TContractDraftStatus.Edit && ToastHelper.recallEditMessageToast();
      !newStatus && ToastHelper.updatedMessageToast();

      await getContractDraftByVendorIdAsync();
    }
  };

  const approveAsync = async () => {
    const procurementId = pcmStore.body.id;
    const contractDraftId = pcmStore.body.contractDraft?.id;
    const vendor = vendorId.value;

    if (!procurementId || !contractDraftId || !vendor) return;

    const res = await showReasonDialogAsync(isLastApproval.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (res.isConfirm) {
      const { status } = await contractDraftService.approveContractDraft(procurementId, contractDraftId, vendor, res.reason);

      if (status == HttpStatusCode.Ok) {
        ToastHelper.approvedMessageToast();

        await pcmStore.getDetailAsync(procurementId);
        await getContractDraftByVendorIdAsync();
      }

      if (status == HttpStatusCode.NotFound) {
        ToastHelper.notFoundMessageToast();
      }
    }
  };

  const rejectAsync = async () => {
    const procurementId = pcmStore.body.id;
    const contractDraftId = pcmStore.body.contractDraft?.id;
    const vendor = vendorId.value;

    if (!procurementId || !contractDraftId || !vendor) return;

    const res = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (res.isConfirm && res.reason) {
      const { status } = await contractDraftService.rejectContractDraft(procurementId, contractDraftId, vendor, res.reason);

      if (status == HttpStatusCode.Ok) {
        ToastHelper.sendEditMessageToast();

        await getContractDraftByVendorIdAsync();
      }
    }
  };

  const getOptionsAsync = async (
    target: Ref<Option[]>,
    groupCode: EGroupCode,
    parentCode?: string
  ): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(groupCode, parentCode);
    if (status === HttpStatusCode.Ok) {
      target.value = data;
    }
  };

  const cmRentalTypeOptions = ref<Option[]>([]);
  const cmRentalTpOptions = ref<Option[]>([]);
  const cmRentalTypeAttacement = ref<Option[]>([]);

  const getCmRentalTypeAsync = async () => getOptionsAsync(cmRentalTypeOptions, EGroupCode.CMRentalType);
  const getcmRentalTpAsync = async () => getOptionsAsync(cmRentalTpOptions, EGroupCode.CMRentalType, "CMRentalType001");
  const getCmRentalTypeAttacement = async (parentCode: string) => getOptionsAsync(cmRentalTypeAttacement, EGroupCode.CMRentalTpl, parentCode);

  const getReviewDocumentAsync = async (id: string, procurementId: string, vendorId: string, documentType: string): Promise<string> => {
    const { data, status } = await contractDraftService.getReviewDocumentAsync(id, procurementId, vendorId, documentType);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };

  const resetDocumentAsync = async (documentType: 'ContractDraft' | 'ApprovalContractDraft' | 'ConfidentialContractDraft'): Promise<void> => {
    const procurementId = pcmStore.body.id;
    const contractDraftId = pcmStore.body.contractDraft?.id;
    const vendor = vendorId.value;

    if (!procurementId || !contractDraftId || !vendor) return;

    const { status } = await contractDraftService.resetDocumentAsync(procurementId, contractDraftId, vendor, documentType);

    if (status === HttpStatusCode.Ok) {
      await getContractDraftByVendorIdAsync();
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
    }
  };

  const switchTemplate = () => {
    const currentBuyer = body.value.detail?.buyer ?? ({} as TBuyerInfo);
    const currentVendor = body.value.detail?.vendor ?? ({} as TVendor);
    const currentAttachments = body.value.detail?.attachments ?? ([] as TAttachmentBase[]);
    const guarantee = body.value.detail?.guarantee ?? ({} as TGuaranteeInfo);

    const commonDetail = {
      attachments: currentAttachments,
      guarantee: guarantee,
      penalty: {} as TPenaltyInfo,
      buyer: currentBuyer,
      vendor: currentVendor,
    };

    switch (body.value.template) {
      case ContractDraftTemplate.CMRentalTpl001:
      case ContractDraftTemplate.CMRentalTpl002:
      case ContractDraftTemplate.CMRentalTpl003:
      case ContractDraftTemplate.CMRentalTpl004:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.RentalDurationWorkplace,
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode ?? body.value?.vatRateTypeCode,
              duration: {},
              workplaceProvince: {},
              workplaceDistrict: {},
              workplaceSubDistrict: {},
            },
            attachments: [] as TAttachmentBase[],
            delivery: {},
            advancePayment: { hasAdvancePayment: false },
            warranty: {
              hasWarranty: false,
              fixingDeadlinePeriod: {},
              warrantyPeriod: {},
            },
            retentionPayment: { hasRetentionPayment: false, amount: 0, percentage: 0 } as unknown as TRetentionPayment,
            termination: { duration: {} },
          }
        } as TContractDraftBody;
        break;

      default:
        break;
    }
  };

  const canSaveDateSign = computed(() => {
    return body.value.status === TContractDraftStatus.Approved && isHasPermission.value;
  });

  const canRecall = computed(() => {
    return body.value.status === TContractDraftStatus.Pending && isHasPermission.value;
  });

  const onGetDefaultAcceptorAsync = async (): Promise<void> => {
    const operators = body.value.operators ?? [];

    const lastOperator = operators.length > 0
      ? operators.reduce((prev, curr) => curr.sequence > prev.sequence ? curr : prev, operators[0])
      : null;

    const dataSearch: defaultAcceptorCriteria = {
      processType: SectionProcessType.ContractDraft,
      budget: body.value.budget,
      supplyMethodCode: pcmStore.body.supplyMethodCode,
      supplyMethodSpecialTypeCode: pcmStore.body.supplyMethodSpecialTypeCode,
      userId: lastOperator ? lastOperator.userId : authStore.profile.id,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(dataSearch);

    if (status === HttpStatusCode.Ok && data.length > 0) {
      const lastItem = data[data.length - 1];

      const existingAcceptorSignIndex = body.value.acceptors?.findIndex(
        a => a.acceptorType === AcceptorType.AcceptorSign
      ) ?? -1;

      if (existingAcceptorSignIndex !== -1) {
        body.value.acceptors[existingAcceptorSignIndex] = {
          ...body.value.acceptors[existingAcceptorSignIndex],
          userId: lastItem.userId,
          fullName: lastItem.fullName,
          positionName: lastItem.fullPositionName,
          departmentName: lastItem.businessUnitName,
        };
      } else {
        const newAcceptorSign: ParticipantsAcceptor = {
          acceptorType: AcceptorType.AcceptorSign,
          fullName: lastItem.fullName,
          positionName: lastItem.fullPositionName,
          sequence: 1,
          status: AcceptorStatus.Draft,
          userId: lastItem.userId,
          departmentName: lastItem.businessUnitName,
        } as ParticipantsAcceptor;

        body.value.acceptors = [newAcceptorSign, ...(body.value.acceptors ?? [])];
      }
    }
  };

  const onGetDefaultContractDirector = async (): Promise<void> => {
    const { data, status } = await operationService.getContractDirectorAsync();

    if (status === HttpStatusCode.Ok) {
      const existingAcceptorSigns = (body.value.acceptors ?? []).filter(
        (a: ParticipantsAcceptor): boolean => a.acceptorType === AcceptorType.AcceptorSign
      );

      const newAcceptors: ParticipantsAcceptor[] = data.map((m, i: number): ParticipantsAcceptor => ({
        sequence: i + 1,
        userId: m.userId,
        fullName: m.fullName,
        positionName: m.positionName,
        departmentName: m.departmentName,
        acceptorType: AcceptorType.Approver,
        status: AcceptorStatus.Draft,
      } as ParticipantsAcceptor));

      body.value.acceptors = [...newAcceptors, ...existingAcceptorSigns];
    }
  };

  return {
    body,
    onClearBody,
    vendorId,
    vendorList,
    switchTemplate,
    cloneBody,
    initial,
    states: {
      canEdit,
      canAcceptAndReject,
      isLastApproval,
      canSaveDateSign,
      canRecall,
      canRestoreContractDraftDocument,
      canRestoreApprovalContractDraftDocument,
      canRestoreConfidentialContractDraftDocument,
    },
    dropdown: {
      cmRentalTypeOptions,
      cmRentalTpOptions,
      cmRentalTypeAttacement
    },
    api: {
      getContractDraftByVendorIdAsync,
      onUpdateContractDraft,
      approveAsync,
      rejectAsync,
      getVendorListAsync,
      getCmRentalTypeAsync,
      getcmRentalTpAsync,
      getCmRentalTypeAttacement,
      getReviewDocumentAsync,
      resetDocumentAsync,
      onGetDefaultAcceptorAsync,
      onGetDefaultContractDirector,
    }
  }
});
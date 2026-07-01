import { defineStore } from "pinia";
import { HttpStatusCode, type AxiosResponse } from "axios";
import { computed, ref, watch } from "vue";
import type { PP009Detail, VendorInfo } from "../../models/PP009/pp009Model";
import { PP009Status } from "../../enums/pp009";
import type { CardSelectItems } from "@/models/shared/option";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import ToastHelper from "@/helpers/toast";
import { usePPDetailStore } from "../../../../stores/PP/ppStore";
import type { ParticipantsAcceptor } from "@/models/shared/participants";
import { AcceptorStatus, AcceptorType } from "@/enums/participants";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import { useAuthenticationStore } from "@/stores/authentication";
import PP009Service from "../../services/PP009/PP009Service";
import operationService from "@/services/Shared/operations";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import { SectionProcessType } from "@/enums/operations";
import type { EntrepreneurType } from "@/enums/shared";
import type { EntrepreneurAttachments, OnlyFileAttachment } from "@/models/shared/uploadFile";

export const usePP009DetailStore = defineStore('pp009-detail-store', () => {
  const procurementStore = usePPDetailStore();

  const initBody = {
    procurementId: procurementStore.procurementDetail.id,
    vendors: [] as Array<VendorInfo>,
    status: PP009Status.Draft,
    acceptors: [] as Array<ParticipantsAcceptor>,
    hasEditPermission: false,
  } as PP009Detail;


  const auth = useAuthenticationStore();

  const body = ref<PP009Detail>(structuredClone(initBody));
  const venderOptions = ref<Array<CardSelectItems>>([]);
  const currentVendor = ref<string>();
  const currentStatus = ref<PP009Status>(PP009Status.Draft);

  const onGetByIdAsync = async (id?: string) => {
    const { data, status } = await PP009Service.onGetByIdAsync(procurementStore.procurementDetail.id!, id);

    if (status === HttpStatusCode.Ok) {
      body.value = structuredClone(data);
      currentStatus.value = data.status;

      const previousVendor = currentVendor.value;
      venderOptions.value = [];
      currentVendor.value = undefined;

      if (data.vendors.length > 0) {
        venderOptions.value = data.vendors.map(s => ({ title: s.contractNumber, description: s.budgetDetail, value: s.purchaseOrderApprovalContractId, isCompleted: !!(s.email && s.contractOfficerPhone && s.coiDate && s.watchlistDate && s.egpDate) } as CardSelectItems));
        const hasVendor = venderOptions.value.some(v => v.value === previousVendor);
        currentVendor.value = hasVendor ? previousVendor : venderOptions.value[0].value as string;
      }
    }
  };

  watch(
    () => body.value.vendors.map(v => `${v.purchaseOrderApprovalContractId}|${v.email}|${v.contractOfficerPhone}|${v.coiDate}|${v.watchlistDate}|${v.egpDate}`),
    () => {
      for (const option of venderOptions.value) {
        const vendor = body.value.vendors.find(v => v.purchaseOrderApprovalContractId === option.value);
        if (vendor) {
          option.isCompleted = !!(vendor.email && vendor.contractOfficerPhone && vendor.coiDate && vendor.watchlistDate && vendor.egpDate);
        }
      }
    }
  );

  const syncCheckDataByTaxId = (sourceVendor: VendorInfo) => {
    const taxId = sourceVendor.entrepreneur?.taxpayerIdentificationNo;
    if (!taxId) return;

    for (const vendor of body.value.vendors) {
      if (vendor.purchaseOrderApprovalContractId === sourceVendor.purchaseOrderApprovalContractId) continue;
      if (vendor.entrepreneur?.taxpayerIdentificationNo !== taxId) continue;

      vendor.egpResult = sourceVendor.egpResult;
      vendor.egpRemark = sourceVendor.egpRemark;
      vendor.egpDate = sourceVendor.egpDate;
      vendor.coiResult = sourceVendor.coiResult;
      vendor.coiRemark = sourceVendor.coiRemark;
      vendor.coiDate = sourceVendor.coiDate;
      vendor.coiCheckerResult = sourceVendor.coiCheckerResult;
      vendor.watchlistResult = sourceVendor.watchlistResult;
      vendor.watchlistRemark = sourceVendor.watchlistRemark;
      vendor.watchlistDate = sourceVendor.watchlistDate;
      vendor.watchlistCheckerResult = sourceVendor.watchlistCheckerResult;
      vendor.shareholder = JSON.parse(JSON.stringify(sourceVendor.shareholder));
    }
  };

  const onResetBody = () => {
    body.value = structuredClone(initBody);
  }

  const onSubmitAsync = async () => {
    if (body.value.id) {
      await onUpdateAsync(body.value.id);

      return;
    }

    await onCreateAsync();
  };

  const validatePayload = (value: PP009Detail): boolean => {
    if (value.acceptors.length <= 0) {
      ToastHelper.approvalAtLeastMessageToast();

      return false;
    }

    const hasIncompleteCoi = value.vendors.some(v => v.coiResult == null);
    if (hasIncompleteCoi) {
      ToastHelper.errorDescription('กรุณาตรวจสอบข้อมูล COI ให้ครบถ้วนทุกราย');

      return false;
    }

    const hasIncompleteWatchlist = value.vendors.some(v => v.watchlistResult == null);
    if (hasIncompleteWatchlist) {
      ToastHelper.errorDescription('กรุณาตรวจสอบข้อมูล Watchlist ให้ครบถ้วนทุกราย');

      return false;
    }

    const hasIncompleteEgp = value.vendors.some(v => v.egpResult == null);
    if (hasIncompleteEgp) {
      ToastHelper.errorDescription('กรุณาตรวจสอบข้อมูล EGP ให้ครบถ้วนทุกราย');

      return false;
    }

    return true;
  };

  const onCreateAsync = async (contractInvitationStatus?: PP009Status) => {
    const payload = {
      ...body.value,
      status: contractInvitationStatus ?? body.value.status,
    } as PP009Detail;

    const { data, status } = await PP009Service.onCreateAsync(procurementStore.procurementDetail.id!, payload);

    if (status === HttpStatusCode.Created) {
      contractInvitationStatus ? ToastHelper.sendApproveConfirmMessageToast() : ToastHelper.createdMessageToast();

      await reStateDataAsync(data);

      return;
    }

    await reStateDataAsync();
  };

  const onUpdateAsync = async (id: string, contractInvitationStatus?: PP009Status) => {
    const payload = {
      ...body.value,
      status: contractInvitationStatus ?? body.value.status,
    } as PP009Detail;

    const mapStatusToast: Record<PP009Status, () => void> = {
      Draft: () => ToastHelper.updatedMessageToast(),
      WaitingApproval: () => ToastHelper.sendApproveMessageToast(),
      Edit: () => ToastHelper.recallEditMessageToast(),
      [PP009Status.Approved]: function (): void {
        throw new Error("Function not implemented.");
      },
      [PP009Status.Rejected]: () => ToastHelper.sendEditMessageToast(),
    };

    const { status } = await PP009Service.onUpdateByIdAsync(procurementStore.procurementDetail.id!, id, payload);

    if (status === HttpStatusCode.Ok) {
      contractInvitationStatus ? mapStatusToast[contractInvitationStatus]() : ToastHelper.updatedMessageToast();
    }

    await onGetByIdAsync(id);
  };

  const onSendApprovalAsync = async () => {
    if (!validatePayload(body.value)) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

    if (body.value.id) {
      await onUpdateAsync(body.value.id, PP009Status.WaitingApproval);

      return;
    }

    await onCreateAsync(PP009Status.WaitingApproval);
  };

  const onRecallAsync = async () => {
    if (!body.value.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

    await onUpdateAsync(body.value.id, PP009Status.Edit);
  };

  const onApprovedRejectedAsync = async (type: 'Approve' | 'Reject') => {
    if (!body.value.id) return;

    const dialogType: Record<'Approve' | 'Reject', ReasonDialogType> = {
      Approve: isLastApproval.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted,
      Reject: ReasonDialogType.Reject
    };

    const resp = await showReasonDialogAsync(dialogType[type], type === 'Reject');

    if (!resp.isConfirm) return;

    const apiMap: Record<'Approve' | 'Reject', () => Promise<AxiosResponse<any, any>>> = {
      Approve: () => PP009Service.onApprovedAsync(procurementStore.procurementDetail.id!, body.value.id!, { remark: resp.reason }),
      Reject: () => PP009Service.onRejectedAsync(procurementStore.procurementDetail.id!, body.value.id!, { remark: resp.reason }),
    };

    const { status } = await apiMap[type]();

    if (status === HttpStatusCode.Ok) {
      type === 'Approve' ? ToastHelper.approvedMessageToast() : ToastHelper.sendEditMessageToast();
    }

    await reStateDataAsync(body.value.id);
  };

  const reStateDataAsync = async (id?: string) => {
    await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id!);
    await onGetByIdAsync(id);
  };

  const isRequired = (purchaseOrderApprovalContractId: string) => computed(() => {

    if (currentStatus.value === PP009Status.Draft) {
      return currentVendor.value === purchaseOrderApprovalContractId;
    }

    return true;
  });

  const onSetDefaultAcceptors = async () => {
    const totalVendorAgreePrice = body.value.vendors.reduce((prev, curr) => prev + curr.agreedPrice, 0);

    if (auth.profile.id !== body.value.hasPermissionUserId || totalVendorAgreePrice === 0) return;

    // For supply method "sixty" over threshold, proceed with unified default acceptor retrieval below.

    const params = {
      processType: SectionProcessType.ContractInvitation,
      budget: totalVendorAgreePrice,
      userId: body.value.hasPermissionUserId ?? auth.profile.id,
      supplyMethodCode: procurementStore.procurementDetail.supplyMethodCode,
      supplyMethodSpecialTypeCode: procurementStore.procurementDetail.supplyMethodSpecialTypeCode,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status === HttpStatusCode.Ok) {
      const uniqueData = Array.from(
        new Map(data.map((item) => [item.userId, item])).values()
      );

      body.value.acceptors = [...uniqueData.map((m, i) => ({
        sequence: i + 1,
        userId: m.userId,
        fullName: m.fullName,
        positionName: m.fullPositionName,
        departmentName: m.businessUnitName,
        acceptorType: AcceptorType.Approver,
        status: AcceptorStatus.Draft,
      }))];
    }
  };

  const isEdit = computed(() => [PP009Status.Draft, PP009Status.Edit, PP009Status.Rejected].includes(body.value.status) && body.value.hasEditPermission);

  const isRecall = computed(() => [PP009Status.WaitingApproval].includes(body.value.status) && body.value.acceptors.every(s => s.status === AcceptorStatus.Pending) && body.value.hasEditPermission);

  const isCurrentApproval = computed(() => {
    if (!body.value.acceptors) return false;
    const status = [PP009Status.WaitingApproval].includes(body.value.status);
    const checkQue = isCurrentPendingAcceptor(body.value.acceptors, auth.profile.id, AcceptorType.Approver);
    return status && checkQue;
  });

  const isLastApproval = computed(() => {
    if (body.value.acceptors.length === 0) return false;

    const pendingData = body.value.acceptors
      .filter(f => f.status === AcceptorStatus.Pending);

    const current = pendingData
      .reduce((prev, curr) => curr.sequence < prev.sequence ? curr : prev,
        pendingData[0]);

    return isCurrentApproval.value && current.sequence === pendingData[pendingData.length - 1].sequence;
  });

  const getReviewDocumentAsync = async (id: string, procurementId: string, vendorId: string): Promise<string> => {
    const { data, status } = await PP009Service.getReviewDocumentAsync(id, procurementId, vendorId);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };

  const onSendEmailInviteAsync = async (vendorId: string, email: string, emailTemplate: string, attachments: Array<OnlyFileAttachment>): Promise<void> => {
    if (!body.value.id || !procurementStore.procurementDetail.id) return;

    const { status } = await PP009Service.sendEmailInviteAsync(procurementStore.procurementDetail.id, body.value.id, vendorId, email, emailTemplate, attachments);

    if (status === HttpStatusCode.Ok) {
      await onGetByIdAsync(body.value.id);

      return ToastHelper.success('ส่งอีเมลเชิญ', 'ส่งอีเมลเชิญสำเร็จ');
    }
  };

  const onUpsertAttachments = async (
    id: string,
    type: EntrepreneurType,
    attachments: EntrepreneurAttachments[]
  ) => {
    const vendorData = body.value.vendors.find(x => x.id === id);
    if (!vendorData?.id) return;

    const otherTypeAttachments =
      vendorData.attachments?.map(a => ({
        ...a,
        fileAttachments: a.fileAttachments.filter(f => f.type !== type)
      })).filter(a => a.fileAttachments.length > 0) ?? [];

    const newAttachments =
      attachments
        ?.map(att => ({
          ...att,
          fileAttachments: att.fileAttachments?.map(f => ({ ...f, type })) ?? []
        }))
        .filter(att => att.fileAttachments.length > 0) ?? [];

    vendorData.attachments = [...otherTypeAttachments, ...newAttachments];

    const { status } = await PP009Service.onUpsertAttachmentsAsync(
      vendorData.id,
      vendorData.attachments
    );

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
    }
  };

  const resetDocumentAsync = async (
    procurementId: string,
    contractInvitationId: string,
    vendorId: string,): Promise<void> => {
    const { status } = await PP009Service.resetDocumentAsync(procurementId, contractInvitationId, vendorId);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success("สำเร็จ", "ทำการรีเซ็ตเอกสารสำเร็จ");
      return;
    }

    ToastHelper.error("ไม่สามารถรีเซ็ตเอกสารได้", "เกิดข้อผิดพลาดในการรีเซ็ตเอกสารการตรวจสอบ");
  };

  return {
    body,
    venderOptions,
    currentVendor,
    currentStatus,
    fn: {
      onGetByIdAsync,
      onSubmitAsync,
      onSendApprovalAsync,
      onRecallAsync,
      onApprovedRejectedAsync,
      getReviewDocumentAsync,
      onResetBody,
      onSetDefaultAcceptors,
      onSendEmailInviteAsync,
      onUpsertAttachments,
      resetDocumentAsync,
      syncCheckDataByTaxId
    },
    states: {
      isRequired,
      isEdit,
      isRecall,
      isCurrentApproval,
      isLastApproval,
    }
  };
});
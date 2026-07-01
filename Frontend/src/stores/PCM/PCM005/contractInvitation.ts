import type { ParticipantsAcceptor } from "@/models/shared/participants";
import type { Option } from "@/models/shared/option";
import type { PP009Detail, VendorInfo } from "@/views/PP/models/PP009/pp009Model";
import { PP009Status } from "@/views/PP/enums/pp009";
import { usePcm005DetailStore } from "./pcm005";
import { useAuthenticationStore } from "@/stores/authentication";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { AcceptorStatus, AcceptorType } from "@/enums/participants";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import { HttpStatusCode, type AxiosResponse } from "axios";
import ToastHelper from "@/helpers/toast";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import PP009Service from "@/views/PP/services/PP009/PP009Service";
import { checkIsSixty } from "@/helpers/supplyMethod";
import { SectionProcessType } from "@/enums/operations";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import operationService from "@/services/Shared/operations";

export const usePcm005ContractInvitationStore = defineStore('pcm005-ci-store', () => {
  const pcm005Store = usePcm005DetailStore();

  const initBody = {
    procurementId: pcm005Store.body.id,
    vendors: [] as Array<VendorInfo>,
    status: PP009Status.Draft,
    acceptors: [] as Array<ParticipantsAcceptor>,
    hasEditPermission: false,
  } as PP009Detail;

  const auth = useAuthenticationStore();

  const body = ref<PP009Detail>(structuredClone(initBody));
  const venderOptions = ref<Array<Option>>([]);
  const currentVendor = ref<string>();
  const currentStatus = ref<PP009Status>(PP009Status.Draft);

  const onGetByIdAsync = async () => {
    const id = body.value.id ?? pcm005Store.body.contractInvitation?.id;
    const { data, status } = await PP009Service.onGetByIdAsync(pcm005Store.body.id!, id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;
      currentStatus.value = data.status;

      venderOptions.value = [];
      currentVendor.value = undefined;

      if (data.vendors.length > 0) {
        venderOptions.value = data.vendors.map(s => ({ label: `${s.contractNumber}: ${s.vendorName}`, value: s.purchaseOrderApprovalContractId }));
        currentVendor.value = currentVendor.value ?? venderOptions.value[0].value as string;
      }
    }
  };

  const onResetBody = () => {
    body.value = structuredClone(initBody);
  };

  const onSubmitAsync = async () => {
    if (body.value.id) {
      await onUpdateAsync(body.value.id);

      return;
    }

    await onCreateAsync();
  };

  const validatePayload = (value: PP009Detail): boolean => {
    if (value.acceptors.length <= 0) {
      ToastHelper.errorDescription("ต้องมีผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");

      return false;
    }

    return true;
  };

  const onCreateAsync = async (contractInvitationStatus?: PP009Status) => {
    const payload = {
      ...body.value,
      status: contractInvitationStatus ?? body.value.status,
    } as PP009Detail;

    const { data, status } = await PP009Service.onCreateAsync(pcm005Store.body.id!, payload);

    if (status === HttpStatusCode.Created) {
      contractInvitationStatus ? ToastHelper.sendApproveConfirmMessageToast() : ToastHelper.createdMessageToast();

      body.value.id = data;
      await reStateDataAsync();

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

    const { status } = await PP009Service.onUpdateByIdAsync(pcm005Store.body.id!, id, payload);

    if (status === HttpStatusCode.Ok) {
      contractInvitationStatus ? mapStatusToast[contractInvitationStatus]() : ToastHelper.updatedMessageToast();
    }

    await onGetByIdAsync();
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

    const resp = await showReasonDialogAsync(dialogType[type]);

    if (!resp.isConfirm) return;

    const apiMap: Record<'Approve' | 'Reject', () => Promise<AxiosResponse<any, any>>> = {
      Approve: () => PP009Service.onApprovedAsync(pcm005Store.body.id!, body.value.id!, { remark: resp.reason }),
      Reject: () => PP009Service.onRejectedAsync(pcm005Store.body.id!, body.value.id!, { remark: resp.reason }),
    };

    const { status } = await apiMap[type]();

    if (status === HttpStatusCode.Ok) {
      type === 'Approve' ? ToastHelper.approvedMessageToast() : ToastHelper.sendEditMessageToast();
    }

    await reStateDataAsync();
  };

  const reStateDataAsync = async () => {
    await pcm005Store.getDetailAsync(pcm005Store.body.id!);
    await onGetByIdAsync();
  };

  const isRequired = (purchaseOrderApprovalContractId: string) => computed(() => {
    if (currentStatus.value === PP009Status.Draft) {
      return currentVendor.value === purchaseOrderApprovalContractId;
    }

    return true;
  });

  const onSetDefaultAcceptors = async () => {
    const totalVendorAgreePrice = body.value.vendors.reduce((prev, curr) => prev + curr.agreedPrice, 0);

    if (totalVendorAgreePrice === 0) return;

    if (checkIsSixty(pcm005Store.body.supplyMethodCode) && totalVendorAgreePrice > 100000) {
      // Default acceptors for supply method 60+ and budget > 100,000 are handled by specific business rules.
      // Current implementation intentionally bypasses automatic population in this branch.
      return;
    }

    const params = {
      processType: SectionProcessType.ContractInvitation,
      budget: totalVendorAgreePrice,
      userId: auth.profile.id,
      supplyMethodCode: pcm005Store.body.supplyMethodCode,
      supplyMethodSpecialTypeCode: pcm005Store.body.supplyMethodSpecialTypeCode,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status === HttpStatusCode.Ok) {
      body.value.acceptors = [...data.map((m, i) => ({
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

  const canRestoreVersion = (vendorId: string) => computed(() => {
    const vendor = body.value.vendors.find(v => v.id === vendorId);
    return isEdit.value && (vendor?.documentVersions?.length ?? 0) > 1;
  });

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

  const resetDocumentAsync = async (vendorId: string): Promise<void> => {
    if (!body.value.id) return;

    const { status } = await PP009Service.resetDocumentAsync(pcm005Store.body.id!, body.value.id, vendorId);

    if (status === HttpStatusCode.Ok) {
      await onGetByIdAsync();
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
    }
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
      resetDocumentAsync,
    },
    states: {
      isRequired,
      isEdit,
      isRecall,
      isCurrentApproval,
      isLastApproval,
      canRestoreVersion,
    },
  };
});
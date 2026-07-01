import { Cam01PoSapStatus } from "@/enums/CAM/CAM01/cam01.poSap";
import type { Cam01PoPayment } from "@/models/CAM/CAM01/cam01";
import type { Cam01PoSapBody } from "@/models/CAM/CAM01/cam01.poSap";
import type { ParticipantsAcceptor } from "@/models/shared/participants";
import Cam01PoSapService from "@/services/CAM/CAM01/cam01.poSap";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { useCam01DetailStore } from "../cam01.detail";
import { useAuthenticationStore } from "@/stores/authentication";
import ToastHelper from "@/helpers/toast";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import { AcceptorStatus, AcceptorType } from "@/enums/participants";

export const useCam01PoSapStore = defineStore('cam01-po-sap-store', () => {
  const amendmentStore = useCam01DetailStore();
  const authStore = useAuthenticationStore();

  const initBody = {
    oldContract: {},
    newContract: {},
    oldPaymentTerms: [] as Array<Cam01PoPayment>,
    paymentTerms: [] as Array<Cam01PoPayment>,
    status: Cam01PoSapStatus.Draft,
    acceptors: [] as Array<ParticipantsAcceptor>,
  } as Cam01PoSapBody;

  const body = ref<Cam01PoSapBody>(structuredClone(initBody));

  const onGetByIdAsync = async (amendmentId: string, id?: string) => {
    const { data, status } = await Cam01PoSapService.onGetByIdAsync(amendmentId, id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;
    }
  };

  const onSubmitAsync = async () => {
    if (body.value.id) {
      await onUpdateAsync(body.value.id);

      return;
    }
    await onCreateAsync();
  };

  const onCreateAsync = async () => {
    if (!amendmentStore.body.id) return;

    const { data, status } = await Cam01PoSapService.onCreateAsync(amendmentStore.body.id, body.value);

    if (status === HttpStatusCode.Created) {
      body.value.id = data;
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, data);

      ToastHelper.createdMessageToast();
    }
  };

  const onUpdateAsync = async (id: string) => {
    if (!body.value.id || !amendmentStore.body.id) return;

    const { status } = await Cam01PoSapService.onUpdateAsync(amendmentStore.body.id, id, body.value);

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);
      ToastHelper.updatedMessageToast();
    }
  };

  const onSendApproveAsync = async () => {
    if (!body.value.id || !amendmentStore.body.id) return;

    if (!body.value.acceptors || body.value.acceptors.length === 0) {
      ToastHelper.approvalAtLeastMessageToast();
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApprove)) return;

    const payload = {
      ...body.value,
      status: Cam01PoSapStatus.WaitingApproval,
    };

    const { status } = await Cam01PoSapService.onUpdateAsync(amendmentStore.body.id, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.sendApproveMessageToast();
    }
  };

  const onRecallAsync = async () => {
    if (!body.value.id || !amendmentStore.body.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

    const payload = {
      ...body.value,
      status: Cam01PoSapStatus.Edit,
    };

    const { status } = await Cam01PoSapService.onUpdateAsync(amendmentStore.body.id, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.recallEditMessageToast();
    }
  };

  const onRejectedAsync = async () => {
    if (!body.value.id || !amendmentStore.body.id) return;

    const resp = await showReasonDialogAsync(ReasonDialogType.Reject);

    if (!resp.isConfirm) return;

    const { status } = await Cam01PoSapService.onRejectedAsync(amendmentStore.body.id, body.value.id, { remark: resp.reason, group: AcceptorType.Approver });

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.sendEditMessageToast();
    }
  };

  const onApprovedAsync = async () => {
    if (!body.value.id || !amendmentStore.body.id) return;

    const resp = await showReasonDialogAsync(isLastApprover.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (!resp.isConfirm) return;

    const { status } = await Cam01PoSapService.onApprovedAsync(amendmentStore.body.id, body.value.id, { remark: resp.reason, group: AcceptorType.Approver });

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.approvedMessageToast();
    }
  };

  const isCanEdit = computed(() => {
    return [Cam01PoSapStatus.Draft, Cam01PoSapStatus.Edit, Cam01PoSapStatus.Rejected].includes(body.value.status) && body.value.hasPermission;
  });

  const isCanReCall = computed(() => {
    const isWaitingApproval = [Cam01PoSapStatus.WaitingApproval].includes(body.value.status);

    return isWaitingApproval && body.value.hasPermission;
  });

  const isCurrentApprover = computed(() => {
    const isWaitingApproval = [Cam01PoSapStatus.WaitingApproval].includes(body.value.status);
    const currentAcceptor = body.value.acceptors.some(s => s.acceptorType === AcceptorType.Approver && (s.delegateeUserId ? s.delegateeUserId : s.userId) === authStore.profile.id && s.isCurrent);

    return isWaitingApproval && currentAcceptor;
  });

  const isLastApprover = computed(() => {
    const lastApprover = body.value.acceptors.filter(s => s.acceptorType === AcceptorType.Approver && s.status === AcceptorStatus.Pending).length === 1;
    const isWaitingApproval = [Cam01PoSapStatus.WaitingApproval].includes(body.value.status);

    return lastApprover && isWaitingApproval;
  });

  return {
    // Variables
    body,

    // Actions
    onGetByIdAsync,
    onSubmitAsync,
    onSendApproveAsync,
    onRecallAsync,
    onApprovedAsync,
    onRejectedAsync,

    // States
    isCanEdit,
    isCanReCall,
    isCurrentApprover,
    isLastApprover,
  };
});
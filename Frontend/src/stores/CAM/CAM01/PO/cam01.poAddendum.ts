import { Cam01PoAddendumStatus } from "@/enums/CAM/CAM01/cam01.poAddendum";
import type { Cam01PoAddendumBody } from "@/models/CAM/CAM01/cam01.poAddendum";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import Cam01PoAddendumService from "@/services/CAM/CAM01/cam01.poAddendum";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { useCam01DetailStore } from "../cam01.detail";
import ToastHelper from "@/helpers/toast";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import { useAuthenticationStore } from "@/stores/authentication";
import { AcceptorStatus, AcceptorType, AssigneeType } from "@/enums/participants";
import type { Cam01PoPayment } from "@/models/CAM/CAM01/cam01";
import { CommitteePositions } from "@/enums/PCM005/principle";

export const useCam01PoAddendumStore = defineStore('cam01-po-addendum-store', () => {
  const amendmentStore = useCam01DetailStore();
  const authStore = useAuthenticationStore();

  const initBody = {
    oldContract: {},
    newContract: {
      sapNumber: '',
    },
    vendor: {},
    acceptors: [] as Array<ParticipantsCommitteeAcceptor>,
    assignees: [] as Array<ParticipantsAssignee>,
    oldPaymentTerms: [] as Array<Cam01PoPayment>,
    newPaymentTerms: [] as Array<Cam01PoPayment>,
    status: Cam01PoAddendumStatus.Draft,
  } as Cam01PoAddendumBody;

  const body = ref<Cam01PoAddendumBody>(structuredClone(initBody));

  const onGetByIdAsync = async (amendmentId: string, id?: string) => {
    const { data, status } = await Cam01PoAddendumService.onGetByIdAsync(amendmentId, id);

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

    const { data, status } = await Cam01PoAddendumService.onCreateAsync(amendmentStore.body.id, body.value);

    if (status === HttpStatusCode.Created) {
      body.value.id = data;
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, data);

      ToastHelper.createdMessageToast();
    }
  };

  const onUpdateAsync = async (id: string) => {
    if (!body.value.id || !amendmentStore.body.id) return;

    const { status, data } = await Cam01PoAddendumService.onUpdateAsync(amendmentStore.body.id, id, body.value);

    if (status === HttpStatusCode.Ok) {
      if (data?.newContractAddendumDocumentFileId) {
        body.value.contractAddendumDocumentId = data.newContractAddendumDocumentFileId;
      }
      if (data?.newContractAmendmentRequestDocumentFileId) {
        body.value.contractAmendmentRequestDocumentId = data.newContractAmendmentRequestDocumentFileId;
      }
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);
      ToastHelper.updatedMessageToast();
    }
  };

  const onSendCommitteeApprovalAsync = async () => {
    if (!body.value.id || !amendmentStore.body.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApprove)) return;

    const payload = {
      ...body.value,
      status: Cam01PoAddendumStatus.WaitingCommitteeApproval,
    };

    const { status } = await Cam01PoAddendumService.onUpdateAsync(amendmentStore.body.id, body.value.id, payload);

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
      status: body.value.status === Cam01PoAddendumStatus.WaitingCommitteeApproval ? Cam01PoAddendumStatus.Edit : Cam01PoAddendumStatus.WaitingComment,
    };

    const { status } = await Cam01PoAddendumService.onUpdateAsync(amendmentStore.body.id, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.recallEditMessageToast();
    }
  };

  const onUpdateDutiesStatusAsync = async (isUnableToPerformDuties: boolean, acceptorId: string, remark?: string) => {

    if (!amendmentStore.body.id || !body.value.id) return;

    const { status } = await Cam01PoAddendumService.onUpdateAcceptorDutiesAsync(amendmentStore.body.id, body.value.id, acceptorId, isUnableToPerformDuties, remark);

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      return ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
    };
  };

  const onCommitteeRejectedAsync = async () => {
    if (!amendmentStore.body.id || !body.value.id) return;

    const resp = await showReasonDialogAsync(ReasonDialogType.NotAgree);

    if (!resp.isConfirm) return;
    const { status } = await Cam01PoAddendumService.onRejectedAsync(amendmentStore.body.id, body.value.id, { remark: resp.reason });

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.notAgreeMessageToast();
    }
  };

  const onCommitteeApprovedAsync = async () => {
    if (!amendmentStore.body.id || !body.value.id) return;

    const resp = await showReasonDialogAsync(ReasonDialogType.Accepted);

    if (!resp.isConfirm) return;
    const { status } = await Cam01PoAddendumService.onApprovedAsync(amendmentStore.body.id, body.value.id, { remark: resp.reason });

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.approvedMessageToast();
    }
  };

  const onConfirmAssignAsync = async () => {
    if (!amendmentStore.body.id || !body.value.id) return;

    if (body.value.assignees.filter(x => x.assigneeType === AssigneeType.Assignee).length <= 0) {
      ToastHelper.assignAtLeastMessageToast();

      return;
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

    const payload = {
      ...body.value,
      status: Cam01PoAddendumStatus.WaitingComment,
    };

    const { status } = await Cam01PoAddendumService.onUpdateAsync(amendmentStore.body.id, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.assignedMessageToast();
    }
  };

  const onSendApprovalAsync = async () => {
    if (!amendmentStore.body.id || !body.value.id) return;

    if (!body.value.assignees?.some(x => x.remark)) {
      ToastHelper.assignneeCommentAtLeastMessageToast();

      return;
    }

    if (body.value.acceptors.filter(x => x.acceptorType === AcceptorType.Approver).length <= 0) {
      ToastHelper.approvalAtLeastMessageToast();

      return;
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;


    const payload = {
      ...body.value,
      status: Cam01PoAddendumStatus.WaitingApproval,
    };

    const { status } = await Cam01PoAddendumService.onUpdateAsync(amendmentStore.body.id, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.sendApproveConfirmMessageToast();
    }
  };

  const onAssigneeCommentAsync = async (remark: string) => {
    if (!amendmentStore.body.id || !body.value.id) return;

    const { status } = await Cam01PoAddendumService.onAssigneeCommentAsync(amendmentStore.body.id, body.value.id, remark);

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      return ToastHelper.success('แสดงความคิดเห็น', 'แสดงความคิดเห็นสำเร็จ');
    }
  };

  const onRejectedAsync = async () => {
    if (!amendmentStore.body.id || !body.value.id) return;

    const resp = await showReasonDialogAsync(ReasonDialogType.Reject);

    if (!resp.isConfirm) return;
    const { status } = await Cam01PoAddendumService.onRejectedAsync(amendmentStore.body.id, body.value.id, { remark: resp.reason });

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.sendEditMessageToast();
    }
  };

  const onAcceptorApprovedAsync = async () => {
    if (!amendmentStore.body.id || !body.value.id) return;

    const resp = await showReasonDialogAsync(isLastApprover.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (!resp.isConfirm) return;

    const { status } = await Cam01PoAddendumService.onApprovedAsync(amendmentStore.body.id, body.value.id, { remark: resp.reason });

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.approvedMessageToast();
    }
  };

  const getReviewDocumentAsync = async (documentType: string): Promise<string> => {
    if (!amendmentStore.body.id || !body.value.id) return '';

    const { data, status } = await Cam01PoAddendumService.getReviewDocumentAsync(amendmentStore.body.id, body.value.id, documentType);

    if (status !== HttpStatusCode.Ok) {
      return '';
    }

    return data;
  };

  const isCanEdit = computed(() => {
    return [Cam01PoAddendumStatus.Draft, Cam01PoAddendumStatus.Edit, Cam01PoAddendumStatus.Rejected].includes(body.value.status) && body.value.acceptors.some(s => s.acceptorType === AcceptorType.AcceptanceCommittee && s.userId === authStore.profile.id);
  });

  const isCanReCall = computed(() => {
    if (!body.value.assignees) return false;

    const status = body.value.status === Cam01PoAddendumStatus.WaitingApproval;
    const checkUser = body.value.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    if (!body.value.acceptors) return false;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.Approver);

    return status && checkUser && !isAnyApprovalAndRejected ;
  });

  const isCommitteeRecall = computed(() => {
    if (!body.value.acceptors) return false;

    const status = body.value.status === Cam01PoAddendumStatus.WaitingCommitteeApproval;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.AcceptanceCommittee && a.committeePositionsCode == CommitteePositions.PosBoard001);

    const isCommittee = body.value.acceptors.some(s => s.acceptorType === AcceptorType.AcceptanceCommittee && s.userId === authStore.profile.id);

    return status && !isAnyApprovalAndRejected && isCommittee;
  });

  const isCommitteeApproval = computed(() => {
    if (!body.value.acceptors) return false;

    const committee = body.value.acceptors.filter(s => s.acceptorType === AcceptorType.AcceptanceCommittee).some(s => s.userId === authStore.profile.id && s.isCurrent);
    const isWaitingCommitteeApproval = [Cam01PoAddendumStatus.WaitingCommitteeApproval].includes(body.value.status);

    return committee && isWaitingCommitteeApproval;
  });

  const isCanAssign = computed(() => {
    const assignee = body.value.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === authStore.profile.id);
    const isWaitingAssign = [Cam01PoAddendumStatus.WaitingAssigned].includes(body.value.status);

    return assignee && isWaitingAssign;
  });

  const isCanComment = computed(() => {
    const assignee = body.value.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === authStore.profile.id);
    const isWaitingAssign = [Cam01PoAddendumStatus.WaitingComment].includes(body.value.status);

    return assignee && isWaitingAssign;
  });

  const isCanApprover = computed(() => {
    const approver = body.value.acceptors.filter(s => s.acceptorType === AcceptorType.Approver).some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === authStore.profile.id && s.isCurrent);
    const isWaitingApproval = [Cam01PoAddendumStatus.WaitingApproval].includes(body.value.status);

    return approver && isWaitingApproval;
  });

  const isLastApprover = computed(() => {
    const lastApprover = body.value.acceptors.filter(s => s.acceptorType === AcceptorType.Approver && s.status === AcceptorStatus.Pending).length === 1;
    const isWaitingApproval = [Cam01PoAddendumStatus.WaitingApproval].includes(body.value.status);

    return lastApprover && isWaitingApproval;
  });

  const canRestoreContractAddendumDocument = computed(() => {
    return isCanEdit.value && (body.value.contractAddendumDocumentVersions?.length ?? 0) > 1;
  });

  const canRestoreContractAmendmentRequestDocument = computed(() => {
    return isCanEdit.value && (body.value.contractAmendmentRequestDocumentVersions?.length ?? 0) > 1;
  });

  const resetDocumentAsync = async (documentType: 'ContractAddendum' | 'ContractAmendmentRequest'): Promise<void> => {
    if (!amendmentStore.body.id || !body.value.id) return;

    const { status } = await Cam01PoAddendumService.resetDocumentAsync(amendmentStore.body.id, body.value.id, documentType);

    if (status === HttpStatusCode.Ok) {
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
    }
  };

  return {
    // Variables
    body,

    // Actions
    getReviewDocumentAsync,
    resetDocumentAsync,
    onGetByIdAsync,
    onSubmitAsync,
    onSendCommitteeApprovalAsync,
    onRecallAsync,
    onUpdateDutiesStatusAsync,
    onCommitteeRejectedAsync,
    onCommitteeApprovedAsync,
    onConfirmAssignAsync,
    onSendApprovalAsync,
    onAssigneeCommentAsync,
    onRejectedAsync,
    onAcceptorApprovedAsync,

    // States
    isCanEdit,
    isCanReCall,
    isCommitteeApproval,
    isCanAssign,
    isCanComment,
    isCanApprover,
    isLastApprover,
    isCommitteeRecall,
    canRestoreContractAddendumDocument,
    canRestoreContractAmendmentRequestDocument,
  };
});
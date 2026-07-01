import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { Option } from "@/models/shared/option";
import { EGroupCode } from "@/enums/shared";
import SharedService from "@/services/Shared/dropdown";
import { HttpStatusCode } from "axios";
import type { Cam01AdjustContractBody, Cam01AdjustContractPayment } from "@/models/CAM/CAM01/cam.adjustContract";
import { Cam01AdjustContractStatus } from "@/enums/CAM/CAM01/cam01.adjustContract";
import Cam01AdjustContractService from "@/services/CAM/CAM01/cam01.adjustContract";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import { AcceptorStatus, AcceptorType, AssigneeType } from "@/enums/participants";
import { useCam01DetailStore } from "../cam01.detail";
import { useAuthenticationStore } from "@/stores/authentication";
import ToastHelper from "@/helpers/toast";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import { CommitteePositions } from "@/enums/PCM005/principle";

export const useCam01AdjustContractStore = defineStore('cam01AdjustContract', () => {
  const amendmentStore = useCam01DetailStore();
  const authStore = useAuthenticationStore();

  const initBody = {
    adjustContractDurationNew: {
      paymentTerms: [] as Array<Cam01AdjustContractPayment>,
    },
    adjustContractDurationOld: {
      paymentTerms: [] as Array<Cam01AdjustContractPayment>,
    },
    acceptors: [] as Array<ParticipantsCommitteeAcceptor>,
    assignees: [] as Array<ParticipantsAssignee>,
    status: Cam01AdjustContractStatus.Draft,
  } as Cam01AdjustContractBody;

  const body = ref<Cam01AdjustContractBody>(structuredClone(initBody));

  const options = ref<{ paymentType: Array<Option> }>({
    paymentType: [],
  });

  const onGetDropdownAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PayType);

    if (status === HttpStatusCode.Ok) {
      options.value.paymentType = data;
    }
  };

  const onGetByIdAsync = async (amendmentId: string, id?: string) => {
    const { data, status } = await Cam01AdjustContractService.onGetByIdAsync(amendmentId, id);

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

    const { data, status } = await Cam01AdjustContractService.onCreateAsync(amendmentStore.body.id, body.value);

    if (status === HttpStatusCode.Created) {
      body.value.id = data;
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, data);

      ToastHelper.createdMessageToast();
    }
  };

  const onUpdateAsync = async (id: string) => {
    if (!body.value.id || !amendmentStore.body.id) return;

    const { status, data } = await Cam01AdjustContractService.onUpdateAsync(amendmentStore.body.id, id, body.value);

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
      status: Cam01AdjustContractStatus.WaitingCommitteeApproval,
    };

    const { status } = await Cam01AdjustContractService.onUpdateAsync(amendmentStore.body.id, body.value.id, payload);

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
      status: body.value.status === Cam01AdjustContractStatus.WaitingCommitteeApproval ? Cam01AdjustContractStatus.Edit : Cam01AdjustContractStatus.WaitingComment,
    };

    const { status } = await Cam01AdjustContractService.onUpdateAsync(amendmentStore.body.id, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.recallEditMessageToast();
    }
  };

  const onUpdateDutiesStatusAsync = async (isUnableToPerformDuties: boolean, acceptorId: string, remark?: string) => {
    if (!amendmentStore.body.id || !body.value.id) return;

    const { status } = await Cam01AdjustContractService.onUpdateAcceptorDutiesAsync(amendmentStore.body.id, body.value.id, acceptorId, isUnableToPerformDuties, remark);

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
    const { status } = await Cam01AdjustContractService.onRejectedAsync(amendmentStore.body.id, body.value.id, { remark: resp.reason });

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
    const { status } = await Cam01AdjustContractService.onApprovedAsync(amendmentStore.body.id, body.value.id, { remark: resp.reason });

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
      status: Cam01AdjustContractStatus.WaitingComment,
    };

    const { status } = await Cam01AdjustContractService.onUpdateAsync(amendmentStore.body.id, body.value.id, payload);

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
      status: Cam01AdjustContractStatus.WaitingApproval,
    };

    const { status } = await Cam01AdjustContractService.onUpdateAsync(amendmentStore.body.id, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.sendApproveConfirmMessageToast();
    }
  };

  const onAssigneeCommentAsync = async (remark: string) => {
    if (!amendmentStore.body.id || !body.value.id) return;

    const { status } = await Cam01AdjustContractService.onAssigneeCommentAsync(amendmentStore.body.id, body.value.id, remark);

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
    const { status } = await Cam01AdjustContractService.onRejectedAsync(amendmentStore.body.id, body.value.id, { remark: resp.reason });

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

    const { status } = await Cam01AdjustContractService.onApprovedAsync(amendmentStore.body.id, body.value.id, { remark: resp.reason });

    if (status === HttpStatusCode.Ok) {
      await amendmentStore.onGetByIdAsync(amendmentStore.body.id);
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);

      ToastHelper.approvedMessageToast();
    }
  };

  const getReviewDocumentAsync = async (documentType: string): Promise<string> => {
    if (!amendmentStore.body.id || !body.value.id) return '';

    const { data, status } = await Cam01AdjustContractService.getReviewDocumentAsync(amendmentStore.body.id, body.value.id, documentType);

    if (status !== HttpStatusCode.Ok) {
      return '';
    }

    return data;
  };

  const isCanEdit = computed(() => {
    return [Cam01AdjustContractStatus.Draft, Cam01AdjustContractStatus.Edit, Cam01AdjustContractStatus.Rejected].includes(body.value.status) && body.value.acceptors.filter(f => f.acceptorType === AcceptorType.AcceptanceCommittee).some(s => s.userId === authStore.profile.id);
  });

  const isCanReCall = computed(() => {
    if (!body.value.assignees) return false;

    const status = body.value.status === Cam01AdjustContractStatus.WaitingApproval;
    const checkUser = body.value.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    if (!body.value.acceptors) return false;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.Approver);

    return status && checkUser && !isAnyApprovalAndRejected ;
  });

  const isCommitteeRecall = computed(() => {
    if (!body.value.acceptors) return false;

    const status = body.value.status === Cam01AdjustContractStatus.WaitingCommitteeApproval;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.AcceptanceCommittee && a.committeePositionsCode == CommitteePositions.PosBoard001);

    const isCommittee = body.value.acceptors.some(s => s.acceptorType === AcceptorType.AcceptanceCommittee && s.userId === authStore.profile.id);

    return status && !isAnyApprovalAndRejected && isCommittee;
  });

  const isCommitteeApproval = computed(() => {
    if (!body.value.acceptors) return false;

    const committee = body.value.acceptors.filter(s => s.acceptorType === AcceptorType.AcceptanceCommittee).some(s => s.userId === authStore.profile.id && s.isCurrent);
    const isWaitingCommitteeApproval = [Cam01AdjustContractStatus.WaitingCommitteeApproval].includes(body.value.status);

    return committee && isWaitingCommitteeApproval;
  });

  const isCanAssign = computed(() => {
    const assignee = body.value.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === authStore.profile.id);
    const isWaitingAssign = [Cam01AdjustContractStatus.WaitingAssigned].includes(body.value.status);

    return assignee && isWaitingAssign;
  });

  const isCanComment = computed(() => {
    const assignee = body.value.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === authStore.profile.id);
    const isWaitingAssign = [Cam01AdjustContractStatus.WaitingComment].includes(body.value.status);

    return assignee && isWaitingAssign;
  });

  const isCanApprover = computed(() => {
    const approver = body.value.acceptors.filter(s => s.acceptorType === AcceptorType.Approver).some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === authStore.profile.id && s.isCurrent);
    const isWaitingApproval = [Cam01AdjustContractStatus.WaitingApproval].includes(body.value.status);

    return approver && isWaitingApproval;
  });

  const isLastApprover = computed(() => {
    const lastApprover = body.value.acceptors.filter(s => s.acceptorType === AcceptorType.Approver && s.status === AcceptorStatus.Pending).length === 1;
    const isWaitingApproval = [Cam01AdjustContractStatus.WaitingApproval].includes(body.value.status);

    return lastApprover && isWaitingApproval;
  });

  const canRestoreContractAddendumDocument = computed(() => {
    return isCanEdit.value && (body.value.contractAddendumDocumentVersions?.length ?? 0) > 1;
  });

  const canRestoreContractAmendmentRequestDocument = computed(() => {
    return isCanEdit.value && (body.value.contractAmendmentRequestDocumentVersions?.length ?? 0) > 1;
  });

  const resetDocumentAsync = async (documentType: 'ExtendChange' | 'Approved'): Promise<void> => {
    if (!amendmentStore.body.id || !body.value.id) return;

    const { status } = await Cam01AdjustContractService.resetDocumentAsync(amendmentStore.body.id, body.value.id, documentType);

    if (status === HttpStatusCode.Ok) {
      await onGetByIdAsync(amendmentStore.body.id, body.value.id);
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
    }
  };

  return {
    // Variables
    body,
    options,

    // Functions
    onGetDropdownAsync,
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
    getReviewDocumentAsync,
    resetDocumentAsync,

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
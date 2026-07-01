import { Cm007Status, Cm007AccordionTab } from "@/enums/CM/cm007";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeType } from "@/enums/participants";
import { CommitteePositions } from "@/enums/PCM005/principle";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import ToastHelper from "@/helpers/toast";
import type { Cm007Detail, Cm007Component } from "@/models/CM/cm007";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import type { Attachments } from "@/models/shared/uploadFile";
import Cm007Service from "@/services/CM/cm007";
import operationService from "@/services/Shared/operations";
import { apiToPP010, pp010ToApi } from "@/views/CM/CM007/composables/usePP010BodyWrapper";
import { useAuthenticationStore } from "@/stores/authentication";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { SectionProcessType } from "@/enums/operations";
import { isCurrentPendingAcceptor } from "@/helpers/participants";

export const useCm007DetailStore = defineStore('cm007Detail', () => {
  const auth = useAuthenticationStore();

  const initBody = {
    id: '',
    contractDraftVendorId: '',
    procurementId: '',
    status: Cm007Status.Draft,
    email: '',
    contractName: '',
    poNumber: '',
    contractDraftNumber: '',
    contractNumber: '',
    budget: 0,
    isWorkingDayOnly: false,
    contractStatus: '',
    assignees: [] as Array<ParticipantsAssignee>,
    acceptors: [] as Array<ParticipantsCommitteeAcceptor>,
    components: [] as Array<Cm007Component>,
    attachments: [],
    shareholders: [],
    fileAttachments: [],
  } as Cm007Detail;

  const body = ref<Cm007Detail>(structuredClone(initBody));
  const accordion = ref<string[]>([Cm007AccordionTab.Committee]);

  const onResetBody = () => {
    body.value = structuredClone(initBody);
  };

  const onGetById = async (id: string) => {
    const { data, status } = await Cm007Service.getByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      // Transform BEFORE assigning to reactive ref
      // so Vue sees `detail` as an existing property from the start
      if (data.oldData) {
        apiToPP010(data.oldData, data);
      }
      apiToPP010(data);

      // Normalize isUnableToPerformDuties for AcceptanceCommittee members (null → false)
      data.acceptors = data.acceptors.map(a =>
        a.acceptorType === AcceptorType.AcceptanceCommittee
          ? { ...a, isUnableToPerformDuties: a.isUnableToPerformDuties ?? false }
          : a
      );

      // Map fileAttachments with proper sequence numbers
      data.fileAttachments = ((data as any).fileAttachments ?? []).map(
        (a: Attachments, i: number) => ({
          ...a,
          sequence: i + 1,
          fileAttachments: (a.fileAttachments ?? []).map(f => ({
            ...f,
            createdBy: (f as any).createdBy ?? '',
          })),
        })
      );

      body.value = data;
    }
  };

  const onSaveAsync = async () => {
    if (!body.value.id) return;

    // Sync detail back to flat CM007 structure before saving
    pp010ToApi(body.value);

    const { status } = await Cm007Service.onUpdateAsync(body.value.id, body.value);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onUpdateComponents = (components: Cm007Component[]) => {
    body.value.components = components;
  };

  const assigneeDefaultAcceptor = async (): Promise<void> => {
    const assignees = body.value.assignees.filter(f => f.assigneeType === AssigneeType.Assignee);
    const lastAssignee = assignees[assignees.length - 1];
    let defaultUserId = auth.profile.id;

    if (lastAssignee) {
      defaultUserId = lastAssignee.delegateeUserId || lastAssignee.userId;
    }

    const params = {
      processType: SectionProcessType.ContractAmendment,
      userId: defaultUserId,
      budget: body.value.budget ?? 0,
      supplyMethodCode: body.value.supplyMethodCode,
      supplyMethodSpecialTypeCode: body.value.supplyMethodSpecialTypeCode,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status === HttpStatusCode.Ok) {
      body.value.acceptors = body.value.acceptors.filter(f => f.acceptorType !== AcceptorType.Approver);

      data.forEach(item => body.value.acceptors.push({
        acceptorType: AcceptorType.Approver,
        fullName: item.fullName,
        positionName: item.fullPositionName,
        sequence: body.value.acceptors.length + 1,
        status: AcceptorStatus.Draft,
        userId: item.userId,
        departmentName: item.businessUnitName,
        organizationLevel: item.organizationLevel,
      } as ParticipantsCommitteeAcceptor));
    }
  };

  const loginUserDefaultAcceptor = async (): Promise<void> => {
    if (!body.value.budget || body.value.budget <= 0) {
      return;
    }

    const params = {
      processType: SectionProcessType.ContractAmendment,
      userId: auth.profile.id,
      budget: body.value.budget,
      supplyMethodCode: body.value.supplyMethodCode,
      supplyMethodSpecialTypeCode: body.value.supplyMethodSpecialTypeCode,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status === HttpStatusCode.Ok && data.length > 0) {
      const lastItem = data[data.length - 1];

      const existingIndex = body.value.acceptors.findIndex(f => f.acceptorType === AcceptorType.AcceptorSign);

      if (existingIndex !== -1) {
        body.value.acceptors[existingIndex] = {
          ...body.value.acceptors[existingIndex],
          userId: lastItem.userId,
          fullName: lastItem.fullName,
          positionName: lastItem.fullPositionName,
          departmentName: lastItem.businessUnitName,
        };
      } else {
        body.value.acceptors.push({
          acceptorType: AcceptorType.AcceptorSign,
          fullName: lastItem.fullName,
          positionName: lastItem.fullPositionName,
          sequence: body.value.acceptors.length + 1,
          status: AcceptorStatus.Draft,
          userId: lastItem.userId,
          departmentName: lastItem.businessUnitName,
          organizationLevel: lastItem.organizationLevel,
        } as ParticipantsCommitteeAcceptor);
      }
    }
  };

  // Workflow Functions

  const onSubmitCommitteeApprovalAsync = async () => {
    if (!body.value.id) return;

    pp010ToApi(body.value);

    const { status } = await Cm007Service.onUpdateAsync(body.value.id, body.value);

    if (status !== HttpStatusCode.Ok) return;

    const { status: submitStatus } = await Cm007Service.onSubmitCommitteeApprovalAsync(body.value.id, body.value.documentDate);

    if (submitStatus === HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onCommitteeApprovedAsync = async () => {
    if (!body.value.id) return;

    const resp = await showReasonDialogAsync(ReasonDialogType.Accepted);
    if (!resp.isConfirm) return;

    const { status } = await Cm007Service.onApproveAsync(body.value.id, resp.reason);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.approvedMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onRejectedAsync = async () => {
    if (!body.value.id) return;

    const resp = await showReasonDialogAsync(ReasonDialogType.Reject);
    if (!resp.isConfirm) return;

    const { status } = await Cm007Service.onRejectAsync(body.value.id, resp.reason);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onAssignAsync = async () => {
    if (!body.value.id) return;

    if (!body.value.assignees.some(s => s.assigneeType === AssigneeType.Assignee)) {
      ToastHelper.assignAtLeastMessageToast();
      return;
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

    pp010ToApi(body.value);
    const payload = {
      ...body.value,
      status: Cm007Status.WaitingComment,
    } as Cm007Detail;

    const { status } = await Cm007Service.onUpdateAsync(body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.assignedMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onSubmitCommentAsync = async (reason: string) => {
    if (!body.value.id) return;

    const { status } = await Cm007Service.onCommentAsync(body.value.id, reason);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.remarkOfficerMessageToast();
      await onGetById(body.value.id);

      if (body.value.amendmentDocumentId) {
        const docId = await getReviewDocumentAsync('Amendment');
        if (docId) body.value.amendmentDocumentId = docId;
      }
      if (body.value.amendmentApprovalRequestDocumentId) {
        const docId = await getReviewDocumentAsync('AmendmentApprovalRequest');
        if (docId) body.value.amendmentApprovalRequestDocumentId = docId;
      }
    }
  };

  const onSubmitToApprovalAsync = async () => {
    if (!body.value.id) return;

    if (body.value.assignees.filter(f => f.remark).length <= 0) {
      return ToastHelper.assignneeCommentAtLeastMessageToast();
    }

    if (body.value.acceptors.filter(f => f.acceptorType === AcceptorType.Approver).length <= 0) {
      return ToastHelper.approvalAtLeastMessageToast();
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

    pp010ToApi(body.value);
    const payload = {
      ...body.value,
      status: Cm007Status.WaitingApproval,
    } as Cm007Detail;

    const { status } = await Cm007Service.onUpdateAsync(body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onApproverApproveAsync = async () => {
    if (!body.value.id) return;

    const resp = await showReasonDialogAsync(isLastApprover.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);
    if (!resp.isConfirm) return;

    const { status } = await Cm007Service.onApproveAsync(body.value.id, resp.reason);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.approvedMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onRecallAsync = async () => {
    if (!body.value.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

    pp010ToApi(body.value);

    const recallStatus = body.value.status === Cm007Status.WaitingCommitteeApproval
      ? Cm007Status.Editing
      : Cm007Status.WaitingComment;

    const payload = {
      ...body.value,
      status: recallStatus,
    } as Cm007Detail;

    const { status } = await Cm007Service.onUpdateAsync(body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onConfirmAddendumDrafterAsync = async () => {
    if (!body.value.id) return;

    const requiredGroup = body.value.status === Cm007Status.WaitingAssignment
      ? AssigneeGroup.Contract
      : AssigneeGroup.AddendumDrafter;

    if (!body.value.assignees.some(s => s.assigneeGroup === requiredGroup && s.assigneeType === AssigneeType.Assignee)) {
      ToastHelper.assignAtLeastMessageToast();
      return;
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

    const targetStatus = body.value.status === Cm007Status.WaitingAssignment
      ? Cm007Status.WaitingComment
      : Cm007Status.WaitingDraftAddendum;

    pp010ToApi(body.value);
    const payload = {
      ...body.value,
      status: targetStatus,
    } as Cm007Detail;

    const { status } = await Cm007Service.onUpdateAsync(body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.assignedMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onConfirmDraftAddendumAsync = async () => {
    if (!body.value.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

    pp010ToApi(body.value);
    const payload = {
      ...body.value,
      status: Cm007Status.WaitingReview,
    } as Cm007Detail;

    const { status } = await Cm007Service.onUpdateAsync(body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.assignedMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onReviewerRejectAsync = async () => {
    if (!body.value.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

    pp010ToApi(body.value);
    const payload = {
      ...body.value,
      status: Cm007Status.WaitingDraftAddendum,
    } as Cm007Detail;

    const { status } = await Cm007Service.onUpdateAsync(body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onReviewerApproveAsync = async () => {
    if (!body.value.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData)) return;

    pp010ToApi(body.value);
    const payload = {
      ...body.value,
      status: Cm007Status.Approved,
    } as Cm007Detail;

    const { status } = await Cm007Service.onUpdateAsync(body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.approvedMessageToast();
      await onGetById(body.value.id);
    }
  };

  const onDeleteAsync = async (): Promise<boolean> => {
    if (!body.value.id) return false;

    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return false;

    const { status } = await Cm007Service.deleteAsync(body.value.id);

    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();
      return true;
    }

    return false;
  };

  const onUpsertAttachments = async () => {
    if (!body.value.id) return;

    const { status } = await Cm007Service.attachmentsAsync(body.value.id, body.value.fileAttachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await onGetById(body.value.id);
    }
  };

  // Document Functions

  const getReviewDocumentAsync = async (documentType: string): Promise<string> => {
    if (!body.value.id) return '';

    const { data, status } = await Cm007Service.getReviewDocumentAsync(body.value.id, documentType);

    if (status !== HttpStatusCode.Ok) {
      return '';
    }

    return data;
  };

  const resetDocumentAsync = async (documentType: string): Promise<void> => {
    if (!body.value.id) return;

    const { status } = await Cm007Service.resetDocumentAsync(body.value.id, documentType);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
      await onGetById(body.value.id);
    }
  };

  const onUpdateDutiesStatusAsync = async (flag: boolean, acceptorId?: string, remark?: string) => {
    if (!body.value.id || !acceptorId) return;

    const { status } = await Cm007Service.setDutiesStatusAsync(body.value.id, acceptorId, flag, remark);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
      await onGetById(body.value.id);
    }
  };

  // Computed Permissions

  const isCanEdit = computed(() =>
    [Cm007Status.Draft, Cm007Status.Editing, Cm007Status.Rejected].includes(body.value.status)
    && (
      body.value.acceptors.filter(f => f.acceptorType === AcceptorType.AcceptanceCommittee)
        .some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id)
      || body.value.isPurchaseOrderApprovalAssignee
    )
  );

  const isCommittee = computed(() =>
    body.value.acceptors.filter(f => f.acceptorType === AcceptorType.AcceptanceCommittee)
      .some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id)
  );

  const isCurrentCommitteeApproval = computed(() => {
    if (body.value.status !== Cm007Status.WaitingCommitteeApproval) return false;

    const committees = body.value.acceptors.filter(f => f.acceptorType === AcceptorType.AcceptanceCommittee);
    if (committees.length === 0) return false;

    const isMe = (s: ParticipantsCommitteeAcceptor) => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id;

    if (!committees.some(isMe)) return false;

    // Single committee member
    if (committees.length === 1) return true;

    // Boss (first = chairman)
    const isBoss = isMe(committees[0]);

    if (isBoss) {
      // Boss can approve only when all non-unable members are done
      return committees
        .filter((_, index) => index !== 0 && !_.isUnableToPerformDuties)
        .every(s => s.status !== AcceptorStatus.Pending);
    }

    // Member: can approve if Pending and not unable
    return committees.some(s => isMe(s) && !s.isUnableToPerformDuties && s.status === AcceptorStatus.Pending);
  });

  const isCanAssign = computed(() =>
    [Cm007Status.WaitingAssignment].includes(body.value.status)
    && body.value.assignees.some(s =>
      s.assigneeGroup !== AssigneeGroup.Contract
      && (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id)
  );

  const isWaitingComment = computed(() =>
    [Cm007Status.WaitingComment, Cm007Status.RejectedToAssignee].includes(body.value.status)
    && body.value.assignees.some(s => s.assigneeType === AssigneeType.Assignee
      && (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id)
  );

  const isCurrentApprover = computed(() =>
    [Cm007Status.WaitingApproval].includes(body.value.status)
    && isCurrentPendingAcceptor(body.value.acceptors, auth.profile.id, AcceptorType.Approver)
  );

  const isLastApprover = computed(() => {
    if (!isCurrentApprover.value) return false;

    const pendingApprovers = body.value.acceptors
      .filter(f => f.acceptorType === AcceptorType.Approver && f.status === AcceptorStatus.Pending);

    if (pendingApprovers.length === 0) return false;

    const current = pendingApprovers
      .reduce((prev, curr) => curr.sequence < prev.sequence ? curr : prev, pendingApprovers[0]);

    return current.sequence === pendingApprovers[pendingApprovers.length - 1].sequence;
  });

  const isCommitteeRecall = computed(() => {
    if (!body.value.acceptors) return false;

    const status = body.value.status === Cm007Status.WaitingCommitteeApproval;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a =>
      [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status)
      && a.acceptorType === AcceptorType.AcceptanceCommittee
      && a.committeePositionsCode == CommitteePositions.PosBoard001
    );

    const isCommitteeMember = body.value.acceptors.some(s =>
      s.acceptorType === AcceptorType.AcceptanceCommittee
      && (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id
    );

    return status && !isAnyApprovalAndRejected && isCommitteeMember;
  });

  const isRecall = computed(() => {
    if (!body.value.assignees) return false;

    const status = body.value.status === Cm007Status.WaitingApproval;
    const checkUser = body.value.assignees.some(a =>
      a.assigneeType === AssigneeType.Assignee
      && (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id
    );

    if (!body.value.acceptors) return false;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a =>
      [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status)
      && a.acceptorType === AcceptorType.Approver
    );

    return status && checkUser && !isAnyApprovalAndRejected;
  });

  const isDirectorWaitingComment = computed(() =>
    body.value.status === Cm007Status.WaitingComment
    && body.value.assignees.some(a =>
      a.assigneeType === AssigneeType.Director
      && (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id
    )
  );

  const isCanAssignContractDrafter = computed(() =>
    body.value.status === Cm007Status.WaitingAssignment
    && body.value.assignees.some(s => s.assigneeGroup === AssigneeGroup.Contract
      && (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id)
  );

  const isCanAssignAddendumDrafter = computed(() =>
    body.value.status === Cm007Status.WaitingAddendumAssignment
    && body.value.assignees.some(s => s.assigneeGroup === AssigneeGroup.AddendumDrafter
      && (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id)
  );

  const isWaitingDraftAddendumDrafter = computed(() =>
    [Cm007Status.WaitingDraftAddendum].includes(body.value.status)
    && body.value.assignees.some(s => s.assigneeGroup === AssigneeGroup.AddendumDrafter
      && s.assigneeType === AssigneeType.Assignee
      && (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id)
  );

  const isCurrentReviewer = computed(() =>
    [Cm007Status.WaitingReview].includes(body.value.status)
    && body.value.acceptors.some(s => s.acceptorType === AcceptorType.Reviewer
      && (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id)
  );

  const canRestoreVersion = computed(() => isCanEdit.value);

  return {
    // Variables
    body,
    accordion,

    // Functions
    onResetBody,
    onGetById,
    onSaveAsync,
    onUpdateComponents,
    assigneeDefaultAcceptor,
    loginUserDefaultAcceptor,
    onSubmitCommitteeApprovalAsync,
    onCommitteeApprovedAsync,
    onRejectedAsync,
    onAssignAsync,
    onSubmitCommentAsync,
    onSubmitToApprovalAsync,
    onApproverApproveAsync,
    onRecallAsync,
    onUpsertAttachments,
    onDeleteAsync,
    onConfirmAddendumDrafterAsync,
    onConfirmDraftAddendumAsync,
    onReviewerRejectAsync,
    onReviewerApproveAsync,
    getReviewDocumentAsync,
    resetDocumentAsync,
    onUpdateDutiesStatusAsync,

    // Computed
    isCanEdit,
    isCommittee,
    isCurrentCommitteeApproval,
    isCanAssign,
    isWaitingComment,
    isCurrentApprover,
    isLastApprover,
    isCommitteeRecall,
    isRecall,
    isDirectorWaitingComment,
    isCanAssignContractDrafter,
    isCanAssignAddendumDrafter,
    isWaitingDraftAddendumDrafter,
    isCurrentReviewer,
    canRestoreVersion,
  };
});

import { Cm006Status } from "@/enums/CM/cm006";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import { SectionProcessType } from "@/enums/operations";
import { AcceptorStatus, AcceptorType, AssigneeType } from "@/enums/participants";
import { CommitteePositions } from "@/enums/PCM005/principle";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import ToastHelper from "@/helpers/toast";
import type { Cm006Detail, Cm006GuaranteeReturn, Cm006GuaranteeReturnCondition, Cm006GuaranteeReturnRequiredDocument } from "@/models/CM/cm006";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import type { Attachments, OnlyFileAttachment } from "@/models/shared/uploadFile";
import router from "@/router";
import Cm006Service from "@/services/CM/cm006";
import operationService from "@/services/Shared/operations";
import { useAuthenticationStore } from "@/stores/authentication";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { isCurrentPendingAcceptor } from "@/helpers/participants";

export const useCm006DetailStore = defineStore('cm006Detail', () => {
  const auth = useAuthenticationStore();

  const initBody = {
    guaranteeReturn: {
      isDeducted: false,
      acceptors: [] as Array<ParticipantsCommitteeAcceptor>,
      assignees: [] as Array<ParticipantsAssignee>,
      conditions: [] as Array<Cm006GuaranteeReturnCondition>,
      requiredDocuments: [] as Array<Cm006GuaranteeReturnRequiredDocument>,
      attachments: [] as Array<Attachments>,
    },
  } as Cm006Detail;

  const body = ref<Cm006Detail>(structuredClone(initBody));

  const onResetBody = () => {
    body.value = structuredClone(initBody);
  }

  const onGetById = async (contractVendorId: string, id?: string) => {
    const { data, status } = await Cm006Service.getByIdAsync(contractVendorId, id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;

      if (body.value.guaranteeReturn.status === Cm006Status.WaitingDisbursementDate
        && !body.value.guaranteeReturn.acceptors.some(a => a.acceptorType === AcceptorType.AccountingConfirmer)) {
        body.value.guaranteeReturn.acceptors.push({
          acceptorType: AcceptorType.AccountingConfirmer,
          sequence: body.value.guaranteeReturn.acceptors.length + 1,
          userId: auth.profile.id,
          fullName: auth.profile.name,
          positionName: auth.profile.positionName,
          status: AcceptorStatus.Draft,
        } as ParticipantsCommitteeAcceptor);
      }
    }
  };

  const onSubmitAsync = async () => {
    if (body.value.guaranteeReturn.id && await onUpdateAsync(body.value.guaranteeReturn.id, body.value.guaranteeReturn) === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      return;
    }

    if (await onCreateAsync() === HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();
    }
  };

  const onSendApprovalAsync = async () => {
    if (body.value.guaranteeReturn.conditions.some(s => !s.isSatisfied)) {
      return ToastHelper.errorDescription("กรุณาบันทึกผลพิจารณาคืนหลักประกันสัญญาให้ครบถ้วน");
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApprove)) return;

    const payload = {
      ...body.value.guaranteeReturn,
      status: Cm006Status.WaitingCommitteeApproval,
    } as Cm006GuaranteeReturn;

    if (body.value.guaranteeReturn.id && await onUpdateAsync(body.value.guaranteeReturn.id, payload) === HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast();
    }
  };

  const onCreateAsync = async () => {
    if (!body.value.id) return;
    const { data, status } = await Cm006Service.onCreateAsync(body.value.id, body.value.guaranteeReturn);

    if (status === HttpStatusCode.Created) {
      router.replace({ name: 'cm006Detail', params: { id: data, contractVendorId: body.value.id } });
      await onGetById(body.value.id, data);
    }

    return status;
  };

  const onUpdateAsync = async (guaranteeId: string, payload: Cm006GuaranteeReturn) => {
    if (!body.value.guaranteeReturn.id || !body.value.id) return;

    const { status, data } = await Cm006Service.onUpdateAsync(body.value.id, guaranteeId, payload);

    if (status === HttpStatusCode.Ok) {
      if (data?.newApprovalDocumentFileId) {
        body.value.guaranteeReturn.approvalCmContractGuaranteeReturnDocumentId = data.newApprovalDocumentFileId;
      }
      if (data?.newReturnDocumentFileId) {
        body.value.guaranteeReturn.contractGuaranteeReturnResultDocumentId = data.newReturnDocumentFileId;
      }
      await onGetById(body.value.id, guaranteeId);
    }

    return status;
  };

  const onCommitteeApprovedAsync = async () => {
    if (!body.value.guaranteeReturn.id || !body.value.id) return;

    const resp = await showReasonDialogAsync(ReasonDialogType.Accepted);

    if (!resp.isConfirm) return;
    const { status } = await Cm006Service.onApproveAsync(body.value.id, body.value.guaranteeReturn.id, resp.reason);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.approvedMessageToast();

      await onGetById(body.value.id, body.value.guaranteeReturn.id);
    }
  };

  const onRejectedAsync = async () => {
    if (!body.value.guaranteeReturn.id || !body.value.id) return;

    const resp = await showReasonDialogAsync(ReasonDialogType.Reject);

    if (!resp.isConfirm) return;
    const { status } = await Cm006Service.onRejectedAsync(body.value.id, body.value.guaranteeReturn.id, resp.reason);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();

      await onGetById(body.value.id, body.value.guaranteeReturn.id);
    }
  };

  const onConfirmAssignedAsync = async () => {
    if (!body.value.guaranteeReturn.id) return;

    if (!body.value.guaranteeReturn.assignees.some(s => s.assigneeType === AssigneeType.Assignee)) {
      ToastHelper.assignAtLeastMessageToast();

      return;
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;
    const payload = {
      ...body.value.guaranteeReturn,
      status: Cm006Status.Assigned,
    } as Cm006GuaranteeReturn;

    if (body.value.guaranteeReturn.id && await onUpdateAsync(body.value.guaranteeReturn.id, payload) === HttpStatusCode.Ok) {
      ToastHelper.assignedMessageToast();
    }
  };

  const onReCallAsync = async () => {
    if (!body.value.guaranteeReturn.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;
    const payload = {
      ...body.value.guaranteeReturn,
      status: body.value.guaranteeReturn.status === Cm006Status.WaitingCommitteeApproval ? Cm006Status.Draft : Cm006Status.Assigned,
    } as Cm006GuaranteeReturn;

    if (body.value.guaranteeReturn.id && await onUpdateAsync(body.value.guaranteeReturn.id, payload) === HttpStatusCode.Ok) {
      ToastHelper.assignedMessageToast();
    }
  };

  const isRecall = computed(() => {
    if (!body.value.guaranteeReturn.assignees) return false;

    const status = body.value.guaranteeReturn.status === Cm006Status.WaitingAcceptance;
    const checkUser = body.value.guaranteeReturn.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id);

    if (!body.value.guaranteeReturn.acceptors) return false;

    const isAnyApprovalAndRejected = body.value.guaranteeReturn.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.Approver);

    return status && checkUser && !isAnyApprovalAndRejected;
  });

  const onSendAcceptorApproveAsync = async () => {
    if (!body.value.guaranteeReturn.id) return;

    if (!body.value.guaranteeReturn.acceptors.some(s => s.acceptorType === AcceptorType.Approver)) {
      ToastHelper.approvalAtLeastMessageToast();

      return;
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;
    const payload = {
      ...body.value.guaranteeReturn,
      status: Cm006Status.WaitingAcceptance,
    } as Cm006GuaranteeReturn;

    if (body.value.guaranteeReturn.id && await onUpdateAsync(body.value.guaranteeReturn.id, payload) === HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast();
    }
  };

  const onAcceptorApproverAsync = async () => {
    if (!body.value.guaranteeReturn.id || !body.value.id) return;

    const resp = await showReasonDialogAsync(isLastApprover.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (!resp.isConfirm) return;
    const { status } = await Cm006Service.onApproveAsync(body.value.id, body.value.guaranteeReturn.id, resp.reason);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.approvedMessageToast();

      await onGetById(body.value.id, body.value.guaranteeReturn.id);
    }
  };

  const onUpsertAttachments = async () => {
    if (!body.value.guaranteeReturn.id || !body.value.id) return;

    const { status } = await Cm006Service.attachmentsAsync(body.value.guaranteeReturn.id, body.value.guaranteeReturn.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await onGetById(body.value.id, body.value.guaranteeReturn.id);
    }
  };

  const onUpsertAttachmentsFromExpenseDisbursement = async (id: string, attachments: Attachments[]) => {
    const { status } = await Cm006Service.attachmentsAsync(id, attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
    }
  };

  const getReviewDocumentAsync = async (contractId: string, id: string, documentType: string): Promise<string> => {
    const { data, status } = await Cm006Service.getReviewDocumentAsync(contractId, id, documentType);

    if (status !== HttpStatusCode.Ok) {
      return '';
    }

    return data;
  };

  const onSetDuties = async (acceptorId: string, isUnableDuties: boolean, remark?: string) => {
    if (!body.value.guaranteeReturn.id || !body.value.id) return;

    const { status } = await Cm006Service.onSetDutiesAsync(body.value.guaranteeReturn.id, { acceptorId, isUnableToPerformDuties: isUnableDuties, remark });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');

      await onGetById(body.value.id, body.value.guaranteeReturn.id);
    }
  };

  const isCanEdit = computed(() => [Cm006Status.Draft, Cm006Status.Rejected].includes(body.value.guaranteeReturn.status) && body.value.guaranteeReturn.acceptors.filter(f => f.acceptorType === AcceptorType.AcceptanceCommittee).some(s => s.userId === auth.profile.id));

  const isCommittee = computed(() => body.value.guaranteeReturn.acceptors.filter(f => f.acceptorType === AcceptorType.AcceptanceCommittee).some(s => s.userId === auth.profile.id));

  const isCommiteeApproval = computed(() => [Cm006Status.WaitingCommitteeApproval].includes(body.value.guaranteeReturn.status) && isCommittee.value);

  const isCurrentCommiteeApproval = computed(() => isCommiteeApproval.value && body.value.guaranteeReturn.acceptors.filter(f => f.acceptorType === AcceptorType.AcceptanceCommittee).some(s => s.userId === auth.profile.id && s.isCurrent));

  const isCanAssign = computed(() => [Cm006Status.WaitingAssigned].includes(body.value.guaranteeReturn.status) && body.value.guaranteeReturn.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isConfirmAssigned = computed(() => [Cm006Status.Assigned].includes(body.value.guaranteeReturn.status) && body.value.guaranteeReturn.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isCurrentApprover = computed(() =>
    [Cm006Status.WaitingAcceptance].includes(body.value.guaranteeReturn.status)
    && isCurrentPendingAcceptor(body.value.guaranteeReturn.acceptors, auth.profile.id, AcceptorType.Approver)
  );

  const isLastApprover = computed(() => isCurrentApprover.value && body.value.guaranteeReturn.acceptors.filter(f => f.acceptorType === AcceptorType.Approver).filter(f => f.status === AcceptorStatus.Pending).length === 1);

  const isCommitteeRecall = computed(() => {
    if (!body.value.guaranteeReturn.acceptors) return false;

    const status = body.value.guaranteeReturn.status === Cm006Status.WaitingCommitteeApproval;

    const isAnyApprovalAndRejected = body.value.guaranteeReturn.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status)
      && a.acceptorType === AcceptorType.AcceptanceCommittee
      && a.committeePositionsCode == CommitteePositions.PosBoard001);

    const isCommittee = body.value.guaranteeReturn.acceptors.some(s => s.acceptorType === AcceptorType.AcceptanceCommittee && s.userId === auth.profile.id);

    return status && !isAnyApprovalAndRejected && isCommittee;
  });

  const canRestoreVersion = computed(() => isCanEdit.value);

  const resetDocumentAsync = async (contractVendorId: string, id: string, documentType: string): Promise<void> => {
    const { status } = await Cm006Service.resetDocumentAsync(contractVendorId, id, documentType);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
      await onGetById(contractVendorId, id);
    }
  };

  const assigneeDefaultAcceptor = async (): Promise<void> => {
    const assignees = body.value.guaranteeReturn.assignees.filter(f => f.assigneeType === AssigneeType.Assignee);
    const lastAssignee = assignees[assignees.length - 1];

    const params = {
      processType: SectionProcessType.ContractGuaranteeReturn,
      userId: lastAssignee?.delegateeUserId ? lastAssignee?.delegateeUserId : lastAssignee?.userId,
      budget: body.value.budget,
      supplyMethodCode: "SMethod002",
      supplyMethodSpecialTypeCode: undefined,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status == HttpStatusCode.Ok) {
      body.value.guaranteeReturn.acceptors = body.value.guaranteeReturn.acceptors.filter(f => f.acceptorType !== AcceptorType.Approver);

      data.forEach(item => body.value.guaranteeReturn.acceptors.push({
        acceptorType: AcceptorType.Approver,
        fullName: item.fullName,
        positionName: item.fullPositionName,
        sequence: body.value.guaranteeReturn.acceptors.length + 1,
        status: AcceptorStatus.Draft,
        userId: item.userId,
        departmentName: item.businessUnitName,
        organizationLevel: item.organizationLevel,
      } as ParticipantsCommitteeAcceptor))
    }
  };

  const getDefaultAccountingAcceptor = async (): Promise<void> => {
    const { data: defaultExpense, status: defaultStatus } =
      await operationService.getDefaultExpenseDisbursementAsync();

    if (defaultStatus === HttpStatusCode.Ok) {
      const params = {
        processType: SectionProcessType.ExpenseDisbursement,
        budget: body.value.budget,
        userId: defaultExpense.userId,
        supplyMethodCode: "SectionApprover001",
        skipCurrentEmployee: false,
      } as defaultAcceptorCriteria;

      const { data: acceptorList, status: acceptorStatus } =
        await operationService.getOperationsDefaultAcceptorAsync(params);

      if (acceptorStatus === HttpStatusCode.Ok) {
        body.value.guaranteeReturn.acceptors = body.value.guaranteeReturn.acceptors.filter(
          f => f.acceptorType !== AcceptorType.Accounting
        );

        acceptorList.forEach(item =>
          body.value.guaranteeReturn.acceptors.push({
            acceptorType: AcceptorType.Accounting,
            fullName: item.fullName,
            positionName: item.fullPositionName,
            sequence: body.value.guaranteeReturn.acceptors.length + 1,
            status: AcceptorStatus.Draft,
            userId: item.userId,
            departmentName: item.businessUnitName,
          } as ParticipantsCommitteeAcceptor)
        );
      }
    }
  };

  const isAccountingCanAssign = computed(() => {
    const accountingAcceptors = body.value.guaranteeReturn.acceptors.filter(
      f => f.acceptorType === AcceptorType.Accounting
    );
    if (!accountingAcceptors.length) return false;

    const status = [Cm006Status.WaitingAccountingApproval, Cm006Status.AccountingRejected]
      .includes(body.value.guaranteeReturn.status);
    const currentUser = accountingAcceptors.find(
      a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id
    );

    if (!currentUser) return false;

    const isCurrentUserPending = currentUser.status === AcceptorStatus.Pending && currentUser.isCurrent;
    const allAccountPending = accountingAcceptors.every(
      s => s.status === AcceptorStatus.Pending || s.status === AcceptorStatus.Draft
    );

    return status && isCurrentUserPending && allAccountPending;
  });

  const isAccountingApprover = computed(() => {
    return body.value.guaranteeReturn.acceptors
      .filter(f => f.acceptorType === AcceptorType.Accounting && f.isCurrent)
      .some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id);
  });

  const isLastAccountingApprover = computed(() => {
    const accountingAcceptors = body.value.guaranteeReturn.acceptors.filter(
      f => f.acceptorType === AcceptorType.Accounting
    );
    if (!accountingAcceptors.length) return false;

    const currentUser = accountingAcceptors.find(
      a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id
    );

    if (!currentUser || currentUser.status !== AcceptorStatus.Pending) return false;

    const pendingUsers = accountingAcceptors.filter(a => a.status === AcceptorStatus.Pending);
    const maxSequence = Math.max(...pendingUsers.map(a => a.sequence));

    return currentUser.sequence === maxSequence;
  });

  const isAccountingCanEdit = computed(() => {
    return [Cm006Status.WaitingDisbursementDate].includes(body.value.guaranteeReturn.status)
      && auth.profile.businessUnitCode == "88811";
  });

  const onAccountingApproveOrRejectAsync = async (type: 'Approve' | 'Reject') => {
    if (!body.value.guaranteeReturn.id || !body.value.id) return;

    const reasonDialogType = {
      'Approve': ReasonDialogType.Confirm,
      'Reject': ReasonDialogType.Reject,
    };

    const resp = await showReasonDialogAsync(reasonDialogType[type]);
    if (!resp.isConfirm) return;

    const mapApi = {
      'Approve': () => Cm006Service.onAccountingApproveAsync(body.value.id!, body.value.guaranteeReturn.id!, { remark: resp.reason }),
      'Reject': () => Cm006Service.onAccountingRejectAsync(body.value.id!, body.value.guaranteeReturn.id!, { remark: resp.reason }),
    };

    const { status } = await mapApi[type]();
    if (status === HttpStatusCode.Ok) {
      type === 'Approve' ? ToastHelper.approvedMessageToast() : ToastHelper.sendEditMessageToast();
    }

    await onGetById(body.value.id, body.value.guaranteeReturn.id);
  };

  const onSetDisbursementDateAsync = async () => {
    if (!body.value.guaranteeReturn.id || !body.value.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData, "ยืนยันวันที่เบิกจ่าย")) return;

    const payload = {
      ...body.value.guaranteeReturn,
      status: Cm006Status.Paid,
    } as Cm006GuaranteeReturn;

    const { status } = await Cm006Service.onUpdateAsync(body.value.id, body.value.guaranteeReturn.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await onGetById(body.value.id, body.value.guaranteeReturn.id);
    }
  };

  const onSendEmailAsync = async (
    email: string,
    editorContent: string,
    attachments: OnlyFileAttachment[]
  ) => {
    if (!body.value.guaranteeReturn.id || !body.value.id) return;

    const { status } = await Cm006Service.sendEmailAsync(
      body.value.id,
      body.value.guaranteeReturn.id,
      email,
      editorContent,
      attachments
    );

    if (status === HttpStatusCode.Ok || status === HttpStatusCode.Accepted) {
      ToastHelper.success('ส่งอีเมลคืนหลักประกัน', 'ส่งอีเมลคืนหลักประกันสำเร็จ');
      await onGetById(body.value.id, body.value.guaranteeReturn.id);
    }
  };

  return {
    // Variables
    body,

    // Functions
    onUpsertAttachmentsFromExpenseDisbursement,
    getReviewDocumentAsync,
    onGetById,
    onSubmitAsync,
    onSendApprovalAsync,
    onCommitteeApprovedAsync,
    onRejectedAsync,
    onConfirmAssignedAsync,
    onSendAcceptorApproveAsync,
    onAcceptorApproverAsync,
    onUpsertAttachments,
    onSetDuties,
    onReCallAsync,
    resetDocumentAsync,
    onResetBody,
    onAccountingApproveOrRejectAsync,
    onSetDisbursementDateAsync,
    onSendEmailAsync,
    // States
    isCanEdit,
    isCommittee,
    isCommiteeApproval,
    isCurrentCommiteeApproval,
    isCanAssign,
    isConfirmAssigned,
    isCurrentApprover,
    isLastApprover,
    isCommitteeRecall,
    isRecall,
    canRestoreVersion,
    assigneeDefaultAcceptor,
    getDefaultAccountingAcceptor,
    isAccountingCanAssign,
    isAccountingApprover,
    isLastAccountingApprover,
    isAccountingCanEdit,
  };
});
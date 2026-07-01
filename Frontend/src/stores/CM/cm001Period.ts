import { CM001PeriodStatus, CmDeliveryAcceptancePeriodAccountStatus } from "@/enums/CM/cm001";
import { OrganizationLevelEnum } from "@/enums/shared";
import ToastHelper from "@/helpers/toast";
import type { AcceptanceDeductionItem, Cm001PaymentTerm, CM001PeriodBody, Delivery, PeriodAcceptanceInfo } from "@/models/CM/cm001";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { useAuthenticationStore } from "../authentication";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import { AcceptorStatus, AcceptorType, AssigneeType } from "@/enums/participants";
import CM001PeriodService from "@/services/CM/cm001Period";
import operationService from "@/services/Shared/operations";
import { SectionProcessType } from "@/enums/operations";
import type { ParticipantsAcceptor } from "@/models/shared/participants";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import { CommitteePositions } from "@/enums/PCM005/principle";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import router from "@/router";
import type { Option } from "@/models/shared/option";

export const useCM001PeriodStore = defineStore('cm001-period-store', () => {
  const auth = useAuthenticationStore();

  const initBody = {
    status: CM001PeriodStatus.Draft,
    deliveries: [] as Array<Delivery>,
    periodAcceptanceInfo: {
      acceptanceDeductionItems: [] as Array<AcceptanceDeductionItem>,
    } as PeriodAcceptanceInfo,
    acceptanceCommittees: [],
    assignees: [],
    acceptors: [],
    paymentTerms: [],
    budgetDetails: [],
    inspectionCommittees: {
      committees: [],
      isCommittee: true,
    },
    acceptanceOfAccounting: [],
    acceptanceConfirmers: [],
    acceptorStatus: CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval,
    paymentTermOnUser: [],
    totalPaymentOnUser: 0,
    attachments: [],
  } as unknown as CM001PeriodBody;

  const positionInspOptions = ref<Option[]>([]);

  const resetBody = () => {
    body.value = structuredClone(initBody);
    hasJorPorAssign.value = false;
  }

  const cloneAccountingAcceptors = ref<Array<ParticipantsAcceptor>>([]);
  const hasJorPorAssign = ref(false);
  const isCurrentUserAccountingSegmentMember = ref(false);

  const isLoading = ref(false);
  const body = ref<CM001PeriodBody>({ ...initBody } as CM001PeriodBody);

  const onGetByIdAsync = async (deliveryAcceptanceId: string, id?: string, clearBranchAccounting = false): Promise<void> => {
    const { data, status } = await CM001PeriodService.onGetByIdAsync(deliveryAcceptanceId, id);

    if (status === HttpStatusCode.Ok) {
      isLoading.value = true;
      body.value = data;


      if (body.value.paymentTerms.length == 0) {
        body.value.paymentTerms = [{ sequence: 1, paymentTerm: 1, description: '', amount: 0 } as Cm001PaymentTerm];
      }

      if (!body.value.objectiveDescription || body.value.objectiveDescription === "") {
        body.value.objectiveDescription = "ตามที่...................(ฝ่าย/สำนัก/ศูนย์/เขต/สาขา)......................ได้รับอนุมัติซื้อ/จ้าง.................................................จำนวน........................................จาก............................(ผู้ขาย/ผู้รับจ้าง)................รวมเป็นเงินทั้งสิ้น..................................(..................................) ซึ่งเป็นราคาที่รวมภาษีมูลค่าเพิ่ม ภาษีอื่น ค่าขนส่งค่าจดทะเบียน และค่าใช้จ่ายอื่นๆ เรียบร้อยแล้ว รายละเอียดปรากฏตามรายงานขอซื้อขอจ้างและรายงานผลการ พิจารณาและขออนุมัติสั่งซื้อสั่งจ้าง"
      }

      if (!body.value.description || body.value.description === "") {
        body.value.description = "...........................(ผู้ขาย/ผู้รับจ้าง).................................. ได้ดำเนินการจัดทำ/จัดส่งพัสดุให้แก่ธนาคารแล้ว โดยมีรายละเอียด ดังนี้............................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................"
      }

      if (!body.value.contractBudgetAmount) {
        body.value.contractBudgetAmount = body.value.cm001Info?.contractBudget || 0;
      }

      if (!body.value.acceptanceConfirmers) {
        body.value.acceptanceConfirmers = [];
      }

      if (clearBranchAccounting
        && [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)].includes(body.value.departmentOrganizationLevel ?? '')) {
        body.value.acceptanceOfAccounting = [];
      }

      if (body.value.id &&
        body.value.status === CM001PeriodStatus.Approved &&
        [CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval, CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate].includes(body.value.accountStatus) &&
        ![String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)].includes(body.value.departmentOrganizationLevel ?? '') &&
        auth.profile) {
        const { data: accountingMembers, status: accountingStatus } =
          await operationService.getSegmentAccountingMembersAsync(true);

        if (accountingStatus === HttpStatusCode.Ok) {
          isCurrentUserAccountingSegmentMember.value =
            accountingMembers?.some((m): boolean => m.userId === auth.profile.id) ?? false;

          if (
            body.value.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval &&
            isCurrentUserAccountingSegmentMember.value &&
            !(body.value.acceptanceOfAccounting ?? []).some((a): boolean => a.acceptorType === AcceptorType.AccountingOperator) &&
            !(body.value.acceptanceOfAccounting ?? []).some((a): boolean => a.acceptorType === AcceptorType.Accounting && a.userId === auth.profile.id)
          ) {
            if (!body.value.acceptanceOfAccounting) {
              body.value.acceptanceOfAccounting = [];
            }
            body.value.acceptanceOfAccounting.push({
              sequence: 1,
              userId: auth.profile.id,
              fullName: auth.profile.name,
              positionName: auth.profile.positionName,
              acceptorType: AcceptorType.AccountingOperator,
              status: AcceptorStatus.Draft,
            } as ParticipantsAcceptor);
          }

          if (
            body.value.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate &&
            isCurrentUserAccountingSegmentMember.value &&
            body.value.acceptanceConfirmers.length === 0
          ) {
            body.value.acceptanceConfirmers = [{
              sequence: 1,
              userId: auth.profile.id,
              fullName: auth.profile.name,
              positionName: auth.profile.positionName,
              acceptorType: AcceptorType.AccountingConfirmer,
              status: AcceptorStatus.Draft,
            } as ParticipantsAcceptor];
          }
        }
      }

      cloneAccountingAcceptors.value = [...(body.value.acceptanceOfAccounting || [])];

      if (body.value.id &&
        [CM001PeriodStatus.Draft, CM001PeriodStatus.Rejected, CM001PeriodStatus.Edit, CM001PeriodStatus.Approved].includes(body.value.status) &&
        ![String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)].includes(body.value.departmentOrganizationLevel ?? '') &&
        (!body.value.acceptanceOfAccounting || body.value.acceptanceOfAccounting.length === 0)) {
        await getDefaultDisbursementAcceptor();
      }

      isLoading.value = false;
      return;
    }

    router.back();
  };

  const onSubmitAsync = async (deliveryAcceptanceId: string) => {
    if (body.value.id) {
      await onUpdateAsync(deliveryAcceptanceId);

      return;
    }

    await onCreateAsync(deliveryAcceptanceId);
  };

  const onCreateAsync = async (deliveryAcceptanceId: string) => {
    const { status, data } = await CM001PeriodService.onCreateAsync(deliveryAcceptanceId, body.value);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.createdMessageToast();

      router.replace({ name: 'cm001PeriodDetail', params: { id: deliveryAcceptanceId, periodId: data } });

      await onGetByIdAsync(deliveryAcceptanceId, data);
    }
  }

  const onUpdateAsync = async (deliveryAcceptanceId: string, showToast: boolean = true) => {
    if (!body.value.id) return;
    const { status, data } = await CM001PeriodService.onUpdateAsync(deliveryAcceptanceId, body.value.id, body.value);

    if (status === HttpStatusCode.Ok) {
      if (data?.newDocumentFileId) {
        body.value.documentId = data.newDocumentFileId;
      }
      if (showToast) {
        ToastHelper.updatedMessageToast();

        await onGetByIdAsync(deliveryAcceptanceId, body.value.id);
      }
    }
  };

  const onSetDisburmentDateAsync = async (deliveryAcceptanceId: string) => {
    if (!body.value.id) return;

    const mockData = {
      ...body.value,
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData, "ยืนยันวันที่เบิกจ่าย")) return;

    mockData.accountStatus = CmDeliveryAcceptancePeriodAccountStatus.Paid;

    const { status } = await CM001PeriodService.onUpdateAsync(deliveryAcceptanceId, body.value.id, mockData);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await onGetByIdAsync(deliveryAcceptanceId, body.value.id);
    }
  }


  const onSendCommitteeApprovalAsync = async (deliveryAcceptanceId: string) => {
    if (!body.value.id) return;

    if (body.value.acceptors && body.value.acceptors.length == 0) {
      return ToastHelper.approvalAtLeastMessageToast();
    }

    const totalPayment = body.value.paymentTerms.reduce((sum, item) => sum + (item.amount || 0), 0);
    const totalDeductions = body.value.hasInvoiceSlip ? (body.value.invoiceSlipAmount || 0) : 0;
    const finalAmount = totalPayment - totalDeductions;
    const budgetLimit = body.value.cm001Info?.budget || body.value.cm001Info?.contractBudget || 0;

    if (finalAmount > (body.value.contractBudgetAmount ?? 0)) {
      return ToastHelper.error('ไม่สามารถตรวจรับได้', 'ผลรวมของจำนวนเงินรับพัสดุเกินกว่างบประมาณตามสัญญา');
    }

    if (totalPayment + (body.value.totalPaymentOnUser ?? 0) > budgetLimit) {
      const exceed = (totalPayment + (body.value.totalPaymentOnUser ?? 0)) - budgetLimit;
      return ToastHelper.error('ไม่สามารถตรวจรับได้', `ผลรวมของจำนวนเงินรับพัสดุรวมกับยอดที่ผู้ใช้ระบุเกินกว่างบประมาณตามสัญญา ${exceed.toLocaleString()} บาท`);
    }

    const totalBudget = body.value.budgetDetails.reduce((sum, item) => sum + (item.budget || 0), 0);

    if (totalBudget != finalAmount) {
      return ToastHelper.error('ไม่สามารถตรวจรับได้', 'ผลรวมจำนวนเงินของรหัสบัญชีไม่เท่ากับรวมจำนวนเงินทั้งสิ้น');
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

    const payload: CM001PeriodBody = {
      ...body.value,
      isDocumentReplaced: false,
      status: CM001PeriodStatus.WaitingCommitteeApproval,
    };

    const { status } = await CM001PeriodService.onUpdateAsync(deliveryAcceptanceId, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast();
    }

    await onGetByIdAsync(deliveryAcceptanceId, body.value.id);
  };

  const onRecallAsync = async (deliveryAcceptanceId: string) => {
    if (!body.value.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

    const payload: CM001PeriodBody = {
      ...body.value,
      isDocumentReplaced: false,
      status: CM001PeriodStatus.Edit,
    };

    const { status } = await CM001PeriodService.onUpdateAsync(deliveryAcceptanceId, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.recallEditMessageToast();
    }

    await onGetByIdAsync(deliveryAcceptanceId, body.value.id);
  };

  const onSendApproveOrRejectAsync = async (deliveryAcceptanceId: string, type: 'Approve' | 'Reject', group: AcceptorType.AcceptanceCommittee | AcceptorType.Approver) => {
    if (!body.value.id) return;

    const reasonDialogType = {
      'Approve': (isLastApprover.value) ? ReasonDialogType.Approve : ReasonDialogType.Accepted,
      'Reject': group === AcceptorType.AcceptanceCommittee ? ReasonDialogType.NotAgree : ReasonDialogType.Reject,
    }

    const resp = await showReasonDialogAsync(reasonDialogType[type]);

    if (!resp.isConfirm) return;

    const mapApi = {
      'Approve': async () => await CM001PeriodService.onApproveAsync(deliveryAcceptanceId, body.value.id!, { group: group, remark: resp.reason }),
      'Reject': async () => await CM001PeriodService.onRejectAsync(deliveryAcceptanceId, body.value.id!, { group: group, remark: resp.reason }),
    };

    const { status } = await mapApi[type]();

    if (status === HttpStatusCode.Ok) {
      if (type === 'Approve') {
        ToastHelper.approvedMessageToast();
      } else if (group === AcceptorType.AcceptanceCommittee) {
        ToastHelper.notAgreeMessageToast();
      } else {
        ToastHelper.sendEditMessageToast();
      }
    }

    await onGetByIdAsync(deliveryAcceptanceId, body.value.id, true);
  };

  const onAccountingApproveOrRejectAsync = async (deliveryAcceptanceId: string, type: 'Approve' | 'Reject') => {
    if (!body.value.id) return;

    let dialogType: ReasonDialogType;

    if (type === 'Reject') {
      dialogType = ReasonDialogType.Reject;
    } else if (isAccountingApprover.value) {
      dialogType = ReasonDialogType.Confirm;
    } else if (isLastAccountingApprover.value) {
      dialogType = ReasonDialogType.Approve;
    } else {
      dialogType = ReasonDialogType.Accepted;
    }

    const isRequired = type === 'Approve' && isLastAccountingApprover.value;
    const resp = await showReasonDialogAsync(dialogType, isRequired);

    if (!resp.isConfirm) return;

    const mapApi = {
      'Approve': async () => await CM001PeriodService.onAccountingApproveAsync(body.value.id!, { remark: resp.reason }),
      'Reject': async () => await CM001PeriodService.onAccountingRejectAsync(deliveryAcceptanceId, body.value.id!, { remark: resp.reason }),
    }

    const { status } = await mapApi[type]();

    if (status === HttpStatusCode.Ok) {
      if (type === 'Approve') {
        ToastHelper.approvedMessageToast();
      } else {
        ToastHelper.sendEditMessageToast();
      }
    }

    await onGetByIdAsync(deliveryAcceptanceId, body.value.id);
  }

  const onRejectAssigneeAsync = async (deliveryAcceptanceId: string) => {
    if (!body.value.id) return;

    const res = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (!res.isConfirm) return;

    const { status } = await CM001PeriodService.onJorPorRejectedAsync(deliveryAcceptanceId, body.value.id, { remark: res.reason });

    if (status === HttpStatusCode.Ok) {
      await onGetByIdAsync(deliveryAcceptanceId, body.value.id);

      return ToastHelper.sendEditMessageToast();
    }
  };

  const onAssigneeCommentAsync = async (deliveryAcceptanceId: string, reason: string) => {
    if (!body.value.id) return;

    const { status } = await CM001PeriodService.onCommentAsync(deliveryAcceptanceId, body.value.id, { remark: reason });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.remarkOfficerMessageToast();
    }

    await onGetByIdAsync(deliveryAcceptanceId, body.value.id);
  };

  const onConfirmAssignedAsync = async (deliveryAcceptanceId: string) => {
    if (!body.value.id) return;

    if (!body.value.assignees.some(s => s.assigneeType === AssigneeType.Assignee)) {
      return ToastHelper.assignAtLeastMessageToast();
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

    const payload: CM001PeriodBody = {
      ...body.value,
      isDocumentReplaced: false,
      status: CM001PeriodStatus.WaitingComment,
    };

    const { status } = await CM001PeriodService.onUpdateAsync(deliveryAcceptanceId, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.assignedMessageToast();
    }

    await onGetByIdAsync(deliveryAcceptanceId, body.value.id);
  };

  const onSendApproveAsync = async (deliveryAcceptanceId: string) => {
    if (!body.value.id) return;

    if (body.value.assignees.filter(f => f.remark).length <= 0) {
      return ToastHelper.assignneeCommentAtLeastMessageToast();
    }

    if (body.value.acceptors.length <= 0) {
      return ToastHelper.approvalAtLeastMessageToast();
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

    const payload: CM001PeriodBody = {
      ...body.value,
      isDocumentReplaced: false,
      status: CM001PeriodStatus.WaitingAcceptance,
    };

    const { status } = await CM001PeriodService.onUpdateAsync(deliveryAcceptanceId, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast();
    }

    await onGetByIdAsync(deliveryAcceptanceId, body.value.id);
  };

  const onUpdateDutiesStatusAsync = async (deliveryAcceptanceId: string, flag: boolean, remark?: string, acceptorId?: string) => {
    if (!body.value.id) return;

    if (!acceptorId) return;

    const { status } = await CM001PeriodService.setDutyAsync(deliveryAcceptanceId, body.value.id, { acceptorId: acceptorId, isUnableToPerformDuties: flag, remark: remark });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');

      const acceptor = body.value.acceptanceCommittees.find(a => a.id === acceptorId);
      if (acceptor) {
        acceptor.isUnableToPerformDuties = flag;
        acceptor.remark = remark;
        acceptor.status = flag ? AcceptorStatus.UnableToPerformDuties : AcceptorStatus.Pending;
      }
    }
  };

  const getReviewDocumentAsync = async (deliveryAcceptanceId: string): Promise<string> => {
    if (!body.value.id) return '';

    const { data, status } = await CM001PeriodService.getReviewDocumentAsync(deliveryAcceptanceId, body.value.id);

    if (status !== HttpStatusCode.Ok) {
      return '';
    }

    return data;
  };

  const isEdit = computed(() => [CM001PeriodStatus.Draft, CM001PeriodStatus.Edit, CM001PeriodStatus.Rejected].includes(body.value.status) && body.value.hasEditPermission);

  const canAssignee = computed(() => [CM001PeriodStatus.WaitingAssign, CM001PeriodStatus.WaitingComment, CM001PeriodStatus.Approved].includes(body.value.status) || (body.value.assignees.some(s => s.assigneeType === AssigneeType.Assignee)));

  const jorporCanAssignByAssignee = computed(() => {
    if (!body.value.assignees) return false;

    const status = [CM001PeriodStatus.WaitingAssign, CM001PeriodStatus.RejectToAssignee].includes(body.value.status);
    const checkUser = body.value.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id);

    return status && checkUser;
  });

  const canEditDocument = computed(() => {
    if (!body.value.acceptanceCommittees) return false;

    const statusList = [CM001PeriodStatus.Draft, CM001PeriodStatus.Rejected, CM001PeriodStatus.Edit, CM001PeriodStatus.WaitingComment];
    const status = statusList.includes(body.value.status) || !body.value.status;
    const checkUser = body.value.acceptanceCommittees.some(a => a.acceptorType === AcceptorType.AcceptanceCommittee &&
      a.userId === auth.profile.id);

    const checkUserAssignees = body.value?.assignees?.some(a => a.assigneeType === AssigneeType.Assignee &&
      (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id);

    return status && (checkUser || checkUserAssignees);
  });

  const isCommitteeApproval = computed(() => [CM001PeriodStatus.WaitingCommitteeApproval].includes(body.value.status) &&
    (body.value.acceptanceCommittees.some(s => s.acceptorType === AcceptorType.AcceptanceCommittee && s.userId === auth.profile.id)));

  const isCommitteeCurrentApproval = computed(() => {
    if (!isCommitteeApproval.value) return false;

    if (body.value.acceptanceCommittees.length === 1) {
      return true;
    }

    const isBoss = body.value.acceptanceCommittees[0].userId === auth.profile.id;

    if (isBoss) {
      return body.value.acceptanceCommittees
        .filter((value, index) => (index !== 0 && !value.isUnableToPerformDuties))
        .every(s => s.status !== AcceptorStatus.Pending);
    }

    return body.value.acceptanceCommittees
      .some(s => s.userId === auth.profile.id && !s.isUnableToPerformDuties && s.status === AcceptorStatus.Pending);
  });

  const isRecall = computed(() => {
    if (!isCommitteeApproval.value) {
      return false;
    }

    return body.value.acceptanceCommittees.every(s => s.status === AcceptorStatus.Pending);
  });

  const canCommitteeRecall = computed(() => {
    if (!body.value.acceptanceCommittees) return false;

    const status = body.value.status === CM001PeriodStatus.WaitingCommitteeApproval;

    const isAnyApprovalAndRejected = body.value.acceptanceCommittees.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status)
      && a.acceptorType === AcceptorType.AcceptanceCommittee
      && a.committeePositionsCode == CommitteePositions.PosBoard001);

    const isCommittee = body.value.acceptanceCommittees.some(s => s.acceptorType === AcceptorType.AcceptanceCommittee && s.userId === auth.profile.id);

    return status && !isAnyApprovalAndRejected && isCommittee;
  });

  const isAssignee = computed(() => body.value.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isAssigned = computed(() => {
    return [CM001PeriodStatus.WaitingAssign, CM001PeriodStatus.RejectToAssignee].includes(body.value.status) && isAssignee.value;
  })

  const isComment = computed(() => {
    return [CM001PeriodStatus.WaitingComment].includes(body.value.status) && isAssignee.value;
  });

  const isCurrentApprover = computed(() =>
    [CM001PeriodStatus.WaitingAcceptance].includes(body.value.status)
    && isCurrentPendingAcceptor(body.value.acceptors, auth.profile.id, AcceptorType.Approver)
  );

  const isLastApprover = computed(() => {
    const acceptorData = body.value.acceptors
      .filter(f => f.status === AcceptorStatus.Pending);

    const current = acceptorData
      .reduce((prev, curr) => curr.sequence < prev.sequence ? curr : prev,
        acceptorData[0]);

    return isCurrentApprover.value && current.sequence === acceptorData[acceptorData.length - 1].sequence;
  });

  const getDefaultAcceptor = async (overrideUserId?: string) => {
    let processType = SectionProcessType.DeliveryAcceptancePeriod;

    if (body.value.hasDeduction) {
      processType = SectionProcessType.DeliveryAcceptancePeriodPenalty;
    } else if (body.value.isCommercialMaterial) {
      processType = SectionProcessType.DeliveryAcceptancePeriodCommercialParcel;
    }

    let userId = overrideUserId;

    if (!userId) {
      const committeeUserDepartment = body.value.acceptanceCommittees.filter(c => c.departmentCode === body.value.departmentCode);

      userId = committeeUserDepartment.length > 0
        ? committeeUserDepartment[committeeUserDepartment.length - 1].userId
        : auth.profile.id;
    }

    const params = {
      processType: processType,
      userId: userId,
      budget: body.value.contractBudgetAmount ?? 0,
      supplyMethodCode: body.value.cm001Info?.supplyMethodCode,
      supplyMethodSpecialTypeCode: body.value.cm001Info?.supplyMethodSpecialTypeCode,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status == HttpStatusCode.Ok) {
      hasJorPorAssign.value = data.some(item => item.organizationLevel == 300 || item.organizationLevel == 200 || item.organizationLevel == 100);
    }

    return { data, status };
  };

  const setDefaultAcceptor = async () => {
    const { data, status } = await getDefaultAcceptor();

    if (status == HttpStatusCode.Ok) {
      body.value.acceptors = [];

      data.forEach(item => body.value.acceptors.push({
        acceptorType: AcceptorType.Approver,
        fullName: item.fullName,
        positionName: item.fullPositionName,
        sequence: body.value.acceptors.length + 1,
        status: AcceptorStatus.Draft,
        userId: item.userId,
        departmentName: item.businessUnitName,
        organizationLevel: item.organizationLevel,
      } as ParticipantsAcceptor))
    }
  };

  const assigneeDefaultAcceptor = async (): Promise<void> => {
    const assignees = body.value.assignees.filter(f => f.assigneeType === AssigneeType.Assignee);
    const lastAssignee = assignees[assignees.length - 1];

    const { data, status } = await getDefaultAcceptor(lastAssignee.userId);

    if (status == HttpStatusCode.Ok) {
      body.value.acceptors = [];

      data.forEach(item => body.value.acceptors.push({
        acceptorType: AcceptorType.Approver,
        fullName: item.fullName,
        positionName: item.fullPositionName,
        sequence: body.value.acceptors.length + 1,
        status: AcceptorStatus.Draft,
        userId: item.userId,
        departmentName: item.businessUnitName,
        organizationLevel: item.organizationLevel,
      } as ParticipantsAcceptor))
    }
  };

  const setCommercialMaterialUnderDirectorDepartment = async (): Promise<void> => {
    const assignees = body.value.assignees.filter(f => f.assigneeType === AssigneeType.Assignee);
    const lastAssignee = assignees[assignees.length - 1];

    if (!lastAssignee) return;

    await getDefaultAcceptor(lastAssignee.userId);

    if (hasJorPorAssign.value) {
      body.value.hasJorPorAssign = true;
    }
  };

  const isCanSetDefaultApprover = computed(() => [CM001PeriodStatus.Draft, CM001PeriodStatus.Edit, CM001PeriodStatus.Rejected, CM001PeriodStatus.RejectToAssignee, CM001PeriodStatus.WaitingAssign, , CM001PeriodStatus.WaitingComment].includes(body.value.status));

  const canRestoreVersion = computed(() => isEdit.value);

  const resetDocumentAsync = async (deliveryAcceptanceId: string): Promise<void> => {
    if (!body.value.id) return;

    const { status } = await CM001PeriodService.resetDocumentAsync(deliveryAcceptanceId, body.value.id);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
      await onGetByIdAsync(deliveryAcceptanceId, body.value.id);
    }
  };

  const isBranchDepartment = computed<boolean>((): boolean =>
    [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)].includes(body.value.departmentOrganizationLevel ?? '')
  );

  const isAccountingCanEdit = computed((): boolean => {
    const isWaitingDisbursement = body.value.status == CM001PeriodStatus.Approved
      && [CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate].includes(body.value.accountStatus);

    if (isBranchDepartment.value) {
      return isWaitingDisbursement && auth.profile.departmentCode === body.value.departmentCode;
    }

    return isWaitingDisbursement && isCurrentUserAccountingSegmentMember.value;
  });

  // ผู้ที่แก้ข้อมูลเบิกจ่าย (จบงาน) ได้:
  // - มี confirmer (จบงาน) → เฉพาะ confirmer
  // - ไม่มี confirmer → AccountingOperator หรือ (กรณีไม่ใช่สาขา) สมาชิกกลุ่มงานบัญชี segment
  // - กรณีสาขา ไม่เช็ค departmentCode
  const isDisbursementManageable = computed((): boolean => {
    const isWaitingDisbursement = body.value.status === CM001PeriodStatus.Approved
      && body.value.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate;
    if (!isWaitingDisbursement) return false;

    const currentUserId = auth.profile.id;

    // สาขา → จัดการได้เฉพาะคณะกรรมการตรวจรับ (AcceptanceCommittee) เท่านั้น (เช็คก่อน confirmer ที่ถูก auto-add)
    if (isBranchDepartment.value) {
      return (body.value.acceptanceCommittees ?? []).some((a): boolean =>
        a.acceptorType === AcceptorType.AcceptanceCommittee && (a.delegateeUserId ?? a.userId) === currentUserId);
    }

    // ไม่ใช่สาขา → มี confirmer → confirmer / ไม่มี → operator หรือ กลุ่มงานบัญชี segment
    const confirmers = body.value.acceptanceConfirmers ?? [];
    if (confirmers.length > 0) {
      return confirmers.some((a): boolean => (a.delegateeUserId ?? a.userId) === currentUserId);
    }

    const isOperator = (body.value.acceptanceOfAccounting ?? [])
      .some((a): boolean => a.acceptorType === AcceptorType.AccountingOperator && (a.delegateeUserId ?? a.userId) === currentUserId);
    if (isOperator) return true;

    return isCurrentUserAccountingSegmentMember.value;
  });

  // ผู้กรอก/บันทึก/ยืนยัน "ข้อมูลเบิกจ่าย" = เฉพาะผู้ที่อยู่ในกล่อง ส่วนบัญชีค่าใช้จ่าย (จบงาน) เท่านั้น
  const isDisbursementInputManageable = computed((): boolean => {
    const isWaitingDisbursement = body.value.status === CM001PeriodStatus.Approved
      && body.value.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate;
    if (!isWaitingDisbursement) return false;

    const currentUserId = auth.profile.id;
    return (body.value.acceptanceConfirmers ?? []).some((a): boolean =>
      (a.delegateeUserId ?? a.userId) === currentUserId);
  });

  const isAccountingCanAssign = computed(() => {
    if (!body.value.acceptanceOfAccounting) return false;

    const status = [CM001PeriodStatus.Approved].includes(body.value.status) && [CmDeliveryAcceptancePeriodAccountStatus.AccountingRejected, CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval].includes(body.value.accountStatus);

    const isBranchWork = [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)].includes(body.value.departmentOrganizationLevel ?? '');
    const isBranchCommittee = (body.value.acceptanceCommittees ?? []).some(a =>
      a.acceptorType === AcceptorType.AcceptanceCommittee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id);

    if (isBranchWork && (auth.profile.departmentCode === body.value.departmentCode || isBranchCommittee)) {
      return status;
    }

    if (body.value.acceptanceOfAccounting.length === 0) return status;

    const currentUser = body.value.acceptanceOfAccounting.find(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id);

    if (!currentUser) return false;

    const firstPending = body.value.acceptanceOfAccounting
      .filter(s => s.status === AcceptorStatus.Pending)
      .sort((a, b) => {
        const typeA = a.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;
        const typeB = b.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;

        if (typeA !== typeB) return typeA - typeB;

        return a.sequence - b.sequence;
      })[0];

    const isCurrentUserFirstPending = firstPending != null
      && (firstPending.delegateeUserId ? firstPending.delegateeUserId : firstPending.userId) === auth.profile.id;
    const allAccountPending = body.value.acceptanceOfAccounting.every(s => s.status === AcceptorStatus.Pending || s.status === AcceptorStatus.Draft);

    return status && (isCurrentUserFirstPending || allAccountPending);
  });

  const isLastAccountingApprover = computed(() => {
    if (isBranchDepartment.value
      && auth.profile.departmentCode === body.value.departmentCode
      && (!body.value.acceptanceOfAccounting || body.value.acceptanceOfAccounting.length === 0)) {
      return true;
    }

    if (!body.value.acceptanceOfAccounting || body.value.acceptanceOfAccounting.length === 0) return false;

    const currentUser = body.value.acceptanceOfAccounting.find(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id);

    if (!currentUser || currentUser.status !== AcceptorStatus.Pending) return false;

    const pendingUsers = body.value.acceptanceOfAccounting.filter(a => a.status === AcceptorStatus.Pending);
    const maxSequence = Math.max(...pendingUsers.map(a => a.sequence));

    return currentUser.sequence === maxSequence;
  });

  const isAccountingApprover = computed(() => {

    if (body.value.status !== CM001PeriodStatus.Approved) return false;

    if (!body.value.acceptanceOfAccounting || body.value.acceptanceOfAccounting.length === 0) return false;

    const firstPending = body.value.acceptanceOfAccounting
      .filter(s => s.status === AcceptorStatus.Pending)
      .sort((a, b) => {
        const typeA = a.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;
        const typeB = b.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;

        if (typeA !== typeB) return typeA - typeB;

        return a.sequence - b.sequence;
      })[0];

    if (!firstPending) return false;

    return (firstPending.delegateeUserId ? firstPending.delegateeUserId : firstPending.userId) === auth.profile.id;
  });

  const isCurrentAccountingOperator = computed(() => {
    if (!body.value.acceptanceOfAccounting || body.value.acceptanceOfAccounting.length === 0) return false;

    const firstPending = body.value.acceptanceOfAccounting
      .filter(s => s.status === AcceptorStatus.Pending)
      .sort((a, b) => {
        const typeA = a.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;
        const typeB = b.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;

        if (typeA !== typeB) return typeA - typeB;

        return a.sequence - b.sequence;
      })[0];

    if (!firstPending) return false;

    return firstPending.acceptorType === AcceptorType.AccountingOperator
      && (firstPending.delegateeUserId ? firstPending.delegateeUserId : firstPending.userId) === auth.profile.id;
  });

  const checkAssignee = computed(() => {
    return hasJorPorAssign.value || body.value.hasDeduction;
  });

  const onUpsertAttachments = async (deliveryAcceptanceId: string) => {
    if (!body.value.id) return;

    const { status } = await CM001PeriodService.attachmentsAsync(
      deliveryAcceptanceId,
      body.value.id,
      body.value.attachments
    );

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
    }
  };

  const getDefaultDisbursementAcceptor = async () => {

    if ([String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)].includes(body.value.departmentOrganizationLevel ?? '')) return;

    const { data: defaultExpense, status: defaultStatus } =
      await operationService.getDefaultExpenseDisbursementAsync();

    const paymentTermAmount = body.value.paymentTerms.reduce((sum, item) => sum + (item.amount || 0), 0);

    if (defaultStatus === HttpStatusCode.Ok) {
      const params = {
        processType: SectionProcessType.ExpenseDisbursement,
        budget: paymentTermAmount,
        userId: defaultExpense.userId,
        supplyMethodCode: "SectionApprover001",
        skipCurrentEmployee: false,
      } as defaultAcceptorCriteria;

      const { data: acceptorList, status: acceptorStatus } =
        await operationService.getOperationsDefaultAcceptorAsync(params);

      if (acceptorStatus === HttpStatusCode.Ok) {
        body.value.acceptanceOfAccounting = body.value.acceptanceOfAccounting?.filter(
          a => a.acceptorType !== AcceptorType.Accounting
        ) || [];

        acceptorList.forEach(item =>
          body.value.acceptanceOfAccounting?.push({
            acceptorType: AcceptorType.Accounting,
            fullName: item.fullName,
            positionName: item.fullPositionName,
            sequence: body.value.acceptanceOfAccounting?.length + 1,
            status: AcceptorStatus.Pending,
            userId: item.userId,
            departmentName: item.businessUnitName,
          })
        );
      }
    }
  };

  return {
    body,
    isLoading,
    cloneAccountingAcceptors,
    positionInspOptions,
    checkAssignee,
    fn: {
      getReviewDocumentAsync,
      onGetByIdAsync,
      onSubmitAsync,
      onUpdateDutiesStatusAsync,
      onSendCommitteeApprovalAsync,
      onRecallAsync,
      onSendApproveOrRejectAsync,
      onSendApproveAsync,
      onAssigneeCommentAsync,
      onConfirmAssignedAsync,
      onRejectAssigneeAsync,
      getDefaultAcceptor,
      setDefaultAcceptor,
      assigneeDefaultAcceptor,
      setCommercialMaterialUnderDirectorDepartment,
      onCreateAsync,
      resetBody,
      resetDocumentAsync,
      onAccountingApproveOrRejectAsync,
      onSetDisburmentDateAsync,
      getDefaultDisbursementAcceptor,
      onUpsertAttachments
    },
    states: {
      isEdit,
      isRecall,
      isCommitteeApproval,
      isCommitteeCurrentApproval,
      isAssigned,
      isAssignee,
      isComment,
      isCurrentApprover,
      isLastApprover,
      isCanSetDefaultApprover,
      canAssignee,
      jorporCanAssignByAssignee,
      canCommitteeRecall,
      canRestoreVersion,
      isAccountingCanAssign,
      isAccountingApprover,
      isAccountingCanEdit,
      isDisbursementManageable,
      isDisbursementInputManageable,
      isLastAccountingApprover,
      isCurrentAccountingOperator,
      canEditDocument,
      isBranchDepartment,
      isCurrentUserAccountingSegmentMember
    },
  };
});
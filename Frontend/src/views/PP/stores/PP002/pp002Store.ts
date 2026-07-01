import type {
  AcceptorTorDraft,
  ActionTorDraft,
  ApproveTor,
  Budgets,
  FineRates,
  PaymentTerms,
  PP002Detail,
  TechnicalPeriods,
  TorTrainingModel,
  Warranties,
} from '../../models/PP002/pp002Model';
import type { Option } from '@/models/shared/option';
import type { defaultAcceptorCriteria, OperationBody } from '@/models/shared/operations';
import type { ParticipantsAssignee } from '@/models/shared/participants';
import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { HttpStatusCode } from 'axios';
import { PP002Status } from '../../enums/pp002';
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from '@/enums/participants';
import { useAuthenticationStore } from '@/stores/authentication';
import { showReasonDialogAsync } from '@/helpers/dialog';
import { ReasonDialogType } from '@/enums/dialog';
import { CommitteeType, ProRateTypeCodeEnum, TemplateGroup } from '@/enums/shared';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import { OrganizationLevel, SectionProcessType } from '@/enums/operations';
import { SupplyMethodCode } from '@/enums/supplyMethod';
import PP002Service from '../../services/PP002/PP002Service';
import ToastHelper from '@/helpers/toast';
import operationService from '@/services/Shared/operations';
import SharedService from '@/services/Shared/dropdown';
import { errorMessageHandler } from '@/helpers/error';
import { checkIsEighty } from '@/helpers/supplyMethod';
import { CommitteePositions } from '@/enums/PCM005/principle';
import { pp004status } from '../../enums/pp004';
import { isCurrentPendingAcceptor } from '@/helpers/participants';

export const usePP002DetailStore = defineStore('PP-002-detail-store', () => {
  const authStore = useAuthenticationStore();
  const procurementStore = usePPDetailStore();

  const showDocument = ref(false);
  const templateDDL = ref<Option[]>([]);
  const PP002Detail = ref<PP002Detail>({
    technicalPeriods: [] as TechnicalPeriods[],
    budgets: [] as Budgets[],
    warranties: [] as Warranties[],
    paymentTerms: [] as PaymentTerms[],
    fineRates: [] as FineRates[],
    trainingItems: [] as TorTrainingModel[],
    status: PP002Status.Draft,
  } as PP002Detail);

  const canEditTor = computed(() => {
    if (!PP002Detail.value.acceptors) return false;

    const statusList = [PP002Status.Draft, PP002Status.Rejected, PP002Status.Edit];
    const status = statusList.includes(PP002Detail.value.status) || !PP002Detail.value.status;
    const checkUser = PP002Detail.value.acceptors.some(a => a.acceptorType === AcceptorType.TorDraftCommittee &&
      a.userId === authStore.profile.id);

    return status && checkUser;
  });

  const canEditTorApproval = computed(() => {
    if (!PP002Detail.value.acceptors) return false;

    const statusList = [PP002Status.Draft, PP002Status.Rejected, PP002Status.Edit, PP002Status.WaitingComment];
    const status = statusList.includes(PP002Detail.value.status) || !PP002Detail.value.status;
    const checkUser = PP002Detail.value.acceptors.some(a => a.acceptorType === AcceptorType.TorDraftCommittee &&
      a.userId === authStore.profile.id);

    const checkUserAssignees = PP002Detail.value?.assignees?.some(a => a.assigneeType === AssigneeType.Assignee &&
      (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && (checkUser || checkUserAssignees);
  });

  const conRestoreState = computed(() => {
    return canEditTor.value && (PP002Detail.value.isChange || PP002Detail.value.isCancel);
  });

  const isCanSetDefaultUnitnApprover = computed(() => [PP002Status.Draft, PP002Status.Edit].includes(PP002Detail.value.status));

  const canCommitteeRecall = computed(() => {
    if (!PP002Detail.value.acceptors) return false;

    const status = PP002Detail.value.status === PP002Status.WaitingCommitteeApproval;

    const isAnyApprovalAndRejected = PP002Detail.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.TorDraftCommittee && a.committeePositionsCode == CommitteePositions.PosBoard001);

    const isCommittee = PP002Detail.value.acceptors.some(s => s.acceptorType === AcceptorType.TorDraftCommittee && s.userId === authStore.profile.id);

    return status && !isAnyApprovalAndRejected && isCommittee;
  });

  const canRecall = computed(() => {
    if (!PP002Detail.value.assignees) return false;

    const status = PP002Detail.value.status === PP002Status.WaitingApproval;
    const checkUser = PP002Detail.value.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    if (!PP002Detail.value.acceptors) return false;

    const isAnyApprovalAndRejected = PP002Detail.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.Approver);

    return status && checkUser && !isAnyApprovalAndRejected;
  });

  const canAcceptAndReject = computed(() => {
    if (!PP002Detail.value.acceptors) return false;

    const status = PP002Detail.value.status === PP002Status.WaitingCommitteeApproval;
    const checkQue = PP002Detail.value.acceptors.find(a => a.userId === authStore.profile.id && a.acceptorType === AcceptorType.TorDraftCommittee)?.isCurrent;

    return status && checkQue;
  });

  const isDirectorAcceptor = computed(() => {
    if (!canAcceptAndReject.value && !PP002Detail.value.acceptors) {
      return;
    }

    const pendingUser = PP002Detail.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending && f.acceptorType === AcceptorType.TorDraftCommittee);

    if (!pendingUser) {
      return false;
    }

    const directorUser = pendingUser
      .reduce((prev, curr) => curr.sequence < prev.sequence ? curr : prev,
        pendingUser[0]);

    if (!directorUser) {
      return false;
    }

    return directorUser.userId === authStore.profile.id;
  });

  const canApproveAndReject = computed(() => {
    if (!PP002Detail.value.acceptors) return false;

    const status = PP002Detail.value.status === PP002Status.WaitingApproval;
    const checkQue = isCurrentPendingAcceptor(PP002Detail.value.acceptors, authStore.profile.id, AcceptorType.Approver);

    return status && checkQue;
  });

  const isLastApproval = computed(() => {
    if (!canApproveAndReject.value && !PP002Detail.value.acceptors) {
      return;
    }

    const approvalUser = PP002Detail.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending && f.acceptorType === AcceptorType.Approver);


    if (!approvalUser) {
      return false;
    }

    const current = approvalUser.find(f => f.isCurrent);

    if (!current) {
      return false;
    }

    return current.sequence === approvalUser[approvalUser.length - 1].sequence;
  });

  const canCancelOrChange = computed(() => {
    if (!PP002Detail.value.acceptors) return false;

    const status = PP002Detail.value.status === PP002Status.Approved && !PP002Detail.value.isCancel;
    const checkUser = PP002Detail.value.acceptors.some(a => a.acceptorType === AcceptorType.TorDraftCommittee &&
      a.userId === authStore.profile.id);

    return status && checkUser;
  });

  const canApproveAndRejectUnit = computed(() => {
    if (!PP002Detail.value.acceptors) return false;

    const status = PP002Detail.value.status === PP002Status.WaitingUnitApproval;
    const checkQue = isCurrentPendingAcceptor(PP002Detail.value.acceptors, authStore.profile.id, AcceptorType.DepartmentDirectorAgree);

    return status && checkQue;
  });

  const isLastUnit = computed(() => {
    if (!canApproveAndRejectUnit.value && !PP002Detail.value.acceptors) {
      return;
    }

    const approvalUser = PP002Detail.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending && f.acceptorType === AcceptorType.DepartmentDirectorAgree);


    if (!approvalUser) {
      return false;
    }

    const current = approvalUser.find(f => f.isCurrent);

    if (!current) {
      return false;
    }

    return current.sequence === approvalUser[approvalUser.length - 1].sequence;
  });

  const jorporCanAssign = computed(() => {
    if (!PP002Detail.value.assignees) return false;

    const status = [PP002Status.WaitingAssign, PP002Status.RejectToAssignee].includes(PP002Detail.value.status);
    const checkUser = PP002Detail.value.assignees.some(a => a.assigneeType === AssigneeType.Director && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && checkUser;
  });

  const jorporCanAssignByAssignee = computed(() => {
    if (!PP002Detail.value.assignees) return false;

    const status = [PP002Status.WaitingAssign, PP002Status.RejectToAssignee].includes(PP002Detail.value.status);
    const checkUser = PP002Detail.value.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && checkUser;
  });

  const assignCanAssign = computed(() => {
    if (!PP002Detail.value.assignees) return false;

    const status = PP002Detail.value.status === PP002Status.WaitingComment;
    const checkUser = PP002Detail.value.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && checkUser;
  });

  const isJorPorComment = computed(() => {
    if (!PP002Detail.value.assignees) return false;

    const status = PP002Detail.value.status === PP002Status.WaitingComment;
    const checkUser = PP002Detail.value.assignees.some(a => [AssigneeType.Assignee, AssigneeType.Director].includes(a.assigneeType) && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    const lastAssignee = PP002Detail.value.assignees.reduce((prev, curr) => curr.sequence > prev.sequence ? curr : prev, PP002Detail.value.assignees[0]);

    const hasPermissionUser = (lastAssignee?.delegateeUserId ? lastAssignee?.delegateeUserId : lastAssignee?.userId) === authStore.profile.id;

    return status && checkUser && hasPermissionUser;
  });

  const onGetTemplateDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetTemplateDropdownByGroupCodeAsync([TemplateGroup.Tor], { supplyMethodCode: procurementStore.procurementDetail.supplyMethodCode as SupplyMethodCode, budget: procurementStore.procurementDetail.budget });

    if (status === HttpStatusCode.Ok) {
      templateDDL.value = data;
    }
  };

  const onGetCommitteeAcceptorAsync = async (id: string): Promise<void> => {
    const { data, status } = await operationService.getCommitteAcceptorsAsync(id, CommitteeType.TorDraft);
    const value = data.committees as AcceptorTorDraft[];

    if (status === HttpStatusCode.Ok) {
      const acceptor: AcceptorTorDraft[] = value.map((d, i) => ({
        ...d,
        sequence: i + 1,
        acceptorType: AcceptorType.TorDraftCommittee,
        status: AcceptorStatus.Draft,
        isUnableToPerformDuties: false,
      }));

      if (PP002Detail.value.acceptors) {
        PP002Detail.value.acceptors = [...acceptor, ...PP002Detail.value.acceptors];
      } else {
        PP002Detail.value.acceptors = [...acceptor];
      }
    }
  };

  const onGetDefaultAcceptorAsync = async (): Promise<void> => {
    if (!PP002Detail.value.acceptors) return;

    const torCommitee = PP002Detail.value.acceptors.filter(f => f.acceptorType === AcceptorType.TorDraftCommittee && f.departmentCode === procurementStore.procurementDetail.departmentCode);

    if (!torCommitee) return;

    const lastAcceptorCommittee = torCommitee.reduce((prev, current) =>
      prev.sequence > current.sequence ? prev : current, torCommitee[0]);

    const lastAssignee = PP002Detail.value.assignees?.filter(x => x.assigneeType == AssigneeType.Assignee)[PP002Detail.value.assignees.filter(x => x.assigneeType == AssigneeType.Assignee).length - 1];

    let processType: SectionProcessType = procurementStore.procurementDetail.hasMd ? SectionProcessType.TORHasMD : SectionProcessType.TOR;

    const detail = procurementStore.procurementDetail;
    const is80 = checkIsEighty(detail.supplyMethodCode);
    if (is80) {
      if (detail.isStock) {
        processType = SectionProcessType.TORStock;
      } else if (detail.isCommercialMaterial) {
        processType = procurementStore.procurementDetail.hasMd ? SectionProcessType.TORCommercialParcelHasMD : SectionProcessType.TORCommercialParcel;
      }
    }

    const dataSearch: defaultAcceptorCriteria = {
      processType: processType,
      budget: procurementStore.procurementDetail.budget,
      supplyMethodCode: procurementStore.procurementDetail.supplyMethodCode,
      supplyMethodSpecialTypeCode: procurementStore.procurementDetail.supplyMethodSpecialTypeCode,
      userId: procurementStore.procurementDetail.hasMd ? lastAssignee?.userId : lastAcceptorCommittee?.userId,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(dataSearch);

    if (status === HttpStatusCode.Ok) {
      const uniqueData = Array.from(
        new Map(data.map((item) => [item.userId, item])).values()
      );

      setUserApprover(uniqueData, AcceptorType.Approver);
    }
  };

  const getDefaultDepartmentAcceptorAsync = async (): Promise<void> => {
    const userId = findLastDepartmentUserId();

    const { data, status } = await operationService.getDefaultDepartmentApproverByUserIdAsync(
      userId,
      OrganizationLevel.Group,
    );

    if (status === HttpStatusCode.Ok) {
      const uniqueData = Array.from(
        new Map(data.map((item) => [item.userId, item])).values()
      );

      setUserApprover(uniqueData, AcceptorType.DepartmentDirectorAgree);
    }
  };

  const getDefaultJorporAsync = async (): Promise<void> => {
    const { data, status } = await operationService.getJorPorDirectorAsync();

    if (status === HttpStatusCode.Ok) {
      const jorPor = {
        assigneeGroup: AssigneeGroup.JorPor,
        assigneeType: AssigneeType.Director,
        departmentName: data.businessUnitName,
        fullName: data.fullName,
        positionName: data.fullPositionName,
        sequence: 1,
        status: AssigneeStatus.Draft,
        userId: data.userId,
      } as ParticipantsAssignee;

      PP002Detail.value.assignees = [jorPor];
    }
  };

  const findLastDepartmentUserId = (): string => {
    const departmentCode = procurementStore.procurementDetail.departmentCode;
    const lastUserDepartment = PP002Detail.value.acceptors
      ?.filter(a =>
        a.acceptorType === AcceptorType.TorDraftCommittee &&
        a.departmentCode === departmentCode
      )
      .sort((a, b) => b.sequence - a.sequence)[0];

    if (!lastUserDepartment) return '';

    return lastUserDepartment.userId;
  };

  const setUserApprover = (data: OperationBody[], type: AcceptorType): void => {
    const acceptor: AcceptorTorDraft[] = data.map((d, i) => ({
      departmentName: d.businessUnitName,
      employeeCode: d.employeeCode,
      fullName: d.fullName,
      positionName: d.fullPositionName,
      userId: d.userId,
      sequence: i + 1,
      acceptorType: type,
      status: AcceptorStatus.Draft,
      isUnableToPerformDuties: false,
    }));

    if (type === AcceptorType.DepartmentDirectorAgree) {
      if (PP002Detail.value.acceptors) {
        PP002Detail.value.acceptors = [...acceptor, ...PP002Detail.value.acceptors.filter(a => a.acceptorType !== AcceptorType.DepartmentDirectorAgree)];
      } else {
        PP002Detail.value.acceptors = [...acceptor];
      }

      return;
    }

    if (type === AcceptorType.Approver) {
      if (PP002Detail.value.acceptors) {
        PP002Detail.value.acceptors = [...acceptor, ...PP002Detail.value.acceptors.filter(a => a.acceptorType !== AcceptorType.Approver)];
      } else {
        PP002Detail.value.acceptors = [...acceptor];
      }
    }
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    if (!procurementStore.procurementDetail.id) return;

    const { data, status } = await PP002Service.getByIdAsync(id, procurementStore.procurementDetail.id);
    showDocument.value = data.torDraftDocumentId || data.torDraftApprovalDocumentId ? true : false;

    if (status === HttpStatusCode.Ok) {
      PP002Detail.value = data;
    }
  };

  const validatePaymentTerms = (): boolean => {
    const details = PP002Detail.value.paymentTerms?.[0]?.details ?? [];

    if (PP002Detail.value.paymentTerms?.[0]?.proRateTypeCode != ProRateTypeCodeEnum.SplitPayment002) {
      return true;
    }

    if (details != null && details.length > 0) {
      const totalPercent = details.reduce((sum, d) => {
        return sum + (Number(d.percent) || 0);
      }, 0);

      if (totalPercent > 0 && Math.abs(totalPercent - 100) > 0.01) {
        ToastHelper.errorDescription(
          `จำนวนเงิน (%) ต้องรวมกันเท่ากับ 100% (ปัจจุบัน ${totalPercent.toFixed(2)}%)`
        );

        return false;
      }
    }

    return true;
  };

  const onCreateAsync = async (isSaveDraft?: boolean): Promise<void> => {

    if (!procurementStore.procurementDetail.id) return;

    if (!isSaveDraft && !validatePaymentTerms()) return;

    const { data, status } = await PP002Service.createAsync(procurementStore.procurementDetail.id, { ...PP002Detail.value, isSaveDraft } as PP002Detail);

    if (status === HttpStatusCode.Created) {
      PP002Detail.value.id = data;
      await procurementStore.onGetProcurementById(PP002Detail.value.procurementId);
      await onGetByIdAsync(data);

      ToastHelper.createdMessageToast();
    }

    if (status === HttpStatusCode.Conflict) {
      ToastHelper.errorDescription(errorMessageHandler(data));
    }
  };

  const onUpdateAsync = async (id: string, torStatus?: PP002Status, isSaveDraft?: boolean): Promise<void> => {
    if (!procurementStore.procurementDetail.id) return;

    const mapStatusToast: Record<PP002Status, () => void> = {
      [PP002Status.Draft]: () => ToastHelper.updatedMessageToast(),
      [PP002Status.Rejected]: () => ToastHelper.updatedMessageToast(),
      [PP002Status.Edit]: () => ToastHelper.recallEditMessageToast(),
      [PP002Status.WaitingCommitteeApproval]: () => ToastHelper.sendApproveConfirmMessageToast(),
      [PP002Status.WaitingUnitApproval]: () => ToastHelper.sendApproveConfirmMessageToast(),
      [PP002Status.WaitingAssign]: () => ToastHelper.updatedMessageToast(),
      [PP002Status.WaitingComment]: () => ToastHelper.assignedMessageToast(),
      [PP002Status.RejectToAssignee]: function (): void {
        throw new Error('Function not implemented.');
      },
      [PP002Status.WaitingApproval]: () => ToastHelper.sendApproveConfirmMessageToast(),
      [PP002Status.Approved]: function (): void {
        throw new Error('Function not implemented.');
      }
    };

    if (!isSaveDraft && !validatePaymentTerms()) return;

    const payload = {
      ...PP002Detail.value,
      status: torStatus ?? PP002Detail.value.status,
      isSaveDraft: isSaveDraft,
    };

    const { status, data } = await PP002Service.updateAsync(id, procurementStore.procurementDetail.id, payload);

    if (status === HttpStatusCode.Ok) {
      // If API returned new document fileIds, update them immediately
      // This happens when user saves and a new version is created
      if (data?.newApprovalDocumentFileId) {
        PP002Detail.value.torDraftApprovalDocumentId = data.newApprovalDocumentFileId;
      }
      if (data?.newTorDocumentFileId) {
        PP002Detail.value.torDraftDocumentId = data.newTorDocumentFileId;
      }

      await onGetByIdAsync(id);

      return torStatus ? mapStatusToast[torStatus]() : ToastHelper.updatedMessageToast();
    }
  };

  const onActionAsync = async (body: ActionTorDraft): Promise<void> => {
    if (!PP002Detail.value.id || !procurementStore.procurementDetail.id) return;

    const { data, status } = await PP002Service.requestActionAsync(PP002Detail.value.id, procurementStore.procurementDetail.id, body);

    if (status === HttpStatusCode.Created) {
      await procurementStore.onGetProcurementById(PP002Detail.value.procurementId);
      await onGetByIdAsync(data);

      return body.isChange ? ToastHelper.changedMessageToast() : ToastHelper.canceledMessageToast();
    }
  };

  const onApproveAsync = async (group: AcceptorType): Promise<void> => {
    if (!PP002Detail.value.id || !procurementStore.procurementDetail.id) return;

    const mapTypeReasonDialog: Record<AcceptorType, ReasonDialogType> = {
      [AcceptorType.DepartmentDirectorAgree]: ReasonDialogType.Accepted,
      [AcceptorType.Approver]: isLastApproval.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted,
      [AcceptorType.TorDraftCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.MedianPriceCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.ProcurementCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.RentCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.Jp005Committee]: ReasonDialogType.Accepted,
      [AcceptorType.AcceptanceCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.InspectionCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.AcceptorSign]: ReasonDialogType.Accepted,
      [AcceptorType.Accounting]: ReasonDialogType.Accepted,
      [AcceptorType.AccountingApprover]: ReasonDialogType.Accepted,
      [AcceptorType.AccountingConfirmer]: ReasonDialogType.Accepted,
      [AcceptorType.Reviewer]: ReasonDialogType.Accepted,
      [AcceptorType.AccountingOperator]: ReasonDialogType.Accepted,
    }

    const res = await showReasonDialogAsync(mapTypeReasonDialog[group]);

    if (res.isConfirm) {
      const body = {
        group: group,
        remark: res.reason,
        torDraftId: PP002Detail.value.id,
      } as ApproveTor;

      const { status } = await PP002Service.approveAsync(procurementStore.procurementDetail.id, body);

      if (status === HttpStatusCode.Ok) {
        await onGetByIdAsync(PP002Detail.value.id);
        await procurementStore.onGetProcurementById(PP002Detail.value.procurementId);

        return ToastHelper.approvedMessageToast();
      }
    }
  };

  const onRejectAsync = async (group: AcceptorType): Promise<void> => {
    if (!PP002Detail.value.id || !procurementStore.procurementDetail.id) return;

    const res = await showReasonDialogAsync(group === AcceptorType.TorDraftCommittee ? ReasonDialogType.NotAgree : ReasonDialogType.Reject);

    if (res.isConfirm) {
      const body = {
        group: group,
        remark: res.reason,
        torDraftId: PP002Detail.value.id,
      } as ApproveTor;

      const { status } = await PP002Service.rejectAsync(procurementStore.procurementDetail.id, body);

      if (status === HttpStatusCode.Ok) {
        await onGetByIdAsync(PP002Detail.value.id);
        return group === AcceptorType.TorDraftCommittee ? ToastHelper.notAgreeMessageToast() : ToastHelper.sendEditMessageToast();
      }
    }
  };

  const onRejectAssigneeAsync = async () => {
    if (!PP002Detail.value.id || !procurementStore.procurementDetail.id) return;

    const res = await showReasonDialogAsync(ReasonDialogType.Reject, true)

    if (res.isConfirm) {
      const body = {
        group: AcceptorType.DepartmentDirectorAgree,
        remark: res.reason,
        torDraftId: PP002Detail.value.id,
      } as ApproveTor;

      const { status } = await PP002Service.rejectAsync(procurementStore.procurementDetail.id, body);

      if (status === HttpStatusCode.Ok) {
        await onGetByIdAsync(PP002Detail.value.id);
        return ToastHelper.sendEditMessageToast();
      }
    }
  };

  const onUpdateDutieStatusAsync = async (acceptorId: string, dutieStatus: boolean, remark?: string): Promise<void> => {
    if (!PP002Detail.value.id || !PP002Detail.value.acceptors || !procurementStore.procurementDetail.id) return;

    if ([PP002Status.Draft, PP002Status.Rejected, PP002Status.Edit].includes(PP002Detail.value.status) && PP002Detail.value.id) {

      const payload = {
        ...PP002Detail.value,
        acceptors: [
          ...PP002Detail.value.acceptors.filter(f => f.acceptorType != AcceptorType.TorDraftCommittee),
          ...PP002Detail.value.acceptors.filter(f => f.acceptorType == AcceptorType.TorDraftCommittee)
            .map(m => m.id === acceptorId ? { ...m, isUnableToPerformDuties: dutieStatus, remark: remark } : m)],
      } as PP002Detail;

      const { status } = await PP002Service.updateAsync(PP002Detail.value.id, procurementStore.procurementDetail.id, payload);

      if (status === HttpStatusCode.Ok) {
        await onGetByIdAsync(PP002Detail.value.id);

        ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
      }

      return;
    };

    const { status } = await PP002Service.updateDutieStatusAsync(PP002Detail.value.id, procurementStore.procurementDetail.id, acceptorId, dutieStatus, remark);

    if (status === HttpStatusCode.Ok) {
      await onGetByIdAsync(PP002Detail.value.id);

      ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
    }
  };

  const onAssigneeCommentAsync = async (remark: string): Promise<void> => {
    if (!PP002Detail.value.id || PP002Detail.value.status !== PP002Status.WaitingComment) return;

    const { status } = await PP002Service.assigneeCommentAsync(PP002Detail.value.id, PP002Detail.value.procurementId, remark);

    if (status === HttpStatusCode.Ok) {
      await onGetByIdAsync(PP002Detail.value.id);

      return ToastHelper.success('แสดงความคิดเห็น', 'แสดงความคิดเห็นสำเร็จ');
    }
  };

  const onClearForm = async (): Promise<void> => {
    PP002Detail.value = {
      ...PP002Detail.value,
      technicalPeriods: [] as TechnicalPeriods[],
      budgets: [] as Budgets[],
      warranties: [] as Warranties[],
      paymentTerms: [] as PaymentTerms[],
      fineRates: [] as FineRates[],
      evaluationCriteria: '',
      technicalSpecifications: undefined
    } as PP002Detail;
  };

  const onClearDefaultAcceptorAsync = async (): Promise<void> => {
    if (PP002Detail.value.acceptors) {
      PP002Detail.value.acceptors = PP002Detail.value.acceptors.filter(a => a.acceptorType != AcceptorType.Approver);

      procurementStore.procurementDetail.hasMd ?
        await getDefaultDepartmentAcceptorAsync() :
        await onGetDefaultAcceptorAsync();
    }
  };

  const onResetStore = () => {
    PP002Detail.value = {} as PP002Detail;
  };

  const getReviewDocumentAsync = async (id: string, procurementId: string, documentType: string): Promise<string> => {
    const { data, status } = await PP002Service.getReviewDocumentAsync(id, procurementId, documentType);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };

  const isAuthDepartment = computed(() => {
    const isMedianPriceNotSubmitted = !procurementStore.procurementDetail.purchaseRequisition?.status || [pp004status.Draft, pp004status.Rejected, pp004status.Edit].includes(procurementStore.procurementDetail.purchaseRequisition?.status as pp004status);

    return procurementStore.procurementDetail.departmentCode === authStore.profile.departmentCode && isMedianPriceNotSubmitted;
  });

  const onRestoreStateAsync = async () => {
    if (!PP002Detail.value.id) return;

    const res = await showReasonDialogAsync(ReasonDialogType.Confirm, true, PP002Detail.value.isChange ? 'ยืนยันการคืนสถานะคำขอเปลี่ยนแปลง' : 'ยืนยันการคืนสถานะคำขอยกเลิก');

    if (!res.isConfirm) return;

    if (!res.reason) return;

    const { data, status } = await PP002Service.restoreStateAsync(PP002Detail.value.id, res.reason);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await onGetByIdAsync(data);

      await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id!);
    }
  }

  return {
    templateDDL,
    PP002Detail,
    showDocument,
    status: {
      canEditTor,
      canEditTorApproval,
      canRecall,
      canAcceptAndReject,
      isDirectorAcceptor,
      canApproveAndReject,
      isLastApproval,
      canCancelOrChange,
      canApproveAndRejectUnit,
      isLastUnit,
      jorporCanAssign,
      jorporCanAssignByAssignee,
      assignCanAssign,
      isCanSetDefaultUnitnApprover,
      isJorPorComment,
      canCommitteeRecall,
      isAuthDepartment,
      conRestoreState
    },
    onGetTemplateDDLAsync,
    onGetCommitteeAcceptorAsync,
    onGetDefaultAcceptorAsync,
    getDefaultDepartmentAcceptorAsync,
    getDefaultJorporAsync,
    onCreateAsync,
    onUpdateAsync,
    onGetByIdAsync,
    onClearForm,
    onActionAsync,
    onApproveAsync,
    onRejectAsync,
    onUpdateDutieStatusAsync,
    onAssigneeCommentAsync,
    onClearDefaultAcceptorAsync,
    onResetStore,
    onRejectAssigneeAsync,
    getReviewDocumentAsync,
    onRestoreStateAsync
  }
});

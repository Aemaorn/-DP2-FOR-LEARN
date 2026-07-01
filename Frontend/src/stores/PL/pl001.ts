import {
  ProcurementProcess,
  ProcurementPlanType,
} from '@/enums/procurement';
import type { PlanActionReq, PlanBody, TPL001Criteria, TPL001ListResponse } from '@/models/PL/pl001';
import type { Option, OptionBadge } from '@/models/shared/option';
import type { Attachments } from '@/models/shared/uploadFile';
import type { StatusCount } from '@/models/shared/status';
import type { ParticipantsAcceptor, ParticipantsAssignee } from '@/models/shared/participants';
import type { defaultAcceptorCriteria, OperationBody } from '@/models/shared/operations';
import { defineStore } from 'pinia';
import { computed, ref, type Ref } from 'vue';
import { HttpStatusCode } from 'axios';
import { EGroupCode, EWorkProcess, OrganizationLevelEnum } from '@/enums/shared';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { PlanAction, PlanStatus } from '@/enums/plan';
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from '@/enums/participants';
import { OrganizationLevel, SectionProcessType } from '@/enums/operations';
import { useAuthenticationStore } from '../authentication';
import SharedService from '@/services/Shared/dropdown';
import planService from '@/services/PL/pl001';
import ToastHelper from '@/helpers/toast';
import router from '@/router';
import { isCurrentPendingAcceptor } from '@/helpers/participants';
import PlanConstant from '@/constants/plan';
import operationService from '@/services/Shared/operations';
import ProcurementConstants from '@/constants/procurement';

const { PlanStatusName, PlanStatusColor } = PlanConstant;

const getDepartmentAsync = async (target: Ref<Option[]>): Promise<void> => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getSupplyMethodAsync = async (target: Ref<Option[]>, parentCode?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getSupplyMethodTypeAsync = async (target: Ref<Option[]>, parentCode?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethodType, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

export const usePL001ListStore = defineStore('pl001-list-store', () => {
  const initCriteria = {
    pageNumber: 1,
    pageSize: 10,
    sort: [],
    type: ProcurementPlanType.All,
    workProcess: EWorkProcess.InProcess,
    process: ProcurementProcess.All,
    status: PlanStatus.All,
    budgetYear: new Date().getFullYear() + 543,
  } as TPL001Criteria;
  const searchCriteria = ref(structuredClone(initCriteria));
  const statusOptionBadge = ref([] as OptionBadge[]);
  const departmentDDL = ref<Option[]>([]);
  const supplyMethodCodeDDL = ref<Option[]>([]);
  const supplyMethodTypeCodeDDL = ref<Option[]>([]);
  const supplyMethodSpecialTypeCodeDDL = ref<Option[]>([]);
  const planResponse = ref<TPL001ListResponse>({} as TPL001ListResponse);

  const getDepartmentDDLAsync = async (): Promise<void> => {
    await getDepartmentAsync(departmentDDL);
  };

  const getSupplyMethodDDLAsync = async (): Promise<void> => {
    await getSupplyMethodAsync(supplyMethodCodeDDL);
  };

  const getSupplyMethodTypeDDLAsync = async (): Promise<void> => {
    await getSupplyMethodTypeAsync(supplyMethodTypeCodeDDL);
  };

  const getSupplyMethodSpecialTypeDDlAsync = async (parentCode: string): Promise<void> => {
    await getSupplyMethodAsync(supplyMethodSpecialTypeCodeDDL, parentCode);
  }

  const filterIsChangeOrIsCancel = (): void => {
    const process = searchCriteria.value.process;

    searchCriteria.value.isChange = process === ProcurementProcess.DuringChange;
    searchCriteria.value.isCancel = process === ProcurementProcess.DuringCancel;
  };

  const getListAsync = async (): Promise<void> => {
    filterIsChangeOrIsCancel();

    const { data, status } = await planService.getListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      planResponse.value = data;

      getStatusCount(data.statusCount);
    }
  };

  const getStatusCount = (count: StatusCount): void => {
    const planStatusOptions = Object.entries(PlanStatus).map(([, value]) => ({
      label: PlanStatusName(value),
      value: value,
      bgColorClass: PlanStatusColor(value).bgColorClass,
      textColorClass: PlanStatusColor(value).textColorClass,
      count: getCount(count, value),
    } as OptionBadge));

    statusOptionBadge.value = planStatusOptions;
  };

  const getCount = (countAll: StatusCount, status: PlanStatus): number => {
    let count = 0;
    const convertStatus = (status[0]?.toLowerCase() ?? '') + status.slice(1);
    count = countAll[convertStatus as keyof StatusCount];

    return count;
  };

  const deleteAsync = async (id: string): Promise<void> => {
    if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;

    const { status } = await planService.deleteAsync(id);

    if (status === HttpStatusCode.NoContent) {
      await getListAsync();

      return ToastHelper.deletedMessageToast();
    }
  };

  const { ProcurementTypeName } = ProcurementConstants;

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  const onResetCriteria = () => {
    searchCriteria.value = structuredClone(initCriteria);
  };

  const exportExcelPlanAsync = async (): Promise<void> => {
    const { data, status } = await planService.exportExcelPlanAsync(
      searchCriteria.value
    );

    if (status !== HttpStatusCode.Ok) {
      ToastHelper.error('ดึงข้อมูลเอกสาร', 'ไม่พบเอกสารที่ต้องการ');
      return;
    }

    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const dateStr = `${year}${month}${day}`;

    const planType = !searchCriteria.value.type || searchCriteria.value.type === ProcurementPlanType.All ? 'ทั้งหมด' : ProcurementTypeName(searchCriteria.value.type);

    const fileName = `รายงานรายการจัดซื้อจัดจ้าง_${dateStr} (${planType}).xlsx`;
    const url = window.URL.createObjectURL(data);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    a.remove();
  };

  const exportExcelEGPAsync = async (): Promise<void> => {
    const { data, status } = await planService.exportExcelEGPAsync(
      searchCriteria.value
    );

    if (status !== HttpStatusCode.Ok) {
      ToastHelper.error('ดึงข้อมูลเอกสาร', 'ไม่พบเอกสารที่ต้องการ');
      return;
    }

    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const dateStr = `${year}${month}${day}`;

    const planType = !searchCriteria.value.type || searchCriteria.value.type === ProcurementPlanType.All ? 'ทั้งหมด' : ProcurementTypeName(searchCriteria.value.type);

    const fileName = `รายงาน e-GP_${dateStr} (${planType}).xlsx`;
    const url = window.URL.createObjectURL(data);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    a.remove();
  };

  return {
    searchCriteria,
    departmentDDL,
    planResponse,
    statusOptionBadge,
    supplyMethodCodeDDL,
    supplyMethodTypeCodeDDL,
    supplyMethodSpecialTypeCodeDDL,
    getSupplyMethodDDLAsync,
    getSupplyMethodTypeAsync,
    getSupplyMethodTypeDDLAsync,
    getSupplyMethodSpecialTypeDDlAsync,
    getDepartmentDDLAsync,
    onChangePageSize,
    onResetCriteria,
    getListAsync,
    deleteAsync,
    exportExcelEGPAsync,
    exportExcelPlanAsync,
  };
});

export const usePL001DetailStore = defineStore('pl001-detail-store', () => {
  const authenStore = useAuthenticationStore();
  const departmentDDL = ref<Option[]>([]);
  const assignDepartmentDDL = ref<Option[]>([]);
  const supplyMethodCodeDDL = ref<Option[]>([]);
  const supplyMethodTypeCodeDDL = ref<Option[]>([]);
  const supplyMethodSpecialTypeCodeDDL = ref<Option[]>([]);
  const jorpor = ref<OperationBody>({} as OperationBody);

  const getDefaultBudgetYear = (planType?: ProcurementPlanType): number => {
    const currentDate = new Date();
    const currentYear = currentDate.getFullYear();
    const isNextBudgetYear = currentDate.getMonth() >= 9;

    if (planType === ProcurementPlanType.AnnualPlan && isNextBudgetYear) {
      return currentYear + 544;
    }

    return currentYear + 543;
  };

  const initBody = {
    isCommercialMaterial: null,
    acceptors: [] as ParticipantsAcceptor[],
    attachments: [] as Attachments[],
    assignees: [] as Array<ParticipantsAssignee>,
    budgetYear: getDefaultBudgetYear(),
    status: PlanStatus.DraftPlan,
    isCancel: false,
    isChange: false,
  } as PlanBody;

  const body = ref<PlanBody>(structuredClone(initBody));

  const budgetCondition = computed(() => {
    return body.value.budget > 500000 && (body.value.type === ProcurementPlanType.InYearPlan || (body.value.isCancel || body.value.isChange));
  });

  const conditionBudget = computed(() => {
    const statusList = [
      PlanStatus.WaitingAssign,
      PlanStatus.ApprovePlan,
      PlanStatus.DraftRecordDocument,
      PlanStatus.WaitingAcceptor,
      PlanStatus.WaitingAnnouncement,
      PlanStatus.Announcement,
      PlanStatus.CancelPlan,
      PlanStatus.RejectToAssignee,
    ];

    const status = body.value.status && statusList.includes(body.value.status);

    return (budgetCondition.value && status);
  });

  const isShowAssignee = computed(() => budgetCondition.value && body.value.assignees && body.value.assignees.length > 0);

  const canRecall = computed(() => {
    const canRecall = body.value.acceptors?.filter(a => a.acceptorType === AcceptorType.DepartmentDirectorAgree).every(x => x.status === AcceptorStatus.Pending);
    const isOwn = body.value.createdBy === authenStore.profile.id;
    const status = body.value.status === PlanStatus.WaitingApprovePlan;

    return status && canRecall && isOwn;
  });

  const canApproveReject = computed(() =>
    body.value.status == PlanStatus.WaitingApprovePlan
    && isCurrentPendingAcceptor(body.value.acceptors ?? [], authenStore.profile.id, AcceptorType.DepartmentDirectorAgree)
  );

  const canEdit = computed(() => {
    const statusList = [PlanStatus.DraftPlan, PlanStatus.RejectPlan, PlanStatus.EditPlan, null, undefined];
    const isDepartment = authenStore.profile.departmentCode === body.value.departmentCode;
    const status = statusList.includes(body.value.status);
    const isAcceptor = body.value.acceptors.some(s => s.acceptorType === AcceptorType.DepartmentDirectorAgree && (s.delegateeUserId ? s.delegateeUserId : s.userId) === authenStore.profile.id);

    return status && (isDepartment || isAcceptor);
  });

  const isCanSetDefaultUnit = computed(() => [PlanStatus.DraftPlan, PlanStatus.EditPlan, PlanStatus.RejectPlan].includes(body.value.status));

  const isCanSetDefaultApprover = computed(() => [PlanStatus.DraftRecordDocument, PlanStatus.RejectToAssignee].includes(body.value.status));

  const canAssignAssignee = computed(() => {
    const isAssignee = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authenStore.profile.id);
    const status = body.value.status && [PlanStatus.WaitingAssign].includes(body.value.status);

    return isAssignee && status;
  });

  const canAssignAssigneeTypeAssignee = computed(() => {
    const isAssignee = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authenStore.profile.id && a.assigneeType === AssigneeType.Assignee);
    const status = [PlanStatus.DraftRecordDocument, PlanStatus.RejectToAssignee].includes(body.value.status);

    return isAssignee && status;
  });

  const canAssignAssigneeTypeDirector = computed(() => {
    const isAssignee = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authenStore.profile.id && a.assigneeType === AssigneeType.Director);
    const status = [PlanStatus.DraftRecordDocument, PlanStatus.RejectToAssignee].includes(body.value.status);

    return isAssignee && status;
  });

  const canConfirmAcceptor = computed(() => {
    if (!canAssignAssigneeTypeAssignee.value) return false;

    const assignees = body.value.assignees?.filter(a => a.assigneeType === AssigneeType.Assignee) ?? [];
    if (assignees.length === 0) return false;

    const lastAssignee = assignees.reduce((prev, current) =>
      prev.sequence > current.sequence ? prev : current, assignees[0]);

    return (lastAssignee.delegateeUserId ? lastAssignee.delegateeUserId : lastAssignee.userId) === authenStore.profile.id;
  });

  const canAssignAcceptor = computed(() => {
    const isAssignee = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authenStore.profile.id && a.assigneeType === AssigneeType.Assignee);
    const isJorpor = authenStore.profile.isJorPor;
    const status = [PlanStatus.DraftRecordDocument, PlanStatus.RejectToAssignee].includes(body.value.status);

    return (isAssignee || isJorpor) && status;
  });

  const canRecallDocument = computed(() => {
    const isAssignee = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authenStore.profile.id);
    const notCheckDocument = body.value.acceptors?.filter(a => a.acceptorType === AcceptorType.Approver).every(y => y.status === AcceptorStatus.Pending);
    const status = body.value.status === PlanStatus.WaitingAcceptor;

    return isAssignee && status && notCheckDocument;
  });

  const canApproveDocument = computed(() =>
    body.value.status === PlanStatus.WaitingAcceptor
    && isCurrentPendingAcceptor(body.value.acceptors ?? [], authenStore.profile.id, AcceptorType.Approver)
  );

  const isLastApproveDocument = computed(() => {
    if (!canApproveDocument.value) return false;

    return body.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending && f.acceptorType === AcceptorType.Approver)?.length === 1;
  });

  const canAnnouncement = computed(() => {
    const isAnnounement = (body.value.assigneeAnnouncement?.delegateeUserId ? body.value.assigneeAnnouncement?.delegateeUserId : body.value.assigneeAnnouncement?.userId) === authenStore.profile.id;
    const status = body.value.status === PlanStatus.WaitingAnnouncement;

    return isAnnounement && status;
  });

  const canSendCancelAndChange = computed(() => {
    const isDepartment = authenStore.profile.departmentCode === body.value.departmentCode;

    if (!isDepartment) return false;

    if (body.value.status === PlanStatus.Announcement) {
      return true;
    }

    if (body.value.budget <= 500000 && body.value.status === PlanStatus.ApprovePlan) {
      return true;
    }

    return false;
  });
  const canEditDocument = computed(() => {
    const isOwn = body.value.createdBy === authenStore.profile.id;
    const isAssign = body.value.assignees?.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authenStore.profile.id);

    return isOwn || isAssign;
  });

  const canClosePlan = computed(() => {
    return authenStore.profile.isJorPor && !!body.value.id && body.value.status !== PlanStatus.Closed;
  });

  const canCancelClosePlan = computed(() => {
    return authenStore.profile.isJorPor && !!body.value.id && body.value.status === PlanStatus.Closed;
  });


  const getSupplyMethodDDLAsync = async (): Promise<void> => {
    await getSupplyMethodAsync(supplyMethodCodeDDL);
  };

  const getSupplyMethodTypeDDLAsync = async (): Promise<void> => {
    await getSupplyMethodTypeAsync(supplyMethodTypeCodeDDL);
  };

  const getSupplyMethodSpecialTypeDDlAsync = async (parentCode: string): Promise<void> => {
    await getSupplyMethodAsync(supplyMethodSpecialTypeCodeDDL, parentCode);
  }

  const clearBody = (): void => {
    const newBody = structuredClone(initBody);
    newBody.budgetYear = getDefaultBudgetYear();
    body.value = newBody;
  };

  const updateBudgetYearOnTypeChange = (planType: ProcurementPlanType): void => {
    body.value.budgetYear = getDefaultBudgetYear(planType);
  };

  const getDepartmentDDLAsync = async (): Promise<void> => {
    await getDepartmentAsync(departmentDDL);
  };

  const getAssignDepartmentDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.AssignDept);

    if (status === HttpStatusCode.Ok) {
      assignDepartmentDDL.value = data;
    }
  };

  const getSupplyMethodCodeDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod);

    if (status === HttpStatusCode.Ok) {
      supplyMethodCodeDDL.value = data;
    }
  };

  const getJorPorAsync = async (): Promise<void> => {
    const { data, status } = await operationService.getJorPorDirectorAsync(true);

    if (status === HttpStatusCode.Ok) {
      jorpor.value = data;
    }
  };

  const getReviewDocumentAsync = async (id: string, documentType: string): Promise<string> => {
    const { data, status } = await planService.getReviewDocumentAsync(id, documentType);

    if (status !== HttpStatusCode.Ok) {
      return '';
    }

    return data;
  };

  const getByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await planService.getByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = {
        ...data,
        acceptors: (data.status === PlanStatus.DraftPlan && data.acceptors?.length === 0) ? body.value.acceptors : [...data.acceptors],
      };
    }
  };

  const createAsync = async (statusPlan: PlanStatus): Promise<string | void> => {
    const bodySave = { ...body.value };
    bodySave.status = statusPlan;

    const { data, status } = await planService.createAsync(bodySave);

    if (status === HttpStatusCode.Created) {
      router.replace(`/pl/pl001/detail/${data}`);

      await getByIdAsync(data);

      return ToastHelper.createdMessageToast();
    }
  };

  const updateAsync = async (id: string, statusPlan: PlanStatus): Promise<void> => {
    const bodySave = { ...body.value };
    bodySave.status = statusPlan;

    const { status, data } = await planService.updateAsync(id, bodySave);

    if (status === HttpStatusCode.Ok) {
      if (data?.newPlanDocumentFileId) {
        body.value.planDocumentId = data.newPlanDocumentFileId;
      }
      if (data?.newPlanAnnouncementDocumentFileId) {
        body.value.planAnnouncementDocumentId = data.newPlanAnnouncementDocumentFileId;
      }

      await getByIdAsync(id);

      switch (statusPlan) {
        case PlanStatus.RejectPlan:
        case PlanStatus.DraftPlan:
        case PlanStatus.EditPlan:
          return ToastHelper.updatedMessageToast();
        case PlanStatus.WaitingApprovePlan:
          return ToastHelper.sendApproveConfirmMessageToast();
        case PlanStatus.WaitingAssign:
        case PlanStatus.DraftRecordDocument:
          return ToastHelper.updatedMessageToast();
      }
    }
  };

  const getOperDefaultDepartmentApproveAsync = async (): Promise<void> => {
    const orgLevel = authenStore.profile.organizationLevel === String(OrganizationLevelEnum.Branch)
      ? OrganizationLevel.Branch
      : OrganizationLevel.Department;
    const { data, status } = await operationService.getOperationsDefaultDepartmentAsync(orgLevel);

    if (status === HttpStatusCode.Ok) {
      body.value.acceptors = data.map((m, i) => ({
        userId: m.userId,
        fullName: m.fullName,
        sequence: i + 1,
        acceptorType: AcceptorType.DepartmentDirectorAgree,
        departmentName: m.businessUnitName,
        positionName: m.fullPositionName,
        status: AcceptorStatus.Draft,
      } as ParticipantsAcceptor));
    }
  };

  const getJorPorDirectorIdAsync = async (): Promise<string | undefined> => {
    const { data, status } = await operationService.getJorPorDirectorAsync();

    if (status === HttpStatusCode.Ok) {
      return data.userId;
    }
  };

  const actionAsync = async (id: string, reqBody: PlanActionReq): Promise<void> => {
    const { status } = await planService.actionAsync(id, reqBody);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(id);

      switch (reqBody.action) {
        case PlanAction.RejectPlan:
        case PlanAction.AssigneeRejected:
          return ToastHelper.sendEditMessageToast();
        case PlanAction.EditPlan:
          return ToastHelper.recallEditMessageToast();
        case PlanAction.ApprovePlan:
          return ToastHelper.approvedMessageToast();
        case PlanAction.AssignAssignee:
          return ToastHelper.updatedMessageToast();
        case PlanAction.ApprovedAssignee:
          return ToastHelper.assignedMessageToast();
        case PlanAction.AssignAcceptor:
          return ToastHelper.updatedMessageToast();
        case PlanAction.ConfirmAcceptor:
          return ToastHelper.sendApproveConfirmMessageToast();
        case PlanAction.RecallDocument:
          return ToastHelper.recallEditMessageToast();
        case PlanAction.RejectedAcceptor:
          return ToastHelper.sendEditMessageToast();
        case PlanAction.ApprovedAcceptor:
          return ToastHelper.approvedMessageToast();
        case PlanAction.Announcement:
          return ToastHelper.annoucementPlanMessageToast();
        case PlanAction.ClosePlan:
        case PlanAction.CancelClosePlan:
          return ToastHelper.updatedMessageToast();
      }
    }
  };

  const requestActionAsync = async (id: string, reason?: string, isChange: boolean = false): Promise<void> => {
    const { data, status } = await planService.requestActionAsync(id, reason, isChange);

    if (status === HttpStatusCode.Created) {
      router.replace(`/pl/pl001/detail/${data}`);

      await getByIdAsync(data);

      return isChange ? ToastHelper.changedMessageToast() : ToastHelper.canceledMessageToast();
    }
  };

  const getDefaultApproverAsync = async () => {
    if (body.value.assignees.filter(f => f.assigneeType === AssigneeType.Assignee).length === 0) return;

    const lastAssignee = body.value.assignees.filter(f => f.assigneeType === AssigneeType.Assignee).reduce((prev, current) =>
      prev.sequence > current.sequence ? prev : current,
      body.value.assignees.filter(f => f.assigneeType === AssigneeType.Assignee)[0]);

    const params = {
      processType: SectionProcessType.Plan,
      userId: lastAssignee.userId,
      supplyMethodCode: body.value.supplyMethodCode,
      supplyMethodSpecialTypeCode: body.value.supplyMethodSpecialTypeCode,
      budget: 1,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(
      params,
      true);

    if (status === HttpStatusCode.Ok && data.length > 0) {
      body.value.acceptors = body.value.acceptors.filter(f => f.acceptorType !== AcceptorType.Approver);

      data.forEach((item) => {
        body.value.acceptors.push({
          acceptorType: AcceptorType.Approver,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: body.value.acceptors.length + 1,
          status: AcceptorStatus.Pending,
          userId: item.userId,
          departmentName: item.businessUnitName,
        } as ParticipantsAcceptor)
      });
    }
  };

  const getDefaultSegmentOtherManagerApproverAsync = async () => {
    const { data, status } = await operationService.getSegmentOtherManagerAsync(true);

    if (status === HttpStatusCode.Ok) {
      replaceAssigneesOfType(data, AssigneeType.Assignee);
    }
  };

  const getDefaultSegmentITManagerApproverAsync = async () => {
    const { data, status } = await operationService.getSegmentITManagerAsync(true);

    if (status === HttpStatusCode.Ok) {
      replaceAssigneesOfType(data, AssigneeType.Assignee);
    }
  };

  const replaceAssigneesOfType = (data: OperationBody, type: AssigneeType) => {
    const assignee = {
      userId: data.userId,
      fullName: data.fullName,
      positionName: data.fullPositionName,
      sequence: 1,
      departmentName: data.businessUnitName,
      status: AssigneeStatus.Draft,
      assigneeType: type,
      assigneeGroup: AssigneeGroup.JorPor,
    } as ParticipantsAssignee;

    body.value.assignees = body.value.assignees.filter(
      (a) => a.assigneeType !== type
    );

    body.value.assignees.push(assignee);
  };

  const onUpsertAssignees = async () => {
    if (!body.value.id) return;
    if (!body.value.assignees?.length) return;

    await actionAsync(body.value.id, {
      action: PlanAction.AssignAssignee,
      assignees: body.value.assignees,
      assignSegmentCode: body.value.assignSegmentCode,
      egpNumber: body.value.egpNumber,
      groupEgpNumber: body.value.groupEgpNumber,
    });
  };

  const onUpsertAttachments = async () => {
    if (!body.value.id) return;

    const { status } = await planService.attachmentsAsync(body.value.id, body.value.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await getByIdAsync(body.value.id);
    }
  };

  return {
    departmentDDL,
    assignDepartmentDDL,
    body,
    supplyMethodCodeDDL,
    supplyMethodTypeCodeDDL,
    canEdit,
    conditionBudget,
    canRecall,
    canApproveReject,
    canAssignAssignee,
    canAssignAssigneeTypeAssignee,
    canAssignAssigneeTypeDirector,
    canConfirmAcceptor,
    canAssignAcceptor,
    canRecallDocument,
    canApproveDocument,
    isLastApproveDocument,
    canAnnouncement,
    canSendCancelAndChange,
    canEditDocument,
    canClosePlan,
    canCancelClosePlan,
    supplyMethodSpecialTypeCodeDDL,
    jorpor,
    getReviewDocumentAsync,
    getSupplyMethodDDLAsync,
    getSupplyMethodTypeDDLAsync,
    clearBody,
    updateBudgetYearOnTypeChange,
    getDepartmentDDLAsync,
    getAssignDepartmentDDLAsync,
    getSupplyMethodCodeDDLAsync,
    createAsync,
    updateAsync,
    getByIdAsync,
    getOperDefaultDepartmentApproveAsync,
    getJorPorDirectorIdAsync,
    actionAsync,
    getSupplyMethodSpecialTypeDDlAsync,
    requestActionAsync,
    getJorPorAsync,
    isShowAssignee,
    getDefaultApproverAsync,
    getDefaultSegmentOtherManagerApproverAsync,
    getDefaultSegmentITManagerApproverAsync,
    onUpsertAssignees,
    onUpsertAttachments,
    budgetCondition,
    isCanSetDefaultUnit,
    isCanSetDefaultApprover,
  }
});

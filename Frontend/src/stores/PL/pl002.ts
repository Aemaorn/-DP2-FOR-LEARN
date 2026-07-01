import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from '@/enums/participants';
import { PlanAnnouncementAction, PlanAnnouncementStatus } from '@/enums/planAnnouncement';
import { EGroupCode, EWorkProcess } from '@/enums/shared';
import ToastHelper from '@/helpers/toast';
import type {
  pl002Criteria,
  pl002Table,
  PlanAnnouncementBody,
  planSelected,
} from '@/models/PL/pl002';
import type { Option, OptionBadge } from '@/models/shared/option';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { ParticipantsAcceptor, ParticipantsAssignee } from '@/models/shared/participants';
import type { Attachments } from '@/models/shared/uploadFile';
import SharedService from '@/services/Shared/dropdown';
import operationService from '@/services/Shared/operations';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { computed, ref, type Ref } from 'vue';
import planAnnouncementHelper from '@/helpers/planAnnouncement';
import { showConfirmDialogAsync, showReasonDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import { OrganizationLevel, SectionProcessType } from '@/enums/operations';
import { OrganizationLevelEnum } from '@/enums/shared';
import { useAuthenticationStore } from '../authentication';
import { isCurrentPendingAcceptor } from '@/helpers/participants';
import planService from '@/services/PL/pl001';
import PlanAnnouncementService from '@/services/PL/pl002';
import router from '@/router';
import type { defaultAcceptorCriteria, OperationBody } from '@/models/shared/operations';
import { formatCurrency, numberToThaiText } from '@/helpers/currency';
import { checkIsSixty } from '@/helpers/supplyMethod';
import { SupplyMethodSpecialTypeCode } from '@/enums/supplyMethod';

const getSupplyMethodAsync = async (target: Ref<Option[]>, parentCode?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

export const usePl002ListStore = defineStore('pl002ListStore', () => {
  const { announcementStatusAttributes } = planAnnouncementHelper;

  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
    workProcess: EWorkProcess.All,
    status: PlanAnnouncementStatus.All,
  } as pl002Criteria);


  const supplyMethodDDL = ref<Option[]>([]);

  const clearCriteriaAsync = async () => {
    searchCriteria.value = {
      pageNumber: 1,
      pageSize: 10,
      sort: [],
      workProcess: EWorkProcess.All,
      status: PlanAnnouncementStatus.All,
    };

    await getListAsync();
  };

  const statusOptionBadge = ref([] as OptionBadge[]);
  const table = ref<TDataTableResult<pl002Table>>({
    data: [],
    totalRecords: 0,
  });

  const getSupplyMethodDDLAsync = async (): Promise<void> => {
    await getSupplyMethodAsync(supplyMethodDDL);
  };

  const getListAsync = async () => {
    const { data: resp, status } = await PlanAnnouncementService.getListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = resp.data;
      statusOptionBadge.value = announcementStatusAttributes(resp.counts);
    }
  };

  const deleteByIdAsync = async (id: string) => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) {
      return;
    }

    const { status } = await PlanAnnouncementService.deleteByIdAsync(id);

    switch (status) {
      case HttpStatusCode.NoContent:
        ToastHelper.deletedMessageToast();
        break;
      case HttpStatusCode.Conflict:
        ToastHelper.warning("เกิดข้อผิดพลาด", "ไม่สามารถลบรายการที่ดำเนินการได้");
        break;
    }

    await getListAsync();
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  return {
    clearCriteriaAsync,
    getListAsync,
    getSupplyMethodDDLAsync,
    deleteByIdAsync,
    searchCriteria,
    supplyMethodDDL,
    statusOptionBadge,
    table,
    onChangePageSize,
  };
});

export const usePl002DetailStore = defineStore('pl002DetailStore', () => {
  const userStore = useAuthenticationStore();
  const assignDepartmentDDL = ref<Option[]>([]);

  const body = ref<PlanAnnouncementBody>({
    assignees: [] as ParticipantsAssignee[],
    attachments: [] as Attachments[],
    planSelected: [] as planSelected[],
    acceptors: [] as ParticipantsAcceptor[],
    status: PlanAnnouncementStatus.Draft
  } as PlanAnnouncementBody);

  const onClearBody = () => {
    body.value = {
      assignees: [] as ParticipantsAssignee[],
      attachments: [] as Attachments[],
      planSelected: [] as planSelected[],
      status: PlanAnnouncementStatus.Draft
    } as PlanAnnouncementBody;
  };

  const isRequiredPublished = ref<boolean>(false);

  const supplyMethodCodeDDL = ref<Option[]>([]);

  const getAnnualPlan = async (): Promise<void> => {
    const { data, status } = await PlanAnnouncementService.getAnnualPlanAsync(
      body.value.year,
      body.value.supplyMethodCode
    );

    if (status === HttpStatusCode.Ok) {
      data.forEach(item => {
        const exists = body.value.planSelected.some(p => p.planId === item.planId);
        if (!exists) {
          body.value.planSelected.push(item);
        }
      });

      buildRemarkText(body.value.year, data);
    }
  };

  const fetchAnnualPlanList = async (year?: number, supplyMethodCode?: string): Promise<planSelected[]> => {
    const { data, status } = await PlanAnnouncementService.getAnnualPlanAsync(
      year ?? body.value.year,
      supplyMethodCode ?? body.value.supplyMethodCode
    );

    if (status === HttpStatusCode.Ok) {
      return data;
    }

    return [];
  };

  const applySelectedPlans = (selected: planSelected[]): void => {
    selected.forEach(item => {
      const exists = body.value.planSelected.some(p => p.planId === item.planId);
      if (!exists) {
        body.value.planSelected.push(item);
      }
    });

    buildRemarkText(body.value.year, body.value.planSelected);
  };

  const buildRemarkText = async (
    year: number | string,
    data: { budget: number | string }[]
  ): Promise<void> => {
    if (data.length == 0) {
      body.value.remark = '';
      return;
    }

    const summary = data.reduce((sum, { budget }) => sum + (Number(budget) || 0), 0);

    if (summary <= 0) {
      body.value.remark = '';
      return;
    }

    const thaiText = numberToThaiText(summary);

    body.value.remark = `ธนาคารอาคารสงเคราะห์ได้จัดทำประกาศเผยแพร่แผนการจัดซื้อจัดจ้างประจำปีงบประมาณ ${year} รวม ${data.length} โครงการ รวมเป็นเงิน ${formatCurrency(summary)} บาท (${thaiText})`;
  };

  const getSupplyMethodDDLAsync = async (): Promise<void> => {
    await getSupplyMethodAsync(supplyMethodCodeDDL);
  };

  const getJorPorDirector = async (): Promise<void> => {
    const { data, status } = await operationService.getJorPorDirectorAsync();

    if (status == HttpStatusCode.Ok) {
      const jorProDirector = {
        userId: data.userId,
        fullName: data.fullName,
        positionName: data.fullPositionName,
        sequence: 1,
        departmentName: data.businessUnitName,
        status: AssigneeStatus.Draft,
        assigneeType: AssigneeType.Director,
        assigneeGroup: AssigneeGroup.JorPor,
      } as ParticipantsAssignee;

      body.value.assignees.push(jorProDirector);
    }
  };

  const createPlanAnnouncement = async (): Promise<string | undefined> => {
    const { data, status } = await PlanAnnouncementService.createPlanAnnouncementAsync(body.value);

    if (status == HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();

      return data;
    }
  };

  const updatePlanAnnouncement = async (id: string, announcementStatus?: PlanAnnouncementStatus): Promise<void> => {
    const { status, data } = await PlanAnnouncementService.updatePlanAnnouncementAsync(id, body.value);

    const toast = {
      [PlanAnnouncementStatus.WaitingAssign]: () => ToastHelper.assignedMessageToast(),
      [PlanAnnouncementStatus.WaitingAcceptor]: () => ToastHelper.sendApproveConfirmMessageToast(),
    };

    if (status == HttpStatusCode.Ok) {
      if (data?.newApproveDocumentFileId) {
        body.value.approveDocumentId = data.newApproveDocumentFileId;
      }
      if (data?.newAnnouncementDocumentFileId) {
        body.value.announcementDocumentId = data.newAnnouncementDocumentFileId;
      }

      announcementStatus ?
        toast[announcementStatus as PlanAnnouncementStatus.WaitingAssign | PlanAnnouncementStatus.WaitingAcceptor]()
        : ToastHelper.updatedMessageToast();

      // Refresh data to get updated document versions
      await getPlanAnnouncement(id);
    }

    if (status == HttpStatusCode.Conflict) {
      ToastHelper.warning("ขออนุมัติเผยแพร่แผนจัดซื้อจัดจ้าง", "ต้องมีผู้รับผิดชอบอย่างน้อย 1 คน");
    }
  }

  const getPlanAnnouncement = async (id: string): Promise<void> => {
    const { data, status } = await PlanAnnouncementService.getPlanAnnouncementAsync(id);

    if (status == HttpStatusCode.Ok) {
      body.value = data;
    }
  };

  const rejectAnnualPlan = async (planId: string): Promise<void> => {
    if (!(await showConfirmDialogAsync(ConfirmDialogType.SendEdit))) return;

    const { status } = await PlanAnnouncementService.rejectAnnualPlanAsync(planId);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();

      if (body.value.planAnnouncementId) {
        await getPlanAnnouncement(body.value.planAnnouncementId);
      };

      buildRemarkText(body.value.year, body.value.planSelected);
    }
  }

  const onRemovePlan = async (index: number): Promise<void> => {
    const selectPlan = body.value.planSelected[index];

    if (!selectPlan) {
      buildRemarkText(body.value.year, body.value.planSelected);

      return;
    };

    if (selectPlan.id) {
      if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;

      await PlanAnnouncementService.deletePlanAnnouncementById(selectPlan.id);
    }

    body.value.planSelected.splice(index, 1);
    buildRemarkText(body.value.year, body.value.planSelected);
  }

  const onActionPlan = async (action: PlanAnnouncementAction) => {
    let remark;

    switch (action) {
      case PlanAnnouncementAction.AcceptorApprove:
        {
          const resp = await showReasonDialogAsync(isLastApproval.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

          if (!resp.isConfirm) return;

          remark = resp.reason;
        }
        break;

      case PlanAnnouncementAction.AcceptorReject:
        {
          const resp = await showReasonDialogAsync(ReasonDialogType.Reject, true);

          if (!resp.isConfirm) return;

          remark = resp.reason;
        }
        break;

      default:
        break;
    }

    const { status } = await PlanAnnouncementService.actionPlanAnnouncementAsync(
      body.value.planAnnouncementId!,
      action,
      remark,
      body.value.announcementTitle,
      body.value.announcementDate
    );

    if (status == HttpStatusCode.Ok) {
      switch (action) {
        case PlanAnnouncementAction.Recall:
          ToastHelper.recallEditMessageToast();

          break;
        case PlanAnnouncementAction.AcceptorApprove:
          ToastHelper.approvedMessageToast();

          break;

        case PlanAnnouncementAction.AcceptorReject:
          ToastHelper.sendEditMessageToast();

          break;

        case PlanAnnouncementAction.DirectorAnnouncement:
          ToastHelper.annoucementPlanMessageToast();

          break;
        default:
          break;
      }
      getPlanAnnouncement(body.value.planAnnouncementId!);
    }

    if (status == HttpStatusCode.Conflict) {
      ToastHelper.confirmMessageToast();
    }
  };

  const getAssignDepartmentDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.AssignDept);

    if (status === HttpStatusCode.Ok) {
      assignDepartmentDDL.value = data;
    }
  };

  const getOperDefaultDepartmentApproveAsync = async (): Promise<void> => {
    const orgLevel = userStore.profile.organizationLevel === String(OrganizationLevelEnum.Branch)
      ? OrganizationLevel.Branch
      : OrganizationLevel.Department;
    const { data, status } = await operationService.getOperationsDefaultDepartmentAsync(orgLevel);

    if (status === HttpStatusCode.Ok) {
      body.value.acceptors = [];

      body.value.acceptors = data.map((m, i) => ({
        userId: m.userId,
        fullName: m.fullName,
        sequence: i + 1,
        acceptorType: AcceptorType.Approver,
        departmentName: m.businessUnitName,
        positionName: m.fullPositionName,
        status: AcceptorStatus.Draft,
      }));
    }
  };

  const getDefaultApproverAsync = async () => {
    const lastAssignee = body.value.assignees.filter(f => f.assigneeType === AssigneeType.Assignee).reduce((prev, current) =>
      prev.sequence > current.sequence ? prev : current,
      body.value.assignees.filter(f => f.assigneeType === AssigneeType.Assignee)[0]);

      let supplyMethodSpecialTypeCode = undefined;
      const is60 = checkIsSixty(body.value.supplyMethodCode);

      if (is60) {
         supplyMethodSpecialTypeCode = SupplyMethodSpecialTypeCode.specificMethod;
      }

    const params = {
      processType: SectionProcessType.Plan,
      userId: lastAssignee.userId,
      supplyMethodCode: body.value.supplyMethodCode,
      supplyMethodSpecialTypeCode: supplyMethodSpecialTypeCode,
      budget: 1,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(
      params,
      true);

    if (status === HttpStatusCode.Ok && data.length > 0) {
      body.value.acceptors = [];
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

  const onSendEditOrSendCancelAsync = async (planId: string, isChange: boolean = false) => {
    const resp = await showReasonDialogAsync(isChange ? ReasonDialogType.RequestChange : ReasonDialogType.RequestCancel);

    if (!resp.isConfirm) return;

    const { data, status } = await planService.requestActionAsync(planId, resp.reason, isChange);

    if (status === HttpStatusCode.Created) {
      router.push(`/pl/pl001/detail/${data}`);

      return isChange ? ToastHelper.changedMessageToast() : ToastHelper.canceledMessageToast();
    }
  };

  const canEdit = computed(() =>
    body.value.status == PlanAnnouncementStatus.Draft ||
    ([PlanAnnouncementStatus.WaitingAssign, PlanAnnouncementStatus.Rejected].includes(body.value.status) &&
      body.value.assignees.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === userStore.profile.id)));

  const jorPorAssign = computed(() => [PlanAnnouncementStatus.Draft].includes(body.value.status) &&
    body.value.planAnnouncementId &&
    body.value.assignees.filter(a => a.assigneeType === AssigneeType.Director).some(item => (item.delegateeUserId ? item.delegateeUserId : item.userId) === userStore.profile.id));

  const canRecall = computed(() => {
    const canRecall = body.value.acceptors?.filter(a => a.acceptorType === AcceptorType.Approver).every(x => x.status === AcceptorStatus.Pending);
    const status = body.value.status === PlanAnnouncementStatus.WaitingAcceptor;

    return status && canRecall && body.value.assignees.some(x => (x.delegateeUserId ? x.delegateeUserId : x.userId) == userStore.profile.id);
  });

  const isConfirmAssign = computed(() =>
    [
      PlanAnnouncementStatus.WaitingAssign,
      PlanAnnouncementStatus.Rejected
    ].includes(body.value.status) &&
    body.value.planAnnouncementId &&
    body.value.assignees.some(item =>
      (item.delegateeUserId ? item.delegateeUserId : item.userId) === userStore.profile.id));

  const AssignCanEdit = computed(() =>
    [
      PlanAnnouncementStatus.WaitingAssign,
      PlanAnnouncementStatus.Rejected
    ].includes(body.value.status) &&
    body.value.planAnnouncementId &&
    body.value.assignees.some(x =>
      (x.delegateeUserId ? x.delegateeUserId : x.userId) == userStore.profile.id &&
      x.assigneeType === AssigneeType.Assignee));

  // ปุ่มส่งเห็นชอบ/อนุมัติ แสดงเฉพาะผู้รับผิดชอบ (Assignee) คนสุดท้ายตามลำดับเท่านั้น (แบบ PL001)
  const canSendApprove = computed(() => {
    if (!AssignCanEdit.value) return false;

    const assignees = body.value.assignees?.filter(a => a.assigneeType === AssigneeType.Assignee) ?? [];
    if (assignees.length === 0) return false;

    const lastAssignee = assignees.reduce((prev, current) =>
      prev.sequence > current.sequence ? prev : current, assignees[0]);

    return (lastAssignee.delegateeUserId ? lastAssignee.delegateeUserId : lastAssignee.userId) === userStore.profile.id;
  });

  const canPublish = computed(() =>
    body.value.assigneeAnnouncement &&
    body.value.status == PlanAnnouncementStatus.WaitingAnnouncement &&
    (body.value.assigneeAnnouncement.delegateeUserId ? body.value.assigneeAnnouncement.delegateeUserId : body.value.assigneeAnnouncement.userId) == userStore.profile.id
  );

  const isAnnouncementVisible = computed(() =>
    [PlanAnnouncementStatus.Announcement, PlanAnnouncementStatus.WaitingAnnouncement].includes(body.value.status)
  );

  const canAcceptAndReject = computed(() =>
    body.value.status == PlanAnnouncementStatus.WaitingAcceptor
    && isCurrentPendingAcceptor(body.value.acceptors ?? [], userStore.profile.id)
  );

  const isLastApproval = computed(() => {
    const pendingData = body.value.acceptors
      .filter(f => f.status === AcceptorStatus.Pending);

    if (pendingData.length === 0 || !canAcceptAndReject.value) {
      return false;
    }

    const current = pendingData
      .reduce((prev, curr) => curr.sequence < prev.sequence ? curr : prev,
        pendingData[0]);

    if (!current) {
      return false;
    }

    return current.sequence === pendingData[pendingData.length - 1].sequence;
  });

  const getReviewDocumentAsync = async (id: string, documentType: string): Promise<string> => {
    const { data, status } = await PlanAnnouncementService.getReviewDocumentAsync(id, documentType);

    if (status !== HttpStatusCode.Ok) {
      ToastHelper.error('ดึงข้อมูลเอกสาร', 'ไม่พบเอกสารที่ต้องการ');

      return '';
    }

    return data;
  }

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
    if (!body.value.planAnnouncementId) return;
    if (!body.value.assignees?.length) return;

    // auto-save เมื่อมีการเพิ่ม/ลบ/จัดลำดับผู้รับผิดชอบ โดยคงสถานะปัจจุบันไว้ (แบบ PL001)
    // ผู้รับผิดชอบยืนยันมอบหมายเองได้ที่สถานะ WaitingAssign ผ่านปุ่มเดิม ไม่ต้องถอยกลับไป Draft
    await updatePlanAnnouncement(body.value.planAnnouncementId);
  };

  return {
    body,
    onClearBody,
    getAnnualPlan,
    fetchAnnualPlanList,
    applySelectedPlans,
    getSupplyMethodDDLAsync,
    supplyMethodCodeDDL,
    assignDepartmentDDL,
    getAssignDepartmentDDLAsync,
    getJorPorDirector,
    createPlanAnnouncement,
    onRemovePlan,
    getPlanAnnouncement,
    rejectAnnualPlan,
    updatePlanAnnouncement,
    onActionPlan,
    onSendEditOrSendCancelAsync,
    getOperDefaultDepartmentApproveAsync,
    canEdit,
    jorPorAssign,
    canRecall,
    canPublish,
    isAnnouncementVisible,
    canAcceptAndReject,
    isLastApproval,
    isConfirmAssign,
    getReviewDocumentAsync,
    AssignCanEdit,
    canSendApprove,
    isRequiredPublished,
    getDefaultApproverAsync,
    getDefaultSegmentOtherManagerApproverAsync,
    getDefaultSegmentITManagerApproverAsync,
    buildRemarkText,
    onUpsertAssignees,
  };
});
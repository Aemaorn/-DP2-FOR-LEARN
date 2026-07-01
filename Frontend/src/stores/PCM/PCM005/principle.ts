import type { Option } from "@/models/shared/option";
import type { Budget, BudgetDetail, Committee, PerfSupportData, PrincipleBody, RentalAnalyses, RoiLoanAndDepositSummary, RoiPerfResult } from "@/models/PCM/PCM005/principle";
import type { ParticipantsAcceptor, ParticipantsAssignee } from "@/models/shared/participants";
import { EGroupCode, OrganizationLevelEnum } from "@/enums/shared";
import { defineStore } from "pinia";
import { computed, ref, type Ref } from "vue";
import { HttpStatusCode } from "axios";
import { usePcm005DetailStore } from "./pcm005";
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from "@/enums/participants";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import { PrincipleStatus } from "@/enums/PCM005/principle";
import { useAuthenticationStore } from "@/stores/authentication";
import { showReasonDialogAsync } from "@/helpers/dialog";
import { ReasonDialogType } from "@/enums/dialog";
import SharedService from "@/services/Shared/dropdown";
import principleService from "@/services/PCM/PCM005/principle";
import ToastHelper from "@/helpers/toast";
import operationService from "@/services/Shared/operations";
import { OrganizationLevel, SectionProcessType } from "@/enums/operations";
import { SupplyMethodCode } from "@/enums/supplyMethod";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";

const getParameterDDLAsync = async (value: Ref<Option[]>, group: EGroupCode) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(group, undefined, true);

  if (status === HttpStatusCode.Ok) {
    value.value = data;
  }
};

export const usePcm005PrincipleStore = defineStore('pcm005-principle-store', () => {
  const authStore = useAuthenticationStore();
  const pcmStore = usePcm005DetailStore();

  const initBody = {
    referencePriceAmount: undefined,
    operationExpense: undefined,
    analysisSummaryNpv: undefined,
    analysisSummaryPaybackYearPeriod: undefined,
    analysisSummaryDiscountedPaybackYearPeriod: undefined,
    status: PrincipleStatus.Draft,
    perfSupportData: {} as PerfSupportData,
    acceptors: [] as ParticipantsAcceptor[],
    assignees: [] as ParticipantsAssignee[],
    committees: [] as Committee[],
    perfSupportDataDetails: [] as PerfSupportData[],
    roiLoanAndDepositSummaries: [] as RoiLoanAndDepositSummary[],
    roiPerfResults: [] as RoiPerfResult[],
    budgets: [
      {
        sequence: 1,
        description: pcmStore.body.planName ?? '',
        details: [
          {
            sequence: 1,
          }
        ] as BudgetDetail[],
      },
    ] as Budget[],
    rentalAnalyses: [] as RentalAnalyses[],
    isAcceptanceCommittee: true,
    isRentCommittee: true,
  } as PrincipleBody;

  const positionOnBoardDDL = ref<Option[]>([]);
  const posOnBardNonCommitteeDDL = ref<Array<Option>>([]);
  const docuementTemplateDDL = ref<Option[]>([]);
  const departmentDDL = ref<Option[]>([]);
  const budgetTypeDDL = ref<Option[]>([]);
  const accountCodeDDL = ref<Option[]>([]);
  const body = ref<PrincipleBody>(structuredClone(initBody));

  const canEdit = computed(() => {
    const status = [PrincipleStatus.Draft, PrincipleStatus.Edit, PrincipleStatus.Rejected].includes(body.value.status);

    return status;
  });

  const canRecall = computed(() => {
    const status = body.value.status === PrincipleStatus.WaitingUnitApproval;
    const checkHasApproveOrReject = body.value.acceptors?.some(a => a.acceptorType === AcceptorType.DepartmentDirectorAgree && [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status));

    return status && !checkHasApproveOrReject;
  });

  const canUnitApprove = computed(() => {
    if (!body.value.acceptors) return false;
    const status = body.value.status === PrincipleStatus.WaitingUnitApproval;
    const checkQue = isCurrentPendingAcceptor(body.value.acceptors, authStore.profile.id, AcceptorType.DepartmentDirectorAgree);
    return status && checkQue;
  });

  const canDirectorAssign = computed(() => {
    const status = [PrincipleStatus.WaitingAssign, PrincipleStatus.RejectToAssignee].includes(body.value.status);
    const canConfirm = body.value.assignees?.some(a => a.assigneeType === AssigneeType.Director && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canConfirm;
  });

  const canAssignAndConfirm = computed(() => {
    const status = [PrincipleStatus.WaitingAssign, PrincipleStatus.RejectToAssignee].includes(body.value.status);
    const canConfirm = body.value.assignees?.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canConfirm;
  });

  const canAssigneesAssign = computed(() => {
    const status = [PrincipleStatus.WaitingAssign, PrincipleStatus.RejectToAssignee].includes(body.value.status);
    const canConfirm = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canConfirm;
  });

  const canAssignee = computed(() => {
    const status = body.value.status === PrincipleStatus.WaitingComment;
    const canAssignees = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canAssignees;
  });

  const canComment = computed(() => {
    const status = body.value.status === PrincipleStatus.WaitingComment;
    if (!body.value.assignees) return false;
    const lastAssignee = body.value.assignees?.reduce((prev, curr) => curr.sequence > prev.sequence ? curr : prev, body.value.assignees[0]);
    const hasPermissionUser = (lastAssignee?.delegateeUserId ? lastAssignee?.delegateeUserId : lastAssignee?.userId) === authStore.profile.id;

    return status && hasPermissionUser;
  });

  const canApprove = computed(() => {
    if (!body.value.acceptors) return false;
    const status = body.value.status === PrincipleStatus.WaitingAcceptance;
    const checkQue = isCurrentPendingAcceptor(body.value.acceptors, authStore.profile.id, AcceptorType.Approver);
    return status && checkQue;
  });

  const isLastApprove = computed(() => body.value.acceptors && body.value.acceptors.filter(f => f.acceptorType === AcceptorType.Approver && f.status === AcceptorStatus.Pending).length === 1);

  const onGetPsoDDLAsync = async (): Promise<void> => {
    await Promise.all([getParameterDDLAsync(positionOnBoardDDL, EGroupCode.PosBoard), getParameterDDLAsync(posOnBardNonCommitteeDDL, EGroupCode.PosBoard)])

    positionOnBoardDDL.value.splice(positionOnBoardDDL.value.length - 1, 1);
    posOnBardNonCommitteeDDL.value = posOnBardNonCommitteeDDL.value.slice(-1);
  };

  const getBudgetTypeDropdownAsync = async (): Promise<void> => {
    await getParameterDDLAsync(budgetTypeDDL, EGroupCode.BudgetTyp);
  };

  const getAccountCodeDropdownAsync = async (): Promise<void> => {
    await getParameterDDLAsync(accountCodeDDL, EGroupCode.GLAcc);
  };

  const onGetDocumentTemplateDDLAsync = async (): Promise<void> => {
    await getParameterDDLAsync(docuementTemplateDDL, EGroupCode.PRentalTpl);
  };

  const getDepartmentDropdownAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

    if (status === HttpStatusCode.Ok) {
      departmentDDL.value = data;
    }
  };

  const onGetJorPorAsync = async (): Promise<void> => {
    const { data, status } = await operationService.getJorPorDirectorAsync();

    if (status === HttpStatusCode.Ok) {

      if (body.value.assignees.length === 0) {
        body.value.assignees.push({
          ...data,
          sequence: 1,
          assigneeType: AssigneeType.Director,
          departmentName: data.businessUnitName,
          positionName: data.fullPositionName,
          status: AssigneeStatus.Draft,
          assigneeGroup: AssigneeGroup.JorPor,
        } as ParticipantsAssignee);
      }
    }
  };

  const createAsync = async (statusBody: PrincipleStatus): Promise<void> => {
    if (!pcmStore.body.id) return;

    const payload = {
      ...body.value,
      status: statusBody ?? body.value.status,
    }

    const { data, status } = await principleService.createAsync(pcmStore.body.id, payload);

    if (status === HttpStatusCode.Created) {
      await getByIdAsync(data);

      switch (statusBody) {
        case PrincipleStatus.Draft:
          return ToastHelper.createdMessageToast();
        case PrincipleStatus.WaitingUnitApproval:
          return ToastHelper.sendApproveMessageToast();
      }
    }
  };

  const updateAsync = async (statusBody?: PrincipleStatus): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const { status, data } = await principleService.updateAsync(pcmStore.body.id, body.value, statusBody);

    if (status === HttpStatusCode.Ok) {
      if (data?.newDocumentFileId) {
        body.value.documentTemplateId = data.newDocumentFileId;
      }

      await getByIdAsync(body.value.id);

      switch (statusBody) {
        case PrincipleStatus.Draft:
          return ToastHelper.updatedMessageToast();
        case PrincipleStatus.WaitingUnitApproval:
          return ToastHelper.sendApproveMessageToast();
        case PrincipleStatus.WaitingComment:
          return ToastHelper.assignedMessageToast();
        case PrincipleStatus.WaitingAcceptance:
          return ToastHelper.sendApproveConfirmMessageToast();
        case PrincipleStatus.Edit:
          return ToastHelper.recallEditMessageToast();
        default:
          return ToastHelper.updatedMessageToast();
      }
    }
  };

  const getByIdAsync = async (id?: string): Promise<void> => {

    if (!id) {
      await onGetJorPorAsync();
    }

    if (!pcmStore.body.id || !id) return;

    const { data, status } = await principleService.getByIdAsync(pcmStore.body.id, id);

    if (status === HttpStatusCode.Ok) {
      const partial: Partial<PrincipleBody> = structuredClone(initBody);

      for (const key in data) {
        const value = data[key as keyof PrincipleBody];

        if (value !== null) {
          partial[key as keyof PrincipleBody] = value;
        }
      };

      if (id) {
        body.value.id = id;
      }

      Object.assign(body.value, partial);

      if (body.value.budgets == null || body.value.budgets.length === 0) {
        body.value.budgets = [
          {
            sequence: 1,
            description: pcmStore.body.planName ?? '',
            details: [
              {
                sequence: 1,
              }
            ] as BudgetDetail[],
          },
        ] as Budget[];
      }

      const hasExistingAcceptors = Array.isArray(data.acceptors) && data.acceptors.length > 0;
      if (!hasExistingAcceptors) {
        await defaultAcceptorAsync();
      }
    }
  };

  const unitAppoveAsync = async () => {
    if (!pcmStore.body.id || !body.value.id) return;

    const confirmRes = await showReasonDialogAsync(ReasonDialogType.Accepted);

    if (!confirmRes.isConfirm) return;

    const { status } = await principleService.approveAsync(pcmStore.body.id, body.value.id, confirmRes.reason);

    if (status === HttpStatusCode.Ok) {
      await pcmStore.getDetailAsync(pcmStore.body.id);
      await getByIdAsync(body.value.id);

      return ToastHelper.approvedMessageToast();
    }
  };

  const unitRejectAsync = async () => {
    if (!pcmStore.body.id || !body.value.id) return;

    const confirmRes = await showReasonDialogAsync(ReasonDialogType.NotAgree, true);

    if (!confirmRes.isConfirm) return;

    const { status } = await principleService.rejectAsync(pcmStore.body.id, body.value.id, confirmRes.reason);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);

      return ToastHelper.notAgreeMessageToast();
    }
  }

  const approveAsync = async (): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const confirmRes = await showReasonDialogAsync(isLastApprove.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (!confirmRes.isConfirm) return;

    const { status } = await principleService.approveAsync(pcmStore.body.id, body.value.id, confirmRes.reason);

    if (status === HttpStatusCode.Ok) {
      await pcmStore.getDetailAsync(pcmStore.body.id);
      await getByIdAsync(body.value.id);

      return ToastHelper.approvedMessageToast();
    }
  };

  const rejectAsync = async (): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const confirmRes = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (!confirmRes.isConfirm) return;

    const { status } = await principleService.rejectAsync(pcmStore.body.id, body.value.id, confirmRes.reason);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);

      return ToastHelper.sendEditMessageToast();
    }
  };

  const assigneeRejectAsync = async (): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const resp = await showReasonDialogAsync(ReasonDialogType.Reject);

    if (!resp.isConfirm) return;

    const { status } = await principleService.rejectAsync(pcmStore.body.id, body.value.id, resp.reason);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);

      return ToastHelper.sendEditMessageToast();
    }
  };

  const defaultAcceptorAsync = async (): Promise<void> => {
    if (body.value.acceptors.length === 0) {
      await getOperDefaultDepartmentApproveAsync();
    }

    const hasAssignee = body.value.assignees?.some(a => a.assigneeType === AssigneeType.Assignee);
    const hasApprover = body.value.acceptors?.some(a => a.acceptorType === AcceptorType.Approver);

    if (hasAssignee && !hasApprover) {
      await getDefaultApproverAsync();
    }
  }

  const getOperDefaultDepartmentApproveAsync = async (): Promise<void> => {
    const orgLevel = authStore.profile.organizationLevel === String(OrganizationLevelEnum.Branch)
      ? OrganizationLevel.Branch
      : OrganizationLevel.Group;
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

  const getLastAssignee = (): ParticipantsAssignee | undefined => {
    const assignees = body.value.assignees?.filter(
      (f) => f.assigneeType === AssigneeType.Assignee
    );

    if (!assignees || assignees.length === 0) return undefined;

    return assignees.reduce((prev, current) =>
      prev.sequence > current.sequence ? prev : current
      , assignees[0]);
  };

  const getDefaultApproverAsync = async (): Promise<void> => {
    const lastAssignee = getLastAssignee();
    if (!lastAssignee) return;

    const params = {
      processType: SectionProcessType.PrincipleRentalApproval,
      userId: lastAssignee.userId,
      supplyMethodCode: SupplyMethodCode.eighty,
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

  const commentAsync = async (remark: string): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const { status } = await principleService.commentAsync(pcmStore.body.id, body.value.id, remark);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);

      return ToastHelper.remarkOfficerMessageToast();
    }
  };

  const onResetBody = () => {
    body.value = structuredClone(initBody);
  };

  const getReviewDocumentAsync = async (id: string) => {
    if (!pcmStore.body.id) return;

    const { data, status } = await principleService.getReviewDocumentAsync(pcmStore.body.id, id);

    if (status !== HttpStatusCode.Ok) {
      ToastHelper.error('ดึงข้อมูลเอกสาร', 'ไม่พบเอกสารที่ต้องการ');

      return '';
    }

    return data;
  };

  const onExportConsiderationAsync = async (id: string | undefined) => {
    if (!pcmStore.body.id) return;

    const { data, status, } = await principleService.exportConsiderationAsync(pcmStore.body.id, id);

    if (status !== HttpStatusCode.Ok) {
      ToastHelper.error('ดึงข้อมูลเอกสาร', 'ไม่พบเอกสารที่ต้องการ');

      return;
    }

    const fileName = `consideration-${new Date().toDateString()}.xlsx`;

    const blob = new Blob([data], { type: data.type });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');

    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    a.remove();
  };

  const onImportConsiderationsAsync = async (file: File) => {
    if (!pcmStore.body.id) return;

    const { data, status } = await principleService.importConsiderationsAsync(pcmStore.body.id, file);

    if (status === HttpStatusCode.Ok) {
      body.value = {
        ...body.value,
        perfSupportDataDetails: data.perfSupportDataDetails,
        roiLoanAndDepositSummaries: data.roiLoanAndDepositSummaries,
        roiPerfResults: data.roiPerfResults,
      };

      return ToastHelper.success('นำเข้าเอกสาร', 'นำเข้าเอกสารสำเร็จ');
    }
  };

  const onImportAnalysisBuildingAsync = async (file: File) => {
    if (!pcmStore.body.id) return;

    const { data, status } = await principleService.importAnalysisBuildingAsync(pcmStore.body.id, file);

    if (status === HttpStatusCode.Ok) {
      body.value = {
        ...body.value,
        rentalAnalyses: data.rentalAnalyses,
        analysisSummaryNpv: data.analysisSummaryNpv,
        analysisSummaryPaybackYearPeriod: data.analysisSummaryPaybackYearPeriod,
        analysisSummaryDiscountedPaybackYearPeriod: data.analysisSummaryDiscountedPaybackYearPeriod,
      };

      return ToastHelper.success('นำเข้าเอกสาร', 'นำเข้าเอกสารสำเร็จ');
    }
  };

  const onExportAnalysisAsync = async (id: string | undefined) => {
    if (!pcmStore.body.id) return;

    const { data, status } = await principleService.exportAnalysisAsync(pcmStore.body.id, id);

    if (status !== HttpStatusCode.Ok) {
      ToastHelper.error('ดึงข้อมูลเอกสาร', 'ไม่พบเอกสารที่ต้องการ');

      return;
    }

    const fileName = `analysis--${new Date().toDateString()}.xlsx`;

    const blob = new Blob([data], { type: data.type });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');

    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    a.remove();
  };

  const canRestoreVersion = computed(() => {
    return canEdit.value && (body.value.documentVersions?.length ?? 0) > 1;
  });

  const resetDocumentAsync = async (): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const { status } = await principleService.resetDocumentAsync(pcmStore.body.id, body.value.id);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
    }
  };

  return {
    positionOnBoardDDL,
    docuementTemplateDDL,
    departmentDDL,
    budgetTypeDDL,
    accountCodeDDL,
    body,
    status: {
      canEdit,
      canAssignAndConfirm,
      canRecall,
      canUnitApprove,
      canComment,
      canAssignee,
      canApprove,
      isLastApprove,
      canRestoreVersion,
      canDirectorAssign,
      canAssigneesAssign,
    },
    onGetPsoDDLAsync,
    onGetDocumentTemplateDDLAsync,
    getDepartmentDropdownAsync,
    getBudgetTypeDropdownAsync,
    getAccountCodeDropdownAsync,
    posOnBardNonCommitteeDDL,
    createAsync,
    updateAsync,
    getByIdAsync,
    onGetJorPorAsync,
    onResetBody,
    approveAsync,
    rejectAsync,
    commentAsync,
    assigneeRejectAsync,
    getReviewDocumentAsync,
    unitAppoveAsync,
    unitRejectAsync,
    onExportConsiderationAsync,
    onImportConsiderationsAsync,
    onExportAnalysisAsync,
    onImportAnalysisBuildingAsync,
    getOperDefaultDepartmentApproveAsync,
    getDefaultApproverAsync,
    defaultAcceptorAsync,
    resetDocumentAsync,
  }
});

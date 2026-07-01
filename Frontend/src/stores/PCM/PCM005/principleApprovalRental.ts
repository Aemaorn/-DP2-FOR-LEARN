import type { Budget, BudgetDetail, Entrepreneurs, EntrepreneursPriceDetailBody, PerfSupportData, PrincipleApprovalRentalBody, RoiPerfResults } from "@/models/PCM/PCM005/principleApprovalRental";
import type { Option } from "@/models/shared/option";
import { EGroupCode, EntrepreneurType, OrganizationLevelEnum } from "@/enums/shared";
import { defineStore } from "pinia";
import { computed, ref, type Ref } from "vue";
import { usePcm005DetailStore } from "./pcm005";
import { HttpStatusCode } from "axios";
import { PrincipleApprovalRentalStatus } from "@/enums/PCM005/principleApprovalRental";
import { AcceptorStatus, AcceptorType, AssigneeType } from "@/enums/participants";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import { useAuthenticationStore } from "@/stores/authentication";
import { showReasonDialogAsync } from "@/helpers/dialog";
import { ReasonDialogType } from "@/enums/dialog";
import principleApprovalRentalService from "@/services/PCM/PCM005/principleApprovalRental";
import SharedService from "@/services/Shared/dropdown";
import ToastHelper from "@/helpers/toast";
import principleService from "@/services/PCM/PCM005/principle";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import { SectionProcessType } from "@/enums/operations";
import { SupplyMethodCode } from "@/enums/supplyMethod";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import operationService from "@/services/Shared/operations";
import type { EntrepreneurAttachments } from "@/models/shared/uploadFile";
import { CommitteePositions } from "@/enums/PCM005/principle";

const getParameterDDLAsync = async (value: Ref<Option[]>, group: EGroupCode) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(group, undefined, true);

  if (status === HttpStatusCode.Ok) {
    value.value = data;
  }
};

export const usePcm005PrinApproveRentStore = defineStore('pcm005-prin-approve-rent-store', () => {
  const authStore = useAuthenticationStore();
  const pcmStore = usePcm005DetailStore();

  const body = ref<PrincipleApprovalRentalBody>({
    budgets: [{
      sequence: 1,
      description: pcmStore.body.planName || '', details: [] as BudgetDetail[]
    }] as Budget[],
    perfSupportData: {} as PerfSupportData,
    entrepreneurs: [] as Entrepreneurs[],
    status: PrincipleApprovalRentalStatus.Draft,
  } as PrincipleApprovalRentalBody);
  const departmentDDL = ref<Option[]>([]);
  const budgetTypeDDL = ref<Option[]>([]);
  const accountCodeDDL = ref<Option[]>([]);
  const parcelUnitDDL = ref<Option[]>([]);
  const vatTypeDDL = ref<Option[]>([]);

  const canEdit = computed(() => {
    return [PrincipleApprovalRentalStatus.Draft, PrincipleApprovalRentalStatus.Edit, PrincipleApprovalRentalStatus.Rejected].includes(body.value.status) && body.value.hasPermission;
  });

  const canRestoreDocumentVersion = computed(() => {
    return canEdit.value && (body.value.documentVersions?.length ?? 0) > 1;
  });

  const canRestoreWinnerDocumentVersion = computed(() => {
    return canEdit.value && (body.value.winnerDocumentVersions?.length ?? 0) > 1;
  });

  const isCanSetDuties = computed(() => {
    const isRentalCommittee = body.value.acceptors.find(a => a.acceptorType === AcceptorType.RentCommittee && a.userId === authStore.profile.id);

    return canEdit.value || (body.value.status === PrincipleApprovalRentalStatus.WaitingCommitteeApproval && isRentalCommittee && !isRentalCommittee.isUnableToPerformDuties);
  });

  const canCommitteeRecall = computed(() => {
    if (!body.value.acceptors) return false;

    const status = body.value.status === PrincipleApprovalRentalStatus.WaitingCommitteeApproval;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.RentCommittee && a.committeePositionsCode == CommitteePositions.PosBoard001);

    const isCommittee = body.value.acceptors.some(s => s.acceptorType === AcceptorType.RentCommittee && s.userId === authStore.profile.id);

    return status && !isAnyApprovalAndRejected && isCommittee;
  });

  const canRecall = computed(() => {
    if (!body.value.assignees) return false;

    const status = body.value.status === PrincipleApprovalRentalStatus.WaitingAcceptance;
    const checkUser = body.value.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    if (!body.value.acceptors) return false;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.Approver);

    return status && checkUser && !isAnyApprovalAndRejected;
  });


  const canCommitteeApprove = computed(() => {
    const status = body.value.status === PrincipleApprovalRentalStatus.WaitingCommitteeApproval;
    const checkQue = body.value.acceptors?.find(a => a.acceptorType === AcceptorType.RentCommittee && a.userId === authStore.profile.id && !a.isUnableToPerformDuties)?.isCurrent;

    return status && checkQue;
  });

  const canDirectorAssign= computed(() => {
    const status = [PrincipleApprovalRentalStatus.WaitingAssign, PrincipleApprovalRentalStatus.RejectToAssignee].includes(body.value.status);
    const canConfirm = body.value.assignees?.some(a => a.assigneeType === AssigneeType.Director && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canConfirm;
  });

  const canAssignAndConfirm = computed(() => {
    const status = [PrincipleApprovalRentalStatus.WaitingAssign, PrincipleApprovalRentalStatus.RejectToAssignee].includes(body.value.status);
    const canConfirm = body.value.assignees?.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canConfirm;
  });

  const canAssigneesAssign = computed(() => {
    const status = [PrincipleApprovalRentalStatus.WaitingAssign, PrincipleApprovalRentalStatus.RejectToAssignee].includes(body.value.status);
    const canConfirm = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canConfirm;
  });

  const canAssignees= computed(() => {
    const status = body.value.status === PrincipleApprovalRentalStatus.WaitingComment;
    const canComment = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canComment;
  });

  const canComment = computed(() => {
    const status = body.value.status === PrincipleApprovalRentalStatus.WaitingComment;
    if (!body.value.assignees) return false;

    const lastAssignee = body.value.assignees?.reduce((prev, curr) => curr.sequence > prev.sequence ? curr : prev, body.value.assignees[0]);
    const hasPermissionUser = (lastAssignee?.delegateeUserId ? lastAssignee?.delegateeUserId : lastAssignee?.userId) === authStore.profile.id;

    return status && hasPermissionUser;
  });

  const isLastApprove = computed(() => {
    const status = body.value.status === PrincipleApprovalRentalStatus.WaitingAcceptance;
    if (!body.value.acceptors) return false;

    const isLast = body.value.acceptors.filter(a => a.acceptorType === AcceptorType.Approver && a.status == AcceptorStatus.Pending).length === 1;

    return status && isLast;
  })

  const canApprove = computed(() => {
    if (!body.value.acceptors) return false;
    const status = body.value.status === PrincipleApprovalRentalStatus.WaitingAcceptance;
    const checkQue = isCurrentPendingAcceptor(body.value.acceptors, authStore.profile.id, AcceptorType.Approver);
    return status && checkQue;
  });

  const canContractAssign = computed(() => {
    const status = [PrincipleApprovalRentalStatus.Approved, PrincipleApprovalRentalStatus.WaitingContractAssign].includes(body.value.status);
    const canAssign = body.value.assignees?.some(a => a.assigneeType === AssigneeType.Director && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canAssign;
  });

  const canAssignedApprove = computed((): boolean => {
    return body.value.status == PrincipleApprovalRentalStatus.ContractAssigned && body.value.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) == authStore.profile.id) && pcmStore.body.purchaseOrderApproval?.status !== "Assigned";
  });

  const getBudgetTypeDropdownAsync = async (): Promise<void> => {
    await getParameterDDLAsync(budgetTypeDDL, EGroupCode.BudgetTyp);
  };

  const getAccountCodeDropdownAsync = async (): Promise<void> => {
    await getParameterDDLAsync(accountCodeDDL, EGroupCode.GLAcc);
  };

  const getParcelUnitDDLAsync = async (): Promise<void> => {
    await getParameterDDLAsync(parcelUnitDDL, EGroupCode.UnitOfMea);
  };

  const getVatTypeDDLAsync = async (): Promise<void> => {
    await getParameterDDLAsync(vatTypeDDL, EGroupCode.VATType);
  };

  const getDepartmentDropdownAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

    if (status === HttpStatusCode.Ok) {
      departmentDDL.value = data;
    }
  };

  const isSetDutiesStatusAsync = async (flag: boolean, acceptorId?: string, remark?: string) => {
    if (!acceptorId) return;

    const { status } = await principleApprovalRentalService.setDutyAsync(pcmStore.body.id!, body.value.id!, { acceptorId: acceptorId, isUnableToPerformDuties: flag, remark: remark });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
    }

    await getByIdAsync(body.value.id);
  };

  const getByIdAsync = async (id?: string): Promise<void> => {
    if (!pcmStore.body.id) return;

    const { data, status } = await principleApprovalRentalService.getByIdAsync(pcmStore.body.id, id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;

      if (body.value.budgets && body.value.budgets.length === 0) {
        body.value.budgets = [{ sequence: 1, description: pcmStore.body.planName ?? '', details: [{ sequence: 1 }] as BudgetDetail[] }] as Budget[];
      }

      const hasAssignee = body.value.assignees?.some(a => a.assigneeType === AssigneeType.Assignee);
      const hasApprover = body.value.acceptors?.some(a => a.acceptorType === AcceptorType.Approver);

      if (hasAssignee && !hasApprover) {
        await getDefaultApproverAsync();
      }
    }
  };

  const updateAsync = async (statusBody?: PrincipleApprovalRentalStatus): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const mockbody = {
      ...body.value,
      status: statusBody ?? body.value.status,
    }

    const { status, data } = await principleApprovalRentalService.updateAsync(pcmStore.body.id, mockbody);

    if (status === HttpStatusCode.Ok) {
      if (data?.newApprovalDocumentFileId) {
        body.value.documentId = data.newApprovalDocumentFileId;
      }
      if (data?.newWinnerDocumentFileId) {
        body.value.winnerDocumentId = data.newWinnerDocumentFileId;
      }
      await getByIdAsync(body.value.id);

      switch (statusBody) {
        case PrincipleApprovalRentalStatus.Draft:
          return ToastHelper.updatedMessageToast();
        case PrincipleApprovalRentalStatus.WaitingCommitteeApproval:
          return ToastHelper.sendApproveMessageToast();
        case PrincipleApprovalRentalStatus.Edit:
          return ToastHelper.recallEditMessageToast();
        case PrincipleApprovalRentalStatus.WaitingComment:
          return ToastHelper.assignedMessageToast();
        case PrincipleApprovalRentalStatus.WaitingAcceptance:
          return ToastHelper.sendApproveConfirmMessageToast();
        case PrincipleApprovalRentalStatus.WaitingContractAssign:
          return ToastHelper.updatedMessageToast();
        case PrincipleApprovalRentalStatus.ContractAssigned:
          await pcmStore.getDetailAsync(pcmStore.body.id);
          return ToastHelper.assignedMessageToast();
        default:
          return ToastHelper.updatedMessageToast();
      }
    }
  };

  const committeeApproveAsync = async () => {
    if (!pcmStore.body.id || !body.value.id) return;

    const confirmRes = await showReasonDialogAsync(ReasonDialogType.Accepted);

    if (!confirmRes.isConfirm) return;

    const { status } = await principleApprovalRentalService.approveAsync(pcmStore.body.id, body.value.id, confirmRes.reason);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);

      return ToastHelper.approvedMessageToast();
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
      processType: SectionProcessType.RentalApproval,
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
        } as ParticipantsCommitteeAcceptor)
      });
    }
  };

  const approveAsync = async (): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const confirmRes = await showReasonDialogAsync(isLastApprove.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (!confirmRes.isConfirm) return;

    const { status } = await principleApprovalRentalService.approveAsync(pcmStore.body.id, body.value.id, confirmRes.reason);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);

      return ToastHelper.approvedMessageToast();
    }
  };

  const rejectAsync = async (isCommitee = false): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const confirmRes = await showReasonDialogAsync(isCommitee ? ReasonDialogType.NotAgree : ReasonDialogType.Reject, true);

    if (!confirmRes.isConfirm) return;

    const { status } = await principleApprovalRentalService.rejectAsync(pcmStore.body.id, body.value.id, confirmRes.reason);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);

      return isCommitee ? ToastHelper.notAgreeMessageToast() : ToastHelper.sendEditMessageToast();
    }
  };

  const assigneeRejectAsync = async (): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const resp = await showReasonDialogAsync(ReasonDialogType.Reject);

    if (!resp.isConfirm) return;

    const { status } = await principleApprovalRentalService.rejectAsync(pcmStore.body.id, body.value.id, resp.reason);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);

      return ToastHelper.sendEditMessageToast();
    }
  };

  const commentAsync = async (remark: string): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const { status } = await principleApprovalRentalService.commentAsync(pcmStore.body.id, body.value.id, remark);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);

      return ToastHelper.remarkOfficerMessageToast();
    }
  };

  const createEntrepreneurAsync = (entreBody: Entrepreneurs): boolean => {
    if (!pcmStore.body.id || !body.value.id) return false;

    body.value.entrepreneurs ??= [];

    body.value.entrepreneurs.push(entreBody);

    return true;
  };

  const createEntrepreneurApiAsync = async (entreBody: Entrepreneurs): Promise<boolean> => {
    if (!pcmStore.body.id || !body.value.id) return false;

    const { status } = await principleApprovalRentalService.createEntrepreneurAsync(pcmStore.body.id, body.value.id, entreBody);

    if (status === HttpStatusCode.Created) {
      await getByIdAsync(body.value.id);
      ToastHelper.createdMessageToast();
      return true;
    }

    return false;
  };

  const updateEntrepreneurAsync = async (
    entreBody: Entrepreneurs,
    message?: string,
    messageDetail?: string,
  ): Promise<boolean> => {
    let res = false;

    if (!pcmStore.body.id || !body.value.id || !entreBody.id) return res;

    const { status } = await principleApprovalRentalService.updateEntrepreneurAsync(pcmStore.body.id, body.value.id, entreBody.id, entreBody);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);

      if (message && messageDetail) {
        ToastHelper.success(message, messageDetail);
      } else {
        ToastHelper.updatedMessageToast();
      }

      res = true;
    }

    if (status === HttpStatusCode.BadRequest) {
      res = false;
    }

    return res;
  };

  const getEntrepreneurAsync = async (id: string) => {
    if (!pcmStore.body.id || !body.value.id) return;

    const { data, status } = await principleApprovalRentalService.getEntrepreneurAsync(pcmStore.body.id, body.value.id, id);

    if (status === HttpStatusCode.Ok) {
      return data;
    }
  };

  const getPriceDetailAsync = async (id: string) => {
    if (!pcmStore.body.id || !body.value.id) return;

    const { data, status } = await principleApprovalRentalService.getPriceDetailAsync(pcmStore.body.id, body.value.id, id);

    if (status === HttpStatusCode.Ok) {
      return data;
    }
  };

  const createPriceDetailAsync = async (id: string, bodyData: EntrepreneursPriceDetailBody) => {
    if (!pcmStore.body.id || !body.value.id) return;

    let res = false;

    const { status } = await principleApprovalRentalService.createPriceDetailAsync(pcmStore.body.id, body.value.id, id, bodyData);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);

      ToastHelper.success('บันทึกรายละเอียดสัญญา', 'บันทึกรายละเอียดสัญญาสำเร็จ');

      res = true;
    }

    return res;
  };

  const getReviewDocumentAsync = async (id: string, documentType: string) => {
    if (!pcmStore.body.id) return;

    const { data, status } = await principleApprovalRentalService.getReviewDocumentAsync(pcmStore.body.id, id, documentType);

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
        roiPerfResults: data.roiPerfResults as unknown as RoiPerfResults[],
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
  }

  const onUpsertAttachments = async (
    id: string,
    type: EntrepreneurType,
    attachments: EntrepreneurAttachments[]
  ) => {
    const vendorData = body.value.entrepreneurs.find(x => x.id === id);
    if (!vendorData?.id) return;

    const otherTypeAttachments =
      vendorData.attachments?.map(a => ({
        ...a,
        fileAttachments: a.fileAttachments.filter(f => f.type !== type)
      })).filter(a => a.fileAttachments.length > 0) ?? [];

    const newAttachments =
      attachments
        ?.map(att => ({
          ...att,
          fileAttachments: att.fileAttachments?.map(f => ({ ...f, type })) ?? []
        }))
        .filter(att => att.fileAttachments.length > 0) ?? [];

    vendorData.attachments = [...otherTypeAttachments, ...newAttachments];

    const { status } = await principleApprovalRentalService.onUpsertAttachmentsAsync(
      vendorData.id,
      vendorData.attachments
    );

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
    }
  };

  const resetDocumentAsync = async (documentType: 'Approval' | 'Winner'): Promise<void> => {
    if (!pcmStore.body.id || !body.value.id) return;

    const { status } = await principleApprovalRentalService.resetDocumentAsync(pcmStore.body.id, body.value.id, documentType);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(body.value.id);
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
    }
  };

  return {
    body,
    departmentDDL,
    budgetTypeDDL,
    accountCodeDDL,
    parcelUnitDDL,
    vatTypeDDL,
    status: {
      canEdit,
      canAssignAndConfirm,
      canRecall,
      canCommitteeApprove,
      canComment,
      canAssignees,
      canApprove,
      canContractAssign,
      isLastApprove,
      isCanSetDuties,
      canAssignedApprove,
      canCommitteeRecall,
      canRestoreDocumentVersion,
      canRestoreWinnerDocumentVersion,
      canDirectorAssign,
      canAssigneesAssign,
    },
    getBudgetTypeDropdownAsync,
    getAccountCodeDropdownAsync,
    getDepartmentDropdownAsync,
    getParcelUnitDDLAsync,
    getVatTypeDDLAsync,
    getByIdAsync,
    updateAsync,
    approveAsync,
    committeeApproveAsync,
    rejectAsync,
    commentAsync,
    createEntrepreneurAsync,
    createEntrepreneurApiAsync,
    updateEntrepreneurAsync,
    getEntrepreneurAsync,
    getPriceDetailAsync,
    createPriceDetailAsync,
    isSetDutiesStatusAsync,
    assigneeRejectAsync,
    getReviewDocumentAsync,
    getDefaultApproverAsync,
    onExportConsiderationAsync,
    onImportConsiderationsAsync,
    onExportAnalysisAsync,
    onImportAnalysisBuildingAsync,
    onUpsertAttachments,
    resetDocumentAsync,
  }
});

import { CmDisbursementApprovalStatus } from "@/enums/CM/cm004";
import { ReasonDialogType } from "@/enums/dialog";
import { OrganizationLevel } from "@/enums/operations";
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from "@/enums/participants";
import { EGroupCode, EWorkProcess, OrganizationLevelEnum } from "@/enums/shared";
import { showReasonDialogAsync } from "@/helpers/dialog";
import ToastHelper from "@/helpers/toast";
import type { GetListResponse } from "@/models/CM/cm";
import type { Cm004Criteria, Cm004Detail, Cm004DisbursementBody } from "@/models/CM/cm004";
import type { OperationBody } from "@/models/shared/operations";
import type { Option } from "@/models/shared/option";
import type { ParticipantsAcceptor, ParticipantsAssignee } from "@/models/shared/participants";
import router from "@/router";
import cm004Service from "@/services/CM/cm004";
import SharedService from "@/services/Shared/dropdown";
import operationService from "@/services/Shared/operations";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { useAuthenticationStore } from "../authentication";

export const useCm004ListStore = defineStore('cm004-list-store', () => {
  const criteria = ref<Cm004Criteria>({
    pageNumber: 1,
    pageSize: 10,
    status: 'all',
    workProcess: EWorkProcess.InProcess,
  } as Cm004Criteria);
  const departmentDDL = ref<Option[]>([]);
  const dataList = ref<GetListResponse>({} as GetListResponse);

  const getDepartmentDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department);

    if (status === HttpStatusCode.Ok) {
      departmentDDL.value = data;
    }
  };

  const getListAsync = async (): Promise<void> => {
    const { data, status } = await cm004Service.getListAsync(criteria.value);

    if (status === HttpStatusCode.Ok) {
      dataList.value = data;
    }
  };

  const onChangePageSizeAsync = async (page: number, size: number): Promise<void> => {
    criteria.value = {
      ...criteria.value,
      pageNumber: page,
      pageSize: size,
    }

    await getListAsync();
  };

  const onResetAsync = async (): Promise<void> => {
    criteria.value = {
      pageNumber: 1,
      pageSize: 10,
      status: 'all',
      workProcess: EWorkProcess.InProcess,
    } as Cm004Criteria;

    await getListAsync();
  };

  return {
    departmentDDL,
    criteria,
    dataList,
    getDepartmentDDLAsync,
    getListAsync,
    onChangePageSizeAsync,
    onResetAsync,
  }
});

export const useCm004DetailStore = defineStore('cm004-detail-store', () => {
  const authStore = useAuthenticationStore();
  const detail = ref<Cm004Detail>({} as Cm004Detail);
  const body = ref<Cm004DisbursementBody>({
    acceptors: [] as ParticipantsAcceptor[],
    assignees: [] as ParticipantsAssignee[],
    status: CmDisbursementApprovalStatus.Draft,
  } as Cm004DisbursementBody);

  const assignDepartmentDDL = ref<Option[]>([]);

  const canAssign = computed(() => {
    const status = [CmDisbursementApprovalStatus.Draft].includes(body.value.status) || !body.value.status;
    const canAssign = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id && a.assigneeType === AssigneeType.Director);

    return status && canAssign;
  });

  const canEdit = computed(() => {
    const status = [CmDisbursementApprovalStatus.Rejected, CmDisbursementApprovalStatus.Assigned].includes(body.value.status) || !body.value.status;
    const canEdit = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return (status || canAssign.value) && canEdit;
  });

  const canSendApprove = computed(() => {
    const status = [CmDisbursementApprovalStatus.Rejected, CmDisbursementApprovalStatus.Assigned].includes(body.value.status) || !body.value.status;
    const canApprove = body.value.assignees?.filter(a => a.assigneeType != AssigneeType.Director).some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canApprove;
  });

  const canApprove = computed(() => {
    const status = body.value.status === CmDisbursementApprovalStatus.WaitingApproval;
    const canApprove = body.value.acceptors?.find(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id && a.acceptorType === AcceptorType.Approver)?.isCurrent;

    return status && canApprove;
  });

  const getDetailAsync = async (id: string): Promise<void> => {
    const { data, status } = await cm004Service.getContractAsync(id);

    if (status === HttpStatusCode.Ok) {
      detail.value = data;
    }
  };

  const getDefaultJorporAsync = async (): Promise<void> => {
    const { data, status } = await operationService.getJorPorDirectorAsync();

    if (status === HttpStatusCode.Ok) {
      body.value.assignees.push({
        assigneeGroup: AssigneeGroup.JorPor,
        assigneeType: AssigneeType.Director,
        departmentName: data.businessUnitName,
        fullName: data.fullName,
        positionName: data.fullPositionName,
        sequence: 1,
        status: AssigneeStatus.Draft,
        userId: data.userId,
      } as ParticipantsAssignee);
    }
  };

  const createDisbursementAsync = async (id: string, bodyStatus?: CmDisbursementApprovalStatus): Promise<void> => {
    if (bodyStatus) {
      body.value.status = bodyStatus;
    }

    const { data, status } = await cm004Service.createDisbursementAsync(id, body.value);

    if (status === HttpStatusCode.Created) {
      router.replace(`/cm/cm004/detail/${id}/disbursement/${data}`);

      await getDetailDisbursementAsync(id, data);

      ToastHelper.createdMessageToast();
    }
  };

  const updateDisbursementAsync = async (id: string, bodyStatus?: CmDisbursementApprovalStatus): Promise<void> => {
    if (!body.value.id) return;

    const payload = {
      ...body.value,
      status: bodyStatus ?? body.value.status,
    } as Cm004DisbursementBody;

    const { status, data } = await cm004Service.updateDisbursementAsync(id, body.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      if (data?.newDocumentFileId) {
        body.value.documentId = data.newDocumentFileId;
      }
      await getDetailDisbursementAsync(id, body.value.id);

      switch (bodyStatus) {
        case CmDisbursementApprovalStatus.Assigned:
          return ToastHelper.assignedMessageToast();
        case CmDisbursementApprovalStatus.WaitingApproval:
          return ToastHelper.sendApproveMessageToast();
        default:
          return ToastHelper.updatedMessageToast();
      }
    }
  };

  const getDetailDisbursementAsync = async (cm004Id: string, id: string): Promise<void> => {
    const { data, status } = await cm004Service.getDetailDisbursementAsync(cm004Id, id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;
    }
  };

  const onClearDisbursementData = (): void => {
    body.value = {
      acceptors: [] as ParticipantsAcceptor[],
      assignees: [] as ParticipantsAssignee[],
    } as Cm004DisbursementBody;
  };

  const onApproveAsync = async (cm004Id: string, id: string): Promise<void> => {
    const res = await showReasonDialogAsync(isLastApprovalApprover.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (!res.isConfirm) return;

    const { status } = await cm004Service.approveAsync(cm004Id, id, res.reason);

    if (status === HttpStatusCode.Ok) {
      await getDetailDisbursementAsync(cm004Id, id);

      return ToastHelper.approvedMessageToast();
    }
  };

  const onRejectAsync = async (cm004Id: string, id: string): Promise<void> => {
    const reason = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (!reason.isConfirm) return;

    const { status } = await cm004Service.rejectAsync(cm004Id, id, reason.reason);

    if (status === HttpStatusCode.Ok) {
      await getDetailDisbursementAsync(cm004Id, id);

      return ToastHelper.sendEditMessageToast();
    }
  };

  const getReviewDocumentAsync = async (cm004Id: string, id: string): Promise<string> => {
    const { data, status } = await cm004Service.getReviewDocumentAsync(cm004Id, id);

    if (status !== HttpStatusCode.Ok) {
      return '';
    }

    return data;
  };

  const isLastApprovalApprover = computed(() => {
    const approvalUser = body.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending && f.acceptorType === AcceptorType.Approver);

    if (!approvalUser) {
      return false;
    }

    const current = approvalUser.find(f => f.isCurrent);

    if (!current) {
      return false;
    }

    return canApprove.value && current.sequence === approvalUser[approvalUser.length - 1].sequence;
  });

  const canRestoreVersion = computed(() => canEdit.value);

  const onUpsertAttachments = async (cm004Id: string, id: string) => {
    if (!body.value.id) return;

    const { status } = await cm004Service.attachmentsAsync(cm004Id, id, body.value.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await getDetailDisbursementAsync(cm004Id, id);;
    }
  };

  const getAssignDepartmentDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.AssignDept);

    if (status === HttpStatusCode.Ok) {
      assignDepartmentDDL.value = data;
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

  const getDefaultApproverAsync = async () => {
    const lastAssignee = body.value.assignees.filter(f => f.assigneeType === AssigneeType.Assignee).reduce((prev, current) =>
      prev.sequence > current.sequence ? prev : current,
      body.value.assignees.filter(f => f.assigneeType === AssigneeType.Assignee)[0]);

    if(!lastAssignee) return

    const { data, status } = await operationService.getDefaultDepartmentApproverByUserIdAsync(lastAssignee.userId, OrganizationLevel.Department);

    if (status === HttpStatusCode.Ok && data.length > 0) {
      body.value.acceptors = [];
      data.forEach((item) => {
        body.value.acceptors.push({
          acceptorType: AcceptorType.Approver,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: body.value.acceptors.length + 1,
          status: AcceptorStatus.Draft,
          userId: item.userId,
          departmentName: item.businessUnitName,
        } as ParticipantsAcceptor)
      });
    }
  };

  const resetDocumentAsync = async (contractVendorId: string, id: string): Promise<void> => {
    const { status } = await cm004Service.resetDocumentAsync(contractVendorId, id);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
      await getDetailDisbursementAsync(contractVendorId, id);
    }
  };

  return {
    detail,
    body,
    status: {
      canAssign,
      canEdit,
      canSendApprove,
      canApprove,
      isLastApprovalApprover,
      canRestoreVersion,
    },
    onUpsertAttachments,
    getReviewDocumentAsync,
    getDetailAsync,
    getDefaultJorporAsync,
    createDisbursementAsync,
    updateDisbursementAsync,
    getDetailDisbursementAsync,
    onClearDisbursementData,
    onApproveAsync,
    onRejectAsync,
    assignDepartmentDDL,
    getAssignDepartmentDDLAsync,
    getDefaultSegmentOtherManagerApproverAsync,
    getDefaultSegmentITManagerApproverAsync,
    getDefaultApproverAsync,
    resetDocumentAsync,
  }
});

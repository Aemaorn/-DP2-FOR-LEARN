import type { Cm005Detail, Cm005ListCriteria, ContractTermination, ContractVendorData, ContractVendorListCriteria, GetCm005ListResponse, StatusCount } from "@/models/CM/cm005";
import type { ParticipantsAcceptor, ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import { SectionProcessType } from "@/enums/operations";
import type { Option, OptionBadge } from "@/models/shared/option";
import { CmContractTerminationStatus } from "@/enums/CM/cm005";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { showReasonDialogAsync } from "@/helpers/dialog";
import { ReasonDialogType } from "@/enums/dialog";
import { useAuthenticationStore } from "../authentication";
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from "@/enums/participants";
import { EGroupCode, EWorkProcess, OrganizationLevelEnum } from "@/enums/shared";
import { souceType } from "@/enums/CM/cm001";
import cm005Service from "@/services/CM/cm005";
import ToastHelper from "@/helpers/toast";
import router from "@/router";
import operationService from "@/services/Shared/operations";
import SharedService from "@/services/Shared/dropdown";
import cm005Constant from "@/constants/CM/cm005";
import { CommitteePositions } from "@/enums/PCM005/principle";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import type { Attachments } from "@/models/shared/uploadFile";
import type { Ref } from "vue";

const getSupplyMethodAsync = async (groupCode: EGroupCode, target: Ref<Option[]>, parentCode?: string) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(groupCode, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getDepartmentAsync = async (target: Ref<Option[]>): Promise<void> => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

export const useContractDialogStore = defineStore('contract-dialog-store', () => {
  const initCriteria: ContractVendorListCriteria = {
    pageNumber: 1,
    pageSize: 10,
  };

  const sourceTypeDropdown: Array<Option> = [
    { label: 'ทำสัญญา (41 / 30)', value: souceType.ContractDraftVendor },
    { label: 'ไม่ทำสัญญา (40) / อื่นๆ', value: souceType.Procurement },
    { label: 'แก้ไขสัญญา', value: souceType.ContractDraftVendorEdit },
  ];

  const dialogCriteria = ref<ContractVendorListCriteria>(structuredClone(initCriteria));
  const dialogDataList = ref<{ data: ContractVendorData[]; totalRecords: number }>({ data: [], totalRecords: 0 });
  const departmentDropdown = ref<Array<Option>>([]);
  const supplyMethodCodeDropdown = ref<Array<Option>>([]);
  const supplyMethodTypeCodeDropdown = ref<Array<Option>>([]);
  const supplyMethodSpecialTypeCodeDropdown = ref<Array<Option>>([]);

  const getContractListAsync = async (): Promise<void> => {
    const { data, status } = await cm005Service.getContractVendorListAsync(dialogCriteria.value);

    if (status === HttpStatusCode.Ok) {
      dialogDataList.value = data;
    }
  };

  const getDepartmentDropdownAsync = async () => {
    await getDepartmentAsync(departmentDropdown);
  };

  const getSupplyMethodDropdownAsync = async () => {
    await getSupplyMethodAsync(EGroupCode.SMethod, supplyMethodCodeDropdown);
  };

  const getSupplyMethodTypeDropdownAsync = async (parentCode?: string) => {
    supplyMethodTypeCodeDropdown.value = [];
    dialogCriteria.value.supplyMethodTypeCode = undefined;
    dialogCriteria.value.supplyMethodSpecialTypeCode = undefined;

    await getSupplyMethodAsync(EGroupCode.SMethodType, supplyMethodTypeCodeDropdown, parentCode);
  };

  const getSupplyMethodSpecialTypeDropdownAsync = async (parentCode?: string) => {
    supplyMethodSpecialTypeCodeDropdown.value = [];
    dialogCriteria.value.supplyMethodSpecialTypeCode = undefined;

    await getSupplyMethodAsync(EGroupCode.SMethod, supplyMethodSpecialTypeCodeDropdown, parentCode);
  };

  const resetCriteria = (): void => {
    dialogCriteria.value = structuredClone(initCriteria);
  };

  const resetData = (): void => {
    dialogCriteria.value = structuredClone(initCriteria);
    dialogDataList.value = { data: [], totalRecords: 0 };
  };

  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    dialogCriteria.value = { ...dialogCriteria.value, pageNumber, pageSize };
  };

  return {
    dialogCriteria,
    dialogDataList,
    sourceTypeDropdown,
    departmentDropdown,
    supplyMethodCodeDropdown,
    supplyMethodTypeCodeDropdown,
    supplyMethodSpecialTypeCodeDropdown,
    getContractListAsync,
    getDepartmentDropdownAsync,
    getSupplyMethodDropdownAsync,
    getSupplyMethodTypeDropdownAsync,
    getSupplyMethodSpecialTypeDropdownAsync,
    resetCriteria,
    resetData,
    onChangePageSize,
  }
});

export const useCM005ListStore = defineStore('use-cm005-list-store', () => {
  const { cm005ColorClass } = cm005Constant;

  const criteria = ref<Cm005ListCriteria>({
    pageNumber: 1,
    pageSize: 10,
    workProcess: EWorkProcess.InProcess,
    status: CmContractTerminationStatus.All,
  } as Cm005ListCriteria);

  const badgeOptions = ref<Array<OptionBadge>>([]);

  const contractTypeDDL = ref<Option[]>([]);
  const dataList = ref<GetCm005ListResponse>({} as GetCm005ListResponse);

  const getContractTypeDDLAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CMType);

    if (status === HttpStatusCode.Ok) {
      contractTypeDDL.value = data;
    }
  };

  const getListAsync = async (): Promise<void> => {
    const { data, status } = await cm005Service.getListAsync(criteria.value);

    if (status === HttpStatusCode.Ok) {
      dataList.value = data;
      mapBadge(data.statusCount);
    }
  };

  const mapBadge = (count: StatusCount) => {
    badgeOptions.value = Object.entries(CmContractTerminationStatus)
      .filter(([, val]) => val !== CmContractTerminationStatus.RejectToAssignee)
      .map(([, value]): OptionBadge => {
        const { bgColorClass, textColorClass } = cm005ColorClass(value);
        const statusKey = value.toLowerCase();
        const statusCountLower = Object.fromEntries(
          Object.entries(count).map(([key, val]) => [key.toLowerCase(), val]))

        return {
          bgColorClass,
          textColorClass,
          count: statusCountLower[statusKey] ?? 0,
          label: cm005Constant.cm005StatusName(value),
          value: value,
        } as OptionBadge;
      });
  };

  const onClearCriteriaAsync = async (): Promise<void> => {
    criteria.value = {
      pageNumber: 1,
      pageSize: 10,
      workProcess: EWorkProcess.InProcess,
    } as Cm005ListCriteria;

    await getListAsync();
  };

  const onChangePageSizeAsync = async (pageNumber: number, pageSize: number): Promise<void> => {
    criteria.value = {
      ...criteria.value,
      pageNumber,
      pageSize,
    };

    await getListAsync();
  };

  return {
    criteria,
    contractTypeDDL,
    dataList,
    badgeOptions,
    getContractTypeDDLAsync,
    getListAsync,
    onClearCriteriaAsync,
    onChangePageSizeAsync,
  }
});

export const useCm005DetailStore = defineStore('use-cm005-detail-store', () => {
  const authStore = useAuthenticationStore();
  const body = ref<Cm005Detail>({
    contractTermination: {
      acceptors: [] as ParticipantsAcceptor[],
      assignees: [] as ParticipantsAssignee[],
      attachments: [] as Attachments[],
      isProposedApprover: false,
      status: CmContractTerminationStatus.Draft,
    }
  } as Cm005Detail);

  const ctrDropdown = ref<Option[]>([]);

  const canCommitteeRecall = computed(() => {
    if (!body.value.contractTermination.acceptors) return false;

    const status = body.value.contractTermination.status === CmContractTerminationStatus.WaitingCommitteeApproval;

    const isAnyApprovalAndRejected = body.value.contractTermination.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status)
      && a.acceptorType === AcceptorType.AcceptanceCommittee
      && a.committeePositionsCode == CommitteePositions.PosBoard001);

    const isCommittee = body.value.contractTermination.acceptors.some(s => s.acceptorType === AcceptorType.AcceptanceCommittee && s.userId === authStore.profile.id);

    return status && !isAnyApprovalAndRejected && isCommittee;
  });

  const canRecall = computed(() => {
    if (!body.value.contractTermination.assignees) return false;

    const status = body.value.contractTermination.status === CmContractTerminationStatus.WaitingApproval;
    const checkUser = body.value.contractTermination.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    if (!body.value.contractTermination.acceptors) return false;

    const isAnyApprovalAndRejected = body.value.contractTermination.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.Approver);

    return status && checkUser && !isAnyApprovalAndRejected;
  });

  const canEdit = computed(() => {
    const status = [CmContractTerminationStatus.Draft, CmContractTerminationStatus.Rejected].includes(body.value.contractTermination.status);
    const isCommittee = body.value.contractTermination.acceptors.filter(f => f.acceptorType === AcceptorType.AcceptanceCommittee).some(a => a.userId === authStore.profile.id);

    return status && isCommittee;
  });

  const canApproveCommittee = computed(() => {
    const status = body.value.contractTermination.status === CmContractTerminationStatus.WaitingCommitteeApproval;
    const checkQue = body.value.contractTermination.acceptors.find(a => a.acceptorType === AcceptorType.AcceptanceCommittee && a.userId === authStore.profile.id)?.isCurrent;

    return status && checkQue;
  });

  const isCommitteeApproval = computed(() => [CmContractTerminationStatus.WaitingCommitteeApproval].includes(body.value.contractTermination.status) &&
    (body.value.contractTermination.acceptors.some(s => s.acceptorType === AcceptorType.AcceptanceCommittee && s.userId === authStore.profile.id)));


  const canAssign = computed(() => {
    const status = [CmContractTerminationStatus.WaitingAssign, CmContractTerminationStatus.RejectToAssignee].includes(body.value.contractTermination.status);
    const canAssign = body.value.contractTermination.assignees.some(a => a.assigneeType === AssigneeType.Director && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canAssign;
  });

  const canComment = computed(() => {
    const status = body.value.contractTermination.status === CmContractTerminationStatus.WaitingComment;
    const canComment = body.value.contractTermination.assignees.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canComment;
  });

  const canApprove = computed(() => {
    const status = body.value.contractTermination.status === CmContractTerminationStatus.WaitingApproval;
    const canApprove = isCurrentPendingAcceptor(body.value.contractTermination.acceptors, authStore.profile.id, AcceptorType.Approver);

    return status && canApprove;
  });

  const isLast = computed(() => {
    const status = body.value.contractTermination.status === CmContractTerminationStatus.WaitingApproval;
    const canApprove = body.value.contractTermination.acceptors.filter(a => a.acceptorType === AcceptorType.Approver && a.status === AcceptorStatus.Pending).length == 1;

    return status && canApprove;
  })

  const canSaveAcceptors = computed(() => {
    const status = [
      CmContractTerminationStatus.WaitingComment,
      CmContractTerminationStatus.WaitingApproval,
    ].includes(body.value.contractTermination.status);
    const isAssignee = body.value.contractTermination.assignees.some(
      a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id
    );
    return status && isAssignee;
  });

  const canEditAsAssignee = computed(() => {
    const status = [
      CmContractTerminationStatus.WaitingAssign,
      CmContractTerminationStatus.WaitingComment,
    ].includes(body.value.contractTermination.status);
    if (body.value.contractTermination.status === CmContractTerminationStatus.WaitingAssign) {
      return status && body.value.contractTermination.assignees.some(
        a => a.assigneeType === AssigneeType.Director
          && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id
      );
    }
    return status && body.value.contractTermination.assignees.some(
      a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id
    );
  });

  const canRestoreVersion = computed(() => canEdit.value || canEditAsAssignee.value);

  const getDefaultJPAsync = async () => {
    const { data, status } = await operationService.getJorPorDirectorAsync();

    if (status === HttpStatusCode.Ok) {
      body.value.contractTermination.assignees.push({
        assigneeGroup: AssigneeGroup.JorPor,
        assigneeType: AssigneeType.Director,
        departmentName: data.businessUnitName,
        fullName: data.fullName,
        positionName: data.fullPositionName,
        sequence: 1,
        status: AssigneeStatus.Draft,
        userId: data.userId,
      });
    }
  };

  const getCTRDrowndownAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CTR);

    if (status === HttpStatusCode.Ok) {
      ctrDropdown.value = data;
    }
  };

  const createAsync = async (contractId: string): Promise<void> => {
    const { data, status } = await cm005Service.createAsync(contractId, body.value.contractTermination);

    if (status === HttpStatusCode.Created) {
      router.replace(`/cm/cm005/contract/${contractId}/detail/${data}`);

      return ToastHelper.createdMessageToast();
    }
  };

  const updateAsync = async (contractId: string, id: string, bodyStatus?: CmContractTerminationStatus): Promise<void> => {
    const payload = {
      ...body.value.contractTermination,
    } as ContractTermination;

    if (bodyStatus) {
      payload.status = bodyStatus;
    }

    const { status, data } = await cm005Service.updateAsync(contractId, id, payload);

    if (status === HttpStatusCode.Ok) {
      if (data?.newDocumentFileId) {
        body.value.contractTermination.contractTerminationDocumentId = data.newDocumentFileId;
      }
      await getDetailAsync(contractId, id);

      switch (bodyStatus) {
        case CmContractTerminationStatus.WaitingCommitteeApproval:
          return ToastHelper.sendApproveMessageToast();
        case CmContractTerminationStatus.WaitingApproval:
          return ToastHelper.sendConfirmMessageToast();
        default:
          return ToastHelper.updatedMessageToast();
      }
    }
  };

  const getDetailAsync = async (contractId: string, id: string,): Promise<void> => {
    const { data, status } = await cm005Service.getDetailAsync(contractId, id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;

      if (body.value.contractTermination.assignees.length === 0) {
        await getDefaultJPAsync();
      }
    }
  };

  const committeeApproveAsync = async (contractId: string, id: string) => {
    const res = await showReasonDialogAsync(ReasonDialogType.Accepted);

    if (!res.isConfirm) return;

    const { status } = await cm005Service.approveAsync(contractId, id, res.reason);

    if (status === HttpStatusCode.Ok) {
      await getDetailAsync(contractId, id);

      return ToastHelper.approvedMessageToast();
    }
  };

  const approveAsync = async (contractId: string, id: string) => {
    const res = await showReasonDialogAsync(isLast.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (!res.isConfirm) return;

    const { status } = await cm005Service.approveAsync(contractId, id, res.reason);

    if (status === HttpStatusCode.Ok) {
      await getDetailAsync(contractId, id);

      return ToastHelper.approvedMessageToast();
    }
  };

  const rejectAsync = async (contractId: string, id: string) => {
    const res = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (!res.isConfirm) return;

    const { status } = await cm005Service.rejectAsync(contractId, id, res.reason);

    if (status === HttpStatusCode.Ok) {
      await getDetailAsync(contractId, id);

      return ToastHelper.sendEditMessageToast();
    }
  };

  const commentAsync = async (contractId: string, id: string, remark: string) => {
    const { status } = await cm005Service.commentAsync(contractId, id, remark);

    if (status === HttpStatusCode.Ok) {
      await getDetailAsync(contractId, id);

      return ToastHelper.remarkOfficerMessageToast();
    }
  };

  const assigneeDefaultAcceptorAsync = async (): Promise<void> => {
    const assignees = body.value.contractTermination.assignees.filter(f => f.assigneeType === AssigneeType.Assignee);
    const lastAssignee = assignees[assignees.length - 1];

    const params = {
      processType: SectionProcessType.ContractTermination,
      userId: lastAssignee.userId,
      budget: body.value.budget ?? 0,
      supplyMethodCode: body.value.supplyMethodCode,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status === HttpStatusCode.Ok) {
      body.value.contractTermination.acceptors = body.value.contractTermination.acceptors.filter(
        f => f.acceptorType !== AcceptorType.Approver
      );

      data.forEach(item => body.value.contractTermination.acceptors.push({
        acceptorType: AcceptorType.Approver,
        fullName: item.fullName,
        positionName: item.fullPositionName,
        sequence: body.value.contractTermination.acceptors.filter(f => f.acceptorType === AcceptorType.Approver).length + 1,
        status: AcceptorStatus.Draft,
        userId: item.userId,
        departmentName: item.businessUnitName,
        isUnableToPerformDuties: false,
      } as ParticipantsCommitteeAcceptor));
    }
  };

  const onResetData = (): void => {
    body.value = {
      contractTermination: {
        acceptors: [] as ParticipantsAcceptor[],
        assignees: [] as ParticipantsAssignee[],
      }
    } as Cm005Detail;
  };

  const onUpdateDutiesStatusAsync = async (contractId: string, id: string, flag: boolean, remark?: string, acceptorId?: string) => {
    if (!acceptorId) return;

    const { status } = await cm005Service.setDutyAsync(contractId, id, { acceptorId: acceptorId, isUnableToPerformDuties: flag, remark: remark });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
    }

    await getDetailAsync(contractId, id);
  };

  const resetDocumentAsync = async (contractId: string, id: string): Promise<void> => {
    const { status } = await cm005Service.resetDocumentAsync(contractId, id);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
      await getDetailAsync(contractId, id);
    }
  };

  const onSetIsProposedApproverAsync = async (contractId: string, id: string, isProposedApprover: boolean): Promise<void> => {
    const { status } = await cm005Service.setIsProposedApproverAsync(contractId, id, isProposedApprover);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('เสนอผู้มีอำนาจเห็นชอบ/อนุมัติ', 'แก้ไขการเสนอผู้มีอำนาจเห็นชอบ/อนุมัติสำเร็จ');
    }
  };

  const onUpsertAttachments = async (contractId: string, id: string) => {
    if (!body.value.contractTermination.id) return;

    const { status } = await cm005Service.onUpsertAttachmentsAsync(contractId, id, body.value.contractTermination.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await getDetailAsync(contractId, id);
    }
  };

  return {
    body,
    ctrDropdown,
    status: {
      canEdit,
      canApproveCommittee,
      canAssign,
      canComment,
      canApprove,
      isLast,
      canCommitteeRecall,
      canRecall,
      isCommitteeApproval,
      canRestoreVersion,
      canSaveAcceptors,
      canEditAsAssignee,
    },
    getDefaultJPAsync,
    createAsync,
    updateAsync,
    getDetailAsync,
    approveAsync,
    rejectAsync,
    commentAsync,
    onResetData,
    committeeApproveAsync,
    onUpdateDutiesStatusAsync,
    resetDocumentAsync,
    onSetIsProposedApproverAsync,
    getCTRDrowndownAsync,
    onUpsertAttachments,
    assigneeDefaultAcceptorAsync,
  }
});

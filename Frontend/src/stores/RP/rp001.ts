import rp001Constant from "@/constants/RP/rp001";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import { AcceptorStatus, AcceptorType } from "@/enums/participants";
import { ProcurementWorkProcess } from "@/enums/procurement";
import { ContractType, rp001Status } from "@/enums/RP/rp001";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import ToastHelper from "@/helpers/toast";
import type { rp001Body, rp001Criteria, rp001Detail, rp001ListResponse } from "@/models/RP/rp001";
import type { OptionBadge } from "@/models/shared/option";
import type { ParticipantsAcceptor } from "@/models/shared/participants";
import type { ContractTypeCount, StatusCount } from "@/models/shared/status";
import rp001Service from "@/services/RP/rp001Service";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { useAuthenticationStore } from "../authentication";
import { ArrayHelper } from "@/helpers/array";
import { setEndDate, setStartDate } from "@/helpers/dateTime";
import type { Attachments } from "@/models/shared/uploadFile";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import operationService from "@/services/Shared/operations";
import { SectionProcessType } from "@/enums/operations";
import { SupplyMethodCode } from "@/enums/supplyMethod";

const { rp001StatusName, rp001StatusColor, rp001ContractName, rp001ContractColor } = rp001Constant;

const authStore = useAuthenticationStore();

export const useRp001ListStore = defineStore("rp001-list-store", () => {
  const criteria = ref<rp001Criteria>({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
    workProcess: ProcurementWorkProcess.InProcess,
    status: rp001Status.All,
  });

  const clearCriteria = async () => {
    criteria.value = {
      pageNumber: 1,
      pageSize: 10,
      sort: [],
      workProcess: ProcurementWorkProcess.InProcess,
      status: rp001Status.All,
    };

    await getListAsync();
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    criteria.value = {
      ...criteria.value,
      pageNumber,
      pageSize,
    };
  };

  const statusOptionBadge = ref([] as OptionBadge[]);

  const table = ref<rp001ListResponse>()

  const getListAsync = async () => {
    const { data, status } = await rp001Service.getListAuditAndRevenueAsync(criteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data;

      getStatusCount(data.statusCount);
    }
  };

  const getStatusCount = (count: StatusCount) => {
    const rp001StatusOptions = Object.entries(rp001Status).map(([, value]) => ({
      label: rp001StatusName(value),
      value: value,
      bgColorClass: rp001StatusColor(value).bgColorClass,
      textColorClass: rp001StatusColor(value).textColorClass,
      count: getCount(count, value),
    } as OptionBadge));

    statusOptionBadge.value = rp001StatusOptions;
  };

  const getCount = (countAll: StatusCount, status: rp001Status): number => {
    let count = 0;
    const convertStatus = (status[0]?.toLowerCase() ?? '') + status.slice(1);
    count = countAll[convertStatus as keyof StatusCount];

    return count;
  };

  const deleteByIdAsync = async (id: string) => {
    if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;

    const { status } = await rp001Service.deleteAuditAndRevenueByIdAsync(id);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.deletedMessageToast();

      await getListAsync();
    }

    if (status == HttpStatusCode.NotFound) {
      ToastHelper.notFoundMessageToast();
    }
  }

  return {
    criteria,
    clearCriteria,
    table,
    onChangePageSize,
    getListAsync,
    statusOptionBadge,
    deleteByIdAsync
  }
});

export const useRp001DetailStore = defineStore("rp001-detail-store", () => {
  const body = ref<rp001Body>({
    approvalAcceptors: [] as ParticipantsAcceptor[],
    details: [] as rp001Detail[],
    status: rp001Status.Draft,
    attachments: [] as Attachments[],
  } as rp001Body);

  const { reSequence } = ArrayHelper();

  const statusOptionBadge = ref([] as OptionBadge[]);

  const contractType = ref<ContractType>(ContractType.All);

  const getContractDraftVendorList = async () => {
    const startDate = setStartDate(new Date(body.value.signStartDate));
    const endDate = setEndDate(new Date(body.value.signEndDate));

    const { data, status } = await rp001Service.getContractDraftVendorOver1mAsync(
      startDate,
      endDate
    );

    if (status === HttpStatusCode.Ok) {
      const existingIds = new Set(body.value.details.map(d => d.caContractDraftVendorId));

      const filteredData = data.filter((d: any) => !existingIds.has(d.caContractDraftVendorId));

      body.value.details.push(...filteredData);

      body.value.details.forEach((d, index) => {
        d.sequence = index + 1;
      });

      getStatusCount();
    }
  };

  const createRp001 = async () => {
    const { data, status } = await rp001Service.createAuditAndRevenueAsync(body.value);

    if (status === HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();

      await getByIdRp001(data);
    }
  };

  const createRp001SendApprove = async () => {
    const mockData = {
      ...body.value,
      status: rp001Status.WaitingApproval
    }

    const { data, status } = await rp001Service.createAuditAndRevenueAsync(mockData);

    if (status === HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();

      await getByIdRp001(data);
    }
  };

  const updateRp001 = async () => {
    if (!body.value.id) return;

    const { status } = await rp001Service.updateAuditAndRevenueAsync(body.value.id, body.value);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await getByIdRp001(body.value.id);
    }
  };

  const updateRp001SendApprove = async () => {
    if (!body.value.id) return;

    const mockData = {
      ...body.value,
      status: rp001Status.WaitingApproval
    }

    const { status } = await rp001Service.updateAuditAndRevenueAsync(body.value.id, mockData);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast();

      await getByIdRp001(body.value.id);
    }
  };


  const updateRp001Recall = async () => {
    if (!body.value.id) return;

    const mockData = {
      ...body.value,
      status: rp001Status.Edit
    }

    const { status } = await rp001Service.updateAuditAndRevenueAsync(body.value.id, mockData);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.recallEditMessageToast();

      await getByIdRp001(body.value.id);
    }
  };

  const getByIdRp001 = async (id: string) => {
    const { data, status } = await rp001Service.getAuditAndRevenueByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;
      getStatusCount();
    }

    if (status === HttpStatusCode.NotFound) {
      ToastHelper.notFoundMessageToast();
    }
  }

  const getStatusCount = () => {
    const countAll = getContractTypeCount(body.value.details);

    const rp001StatusOptions = Object.values(ContractType).map((value) => ({
      label: rp001ContractName(value),
      value: value,
      bgColorClass: rp001ContractColor(value).bgColorClass,
      textColorClass: rp001ContractColor(value).textColorClass,
      count: getCount(countAll, value),
    } as OptionBadge));

    statusOptionBadge.value = rp001StatusOptions;
  };

  const getContractTypeCount = (details: rp001Detail[]): ContractTypeCount => {
    return details.reduce(
      (acc, cur) => {
        acc.all++;
        switch (cur.contractTypeCode) {
          case ContractType.CMType001:
            acc.CMType001++;
            break;
          case ContractType.CMType002:
            acc.CMType002++;
            break;
          case ContractType.CMType003:
            acc.CMType003++;
            break;
        }
        return acc;
      },
      { all: 0, CMType001: 0, CMType002: 0, CMType003: 0 } as ContractTypeCount
    );
  };

  const getCount = (countAll: ContractTypeCount, status: ContractType): number => {
    switch (status) {
      case ContractType.All: return countAll.all;
      case ContractType.CMType001: return countAll.CMType001;
      case ContractType.CMType002: return countAll.CMType002;
      case ContractType.CMType003: return countAll.CMType003;
      default: return 0;
    }
  };

  const onApproveAsync = async () => {
    if (!body.value.id) return;

    const res = await showReasonDialogAsync(isLastApproval.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (res?.isConfirm) {
      const { status } = await rp001Service.approveAuditAndRevenueAsync(body.value.id, res.reason);

      if (status === HttpStatusCode.Ok) {
        await getByIdRp001(body.value.id);

        return ToastHelper.approvedMessageToast();
      }
    }
  }

  const onRejectAsync = async () => {
    if (!body.value.id) return;

    const res = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (res?.isConfirm && res.reason) {
      const { status } = await rp001Service.rejectAuditAndRevenueAsync(body.value.id, res.reason);

      if (status === HttpStatusCode.Ok) {
        await getByIdRp001(body.value.id);

        return ToastHelper.notAgreeMessageToast();
      }
    }
  }

  const clearBody = () => {
    body.value = {
      approvalAcceptors: [] as ParticipantsAcceptor[],
      details: [] as rp001Detail[],
      status: rp001Status.Draft,
      attachments: [] as Attachments[],
    } as rp001Body;

    contractType.value = ContractType.All;

    statusOptionBadge.value = [];
  };

  const canEdit = computed(() => {
    const status = [rp001Status.Draft, rp001Status.Rejected, rp001Status.Edit].includes(body.value.status) || !body.value.status;

    return status;
  });

  const canApproveReject = computed(() => {
    if (!body.value.approvalAcceptors) return;

    const status = body.value.status === rp001Status.WaitingApproval;
    const checkQue = isCurrentPendingAcceptor(body.value.approvalAcceptors, authStore.profile.id, AcceptorType.Approver);

    return status && checkQue;
  });

  const isLastApproval = computed(() => {
    const approvalUser = body.value.approvalAcceptors?.filter(f => f.status === AcceptorStatus.Pending);

    if (!approvalUser) {
      return false;
    }

    const current = approvalUser.find(f => f.isCurrent);

    if (!current) {
      return false;
    }

    return canApproveReject.value && current.sequence === approvalUser[approvalUser.length - 1].sequence;
  });

  const isRecall = computed(() => {
    return body.value.hasPermission && body.value.approvalAcceptors.every(e => e.status === AcceptorStatus.Pending) && [rp001Status.WaitingApproval].includes(body.value.status);
  });

  const isCurrentApproval = computed(() => {
    const acceptor = body.value.approvalAcceptors
      .filter(f => f.status === AcceptorStatus.Pending);

    if (acceptor.length === 0) return false;

    const current = acceptor.reduce(
      (prev, curr) =>
        curr.sequence < prev.sequence ? curr : prev,
      acceptor[0]
    );

    return (current.delegateeUserId ? current.delegateeUserId : current.userId) === authStore.profile.id;
  });

  const onRemoveDetail = async (index: number): Promise<void> => {
    const selectDetail = body.value.details[index];

    if (!selectDetail) return;

    if (selectDetail.id) {
      if (!body.value.id) return;

      if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;

      await rp001Service.deleteDetailByIdAsync(body.value.id, selectDetail.id);
    }

    body.value.details.splice(index, 1);

    getStatusCount();

    reSequence(body.value.details);
  }

  const resetDocumentAsync = async (documentType: string) => {
    if (!body.value.id) return;

    const { status } = await rp001Service.resetDocumentAsync(body.value.id, documentType);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
      await getByIdRp001(body.value.id);
    }
  };

  const onUpsertAttachments = async () => {
    if (!body.value.id) return;

    const { status } = await rp001Service.attachmentsAsync(body.value.id, body.value.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await getByIdRp001(body.value.id);
    }
  };

  const getDefaultAcceptor = async (): Promise<void> => {

    const params = {
      processType: SectionProcessType.ContractAmendment,
      budget: 1,
      userId: authStore.profile.id,
      supplyMethodCode: SupplyMethodCode.eighty
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status == HttpStatusCode.Ok) {
      body.value.approvalAcceptors = [];

      data.forEach(item => body.value.approvalAcceptors.push({
        acceptorType: AcceptorType.Approver,
        fullName: item.fullName,
        positionName: item.fullPositionName,
        sequence: body.value.approvalAcceptors.length + 1,
        status: AcceptorStatus.Draft,
        userId: item.userId,
        departmentName: item.businessUnitName,
      } as ParticipantsAcceptor))
    }
  };

  return {
    body,
    clearBody,
    statusOptionBadge,
    contractType,
    getStatusCount,
    onRemoveDetail,
    api: {
      getContractDraftVendorList,
      createRp001,
      getByIdRp001,
      updateRp001,
      onApproveAsync,
      onRejectAsync,
      updateRp001SendApprove,
      createRp001SendApprove,
      updateRp001Recall,
      onUpsertAttachments,
      resetDocumentAsync,
      getDefaultAcceptor,
    },
    state: {
      canEdit,
      canApproveReject,
      isLastApproval,
      isRecall,
      isCurrentApproval,
    }
  };
});
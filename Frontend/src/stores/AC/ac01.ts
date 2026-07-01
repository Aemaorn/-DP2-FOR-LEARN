import { AC01Status } from "@/enums/AC/ac01";
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from "@/enums/participants";
import { EGroupCode, EWorkProcess, OrganizationLevelEnum } from "@/enums/shared";
import AC01Helper from "@/helpers/AC/ac01";
import ToastHelper from "@/helpers/toast";
import { type GlAccounts, type TA01Detail, type TAC01Criteria, type TAC01ListResponse, type TAC01StatusCount } from "@/models/ACC/acc001";
import type { Option, OptionBadge } from "@/models/shared/option";
import type { ParticipantsAcceptor, ParticipantsAssignee } from "@/models/shared/participants";
import ac01Service from "@/services/AC/ac01";
import SharedService from "@/services/Shared/dropdown";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { computed, ref, type Ref } from "vue";
import { useAuthenticationStore } from "../authentication";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import operationService from "@/services/Shared/operations";
import { SectionProcessType } from "@/enums/operations";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";

const onGetDepartmentDropdown = async (target: Ref<Array<Option>>) => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getSupplyMethodAsync = async (groupCode: EGroupCode, target: Ref<Option[]>, parentCode?: string) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(groupCode, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

export const useAc01ListStore = defineStore('ac01-list-store', () => {
  const initCriteria: TAC01Criteria = {
    workProcess: EWorkProcess.InProcess,
    status: AC01Status.All,
    pageNumber: 1,
    pageSize: 10,
  };

  const initDropdown: Array<Option> = [];

  const searchCriteria = ref<TAC01Criteria>(structuredClone(initCriteria));
  const dataResponse = ref<TAC01ListResponse>({ data: {} } as TAC01ListResponse);
  const deparmentDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const statusOptionBadge = ref([] as OptionBadge[]);

  const onGetDropdownAsync = async () => {
    await Promise.all([onGetDepartmentDropdown(deparmentDropdown)]);
  };

  const { AC01StatusName, MapStatusColor } = AC01Helper;

  const getStatusCount = (count: TAC01StatusCount): void => {
    const statusOptions = Object.entries(AC01Status).map(([, value]) => ({
      label: AC01StatusName(value),
      value: value,
      bgColorClass: MapStatusColor(value).bgColorClass,
      textColorClass: MapStatusColor(value).textColorClass,
      count: getCount(count, value),
    } as OptionBadge));

    statusOptionBadge.value = statusOptions;
  };

  const getCount = (countAll: TAC01StatusCount, status: AC01Status): number => {
    let count = 0;
    const convertStatus = (status[0]?.toLowerCase() ?? '') + status.slice(1);
    count = countAll[convertStatus as keyof TAC01StatusCount];

    return count;
  };

  const onGetListData = async (): Promise<void> => {
    const { data, status } = await ac01Service.getListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      dataResponse.value = data;

      getStatusCount(data.statusCount);
    }
  };

  const onClearCriteria = (): void => {
    searchCriteria.value = structuredClone(initCriteria);
  }

  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    }
  };

  return { searchCriteria, dataResponse, onClearCriteria, onChangePageSize, onGetListData, deparmentDropdown, onGetDropdownAsync, statusOptionBadge };
});

export const useAc01DetailStore = defineStore('ac01-detail-store', () => {
  const authStore = useAuthenticationStore();

  const body = ref<TA01Detail>({
    acceptors: [] as ParticipantsAcceptor[],
    source: {},
    glAccounts: [] as GlAccounts[],
    assignees: [] as ParticipantsAssignee[]
  } as TA01Detail);

  const onClearBody = () => {
    body.value = {
      acceptors: [] as ParticipantsAcceptor[],
    } as TA01Detail
  };

  const onClearPaymentDetail = () => {
    body.value = {
      ...body.value,
      advanceBankAccount: undefined,
      advanceBankAccountName: undefined,
      advancePaymentMethodCode: undefined,
      advanceBankCode: undefined,
      advanceBankBranch: undefined,
      advanceDetail: undefined,
      isInvoiceAmount: false,
      invoiceAmount: undefined,
    }
  }

  const departmentDropdown = ref<Option[]>([]);
  const budgetTypeDropdown = ref<Option[]>([]);
  const accountCodeDropdown = ref<Option[]>([]);
  const paymentMethodDropDown = ref<Option[]>([]);
  const unitOfMeasureDropDown = ref<Option[]>([]);
  const bankDropdown = ref<Option[]>([]);

  const onGetDropdownAsync = async () => {
    await Promise.all([
      getSupplyMethodAsync(EGroupCode.SolId, departmentDropdown),
      getSupplyMethodAsync(EGroupCode.BudgetTyp, budgetTypeDropdown),
      getSupplyMethodAsync(EGroupCode.GLAcc, accountCodeDropdown),
      getSupplyMethodAsync(EGroupCode.PaymentMethod, paymentMethodDropDown),
      getSupplyMethodAsync(EGroupCode.UnitOfMea, unitOfMeasureDropDown),
      getSupplyMethodAsync(EGroupCode.Bank, bankDropdown)
    ]);
  };

  const getDetailById = async (id: string) => {
    const { data, status } = await ac01Service.getByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;
    }

    if (status === HttpStatusCode.NotFound) {
      ToastHelper.notFoundMessageToast();
    }
  };

  const updateById = async () => {
    const { status } = await ac01Service.updateByIdAsync(body.value.id, body.value);

    if (status == HttpStatusCode.Ok) {
      await getDetailById(body.value.id);

      switch (body.value.status) {
        case AC01Status.WaitingApproval:
          return ToastHelper.assignedMessageToast();
        case AC01Status.Approved:
          return ToastHelper.sendApproveMessageToast();
        default:
          return ToastHelper.updatedMessageToast();
      }
    }

    if (status == HttpStatusCode.NotFound) {
      ToastHelper.notFoundMessageToast();
    }
  };

  const onSendApprove = async () => {
    const mockData = {
      ...body.value,
      status: AC01Status.WaitingApproval
    };

    const { status } = await ac01Service.updateByIdAsync(body.value.id, mockData);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast();

      await getDetailById(body.value.id);
    }
  };

  const onApproveAsync = async (): Promise<void> => {
    if (!body.value.id) return;

    const res = await showReasonDialogAsync(!isLastApprover.value
      ? ReasonDialogType.Accepted
      : body.value.status == AC01Status.Approved
        ? ReasonDialogType.Confirm
        : ReasonDialogType.Approve);

    const { status } = await ac01Service.approveAsync(body.value.id, res.reason);

    if (status === HttpStatusCode.Ok) {
      await getDetailById(body.value.id);

      if (body.value.status == AC01Status.Approved) {
        return ToastHelper.confirmMessageToast();
      }

      return ToastHelper.approvedMessageToast();
    }
  };

  const onRejectAsync = async (): Promise<void> => {
    if (!body.value.id) return;

    const res = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (res?.isConfirm) {
      const { status } = await ac01Service.rejectAsync(body.value.id, res.reason);

      if (status === HttpStatusCode.Ok) {
        await getDetailById(body.value.id);

        return ToastHelper.sendEditMessageToast();
      }
    }
  };

  const onRecallAsync = async (): Promise<void> => {
    if (!body.value.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

    const mockData = {
      ...body.value,
      status: AC01Status.Edit
    }

    const { status } = await ac01Service.updateByIdAsync(body.value.id, mockData);

    if (status === HttpStatusCode.Ok) {
      await getDetailById(body.value.id);

      return ToastHelper.recallEditMessageToast();
    }
  }

  const canEdit = computed(() => {
    const status = [AC01Status.Draft, AC01Status.Edit, AC01Status.Rejected].includes(body.value.status);

    return status && body.value.hasPermission;
  });

  const canReCall = computed(() => {
    return [AC01Status.WaitingApproval].includes(body.value.status) && body.value.acceptors.every(e => e.status === AcceptorStatus.Pending) && body.value.hasPermission;
  });

  const canAddAssign = computed(() => {
    const status = [AC01Status.Draft, AC01Status.WaitingApproval].includes(body.value.status) || !body.value.status;
    const canAssign = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canAssign;
  });

  const canAssign = computed(() => {
    const status = [AC01Status.Draft].includes(body.value.status) || !body.value.status;
    const canAssign = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canAssign;
  });

  const canApprove = computed(() => {
    const status = body.value.status === AC01Status.WaitingApproval;
    const checkQue = body.value.acceptors.find(a => a.acceptorType === AcceptorType.Approver && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id)?.isCurrent;

    return status && checkQue;
  });

  const canConfirm = computed(() => {

    const status = body.value.status === AC01Status.WaitingForCompletion;
    return status && body.value.hasPermission;
  });

  const canSendEdit = computed(() => {
    const status = body.value.status === AC01Status.Draft;
    const canApprove = body.value.assignees?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && canApprove;
  });

  const getDefaultExpenseDisbursementAsync = async (): Promise<void> => {
    const { data, status } = await operationService.getDefaultExpenseDisbursementAsync();

    if (status === HttpStatusCode.Ok) {

      if (body.value.assignees === undefined) {
        body.value.assignees = [];
      }

      body.value.assignees.push({
        assigneeGroup: AssigneeGroup.Accounting,
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

  const getDefaultAcceptor = async (budget: number): Promise<void> => {
    const { data: defaultExpense, status: defaultStatus } =
      await operationService.getDefaultExpenseDisbursementAsync();

    if (defaultStatus === HttpStatusCode.Ok) {
      const params = {
        processType: SectionProcessType.ExpenseDisbursement,
        budget: budget,
        userId: defaultExpense.userId,
        supplyMethodCode: "SectionApprover001",
        skipCurrentEmployee: false,
      } as defaultAcceptorCriteria;

      const { data: acceptorList, status: acceptorStatus } =
        await operationService.getOperationsDefaultAcceptorAsync(params);

      if (acceptorStatus === HttpStatusCode.Ok) {
        body.value.acceptors = [];

        acceptorList.forEach(item =>
          body.value.acceptors.push({
            acceptorType: AcceptorType.Approver,
            fullName: item.fullName,
            positionName: item.fullPositionName,
            sequence: body.value.acceptors.length + 1,
            status: AcceptorStatus.Draft,
            userId: item.userId,
            departmentName: item.businessUnitName,
          })
        );
      }
    }
  };

  const isCanSetDefaultApprover = computed(() => [AC01Status.Draft, AC01Status.Edit].includes(body.value.status));
  const isLastApprover = computed(() => body.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending).length === 1);

  const isEditDisbursement = computed(() => {
    const status = [AC01Status.WaitingForCompletion, AC01Status.Approved].includes(body.value.status);

    return status;
  });

  const isCurrentApproval = computed(() => {
    const acceptor = body.value.acceptors
      .filter(f => f.status === AcceptorStatus.Pending);

    if (acceptor.length === 0) return false;

    const current = acceptor.reduce(
      (prev, curr) =>
        curr.sequence < prev.sequence ? curr : prev,
      acceptor[0]
    );

    return (current.delegateeUserId ? current.delegateeUserId : current.userId) === authStore.profile.id;
  });

  return {
    body,
    departmentDropdown,
    budgetTypeDropdown,
    accountCodeDropdown,
    paymentMethodDropDown,
    unitOfMeasureDropDown,
    bankDropdown,
    fn: {
      onClearBody,
      onClearPaymentDetail
    },
    api: {
      getDefaultExpenseDisbursementAsync,
      getDetailById,
      onGetDropdownAsync,
      updateById,
      onApproveAsync,
      onRejectAsync,
      onRecallAsync,
      onSendApprove,
      getDefaultAcceptor,
    },
    state: {
      canEdit,
      canReCall,
      canAssign,
      canAddAssign,
      canApprove,
      canSendEdit,
      isCanSetDefaultApprover,
      isLastApprover,
      canConfirm,
      isEditDisbursement,
      isCurrentApproval,
    }
  }
})
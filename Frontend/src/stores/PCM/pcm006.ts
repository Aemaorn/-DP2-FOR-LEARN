import Pcm006Constant from "@/constants/pcm006";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import { EPcm006Status } from "@/enums/pcm006";
import { EWorkProcess, OrganizationLevelEnum } from "@/enums/shared";
import { ArrayHelper } from "@/helpers/array";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import ToastHelper from "@/helpers/toast";
import type { TPcm006ListResponse, TPcm006Criteria, TPcm006StatusCount, TPcm006Detail, ReimbursementItem } from "@/models/PCM/pcm006";
import type { Option, OptionBadge } from "@/models/shared/option";
import type { ParticipantsAcceptor } from "@/models/shared/participants";
import pcm006Service from "@/services/PCM/PCM006";
import SharedService from "@/services/Shared/dropdown";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { computed, ref, type Ref } from "vue";
import { useAuthenticationStore } from "../authentication";
import { AcceptorStatus, AcceptorType } from "@/enums/participants";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import type { Attachments } from "@/models/shared/uploadFile";
import { PreProcurementStep } from '@/enums/preProcurement';
import operationService from "@/services/Shared/operations";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import { OrganizationLevel, SectionProcessType } from "@/enums/operations";

const getDepartmentAsync = async (target: Ref<Option[]>): Promise<void> => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

export const usePcm006ListStore = defineStore("Pcm-list-store", () => {
  const { Pcm006StatusName, Pcm006StatusColor } = Pcm006Constant;

  const criteria = ref<TPcm006Criteria>({
    pageNumber: 1,
    pageSize: 10,
    workProcess: EWorkProcess.InProcess,
    status: EPcm006Status.All,
  } as TPcm006Criteria);

  const statusOptionBadge = ref([] as OptionBadge[]);

  const onClearCirteria = () => {
    criteria.value = {
      pageNumber: 1,
      pageSize: 10,
      workProcess: EWorkProcess.InProcess,
      status: EPcm006Status.All,
    } as TPcm006Criteria;
  }

  const dataTabel = ref<TPcm006ListResponse>({} as TPcm006ListResponse);

  const onGetList = async () => {
    const { data, status } = await pcm006Service.getListAsync(criteria.value);

    if (status === HttpStatusCode.Ok) {
      dataTabel.value = data;

      getStatusCount(data.statusCount);
    }
  }

  const completedStatuses = new Set([EPcm006Status.All, EPcm006Status.Paid, EPcm006Status.Cancelled]);

  const getStatusCount = (count: TPcm006StatusCount): void => {
    const isCompleted = criteria.value.workProcess === EWorkProcess.Completed;

    const statusOptions = Object.entries(EPcm006Status)
      .filter(([, value]) => !isCompleted || completedStatuses.has(value))
      .map(([, value]) => ({
        label: Pcm006StatusName(value),
        value: value,
        bgColorClass: Pcm006StatusColor(value).bgColorClass,
        textColorClass: Pcm006StatusColor(value).textColorClass,
        count: getCount(count, value),
      } as OptionBadge));

    statusOptionBadge.value = statusOptions;
  };

  const getCount = (countAll: TPcm006StatusCount, status: EPcm006Status): number => {
    let count = 0;
    const convertStatus = (status[0]?.toLowerCase() ?? '') + status.slice(1);
    count = countAll[convertStatus as keyof TPcm006StatusCount];

    return count;
  };

  const departmentDropDown = ref<Option[]>([]);

  const getDropdownAsync = async () => {
    await Promise.all([
      getDepartmentAsync(departmentDropDown)
    ])
  };

  const onDeleteAsync = async (id: string) => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

    const { status } = await pcm006Service.onDeleteAsync(id);

    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();
      await onGetList();
    }
  }

  return {
    criteria,
    dataTabel,
    statusOptionBadge,
    departmentDropDown,
    fn: {
      onClearCirteria
    },
    api: {
      onGetList,
      getDropdownAsync,
      onDeleteAsync
    }
  }
});

export const usePcm006DetailStore = defineStore("pcm006-detail-store", () => {
  const { reSequence } = ArrayHelper();

  const authStore = useAuthenticationStore();

  const cloneAccountingAcceptors = ref<Array<ParticipantsAcceptor>>([]);
  const isCurrentUserAccountingSegmentMember = ref<boolean>(false);

  const body = ref<TPcm006Detail>({
    acceptors: [] as ParticipantsAcceptor[],
    items: [] as ReimbursementItem[],
    status: EPcm006Status.Draft,
    attachments: [] as Attachments[],
    acceptanceConfirmers: [] as ParticipantsAcceptor[],
    steps: [PreProcurementStep.PettyCashReimbursement],
  } as TPcm006Detail);

  const onClearBody = () => {
    body.value = {
      acceptors: [] as ParticipantsAcceptor[],
      items: [] as ReimbursementItem[],
      status: EPcm006Status.Draft,
      attachments: [] as Attachments[],
      acceptanceConfirmers: [] as ParticipantsAcceptor[],
      steps: [PreProcurementStep.PettyCashReimbursement],
    } as TPcm006Detail
  }

  const departmentDropDown = ref<Option[]>([]);

  const onGetDropdownAsync = async () => {
    await Promise.all([
      getDepartmentAsync(departmentDropDown)
    ]);
  };

  const existingIds = () => new Set(body.value.items.map(i => i.pettyCashGlAccountId));

  const onPettyCashData = async () => {
    const { data, status } = await pcm006Service.getGlAccountsAsync(body.value.departmentId);

    if (status == HttpStatusCode.Ok) {
      const ids = existingIds();
      const newItems = data.filter((i: ReimbursementItem) => !ids.has(i.pettyCashGlAccountId));
      if (body.value.id) {
        body.value.items.push(...newItems);
      } else {
        body.value.items = [...body.value.items, ...newItems];
      }

      body.value.items = reSequence(body.value.items);
    }
  }

  const fetchGlAccounts = async (): Promise<ReimbursementItem[]> => {
    const { data, status } = await pcm006Service.getGlAccountsAsync(body.value.departmentId);
    if (status == HttpStatusCode.Ok) {
      const ids = existingIds();
      return data.filter((i: ReimbursementItem) => !ids.has(i.pettyCashGlAccountId));
    }
    return [];
  }

  const applySelectedPettyCashItems = (selected: ReimbursementItem[]) => {
    const ids = existingIds();
    const newItems = selected.filter(i => !ids.has(i.pettyCashGlAccountId));
    body.value.items = [...body.value.items, ...newItems];
    body.value.items = reSequence(body.value.items);
  }

  const getById = async (id: string) => {
    const { data, status } = await pcm006Service.getByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;

      if (!body.value.acceptanceConfirmers) {
        body.value.acceptanceConfirmers = [];
      }

      if (
        body.value.id &&
        body.value.status === EPcm006Status.WaitingDisbursementDate &&
        body.value.acceptanceConfirmers.length === 0
      ) {
        const operators = (body.value.acceptors ?? [])
          .filter((a): a is ParticipantsAcceptor => a.acceptorType === AcceptorType.AccountingOperator);
        if (operators.length > 0) {
          body.value.acceptanceConfirmers = operators.map((op, i): ParticipantsAcceptor => ({
            userId: op.userId,
            fullName: op.fullName,
            positionName: op.positionName,
            departmentName: op.departmentName,
            sequence: i + 1,
            acceptorType: AcceptorType.AccountingConfirmer,
            status: AcceptorStatus.Draft,
          } as ParticipantsAcceptor));
        } else if (![String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)].includes(body.value.departmentOrganizationLevel ?? '')
        && body.value.acceptanceConfirmers.length === 0) {
          const { data: members, status: membersStatus } = await operationService.getSegmentAccountingMembersAsync(true);
          if (membersStatus === HttpStatusCode.Ok && members.some((m): boolean => m.userId === authStore.profile.id)) {
            body.value.acceptanceConfirmers = [{
              sequence: 1,
              userId: authStore.profile.id,
              fullName: authStore.profile.name,
              positionName: authStore.profile.positionName,
              acceptorType: AcceptorType.AccountingConfirmer,
              status: AcceptorStatus.Draft,
            } as ParticipantsAcceptor];
          }
        }
      }

      isCurrentUserAccountingSegmentMember.value = false;
      if (
        body.value.id &&
        [EPcm006Status.WaitingAccountingApproval, EPcm006Status.WaitingDisbursementDate].includes(body.value.status) &&
        authStore.profile &&
        ![String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)].includes(body.value.departmentOrganizationLevel ?? '')
      ) {
        const { data: accountingMembers, status: accountingStatus } =
          await operationService.getSegmentAccountingMembersAsync(true);
        if (accountingStatus === HttpStatusCode.Ok) {
          isCurrentUserAccountingSegmentMember.value =
            accountingMembers?.some((m): boolean => m.userId === authStore.profile.id) ?? false;
          if (
            body.value.status === EPcm006Status.WaitingAccountingApproval &&
            isCurrentUserAccountingSegmentMember.value &&
            !(body.value.acceptors ?? []).some((a): boolean => a.acceptorType === AcceptorType.AccountingOperator) &&
            !(body.value.acceptors ?? []).some((a): boolean => a.acceptorType === AcceptorType.AccountingApprover && a.userId === authStore.profile.id)
          ) {
            body.value.acceptors.push({
              sequence: 1,
              userId: authStore.profile.id,
              fullName: authStore.profile.name,
              positionName: authStore.profile.positionName,
              acceptorType: AcceptorType.AccountingOperator,
              status: AcceptorStatus.Draft,
            } as ParticipantsAcceptor);
          }
        }
      }

      cloneAccountingAcceptors.value = body.value.acceptors?.filter(
        a => a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator
      ) || [];
    }

    if (status === HttpStatusCode.NotFound) {
      ToastHelper.notFoundMessageToast();
    }
  }

  const onCreate = async () => {
    const { data, status } = await pcm006Service.createsync(body.value);

    if (status === HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();

      await getById(data);
    }
  }

  const onUpdate = async (newStatus?: EPcm006Status) => {
    if (!body.value.id) return;

    const mockBody = {
      ...body.value,
      status: newStatus ?? body.value.status
    }

    const { status } = await pcm006Service.updateAsync(body.value.id, mockBody);

    if (status == HttpStatusCode.Ok) {
      if (newStatus === EPcm006Status.WaitingApproval) {
        ToastHelper.sendApproveConfirmMessageToast();
      }

      if (newStatus === EPcm006Status.Edit) {
        ToastHelper.recallEditMessageToast();
      }

      if (!newStatus) {
        ToastHelper.updatedMessageToast();
      }

      await getById(body.value.id);
    }
  }

  const onApproveAsync = async (): Promise<void> => {
    if (!body.value.id) return;

    let dialogType: ReasonDialogType;

    if (isAccountingApproveReject.value) {
      dialogType = ReasonDialogType.Confirm;
    } else if (isLastApprovalApprover.value && isLastAccountingApprover.value) {
      dialogType = ReasonDialogType.Approve;
    } else {
      dialogType = ReasonDialogType.Accepted;
    }

    const res = await showReasonDialogAsync(dialogType);

    if (res?.isConfirm) {
      const { status } = await pcm006Service.approveAsync(body.value.id, res.reason);

      if (status === HttpStatusCode.Ok) {
        await getById(body.value.id);

        return ToastHelper.approvedMessageToast();
      }
    }
  };

  const onUpsertAttachments = async () => {
    if (!body.value.id) return;

    const { status } = await pcm006Service.attachmentsAsync(body.value.id, body.value.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await getById(body.value.id);
    }
  };

  const onRejectAsync = async (): Promise<void> => {
    if (!body.value.id) return;

    const res = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (res?.isConfirm && res?.reason) {
      const { status } = await pcm006Service.rejectAsync(body.value.id, res.reason);

      if (status === HttpStatusCode.Ok) {
        await getById(body.value.id);

        return ToastHelper.sendEditMessageToast();
      }
    }
  };

  const canEdit = computed(() => {
    const status = [EPcm006Status.Draft, EPcm006Status.Edit, EPcm006Status.Rejected].includes(body.value.status);

    return status && body.value.departmentId === authStore.profile.departmentCode;
  });

  const canAcceptAndRejectApprover = computed(() => {
    if (!body.value.acceptors) return false;
    const status = body.value.status === EPcm006Status.WaitingApproval;
    const checkQue = isCurrentPendingAcceptor(body.value.acceptors, authStore.profile.id, AcceptorType.Approver);
    return status && checkQue;
  });

  const canReCall = computed(() => {
    return body.value.hasPermission && body.value.status === EPcm006Status.WaitingApproval && body.value.acceptors.every(x => x.status == AcceptorStatus.Pending)
  })

  const isLastApprovalApprover = computed(() => body.value.acceptors?.filter(f => f.acceptorType === AcceptorType.Approver && f.status === AcceptorStatus.Pending).length === 1);

  const isLastAccountingApprover = computed(() =>
    body.value.acceptors?.filter(f =>
      (f.acceptorType === AcceptorType.AccountingApprover || f.acceptorType === AcceptorType.AccountingOperator) &&
      f.status === AcceptorStatus.Pending
    ).length === 1
  );

  const isAccountingCanAssign = computed(() => {
    const accountingAcceptors = body.value.acceptors?.filter(a =>
      a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator
    );
    if (!accountingAcceptors || accountingAcceptors.length === 0) return false;

    const status = [EPcm006Status.WaitingAccountingApproval].includes(body.value.status);
    const currentUser = accountingAcceptors.find(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    if (!currentUser) return false;

    const firstPending = accountingAcceptors
      .filter(s => s.status === AcceptorStatus.Pending)
      .sort((a, b): number => {
        const typeA = a.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;
        const typeB = b.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;
        if (typeA !== typeB) return typeA - typeB;
        return a.sequence - b.sequence;
      })[0];

    const isCurrentUserFirstPending = firstPending != null &&
      (firstPending.delegateeUserId ? firstPending.delegateeUserId : firstPending.userId) === authStore.profile.id;
    const allAccountPending = accountingAcceptors.every(s => s.status === AcceptorStatus.Pending || s.status === AcceptorStatus.Draft);

    return status && (isCurrentUserFirstPending || allAccountPending);
  });

  const isAccountingApproveReject = computed(() => {
    const isAcceptor = body.value.acceptors?.some(a =>
      (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id &&
      (a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator)
    );
    const status = body.value.status == EPcm006Status.WaitingAccountingApproval;

    return isAcceptor && status;
  });

  const canConfirm = computed((): boolean => {
    const isWaitingDisbursement = body.value.status == EPcm006Status.WaitingDisbursementDate;
    const isAcceptanceConfirmer = body.value.acceptanceConfirmers?.some(
      (a): boolean => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id
    ) ?? false;
    return isWaitingDisbursement && isAcceptanceConfirmer;
  });

  const getDefaultDisbursementAcceptor = async (budget: number): Promise<void> => {
    if ([String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)].includes(body.value.departmentOrganizationLevel ?? '')) return;

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
        if (body.value.acceptors && body.value.acceptors.length > 0) {
          body.value.acceptors = body.value.acceptors.filter(
            a => a.acceptorType !== AcceptorType.AccountingApprover
          );

          acceptorList.forEach(item =>
            body.value.acceptors?.push({
              acceptorType: AcceptorType.AccountingApprover,
              fullName: item.fullName,
              positionName: item.fullPositionName,
              sequence: body.value.acceptors?.length + 1,
              status: AcceptorStatus.Pending,
              userId: item.userId,
              departmentName: item.businessUnitName,
            })
          );

          if (cloneAccountingAcceptors.value.length == 0) {
            cloneAccountingAcceptors.value = body.value.acceptors?.filter(
              a => a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator
            ) || [];
          }
        }
      }
    }
  };

  const isAccountingMember = computed((): boolean => {
    if (!body.value.acceptors) return false;
    if (body.value.status !== EPcm006Status.WaitingAccountingApproval) return false;

    const branchOrZoneLevels = [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)];
    if (
      branchOrZoneLevels.includes(body.value.departmentOrganizationLevel ?? '') &&
      authStore.profile.departmentCode === body.value.departmentId
    ) {
      return true;
    }

    const accountingAcceptors = body.value.acceptors.filter((a): boolean =>
      a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator
    );

    const currentUser = accountingAcceptors.find((a): boolean =>
      (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id
    );

    return currentUser != null;
  });

  const isAccountingBranch = computed((): boolean =>
    body.value.status === EPcm006Status.WaitingAccountingApproval &&
    [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)].includes(body.value.departmentOrganizationLevel ?? '') &&
    authStore.profile.departmentCode === body.value.departmentId
  );

  const getDefaultAcceptor = async (): Promise<void> => {
    const { data, status } = await operationService.getDefaultDepartmentApproverByUserIdAsync(
      authStore.profile.id,
      OrganizationLevel.Department,
      true
    );

    if (status == HttpStatusCode.Ok) {
      body.value.acceptors = [];

      data.forEach((item) =>
        body.value.acceptors.push({
          acceptorType: AcceptorType.Approver,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: body.value.acceptors.length + 1,
          status: AcceptorStatus.Draft,
          userId: item.userId,
          departmentName: item.businessUnitName,
          departmentCode: item.businessUnitId,
        } as ParticipantsAcceptor)
      );
    }
  };

  const saveAcceptorsAsync = async (): Promise<void> => {
    if (!body.value.id) return;
    const { status } = await pcm006Service.updateAsync(body.value.id, body.value);
    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await getById(body.value.id);
    }
  };

  return {
    body,
    departmentDropDown,
    cloneAccountingAcceptors,
    fn: {
      onClearBody
    },
    api: {
      onGetDropdownAsync,
      onPettyCashData,
      fetchGlAccounts,
      applySelectedPettyCashItems,
      getById,
      onCreate,
      onUpdate,
      onApproveAsync,
      onRejectAsync,
      onUpsertAttachments,
      getDefaultDisbursementAcceptor,
      getDefaultAcceptor,
      saveAcceptorsAsync,
    },
    state: {
      canEdit,
      canAcceptAndRejectApprover,
      isLastApprovalApprover,
      canReCall,
      isAccountingApproveReject,
      canConfirm,
      isLastAccountingApprover,
      isAccountingCanAssign,
      isAccountingMember,
      isAccountingBranch,
      isCurrentUserAccountingSegmentMember,
    }
  }
})
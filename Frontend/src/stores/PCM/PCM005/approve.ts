import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from "@/enums/participants";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import ToastHelper from "@/helpers/toast";
import type { ParticipantsAcceptor, ParticipantsAssignee } from "@/models/shared/participants";
import { useAuthenticationStore } from "@/stores/authentication";
import { PP008Status } from "@/views/PP/enums/pp008";
import type { Contract, ContractCreate, ContractGroup, PP008Detail } from "@/views/PP/models/PP008/pp008model";
import PP008Service from "@/views/PP/services/PP008/PP008Service";
import { HttpStatusCode, type AxiosResponse } from "axios";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { usePcm005DetailStore } from "./pcm005";
import operationService from "@/services/Shared/operations";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import { SectionProcessType } from "@/enums/operations";
import { checkIsSixty } from "@/helpers/supplyMethod";
import { ProcurementStatus } from "@/enums/procurement";
import principleApprovalRentalService from "@/services/PCM/PCM005/principleApprovalRental";
import type { PP007GetWinnerCriteria } from "@/views/PP/models/PP007/pp007Model";

export const usePcm005ApproveStore = (procurementId: string) => defineStore('pcm005-approve-store', () => {
  const initData = {
    acceptors: [] as ParticipantsAcceptor[],
    assignees: [] as ParticipantsAssignee[],
    contractBudgetGroups: [] as Array<ContractGroup>,
    status: PP008Status.Draft,
  } as PP008Detail;

  const auth = useAuthenticationStore();
  const pcm005 = usePcm005DetailStore();

  const body = ref<PP008Detail>(structuredClone(initData));

  const onGetByIdAsync = async (id?: string) => {
    const { data, status } = await PP008Service.getByIdAsync(procurementId, id);

    if (status === HttpStatusCode.Ok) {
      body.value = {
        ...data,
        contractType: id ? data.contractType : undefined,
      };
    }
  };

  const resetBody = () => {
    body.value = structuredClone(initData);
  }

  const onSubmitAsync = async () => {
    if (!validateData(body.value)) return;

    if (body.value.id) {
      await updateAsync(body.value.id);

      return;
    }

    await createAsync();
  };

  const onSendApprovalAsync = async () => {
    if (!validateData(body.value, true)) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

    if (body.value.id) {
      await updateAsync(body.value.id, PP008Status.WaitingApproval);

      return;
    }

    await createAsync(PP008Status.WaitingApproval);
  };

  const onRecallAsync = async (): Promise<void> => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

    if (body.value.id) {
      return await updateAsync(body.value.id, PP008Status.Edit);
    }
  };

  const onApproveRejectAsync = async (type: 'Approve' | 'Reject') => {
    if (!body.value.id) return;

    const reasonDialog: Record<'Approve' | 'Reject', ReasonDialogType> = {
      'Approve': isLastApproval.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted,
      'Reject': ReasonDialogType.Reject,
    };

    const resp = await showReasonDialogAsync(reasonDialog[type]);

    if (!resp.isConfirm) return;

    const apiMap: Record<'Approve' | 'Reject', () => Promise<AxiosResponse<any, any>>> = {
      Approve: () => PP008Service.approveAsync(body.value.id!, procurementId, resp.reason),
      Reject: () => PP008Service.rejectAsync(body.value.id!, procurementId, resp.reason),
    };

    const { status } = await apiMap[type]();

    if (status === HttpStatusCode.Ok) {
      type === 'Approve' ? ToastHelper.approvedMessageToast() : ToastHelper.sendEditMessageToast();
    }

    await onGetByIdAsync(body.value.id);
  };

  const onAssignedAsync = async () => {
    if (!body.value.assignees.some(s => s.assigneeType === AssigneeType.Assignee)) {
      return ToastHelper.errorDescription("กรุณามอบหมายผู้รับผิดชอบสัญญาอย่างน้อย 1 คน");
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

    if (body.value.id) {
      return await updateAsync(body.value.id, PP008Status.Assigned);
    }
  };

  const createAsync = async (pp008Status?: PP008Status) => {
    const payload = {
      ...body.value,
      status: pp008Status ?? body.value.status,
      contracts: mapContractPayload(body.value.contractBudgetGroups!),
    } as PP008Detail;

    const { data, status } = await PP008Service.createAsync(procurementId, payload);

    if (status === HttpStatusCode.Created) {
      await onGetByIdAsync(data);

      return pp008Status ? ToastHelper.sendApproveConfirmMessageToast() : ToastHelper.createdMessageToast();
    }
  };

  const updateAsync = async (id: string, pp008Status?: PP008Status) => {
    const payload = {
      ...body.value,
      status: pp008Status ?? body.value.status,
      contracts: mapContractPayload(body.value.contractBudgetGroups!),
    } as PP008Detail;

    const toastByStatus: Record<PP008Status, () => void> = {
      [PP008Status.Edit]: () => ToastHelper.recallEditMessageToast(),
      [PP008Status.WaitingApproval]: () => ToastHelper.sendApproveConfirmMessageToast(),
      [PP008Status.Assigned]: function (): void {
        ToastHelper.assignedMessageToast();
      },
      [PP008Status.WaitingAssign]: function (): void {
        throw new Error("Function not implemented.");
      },
      [PP008Status.Rejected]: function (): void {
        throw new Error("Function not implemented.");
      },
      [PP008Status.Draft]: function (): void {
        throw new Error("Function not implemented.");
      },
    };

    const { status } = await PP008Service.updateAsync(id, procurementId, payload);

    if (status === HttpStatusCode.Ok) {
      await pcm005.getDetailAsync(procurementId);
      await onGetByIdAsync(id);

      return pp008Status ? toastByStatus[pp008Status]() : ToastHelper.updatedMessageToast();
    }
  };

  const mapContractPayload = (data: Array<ContractGroup>): Array<ContractCreate> => {
    return data.flatMap(mapContractGroup);
  };

  const mapContractGroup = ({ budgetId, contracts }: ContractGroup): Array<ContractCreate> => {
    return contracts.map(data => ({
      principleApprovalRentalBudgetId: budgetId,
      ...data,
    }));
  };

  const validateData = (data: PP008Detail, isConfirm: boolean = false) => {
    if (!data.contractBudgetGroups) {
      ToastHelper.warning('ข้อมูลไม่ถูกต้อง', 'ไม่พบข้อมูลวงเงินที่จัดซื้อจัดจ้าง');

      return false;
    }

    if (data.contractBudgetGroups.some(s => s.contracts.length <= 0)) {
      ToastHelper.warning('ข้อมูลไม่ถูกต้อง', 'กรุณาเพิ่มผู้ชนะ');

      return false;
    };

    if (!isConfirm) return true;

    if (data.assignees.length < 1 && body.value.contractType === 'CType001') {
      ToastHelper.warning('ข้อมูลไม่ถูกต้อง', 'จะมอบหมายผู้รับผิดชอบสัญญาอย่างน้อย 1 คน');

      return false;
    };

    if (data.acceptors.length <= 0) {
      ToastHelper.approvalAtLeastMessageToast();

      return false;
    };

    return true;
  };

  const isEdit = computed(() => [PP008Status.Draft, PP008Status.Edit, PP008Status.Rejected].includes(body.value.status) && body.value.hasPermission);

  const isRecall = computed(() => [PP008Status.WaitingApproval].includes(body.value.status) &&
    body.value.acceptors.every(s => s.status === AcceptorStatus.Pending));

  const isCurrentApproval = computed(() => {
    if (!body.value.acceptors) return false;
    const status = [PP008Status.WaitingApproval].includes(body.value.status);
    const checkQue = isCurrentPendingAcceptor(body.value.acceptors, auth.profile.id, AcceptorType.Approver);
    return status && checkQue;
  });

  const isLastApproval = computed(() => {
    if (body.value.acceptors.length === 0) return false;

    const pendingData = body.value.acceptors
      .filter(f => f.status === AcceptorStatus.Pending);

    const current = pendingData
      .reduce((prev, curr) => curr.sequence < prev.sequence ? curr : prev,
        pendingData[0]);

    return isCurrentApproval.value && current.sequence === pendingData[pendingData.length - 1].sequence;
  });

  const isPermissionAssign = computed(() => [PP008Status.WaitingAssign].includes(body.value.status) &&
    body.value.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const canAssignedApprove = computed((): boolean => {
    const status = body.value.status === PP008Status.Assigned;
    const checkAssignees = body.value.assignees.some((a): boolean => (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id);
    const procurementStatus = pcm005.body.status !== ProcurementStatus.Completed;

    return status && checkAssignees && procurementStatus;
  });

  const onGetDefaultSegmentContractManagerAsync = async (): Promise<void> => {
    const { data, status } = await operationService.getSegmentContractManagerAsync();

    if (status === HttpStatusCode.Ok) {
      body.value.assignees.push({
        userId: data.userId,
        assigneeGroup: AssigneeGroup.Contract,
        assigneeType: AssigneeType.Director,
        departmentName: data.businessUnitName,
        fullName: data.fullName,
        positionName: data.fullPositionName,
        sequence: 1,
        status: AssigneeStatus.Draft,
      })
    }
  };


  const onSetDefaultEntrepreneursAsync = async () => {
    if (!body.value.contractBudgetGroups) return;

    const hasContracts = body.value.contractBudgetGroups.some(g => g.contracts.length > 0);
    if (hasContracts) return;

    const principleApprovalRentalId = pcm005.body.principleApprovalRental?.id;
    if (!principleApprovalRentalId) return;

    const params = {
      procurementId,
      jp006Id: principleApprovalRentalId,
      pageNumber: 1,
      pageSize: 999,
      isRental: true,
    } as PP007GetWinnerCriteria;

    const { data, status } = await principleApprovalRentalService.getListRentalWinnerAsync(params);

    if (status !== HttpStatusCode.Ok || !data?.data?.length) return;

    for (const group of body.value.contractBudgetGroups) {
      group.contracts = data.data.map((winner: any, index: number) => ({
        sequence: index + 1,
        agreedPrice: winner.agreedPrice,
        principleApprovalRentalEntrepreneursId: winner.id,
        purchaseOrderEntrepreneurName: winner.name,
        purchaseOrderEntrepreneurEmail: winner.email,
        hasEditContractNumber: true,
      } as Contract));
    }
  };

  const onSetDefaultAcceptors = async () => {
    if (!body.value.contractBudgetGroups) return;

    let totalVendorAgreePrice = 0;
    for (const g of body.value.contractBudgetGroups) {
      let groupSum = 0;
      for (const c of g.contracts) {
        groupSum += c.agreedPrice ?? 0;
      }
      totalVendorAgreePrice += groupSum;
    }

    if (totalVendorAgreePrice === 0) return;

    if (checkIsSixty(pcm005.body.supplyMethodCode) && totalVendorAgreePrice > 100000) {
      // Default acceptors under supply method 60+ and budget > 100,000 require specific business rules.
      // Current implementation intentionally bypasses automatic population in this branch.
      return;
    };

    const params = {
      processType: SectionProcessType.ApprovePurchaseOrder,
      budget: totalVendorAgreePrice,
      userId: auth.profile.id,
      supplyMethodCode: pcm005.body.supplyMethodCode,
      supplyMethodSpecialTypeCode: pcm005.body.supplyMethodSpecialTypeCode,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status === HttpStatusCode.Ok) {
      body.value.acceptors = [...data.map((m, i) => ({
        sequence: i + 1,
        userId: m.userId,
        fullName: m.fullName,
        positionName: m.fullPositionName,
        departmentName: m.businessUnitName,
        acceptorType: AcceptorType.Approver,
        status: AcceptorStatus.Draft,
      }))];
    }
  };

  return {
    body,
    fn: {
      onGetByIdAsync,
      onSubmitAsync,
      onSendApprovalAsync,
      resetBody,
      onRecallAsync,
      onApproveRejectAsync,
      onAssignedAsync,
      onGetDefaultSegmentContractManagerAsync,
      onSetDefaultAcceptors,
      onSetDefaultEntrepreneursAsync,
    },
    states: {
      isEdit,
      isRecall,
      isCurrentApproval,
      isLastApproval,
      isPermissionAssign,
      canAssignedApprove,
    },
  };
})();
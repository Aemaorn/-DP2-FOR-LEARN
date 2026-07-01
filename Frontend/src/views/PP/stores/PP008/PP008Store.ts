import type { Budgets, ContractGroup, Entrepreneurs, PP008Detail } from "../../models/PP008/pp008model";
import type { ParticipantsAcceptor, ParticipantsAssignee } from "@/models/shared/participants";
import { PP008Status } from "../../enums/pp008";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { usePPDetailStore } from "../../../../stores/PP/ppStore";
import { HttpStatusCode } from "axios";
import { AcceptorStatus, AcceptorType, AssigneeType } from "@/enums/participants";
import { useAuthenticationStore } from "@/stores/authentication";
import { showAlertDialogAsync, showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import { DepartmentId } from "@/enums/businessUnit";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import PP008Service from "../../services/PP008/PP008Service";
import pp007service from "../../services/PP007/PP007Service";
import type { PP007GetWinnerCriteria, PP007GetWinnerResponse } from "../../models/PP007/pp007Model";
import ToastHelper from "@/helpers/toast";
import operationService from "@/services/Shared/operations";
import { checkIsEighty } from "@/helpers/supplyMethod";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import { SectionProcessType } from "@/enums/operations";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import { ProcurementStatus } from "@/enums/procurement";
import type { Option } from "@/models/shared/option";
import SharedService from "@/services/Shared/dropdown";
import { EGroupCode } from "@/enums/shared";

export const usePP008DetailStore = defineStore('PP-008-detail-store', () => {
  const procurementStore = usePPDetailStore();
  const authStore = useAuthenticationStore();

  const detail = ref<PP008Detail>({
    acceptors: [] as ParticipantsAcceptor[],
    assignees: [] as ParticipantsAssignee[],
    entrepreneurs: [] as Entrepreneurs[],
    contractBudgetGroups: [] as ContractGroup[],
  } as PP008Detail);

  const onResetDetail = () => {
    detail.value = {
      acceptors: [] as ParticipantsAcceptor[],
      assignees: [] as ParticipantsAssignee[],
      entrepreneurs: [] as Entrepreneurs[],
    } as PP008Detail
  }

  const canEdit = computed((): boolean => {
    const status = [PP008Status.Draft, PP008Status.Rejected, PP008Status.Edit].includes(detail.value.status) || !detail.value.status;

    return status;
  });

  const canAssign = computed((): boolean => {
    const status = detail.value.status === PP008Status.WaitingAssign;

    const assigneesDirector = detail.value.assignees.find((a): boolean => a.assigneeType === AssigneeType.Director);

    const checkJorpor = (assigneesDirector?.delegateeUserId
      ? assigneesDirector?.delegateeUserId
      : assigneesDirector?.userId) === authStore.profile.id;

    return status && checkJorpor;
  });

  const canAssignByAssignee = computed((): boolean => {
    const status = detail.value.status === PP008Status.WaitingAssign;

    const assigneesDirector = detail.value.assignees.find((a): boolean => a.assigneeType === AssigneeType.Assignee);

    const checkJorpor = (assigneesDirector?.delegateeUserId
      ? assigneesDirector?.delegateeUserId
      : assigneesDirector?.userId) === authStore.profile.id;

    return status && checkJorpor;
  });

  const canAssignedApprove = computed((): boolean => {
    const status = detail.value.status === PP008Status.Assigned;
    const checkAssignees = detail.value.assignees.some((a): boolean => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);
    const procurementStatus = ProcurementStatus.Completed !== procurementStore.procurementDetail.status;

    return status && checkAssignees && procurementStatus;
  });

  const canRecall = computed((): boolean => {
    const status = detail.value.status === PP008Status.WaitingApproval;
    const canRecall = detail.value.acceptors.every((a): boolean => [AcceptorStatus.Draft, AcceptorStatus.Pending].includes(a.status));

    return status && canRecall;
  });

  const canApproveOrReject = computed((): boolean => {
    const status = detail.value.status === PP008Status.WaitingApproval;
    const checkQue = isCurrentPendingAcceptor(detail.value.acceptors, authStore.profile.id, AcceptorType.Approver);

    return status && checkQue;
  });

  const isLastApproval = computed((): boolean | undefined => {
    if (!canApproveOrReject.value && !detail.value.acceptors) {
      return;
    }

    const approvalUser = detail.value.acceptors?.filter((f): boolean => f.status === AcceptorStatus.Pending && f.acceptorType === AcceptorType.Approver);

    if (!approvalUser) {
      return false;
    }

    const current = approvalUser.find((f): boolean => f.isCurrent ?? false);

    if (!current) {
      return false;
    }

    return current.sequence === approvalUser[approvalUser.length - 1].sequence;
  });

  const getByIdAsync = async (id?: string): Promise<void> => {
    if (!procurementStore.procurementDetail.id) return;

    const { data, status } = await PP008Service.getByIdAsync(procurementStore.procurementDetail.id, id);

    if (status === HttpStatusCode.Ok) {
      detail.value = {
        ...data,
        contractType: id ? data.contractType : undefined,
      };
    }
  };

  const createAsync = async (statusData?: PP008Status.WaitingApproval): Promise<void> => {
    if (!procurementStore.procurementDetail.id) return;

    if (detail.value.contractBudgetGroups?.some((c): boolean => c.contracts.length === 0)) {
      return ToastHelper.warning('ข้อมูลไม่ถูกต้อง', 'กรุณาเพิ่มผู้ชนะ');
    }

    if (statusData) {
      detail.value.status = statusData;
    }

    detail.value.contracts = detail.value.contractBudgetGroups?.flatMap((group) =>
      (group.contracts ?? []).map((contract) => ({
        torDraftBudgetId: group.budgetId,
        ...contract
      }))
    ) ?? [];

    const { data, status } = await PP008Service.createAsync(procurementStore.procurementDetail.id, detail.value);

    if (status === HttpStatusCode.Created) {
      detail.value.id = data;

      await getByIdAsync(data);

      return statusData ? ToastHelper.sendApproveConfirmMessageToast() : ToastHelper.createdMessageToast();
    }
  };

  const updateAsync = async (id: string, statusData?: PP008Status.Draft | PP008Status.WaitingApproval | PP008Status.Edit | PP008Status.WaitingAssign | PP008Status.Assigned): Promise<void> => {
    if (!procurementStore.procurementDetail.id) return;

    if (detail.value.contractBudgetGroups?.some((c): boolean => c.contracts.length === 0)) {
      return ToastHelper.warning('ข้อมูลไม่ถูกต้อง', 'เพิ่มผู้ค้าให้ครบถ้วนในแต่ละกลุ่มงบประมาณ');
    };

    const toast: Record<PP008Status.Draft | PP008Status.WaitingApproval | PP008Status.Edit | PP008Status.WaitingAssign | PP008Status.Assigned, () => void | Promise<void>> = {
      [PP008Status.Draft]: (): void => ToastHelper.updatedMessageToast(),
      [PP008Status.Edit]: (): void => ToastHelper.recallEditMessageToast(),
      [PP008Status.WaitingApproval]: (): void => ToastHelper.sendApproveConfirmMessageToast(),
      [PP008Status.WaitingAssign]: (): void => ToastHelper.updatedMessageToast(),
      [PP008Status.Assigned]: async (): Promise<void> => {
        await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id!);
        ToastHelper.assignedMessageToast();
      },
    };

    if (statusData) {
      detail.value.status = statusData;
    }

    detail.value.contracts = detail.value.contractBudgetGroups?.flatMap((group) =>
      (group.contracts ?? []).map((contract) => ({
        torDraftBudgetId: group.budgetId,
        ...contract
      }))
    ) ?? [];

    const { status } = await PP008Service.updateAsync(id, procurementStore.procurementDetail.id, detail.value);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(id);

      return statusData ? toast[statusData]() : ToastHelper.updatedMessageToast();
    }
  };

  const approveAsync = async (): Promise<void> => {
    if (!procurementStore.procurementDetail.id || !detail.value.id) return;

    const res = await showReasonDialogAsync(isLastApproval.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (!res.isConfirm) return;

    const { status } = await PP008Service.approveAsync(detail.value.id, procurementStore.procurementDetail.id, res.reason);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(detail.value.id);

      await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id);

      return ToastHelper.approvedMessageToast();
    }
  };

  const rejectAsync = async (): Promise<void> => {
    if (!procurementStore.procurementDetail.id || !detail.value.id) return;

    const res = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (!res.isConfirm) return;

    const { status } = await PP008Service.rejectAsync(detail.value.id, procurementStore.procurementDetail.id, res.reason);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(detail.value.id);

      return ToastHelper.sendEditMessageToast();
    }
  };

  const onSetDefaultAsssignees = async (): Promise<void> => {
    if (!detail.value.contractType) return;

    const { data, status } = await operationService.getContractDirectorAsync();

    if (status === HttpStatusCode.Ok) {
      detail.value.assignees = data;
    }
  };


  const onSetDefaultAcceptors = async (): Promise<void> => {
    if (!detail.value.contractBudgetGroups) return;

    const totalVendorAgreePrice =
      (detail.value.contractBudgetGroups ?? []).reduce(
        (sum, group) =>
          sum +
          (group.contracts ?? []).reduce(
            (subSum, x) => subSum + (x.agreedPrice ?? 0),
            0
          ),
        0
      );

    if (totalVendorAgreePrice === 0) return;

    let processType: SectionProcessType = SectionProcessType.ApprovePurchaseOrder;

    const procurementDetail = procurementStore.procurementDetail;
    const is80 = checkIsEighty(procurementDetail.supplyMethodCode);
    if (is80 && procurementDetail.isCommercialMaterial) {
      processType = SectionProcessType.ApprovePurchaseOrderCommercialParcel;
    }

    const params = {
      processType: processType,
      budget: totalVendorAgreePrice,
      userId: authStore.profile.id,
      supplyMethodCode: procurementStore.procurementDetail.supplyMethodCode,
      supplyMethodSpecialTypeCode: procurementStore.procurementDetail.supplyMethodSpecialTypeCode,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status === HttpStatusCode.Ok) {
      detail.value.acceptors = [...data.map((m, i) => ({
        sequence: i + 1,
        userId: m.userId,
        fullName: m.fullName,
        positionName: m.fullPositionName,
        departmentName: m.businessUnitName,
        organizationLevel: m.organizationLevel,
        acceptorType: AcceptorType.Approver,
        status: AcceptorStatus.Draft,
      }))];

      // เงื่อนไข "ไม่แสดง" modal
      // 1) user เป็น JorPor
      const isJorPor = authStore.profile.departmentCode === DepartmentId.JorPor;
      if (isJorPor) {
        return;
      }

      // 2) (ไม่ใช่ JorPor) AND IsCommercialMaterial=true AND มี default-acceptor มากกว่า 1 รายการ
      //    AND ไม่มี organizationLevel ∈ {100,200,300} AND procurement.departmentCode ตรงกับ user.departmentCode
      const isCommercialMaterial = procurementDetail.isCommercialMaterial === true;
      const hasHighLevelAcceptor = data.some((m): boolean => [100, 200, 300].includes(m.organizationLevel));
      const departmentMatches = procurementDetail.departmentCode === authStore.profile.departmentCode;

      if (isCommercialMaterial && data.length > 1 && !hasHighLevelAcceptor && departmentMatches) {
        return;
      }

      await showAlertDialogAsync('ขั้นตอนอนุมัติใบสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญาอยู่ในอำนาจหน้าที่ของฝ่ายจัดหาและการพัสดุเป็นผู้ดำเนินการ');
    }
  };

  const onCreateApprovalBudgetAsync = async (body: Budgets): Promise<void> => {

    if (detail.value.id && procurementStore.procurementDetail.id) {
      const { status } = await PP008Service.createBudgetAsync(detail.value.id, procurementStore.procurementDetail.id, body);

      if (status === HttpStatusCode.Created) {
        await getByIdAsync(detail.value.id);

        return ToastHelper.success('เพิ่มวงเงินที่จัดซื้อจัดจ้าง', 'เพิ่มวงเงินที่จัดซื้อจัดจ้างสำเร็จ');
      }
    }
  };

  const onUpdateApprovalBudgetAsync = async (body: Budgets): Promise<void> => {

    if (body.id) {
      const { status } = await PP008Service.updateBudgetAsync(body);

      if (status === HttpStatusCode.Created) {
        await getByIdAsync(detail.value.id);

        return ToastHelper.success('แก้ไขวงเงินที่จัดซื้อจัดจ้าง', 'แก้ไขวงเงินที่จัดซื้อจัดจ้างสำเร็จ');
      }
    }
  };


  const onDeleteBudgetAsync = async (id: string): Promise<void> => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

    if (id) {
      const { status } = await PP008Service.onDeleteBudgetAsync(id);

      if (status === HttpStatusCode.NoContent) {
        await getByIdAsync(detail.value.id);

        return ToastHelper.success('ลบวงเงินที่จัดซื้อจัดจ้าง', 'ลบวงเงินที่จัดซื้อจัดจ้างสำเร็จ');
      }
    }
  };


  const onCreateApprovalEntrepreneursAsync = async (body: Entrepreneurs): Promise<void> => {
    if (detail.value.id && procurementStore.procurementDetail.id) {
      const { status } = await PP008Service.createEntrepreneursAsync(detail.value.id, procurementStore.procurementDetail.id, body);

      if (status === HttpStatusCode.Created) {
        await getByIdAsync(detail.value.id);

        return ToastHelper.success('เพิ่มผู้ประกอบการ', 'เพิ่มผู้ประกอบการสำเร็จ');
      }
    }
  };

  const onSetDefaultWinnersAsync = async (): Promise<void> => {
    if (!detail.value.purchaseRequisitionId) return;
    if (!procurementStore.procurementDetail.id || !procurementStore.procurementDetail.purchaseOrder?.id) return;
    if (!detail.value.contractBudgetGroups || detail.value.contractBudgetGroups.length === 0) return;

    const hasEmptyContracts = detail.value.contractBudgetGroups.some((g) => g.contracts.length === 0);
    if (!hasEmptyContracts) return;

    const params = {
      procurementId: procurementStore.procurementDetail.id,
      jp006Id: procurementStore.procurementDetail.purchaseOrder.id,
      pageNumber: 1,
      pageSize: 9999,
    } as PP007GetWinnerCriteria;

    const { data, status } = await pp007service.getListWinnerAsync(params);

    if (status === HttpStatusCode.Ok && data.data?.length > 0) {
      detail.value.contractBudgetGroups.forEach((group) => {
        if (group.contracts.length === 0) {
          group.contracts = data.data.map((winner: PP007GetWinnerResponse, index: number) => ({
            agreedPrice: detail.value.contractBudgetGroups && detail.value.contractBudgetGroups.length > 1 ? undefined : winner.agreedPrice,
            purchaseOrderEntrepreneurId: winner.id,
            sequence: index + 1,
            purchaseOrderEntrepreneurName: winner.name,
            purchaseOrderEntrepreneurEmail: winner.email,
            budget: winner.agreedPrice,
          }));
        }
      });
    }
  };

  const positionInspOptions = ref<Option[]>([]);

  const fetchPositionInspOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoardInsp);
    if (status === HttpStatusCode.Ok) {
      positionInspOptions.value = data;
    }
  };

  return {
    detail,
    status: {
      canEdit,
      canAssign,
      canAssignByAssignee,
      canRecall,
      canApproveOrReject,
      isLastApproval,
      canAssignedApprove,
    },
    fetchPositionInspOptions,
    positionInspOptions,
    getByIdAsync,
    createAsync,
    updateAsync,
    approveAsync,
    rejectAsync,
    onSetDefaultAcceptors,
    onSetDefaultAsssignees,
    onCreateApprovalBudgetAsync,
    onCreateApprovalEntrepreneursAsync,
    onUpdateApprovalBudgetAsync,
    onDeleteBudgetAsync,
    onResetDetail,
    onSetDefaultWinnersAsync,
  }
});
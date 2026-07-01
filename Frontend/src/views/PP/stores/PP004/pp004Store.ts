import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import PP004Service from '@/views/PP/services/PP004/pp004Service';
import type { JorPor04Committee, JorPor04FineRate, JorPor04PaymentTerm, JorPor04Request, JorPor04Requisition, JorPor04SendAction, JorPor04Warranty } from '@/views/PP/models/PP004/pp004Model';
import { HttpStatusCode } from 'axios';
import ToastHelper from '@/helpers/toast';
import type { Option } from '@/models/shared/option';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode, ProRateTypeCodeEnum } from '@/enums/shared';
import type { Budgets, SequenceDescription, TechnicalSpecifications } from '../../models/PP002/pp002Model';
import type { ParticipantsAcceptor, ParticipantsAssignee } from '@/models/shared/participants';
import { OrganizationLevel } from '@/enums/operations';
import operationService from '@/services/Shared/operations';
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from '@/enums/participants';
import { pp004status } from '../../enums/pp004';
import { showReasonDialogAsync } from '@/helpers/dialog';
import { ReasonDialogType } from '@/enums/dialog';
import { useAuthenticationStore } from '@/stores/authentication';
import { usePPDetailStore } from '@/stores/PP/ppStore';
import type { OperationBody } from '@/models/shared/operations';

export const usePP004Store = defineStore('pp004', () => {
  const auth = useAuthenticationStore();
  const procurement = usePPDetailStore();
  const initBody: JorPor04Request = {
    procurementId: '',
    requisition: {
      hasFineRate: false,
      hasWarranty: false,
      hasContractGuarantee: false,
      hasInspectionCommittee: false,
      hasConstructionSupervisor: false,
      status: pp004status.Draft
    } as JorPor04Requisition,
    budgets: [] as Budgets[],
    warranties: [] as JorPor04Warranty[],
    paymentTerms: [] as JorPor04PaymentTerm[],
    fineRates: [] as JorPor04FineRate[],
    committees: [] as JorPor04Committee[],
    acceptors: [] as ParticipantsAcceptor[],
    assignees: [] as ParticipantsAssignee[],
    scopeOfWorks: [] as TechnicalSpecifications[],
    torObjectResponses: [] as SequenceDescription[],
    isProcurementCommittee: true,
    isInspectCommittee: true,
    isMaCommittee: true,
    isSupCommittee: true,
    hasPermission: false,
    departmentCode: undefined,
    supplyMethodCode: '',
    budget: undefined,
    isCommercialMaterial: undefined,
    paymentTypeCode: ProRateTypeCodeEnum.SplitPayment001
  };

  const body = ref<JorPor04Request>(structuredClone(initBody));

  const clearBody = () => {
    body.value = structuredClone(initBody);
  }

  const validatePaymentTerms = (): boolean => {
    const payment = body.value.paymentTerms ?? [];

    if (payment.length > 0) {
      const totalPercent = payment.reduce((sum, d) => sum + (Number(d.percent) || 0), 0);

      if (totalPercent > 0 && Math.abs(totalPercent - 100) > 0.01) {
        ToastHelper.errorDescription(
          `จำนวนเงิน (%) ต้องรวมกันเท่ากับ 100% (ปัจจุบัน ${totalPercent.toFixed(2)}%)`
        );
        return false;
      }
    }

    return true;
  };



  const getDefaultAcceptor = async (): Promise<void> => {
    if (auth.profile.departmentCode !== procurement.procurementDetail.departmentCode) {
      return;
    }

    const { data, status } = await operationService.getDefaultDepartmentApproverByUserIdAsync(auth.profile.id, OrganizationLevel.Department, true);

    if (status == HttpStatusCode.Ok) {
      body.value.acceptors = [];

      data.forEach(item => body.value.acceptors.push({
        acceptorType: AcceptorType.Approver,
        fullName: item.fullName,
        positionName: item.fullPositionName,
        sequence: body.value.acceptors.length + 1,
        status: AcceptorStatus.Draft,
        userId: item.userId,
        departmentName: item.businessUnitName,
        departmentCode: item.businessUnitId,
      } as ParticipantsAcceptor))
    }
  };

  const onCreateAsync = async (procurementId: string, torId?: string) => {
    body.value.procurementId = procurementId;
    body.value.torDraftId = torId;
    body.value.userId = auth.profile.id;

    if (!validatePaymentTerms()) return;

    const { data, status } = await PP004Service.onCreateJorpor04Async(body.value);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.createdMessageToast();

      return data
    }
  };

  const getPp004ByIdAsync = async (id?: string) => {
    if (!procurement.procurementDetail.id) return;

    const { data, status } = await PP004Service.onGetJorpor004ByIdAsync(procurement.procurementDetail.id, id);

    if (status == HttpStatusCode.Ok) {
      body.value = data;
      body.value.requisition.isPurchaseRequisitionDocumentIdReplaced = false;
    }
  };

  const updatePp004Async = async (newStatus?: pp004status) => {
    const mapToast: Record<pp004status, () => void> = {
      [pp004status.Edit]: () => ToastHelper.recallEditMessageToast(),
      [pp004status.WaitingApproval]: () => ToastHelper.sendApproveConfirmMessageToast(),
      [pp004status.WaitingAssign]: () => ToastHelper.updatedMessageToast(),
      [pp004status.Approved]: () => ToastHelper.assignedMessageToast(),
      [pp004status.Rejected]: () => ToastHelper.sendEditMessageToast(),
      [pp004status.Draft]: function (): void {
        throw new Error('Function not implemented.');
      },
      [pp004status.Cancelled]: function (): void {
        throw new Error('Function not implemented.');
      }
    }

    if (!(body.value.id && procurement.procurementDetail.id)) return;

    if (!validatePaymentTerms()) return;

    const { status, data } = await PP004Service.onUpdateJorpor004Async(procurement.procurementDetail.id, body.value.id, body.value, newStatus);

    if (status == HttpStatusCode.Ok) {
      // Update document ID immediately if new version was created
      if (data?.newDocumentFileId) {
        body.value.requisition.purchaseRequisitionDocumentId = data.newDocumentFileId;
      }

      newStatus ? mapToast[newStatus]() : ToastHelper.updatedMessageToast();

      await getPp004ByIdAsync(body.value.id);
    }
  };

  const approvePp004Async = async () => {
    if (!body.value.id) return;

    const acceptor = body.value.acceptors.find(
      (a: ParticipantsAcceptor) => a.status === AcceptorStatus.Pending
    );

    if (!acceptor) return;

    const result = await showReasonDialogAsync(ReasonDialogType.Accepted);

    if (!result.isConfirm) return;

    const Sendbody = {
      id: acceptor.id,
      remarks: result.reason
    } as JorPor04SendAction;

    const { status } = await PP004Service.onApproveAsync(body.value.id, Sendbody);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.approvedMessageToast();

      await getPp004ByIdAsync(body.value.id);
      await procurement.onGetProcurementById(body.value.procurementId);
    }
  };

  const rejectPp004Async = async () => {
    if (!body.value.id) return;

    const acceptor = body.value.acceptors.find(
      (a: ParticipantsAcceptor) => a.status === AcceptorStatus.Pending
    );

    if (!acceptor) return;

    const result = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (!result.isConfirm) return;

    const Sendbody = {
      id: acceptor.id,
      remarks: result.reason
    } as JorPor04SendAction;

    const { status } = await PP004Service.onRejectAsync(body.value.id, Sendbody);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();

      await getPp004ByIdAsync(body.value.id);
    }
  };

  const deliveryConditionOptions = ref<Option[]>([]);
  const fetchDeliveryConditionOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.DelvCUnit);
    if (status === 200) {
      deliveryConditionOptions.value = data;
    }
  };

  const dateTypeOptions = ref<Option[]>([]);
  const fetchDateTypeOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PeriodType);
    if (status === HttpStatusCode.Ok) {
      dateTypeOptions.value = data;
    }
  };

  const warrantyConditionOptions = ref<Option[]>([]);
  const fetchWarrantyConditionOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PeriodCond);
    if (status === HttpStatusCode.Ok) {
      warrantyConditionOptions.value = data;
    }
  };
  const positionProcurementOptions = ref<Option[]>([]);
  const fetchPositionProcurementOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoard);
    if (status === HttpStatusCode.Ok) {
      positionProcurementOptions.value = data;
    }
  };
  const positionInspOptions = ref<Option[]>([]);
  const fetchPositionInspOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoardInsp);
    if (status === HttpStatusCode.Ok) {
      positionInspOptions.value = data;
    }
  };
  const positionProcOptions = ref<Option[]>([]);
  const fetchPositionProcOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoardProc);
    if (status === HttpStatusCode.Ok) {
      positionProcOptions.value = data;
    }
  };
  const positionMaOptions = ref<Option[]>([]);
  const fetchPositionMaOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoardMA);
    if (status === HttpStatusCode.Ok) {
      positionMaOptions.value = data;
    }
  };
  const positionSupOptions = ref<Option[]>([]);
  const fetchPositionSupOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoardSup);
    if (status === HttpStatusCode.Ok) {
      positionSupOptions.value = data;
    }
  };
  const criteriaConditionOptions = ref<Option[]>([]);
  const fetchCriteriaConditionOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CriteriaCons);
    if (status === HttpStatusCode.Ok) {
      criteriaConditionOptions.value = data;
    }
  };

  const IsEdit = computed(() => {
    return [pp004status.Draft, pp004status.Edit, pp004status.Rejected].includes(body.value.requisition.status) && procurement.procurementDetail.departmentCode === auth.profile.departmentCode;
  });

  const isCanSetDefaultAcceptor = computed(() => [pp004status.Draft, pp004status.Edit].includes(body.value.requisition.status));

  const assignDepartmentDDL = ref<Option[]>([]);
  const getAssignDepartmentDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.AssignDept);

    if (status === HttpStatusCode.Ok) {
      assignDepartmentDDL.value = data;
    }
  };

  const canRecall = computed(() => {
    return body.value.requisition.status == pp004status.WaitingApproval && body.value.hasPermission && !body.value.acceptors.some(x => x.status != AcceptorStatus.Pending);
  });

  const setDefaultJorPorDirectorAsync = async (isDisabledLoad?: boolean, isCommercialMaterialUnderDirectorDepartment?: boolean) => {
    if (body.value.assignees.length > 0) return;

    if (isCommercialMaterialUnderDirectorDepartment) {
      body.value.assignees.push({
        userId: auth.profile.id,
        fullName: auth.profile.name,
        positionName: auth.profile.positionName,
        sequence: 1,
        departmentName: auth.profile.departmentName,
        status: AssigneeStatus.Draft,
        assigneeType: AssigneeType.Assignee,
        assigneeGroup: AssigneeGroup.JorPor,
      } as ParticipantsAssignee);

      return;
    }

    const { data, status } = await operationService.getJorPorDirectorAsync(isDisabledLoad);

    if (status === HttpStatusCode.Ok) {
      body.value.assignees.push({
        userId: data.userId,
        fullName: data.fullName,
        positionName: data.fullPositionName,
        sequence: 1,
        departmentName: data.businessUnitName,
        status: AssigneeStatus.Draft,
        assigneeType: AssigneeType.Director,
        assigneeGroup: AssigneeGroup.JorPor,
      } as ParticipantsAssignee);
    }
  };

  const getProcurementDataAsync = async (procurementId: string) => {
    const { data, status } = await PP004Service.onGetProcurementDataAsync(procurementId);

    if (status == HttpStatusCode.Ok) {
      body.value.reason = data.reason;

      body.value.scopeOfWorks = data.scopeOfWorks;
    }
  };

  const assigneeByType = (assigneeType: AssigneeType) => body.value.assignees.filter(f => f.assigneeType === assigneeType).sort(s => s.sequence);

  const isJorPorDirectorAssignee = computed(() => {
    return body.value.requisition.status == pp004status.WaitingAssign && (assigneeByType(AssigneeType.Director).some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) == auth.profile.id));
  });

  const isJorPorAssignee = computed(() => {
    return body.value.requisition.status == pp004status.WaitingAssign && (assigneeByType(AssigneeType.Assignee).some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) == auth.profile.id));
  });

  const assigneesCanAssign = computed(() => {
    return body.value.requisition.status == pp004status.Approved && body.value.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) == auth.profile.id) && procurement.procurementDetail.purchaseOrder?.status !== 'Approved';
  })

  const isLastUnit = computed(() => {
    if (!body.value.acceptors) return;

    const acceptorUser = body.value.acceptors.filter(f => f.status === AcceptorStatus.Pending && f.acceptorType === AcceptorType.Approver);

    if (!acceptorUser) {
      return false;
    }

    const current = acceptorUser.find(f => f.isCurrent);

    if (!current) {
      return false;
    }

    return current.sequence === acceptorUser[acceptorUser.length - 1].sequence;
  })

  const getReviewDocumentAsync = async (id: string, procurementId: string): Promise<string> => {
    const { data, status } = await PP004Service.getReviewDocumentAsync(id, procurementId);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
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

  return {
    body,
    fetchDeliveryConditionOptions,
    fetchDateTypeOptions,
    fetchWarrantyConditionOptions,
    fetchPositionProcurementOptions,
    fetchCriteriaConditionOptions,
    fetchPositionInspOptions,
    fetchPositionProcOptions,
    fetchPositionMaOptions,
    fetchPositionSupOptions,
    deliveryConditionOptions,
    dateTypeOptions,
    warrantyConditionOptions,
    positionProcurementOptions,
    criteriaConditionOptions,
    positionInspOptions,
    positionProcOptions,
    positionMaOptions,
    positionSupOptions,
    getDefaultAcceptor,
    onCreateAsync,
    getPp004ByIdAsync,
    updatePp004Async,
    IsEdit,
    approvePp004Async,
    rejectPp004Async,
    setDefaultJorPorDirectorAsync,
    getProcurementDataAsync,
    isJorPorDirectorAssignee,
    isJorPorAssignee,
    assigneesCanAssign,
    isLastUnit,
    clearBody,
    getReviewDocumentAsync,
    canRecall,
    isCanSetDefaultAcceptor,
    assignDepartmentDDL,
    getAssignDepartmentDDLAsync,
    getDefaultSegmentOtherManagerApproverAsync,
    getDefaultSegmentITManagerApproverAsync,
  };
});
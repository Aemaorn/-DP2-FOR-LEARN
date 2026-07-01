import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { CommitteeDuty, PP005Detail, PP005Response } from "../../models/PP005/pp005Model";
import type { TechnicalSpecifications } from "../../models/PP002/pp002Model";
import type { JorPor04Requisition } from "../../models/PP004/pp004Model";
import PP005Service from "../../services/PP005/PP005Service";
import { HttpStatusCode, type AxiosResponse } from "axios";
import { usePPDetailStore } from "../../../../stores/PP/ppStore";
import { OperatorType, PP005Status } from "../../enums/pp005";
import ToastHelper from "@/helpers/toast";
import operationService from "@/services/Shared/operations";
import { OrganizationLevel, SectionProcessType } from "@/enums/operations";
import type { ParticipantsAcceptor } from "@/models/shared/participants";
import { AcceptorStatus, AcceptorType } from "@/enums/participants";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import { useAuthenticationStore } from "@/stores/authentication";
import type { Option } from "@/models/shared/option";
import SharedService from "@/services/Shared/dropdown";
import { EGroupCode } from "@/enums/shared";
import { checkIsSixty } from "@/helpers/supplyMethod";

export const usePP005DetailStore = defineStore('PP-005-detail-store', () => {
  const procurementStore = usePPDetailStore();
  const auth = useAuthenticationStore();

  const initialModel = () => {
    return structuredClone({
      purchaseRequisition: {
        requisition: {} as JorPor04Requisition,
        scopeOfWorks: [] as Array<TechnicalSpecifications>,
      },
      jp005: {
        inspectionCommittees: {
          committees: [],
          duties: [],
          isCommittee: true,
        } as CommitteeDuty,
        procurementCommittees: {
          committees: [],
          duties: [],
          isCommittee: true,
        } as CommitteeDuty,
        maintenanceInspectionCommittee: {
          committees: [],
          duties: [],
          isCommittee: true,
        } as CommitteeDuty,
        constructionSupervisor: {
          committees: [],
          duties: [],
          isCommittee: true,
        } as CommitteeDuty,
        acceptors: [],
        procurementSuppliesDivision: [],
      } as unknown as PP005Response,
    } as PP005Detail);
  };

  const body = ref<PP005Detail>(initialModel());

  const resetBody = () => {
    body.value = initialModel();
  };

  const onGetByIdAsync = async (procurementId: string, id?: string) => {
    const { data, status } = await PP005Service.onGetJp005ByIdAsync(procurementId, id);

    if (status === HttpStatusCode.Ok) {
      body.value = mapResponseToModel(data, id);
      body.value.jp005.isJp005ApprovalDocumentIdReplaced = false;
      body.value.jp005.isJp005CommandDocumentIdReplaced = false;

      if (!body.value.id) {
        body.value.jp005.procurementCommittees.isCommittee = body.value.purchaseRequisition.isProcurementCommittee;
        body.value.jp005.inspectionCommittees.isCommittee = body.value.purchaseRequisition.isInspectCommittee;
      }
    }
  };

  const mapResponseToModel = (data: PP005Detail, id?: string) => {
    return {
      ...data,
      jp005: {
        ...data.jp005,
        evaluationDueDate: id ? data.jp005.evaluationDueDate : undefined,
        evaluationPeriodTypeCode: id ? data.jp005.evaluationPeriodTypeCode : undefined,
        evaluationPeriodConditionCode: id ? data.jp005.evaluationPeriodConditionCode : 'BRCPType001',
        egpProjectNumber: id ? data.jp005.egpProjectNumber : undefined,
      } as unknown as PP005Response,
    };
  };

  const setDefaultApproverAsync = async () => {
    const assignees = body.value.purchaseRequisition.operators
      .filter(f => f.operatorType === OperatorType.Assignee);

    if (assignees.length === 0) return;

    const assignee = assignees.reduce((prev, curr) =>
      curr.sequence > prev.sequence ? curr : prev
      , assignees[0]);

    if (!assignee) return;

    if (isSixtyOverPrice.value) {
      await getDefaultSegmentAsync(assignee.userId);

      return;
    }

    await getDefaultAcceptorAsync(assignee.userId);
  };

  const getDefaultSegmentAsync = async (operationsUserId: string) => {
    let { data, status } = await operationService.getDefaultDepartmentApproverByUserIdAsync(
      operationsUserId,
      OrganizationLevel.Segment,
      true);

    if (status === HttpStatusCode.Ok && data.length === 0) {
      const response = await operationService.getDefaultDepartmentApproverByUserIdAsync(
        operationsUserId,
        OrganizationLevel.Segment,
        false);

      data = response.data;
      status = response.status;
    }

    if (status === HttpStatusCode.Ok) {
      body.value.jp005.acceptors = [
        ...data.map((item, index) => ({
          acceptorType: AcceptorType.DepartmentDirectorAgree,
          departmentName: item.businessUnitName,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: index + 1,
          userId: item.userId,
          status: AcceptorStatus.Draft,
        } as ParticipantsAcceptor))];
    }
  };

  const getDefaultAcceptorAsync = async (operationsUserId: string) => {
    let processType: SectionProcessType = SectionProcessType.ApprovePurchaseRequest;

    const detail = procurementStore.procurementDetail;

    if (detail.isCommercialMaterial) {
      processType = SectionProcessType.ApprovePurchaseRequestCommercialParcel;
    }

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync({
      budget: procurementStore.procurementDetail.budget,
      processType: processType,
      supplyMethodCode: procurementStore.procurementDetail.supplyMethodCode,
      supplyMethodSpecialTypeCode: procurementStore.procurementDetail.supplyMethodSpecialTypeCode,
      userId: operationsUserId,
    }, true);

    if (status === HttpStatusCode.Ok) {
      body.value.jp005.acceptors = [...data.map((item, index) => ({
        acceptorType: AcceptorType.Approver,
        departmentName: item.businessUnitName,
        fullName: item.fullName,
        positionName: item.fullPositionName,
        sequence: index + 1,
        userId: item.userId,
        status: AcceptorStatus.Draft,
      } as ParticipantsAcceptor))];
    }
  };

  const onSubmitAsync = async (params?: { isJp005ApprovalDocumentIdReplaced?: boolean; }) => {
    if (body.value.id) {
      await onUpdateAsync(body.value.id, undefined, params?.isJp005ApprovalDocumentIdReplaced);

      return;
    }

    await onCreateAsync();
  };

  const onUpdateAsync = async (id: string, jp005Status?: PP005Status, isJp005ApprovalDocumentIdReplaced?: boolean) => {
    const payload = {
      ...body.value,
      status: jp005Status ?? body.value.status,
      jp005: {
        ...body.value.jp005,
        isJp005ApprovalDocumentIdReplaced: isJp005ApprovalDocumentIdReplaced ?? body.value.jp005.isJp005ApprovalDocumentIdReplaced,
      },
    };

    const mapStatusToast: Record<PP005Status, () => void> = {
      Draft: () => ToastHelper.updatedMessageToast(),
      WaitingApproval: () => ToastHelper.sendApproveConfirmMessageToast(),
      Edit: () => ToastHelper.recallEditMessageToast(),
      [PP005Status.Approved]: function (): void {
        throw new Error("Function not implemented.");
      },
      [PP005Status.Rejected]: function (): void {
        throw new Error("Function not implemented.");
      },
      [PP005Status.Cancelled]: function (): void {
        throw new Error("Function not implemented.");
      },
    }

    const { status, data } = await PP005Service.onUpdateAsync(procurementStore.procurementDetail.id!, id, payload);

    if (status === HttpStatusCode.Ok) {
      // Update document IDs immediately if new versions were created
      if (data?.newApprovalDocumentFileId) {
        body.value.jp005.jp005ApprovalDocumentId = data.newApprovalDocumentFileId;
      }
      if (data?.newCommandDocumentFileId) {
        body.value.jp005.jp005CommandDocumentId = data.newCommandDocumentFileId;
      }

      jp005Status ? mapStatusToast[jp005Status]() : ToastHelper.updatedMessageToast();
    }

    await onGetByIdAsync(procurementStore.procurementDetail.id!, id);
  };

  const onCreateAsync = async (jp005Status?: PP005Status) => {
    const payload = {
      ...body.value,
      status: jp005Status ?? body.value.status,
    };

    const { data, status } = await PP005Service.onCreateAsync(procurementStore.procurementDetail.id!, payload);

    if (status === HttpStatusCode.Created) {
      body.value.id = data;
      jp005Status ? ToastHelper.sendApproveConfirmMessageToast() : ToastHelper.createdMessageToast();

      await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id!);
      await onGetByIdAsync(procurementStore.procurementDetail.id!, data);
    }
  };

  const onSendReCallOrApprovalAsync = async (status: PP005Status.WaitingApproval | PP005Status.Edit, isSixtyOverPrice: boolean) => {
    if (status === PP005Status.WaitingApproval && !validateApproval(isSixtyOverPrice)) {
      return;
    }


    const confirmType = status === PP005Status.WaitingApproval ? ConfirmDialogType.SendApproveConfirm : ConfirmDialogType.Edit;

    if (!await showConfirmDialogAsync(confirmType)) return;

    if (body.value.id) {

      body.value.jp005.isJp005CommandDocumentIdReplaced = false;
      await onUpdateAsync(body.value.id, status);

      return;
    }

    await onCreateAsync(status);
  };

  const validateApproval = (isSixtyOverPrice: boolean) => {
    if (body.value.jp005.procurementCommittees.duties.length <= 0) {
      return ToastHelper.errorDescription("กรุณาระบุอำนาจหน้าที่ของผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง");
    }

    if (body.value.jp005.inspectionCommittees.duties.length <= 0) {
      return ToastHelper.errorDescription("กรุณาระบุอำนาจหน้าที่ของผูู้ตรวจรับ/คณะกรรมการตรวจรับพัสดุ");
    }

    if (isSixtyOverPrice && body.value.jp005.acceptors.filter(f => f.acceptorType === AcceptorType.DepartmentDirectorAgree).length <= 0) {
      ToastHelper.segmentAtLeastMessageToast();

      return false;
    }

    if (!isSixtyOverPrice && body.value.jp005.acceptors.filter(f => f.acceptorType === AcceptorType.Approver).length <= 0) {
      ToastHelper.approvalAtLeastMessageToast();

      return false;
    }

    if (body.value.jp005.isHasMaintenanceInspectionCommittee && (body.value.jp005.maintenanceInspectionCommittee.committees.length <= 0 || body.value.jp005.maintenanceInspectionCommittee.duties.length <= 0)) {
      return ToastHelper.errorDescription("กรุณาคณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา) ให้ครบถ้วน");
    }

    if (body.value.jp005.isConstructionSupervisor && (body.value.jp005.constructionSupervisor.committees.length <= 0 || body.value.jp005.constructionSupervisor.duties.length <= 0)) {
      return ToastHelper.errorDescription("กรุณาระบุผู้ควบคุมงาน (เฉพาะงานก่อสร้าง) ให้ครบถ้วน");
    }

    const suppliesDivisionIds = body.value.jp005.procurementSuppliesDivision.map((c): string => c.userId);
    const procCommitteeIds = body.value.jp005.procurementCommittees.committees.map((c): string => c.userId);
    const hasDuplicate = suppliesDivisionIds.some((id): boolean => procCommitteeIds.includes(id));
    if (hasDuplicate) {
      return ToastHelper.warning('ไม่สามารถส่งเห็นชอบ/อนุมัติได้', 'รายชื่อผู้จัดทำและผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้างซ้ำกัน');
    }

    return true;
  };

  const onApprovedRejectedAsync = async (type: 'Approve' | 'Reject') => {
    if (!(body.value.procurementId && body.value.id)) return;

    const dialogType: Record<'Approve' | 'Reject', ReasonDialogType> = {
      Approve: (isSegment.value || !isLastApproval.value) ? ReasonDialogType.Accepted : ReasonDialogType.Approve,
      Reject: isSegment.value ? ReasonDialogType.NotAgree : ReasonDialogType.Reject
    };

    const resp = await showReasonDialogAsync(dialogType[type], type === 'Reject');

    if (!resp.isConfirm) return;

    const apiMap: Record<'Approve' | 'Reject', () => Promise<AxiosResponse<any, any>>> = {
      Approve: () => PP005Service.onApprovedAsync(body.value.procurementId!, body.value.id!, { remark: resp.reason }),
      Reject: () => PP005Service.onRejectedAsync(body.value.procurementId!, body.value.id!, { remark: resp.reason }),
    }

    const { status } = await apiMap[type]();

    if (status === HttpStatusCode.Ok) {
      type === 'Approve' ? ToastHelper.approvedMessageToast() : ToastHelper.sendEditMessageToast();
    }

    await onGetByIdAsync(body.value.procurementId, body.value.id);
    await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id!);
  };

  const onSendEditToPurchaseRequisitionAsync = async () => {
    if (!procurementStore.procurementDetail.id) return;

    const resp = await showReasonDialogAsync(ReasonDialogType.Reject);
    if (!resp.isConfirm) return;

    const { status } = await PP005Service.onSendEditToPurchaseRequisitionAsync(procurementStore.procurementDetail.id, { remark: resp.reason });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();
      await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id);
    }
  };

  const isEdit = computed(() => {
    const allowActionStatus = [PP005Status.Draft, PP005Status.Edit, PP005Status.Rejected].includes(body.value.status);
    const isOperator = body.value?.purchaseRequisition.operators?.some(s => s.userId === auth.profile.id) ?? false;
    const isProcurementSuppliesDivision = body.value?.jp005.procurementSuppliesDivision.some(s => s.userId === auth.profile.id) ?? false;

    return allowActionStatus && (isOperator || isProcurementSuppliesDivision);
  })

  const isCanSetDefault = computed(() => {
    const allowActionStatus = [PP005Status.Draft, PP005Status.Edit].includes(body.value.status);
    const isOperator = body.value?.purchaseRequisition.operators?.some(s => s.userId === auth.profile.id) ?? false;
    const isProcurementSuppliesDivision = body.value?.jp005.procurementSuppliesDivision.some(s => s.userId === auth.profile.id) ?? false;

    return allowActionStatus && (isOperator || isProcurementSuppliesDivision);
  });

  const isRecall = computed(() => {
    const allowActionStatus = [PP005Status.WaitingApproval].includes(body.value.status);
    const isAllAcceptorPending = body.value.jp005.acceptors.every(s => s.status === AcceptorStatus.Pending);
    const isOperator = body.value?.purchaseRequisition.operators?.some(s => s.userId === auth.profile.id) ?? false;
    const isProcurementSuppliesDivision = body.value?.jp005.procurementSuppliesDivision.some(s => s.userId === auth.profile.id) ?? false;

    return allowActionStatus && isAllAcceptorPending && (isOperator || isProcurementSuppliesDivision);
  });

  const isCurrentApproval = computed(() => {
    if (!body.value.jp005.acceptors) return false;
    const status = [PP005Status.WaitingApproval].includes(body.value.status);
    const checkQue = isCurrentPendingAcceptor(body.value.jp005.acceptors, auth.profile.id);
    return status && checkQue;
  });

  const isLastApproval = computed(() => [PP005Status.WaitingApproval].includes(body.value.status) && body.value.jp005.acceptors.filter(f => f.acceptorType === AcceptorType.Approver && f.status === AcceptorStatus.Pending).length === 1);

  const isSegment = computed(() => body.value.jp005.acceptors.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id && s.acceptorType === AcceptorType.DepartmentDirectorAgree));

  const isSixtyOverPrice = computed(() => checkIsSixty(procurementStore.procurementDetail.supplyMethodCode) && procurementStore.procurementDetail.budget > 100000);

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

  const getReviewDocumentAsync = async (id: string, procurementId: string, documentType: string): Promise<string> => {
    const { data, status } = await PP005Service.getReviewDocumentAsync(id, procurementId, documentType);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };

  return {
    body,
    positionProcOptions,
    positionInspOptions,
    positionMaOptions,
    positionSupOptions,
    fn: {
      resetBody,
      onGetByIdAsync,
      setDefaultApproverAsync,
      onSubmitAsync,
      onSendReCallOrApprovalAsync,
      onApprovedRejectedAsync,
      fetchPositionInspOptions,
      fetchPositionProcOptions,
      getReviewDocumentAsync,
      onSendEditToPurchaseRequisitionAsync,
      fetchPositionMaOptions,
      fetchPositionSupOptions,
    },
    states: {
      isEdit,
      isRecall,
      isCurrentApproval,
      isSegment,
      isLastApproval,
      isCanSetDefault,
    },
  };
});
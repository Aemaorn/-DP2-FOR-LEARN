import type {
  TPP001Detail,
  TPP001AppointDto,
  TP001SendAction,
} from '@/views/PP/models/PP001/pp001Model';
import { defineStore } from 'pinia';
import { computed, ref, type Ref } from 'vue';
import { AppointStatus } from '../../enums/pp001';
import type { TCommitteeSection, TDutySection } from '../../../../models/PP/ppModel';
import type { ParticipantsAcceptor } from '@/models/shared/participants';
import appointmentService from '../../services/PP001/PP001Service';
import { HttpStatusCode } from 'axios';
import ToastHelper from '@/helpers/toast';
import type { Option } from '@/models/shared/option';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import operationService from '@/services/Shared/operations';
import type { defaultAcceptorCriteria } from '@/models/shared/operations';
import { SectionProcessType } from '@/enums/operations';
import { AcceptorStatus, AcceptorType } from '@/enums/participants';
import { showConfirmDialogAsync, showReasonDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import { useAuthenticationStore } from '@/stores/authentication';
import { usePPDetailStore } from '@/stores/PP/ppStore';
import { checkIsEighty } from '@/helpers/supplyMethod';
import { PP002Status } from '../../enums/pp002';
import { isCurrentPendingAcceptor } from '@/helpers/participants';

const getPobDropdownAsync = async (target: Ref<Option[]>, parentCode?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoard, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data.slice(0, -1);
  }
};

const getPob1DropdownAsync = async (target: Ref<Option[]>, parentCode?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoard, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data.splice(data.length - 1, 1);
  }
};

export const usePP001DetailStore = defineStore('PP-001-detail-store', () => {
  const auth = useAuthenticationStore();
  const procurementStore = usePPDetailStore();
  const initBody: TPP001Detail = {
    budget: procurementStore.procurementDetail.budget,
    appoint: {
      procurementId: "",
      status: AppointStatus.Draft,
      procurementBudgetYear: 0,
      isChange: false,
      isCancel: false,
    } as TPP001AppointDto,
    torDraftCommittees: [] as TCommitteeSection[],
    torDraftCommitteeDuties: [] as TDutySection[],
    medianPriceCommittees: [] as TCommitteeSection[],
    medianPriceCommitteeDuties: [] as TDutySection[],
    acceptors: [] as ParticipantsAcceptor[],
    isMedianPriceCommittee: true,
    isTorCommittee: true,
    hasPermission: true,
  };

  const pp001Detail = ref<TPP001Detail>(structuredClone(initBody));

  const pobDropdown = ref<Option[]>([] as Option[]);
  const pob1Dropdown = ref<Option[]>([] as Option[]);

  const onGetAppointmentById = async (id: string): Promise<void> => {
    const { data, status } = await appointmentService.getPP001ByIdAsync(id);

    if (status == HttpStatusCode.Ok) {
      pp001Detail.value = {
        ...data,
        budget: procurementStore.procurementDetail.budget,
        appointDocumentVersions: data.appointDocumentVersions,
      };
    }
  };

  const onSetDefaultDuties = async () => {
    if (pp001Detail.value.torDraftCommitteeDuties.length === 0) {
      const torDuties = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.TorDuties, undefined, true);

      if (torDuties.status === HttpStatusCode.Ok) {
        pp001Detail.value.torDraftCommitteeDuties = torDuties.data.map((value, index): TDutySection => ({ sequence: index + 1, description: value.label }));
      }
    }

    if (procurementStore.procurementDetail.budget > 100000 && pp001Detail.value.medianPriceCommitteeDuties.length === 0) {
      const mdpDuties = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.MedianPriceDuties, undefined, true);
      if (mdpDuties.status === HttpStatusCode.Ok) {
        pp001Detail.value.medianPriceCommitteeDuties = mdpDuties.data.map((value, index): TDutySection => ({ sequence: index + 1, description: value.label }));
      }
    }
  };

  const createAppointment = async (): Promise<void> => {
    if (!procurementStore.procurementDetail.id) return;

    const { data, status } = await appointmentService.createPP001Async(pp001Detail.value);

    if (status == HttpStatusCode.Ok) {
      pp001Detail.value.appoint.id = data;
      ToastHelper.createdMessageToast();
      await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id);
      await onGetAppointmentById(data);
    }
  };

  const getPobDDLAsync = async (): Promise<void> => {
    await getPobDropdownAsync(pobDropdown);
  };

  const getPob1DDLAsync = async (): Promise<void> => {
    await getPob1DropdownAsync(pob1Dropdown);
  };

  const resetPP001Detail = (): void => {
    pp001Detail.value = structuredClone(initBody);
  };


  const isAuthDepartment = computed(() => {
    const isTorNotSubmitted = !procurementStore.procurementDetail.torDraft?.status || [PP002Status.Draft, PP002Status.Rejected, PP002Status.Edit].includes(procurementStore.procurementDetail.torDraft?.status as PP002Status);

    return procurementStore.procurementDetail.departmentCode === auth.profile.departmentCode && isTorNotSubmitted;
  });


  const getDefaultAcceptor = async (budget: number, userId: string, supplyMethodCode?: string, supplyMethodSpecialTypeCode?: string, isStock?: boolean, isCommercialMaterial?: boolean): Promise<void> => {
    let processType: SectionProcessType = SectionProcessType.AppointPreProcurement;

    const is80 = checkIsEighty(supplyMethodCode);
    if (is80) {
      if (isStock) {
        processType = SectionProcessType.AppointPreProcurementStock;
      } else if (isCommercialMaterial) {
        processType = SectionProcessType.AppointPreProcurementCommercialParcel;
      }
    }

    const params = {
      supplyMethodCode,
      supplyMethodSpecialTypeCode,
      processType,
      budget,
      userId,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status == HttpStatusCode.Ok) {
      pp001Detail.value.acceptors = [];

      data.forEach(item => pp001Detail.value.acceptors.push({
        acceptorType: AcceptorType.Approver,
        fullName: item.fullName,
        positionName: item.fullPositionName,
        sequence: pp001Detail.value.acceptors.length + 1,
        status: AcceptorStatus.Draft,
        userId: item.userId,
        departmentName: item.businessUnitName,
      } as ParticipantsAcceptor))
    }
  };

  const updateAppointById = async (id: string, newStatus?: AppointStatus): Promise<void> => {
    const payload = {
      ...pp001Detail.value,
      appoint: {
        ...pp001Detail.value.appoint,
        status: newStatus ?? pp001Detail.value.appoint.status
      }
    } as TPP001Detail;

    const toast = {
      [AppointStatus.WaitingApproval]: () => ToastHelper.sendApproveConfirmMessageToast(),
      [AppointStatus.Edit]: () => ToastHelper.recallEditMessageToast(),
    }

    const { status, data } = await appointmentService.updatePP001ByIdAsync(id, payload);

    if (status == HttpStatusCode.Ok) {
      // Update document ID immediately if new version was created
      if (data?.newDocumentFileId) {
        pp001Detail.value.appointDocumentId = data.newDocumentFileId;
      }

      newStatus ? toast[newStatus as AppointStatus.WaitingApproval | AppointStatus.Edit]() : ToastHelper.updatedMessageToast();

      await onGetAppointmentById(id);
    };
  };

  const approveAction = async (): Promise<void> => {
    const acceptor = pp001Detail.value.acceptors.find(
      (a: ParticipantsAcceptor) => a.status === AcceptorStatus.Pending
    );

    if (!acceptor) return;

    const result = await showReasonDialogAsync(isLastAcceptorApproval.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (!result.isConfirm) return;

    const action = {
      acceptorId: acceptor.id,
      appointId: pp001Detail.value.appoint.id,
      acceptorType: acceptor.acceptorType,
      remark: result.reason,
    } as TP001SendAction;

    const { status } = await appointmentService.approvePP001Async(action);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.approvedMessageToast();
      await onGetAppointmentById(pp001Detail.value.appoint.id!);
      await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id!);
    }
  };

  const RejectAction = async (): Promise<void> => {
    const acceptor = pp001Detail.value.acceptors.find(
      (a: ParticipantsAcceptor) => a.status === AcceptorStatus.Pending
    );

    if (!acceptor) return;

    const result = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (!result.isConfirm) return;

    const action = {
      acceptorId: acceptor.id,
      appointId: pp001Detail.value.appoint.id,
      acceptorType: acceptor.acceptorType,
      remark: result.reason,
    } as TP001SendAction;

    const { status } = await appointmentService.rejectPP001Async(action);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();

      await onGetAppointmentById(pp001Detail.value.appoint.id!);
    }
  };

  const isEdit = computed(() => {
    return [AppointStatus.Draft, AppointStatus.Edit, AppointStatus.Rejected].includes(pp001Detail.value.appoint.status) && procurementStore.procurementDetail.departmentCode === auth.profile.departmentCode;
  });

  const isReturn = computed(() => {
    return isEdit.value && (pp001Detail.value.appoint.isChange || pp001Detail.value.appoint.isCancel);
  });

  const isCanSetDefaultApprover = computed(() => [AppointStatus.Draft, AppointStatus.Edit, AppointStatus.Rejected].includes(pp001Detail.value.appoint.status));

  const requestAction = async (isEdit: boolean): Promise<void> => {
    const res = await showReasonDialogAsync(isEdit ? ReasonDialogType.RequestChange : ReasonDialogType.RequestCancel, true);

    if (!res.isConfirm || !res.reason) return;

    const { data, status } = await appointmentService.requestActionPP001Async(pp001Detail.value.appoint.id!, isEdit, res.reason);

    if (status == HttpStatusCode.Created) {
      pp001Detail.value.appoint.id = data;

      isEdit ? ToastHelper.changedMessageToast() : ToastHelper.canceledMessageToast();

      await onGetAppointmentById(data);

      await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id!);
    }
  };

  const onRestoreStateAsync = async () => {
    if (!pp001Detail.value.appoint.id) return;

    const res = await showReasonDialogAsync(ReasonDialogType.Confirm, true, pp001Detail.value.appoint.isChange ? 'ยืนยันการคืนสถานะคำขอเปลี่ยนแปลง' : 'ยืนยันการคืนสถานะคำขอยกเลิก');

    if (!res.isConfirm) return;

    if (!res.reason) return;

    const { data, status } = await appointmentService.restoreStateAsync(pp001Detail.value.appoint.id, res.reason);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast()

      await onGetAppointmentById(data);

      await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id!);
    }
  }

  const onRecallAsync = async () => {
    if (!pp001Detail.value.appoint.id || !await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

    updateAppointById(pp001Detail.value.appoint.id, AppointStatus.Edit);
  };

  const isCurrentApproval = computed(() => {
    if (!pp001Detail.value.acceptors) return false;
    const checkQue = isCurrentPendingAcceptor(pp001Detail.value.acceptors, auth.profile.id, AcceptorType.Approver);
    return checkQue;
  });

  const isLastAcceptorApproval = computed(() => {
    if (pp001Detail.value.acceptors.length === 0) return false;

    const acceptorData = pp001Detail.value.acceptors
      .filter(f => f.status === AcceptorStatus.Pending);

    const current = acceptorData
      .reduce((prev, curr) => curr.sequence < prev.sequence ? curr : prev,
        acceptorData[0]);

    return isCurrentApproval.value && current.sequence === acceptorData[acceptorData.length - 1].sequence;
  });

  const isRecall = computed(() => {
    return pp001Detail.value.hasPermission && pp001Detail.value.acceptors.every(e => e.status === AcceptorStatus.Pending) && [AppointStatus.WaitingApproval].includes(pp001Detail.value.appoint.status);
  });

  const getReviewDocumentAsync = async (id: string): Promise<string> => {
    const { data, status } = await appointmentService.getReviewDocumentAsync(id);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };


  return {
    pp001Detail,
    onGetAppointmentById,
    resetPP001Detail,
    createAppointment,
    pobDropdown,
    getPobDDLAsync,
    getPob1DDLAsync,
    pob1Dropdown,
    getDefaultAcceptor,
    updateAppointById,
    isEdit,
    approveAction,
    RejectAction,
    requestAction,
    onRecallAsync,
    getReviewDocumentAsync,
    onSetDefaultDuties,
    onRestoreStateAsync,
    states: {
      isCurrentApproval,
      isLastAcceptorApproval,
      isRecall,
      isCanSetDefaultApprover,
      isAuthDepartment,
      isReturn,
    }
  };
}
);

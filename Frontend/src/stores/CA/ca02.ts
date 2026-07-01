import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import type { CA02Body, CA02ContractVendorInfo, CA02DeliveryAcceptancePeriodInfo, CA02Entrepreneur, TCA02Criteria, TCA02DialogCriteria, TCA02DialogTable, TCA02List, InspectionCommitteeSection } from '@/models/CA/ca02';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { Option, OptionBadge } from '@/models/shared/option';
import { EGroupCode, EWorkProcess } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { CA02Status } from '@/enums/CA/ca02';
import { CA02Helper } from '@/helpers/CA/ca02';
import type { ParticipantsCommitteeAcceptor } from '@/models/shared/participants';
import { useAuthenticationStore } from '../authentication';
import { AcceptorStatus, AcceptorType } from '@/enums/participants';
import ToastHelper from '@/helpers/toast';
import { showConfirmDialogAsync, showReasonDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import router from '@/router';
import CA02Service from '@/services/CA/ca02';
import SharedService from '@/services/Shared/dropdown';
import { CommitteePositions } from '@/enums/PCM005/principle';

export const useCA02ListStore = defineStore('ca02-list-store', () => {
  const initCriteria: TCA02Criteria = {
    status: CA02Status.All,
    pageNumber: 1,
    pageSize: 10,
    workProcess: EWorkProcess.InProcess,
  };

  const initTable: TDataTableResult<TCA02List> = {
    data: [],
    totalRecords: 0,
  };

  const { MapOptionBadgeStatus } = CA02Helper;
  const searchCriteria = ref<TCA02Criteria>(structuredClone(initCriteria));
  const statusOptions = ref<OptionBadge[]>([]);
  const table = ref<TDataTableResult<TCA02List>>(structuredClone(initTable));

  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    searchCriteria.value.pageNumber = pageNumber;
    searchCriteria.value.pageSize = pageSize;
  };

  const onGetListAsync = async (): Promise<void> => {
    const { data, status } = await CA02Service.onGetListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data.data;
      statusOptions.value = MapOptionBadgeStatus(data.statusCount);
    }
  }

  const onResetCriteria = (): void => {
    searchCriteria.value = structuredClone(initCriteria);
  };

  const onDeleteAsync = async (contractId: string, id: string) => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

    const { status } = await CA02Service.onDeleteAsync(contractId, id);

    if (status === HttpStatusCode.NoContent) {
      await onGetListAsync();
    }
  };

  return {
    searchCriteria,
    table,
    statusOptions,
    fn: {
      onGetListAsync,
      onChangePageSize,
      onResetCriteria,
      onDeleteAsync,
    },
  };
});

export const useCA02ContractDialogStore = defineStore('ca02-contract-dialog-store', () => {
  const initCriteria: TCA02DialogCriteria = {
    pageNumber: 1,
    pageSize: 10,
  };

  const initTable: TDataTableResult<TCA02DialogTable> = {
    data: [],
    totalRecords: 0,
  };

  const searchCriteria = ref<TCA02DialogCriteria>(structuredClone(initCriteria));
  const table = ref<TDataTableResult<TCA02DialogTable>>(structuredClone(initTable));

  const onGetListAsync = async () => {
    const { data, status } = await CA02Service.onGetDialogListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data;
    }
  };

  const onResetCriteria = () => {
    searchCriteria.value = structuredClone(initCriteria);
    onGetListAsync();
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  return {
    searchCriteria,
    table,
    fn: {
      onGetListAsync,
      onResetCriteria,
      onChangePageSize,
    },
  };
});

export const useCA02DetailStore = defineStore('ca02-detail-store', () => {
  const initBody: CA02Body = {
    status: CA02Status.Draft,
    acceptors: [] as Array<ParticipantsCommitteeAcceptor>,
    inspectionCommittees: { committees: [] as Array<ParticipantsCommitteeAcceptor>, isCommittee: true },
    contractVendorInfo: {
      entrepreneur: {} as CA02Entrepreneur,
    } as CA02ContractVendorInfo,
    deliveryAcceptancePeriodInfo: [] as Array<CA02DeliveryAcceptancePeriodInfo>,
    documentId: undefined,
    isReplace: undefined,
    isResetDocument: false,
    attachments: [],
  };

  const auth = useAuthenticationStore();
  const body = ref<CA02Body>(structuredClone(initBody));
  const isManualContractInfoEditing = ref(false);

  const supplyMethodCodeDDL = ref<Option[]>([]);
  const supplyMethodTypeCodeDDL = ref<Option[]>([]);
  const supplyMethodSpecialTypeCodeDDL = ref<Option[]>([]);

  const getSupplyMethodCodeDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, undefined, true);
    if (status === HttpStatusCode.Ok) supplyMethodCodeDDL.value = data;
  };

  const getSupplyMethodTypeCodeDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethodType, undefined, true);
    if (status === HttpStatusCode.Ok) supplyMethodTypeCodeDDL.value = data;
  };

  const getSupplyMethodSpecialTypeCodeDDLAsync = async (parentCode?: string): Promise<void> => {
    supplyMethodSpecialTypeCodeDDL.value = [];
    if (!parentCode) return;
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, parentCode, true);
    if (status === HttpStatusCode.Ok) supplyMethodSpecialTypeCodeDDL.value = data;
  };

  const onResetBody = () => {
    body.value = structuredClone(initBody);
    isManualContractInfoEditing.value = false;
  }

  const getContractVendorId = () => body.value.isManual ? 'manual' : body.value.contractVendorInfo.id;

  const mapInspectionCommittees = (data: Record<string, any>): InspectionCommitteeSection => ({
    committees: (data.inspectionCommittees?.committees ?? []).map((c: any): ParticipantsCommitteeAcceptor => ({
      id: c.id,
      userId: c.userId,
      fullName: c.fullName,
      positionName: c.fullPositionName,
      departmentName: '',
      sequence: c.sequence,
      acceptorType: AcceptorType.AcceptanceCommittee,
      status: AcceptorStatus.Draft,
      isUnableToPerformDuties: false,
      committeePositionsCode: c.committeePositionsCode,
      committeePositionName: c.committeePositionName,
    })),
    isCommittee: data.inspectionCommittees?.isCommittee ?? true,
  });

  const onGetByIdAsync = async (contractDraftVendorId: string | undefined, id?: string) => {
    const { data, status } = await CA02Service.onGetByIdAsync(contractDraftVendorId, id);

    if (status === HttpStatusCode.Ok) {
      body.value = {
        ...data,
        inspectionCommittees: mapInspectionCommittees(data),
      };
    }
  };

  const onSubmitAsync = async () => {
    if (body.value.id) {
      await onUpdateAsync(body.value.id);

      return;
    }

    await onCreateAsync();
  };

  const onCreateAsync = async () => {
    const { data, status } = await CA02Service.onCreateAsync(getContractVendorId(), body.value);

    if (status === HttpStatusCode.Created) {
      isManualContractInfoEditing.value = false;

      router.replace(`/ca/ca02/detail/${data}`);
      ToastHelper.createdMessageToast();

      await onGetByIdAsync(getContractVendorId(), data);
      return;
    }

    await onGetByIdAsync(getContractVendorId());
  };

  const onUpdateAsync = async (id: string, ca02Status?: CA02Status.WaitingForCommitteeApproval | CA02Status.Edit | CA02Status.Cancelled) => {
    const toast = {
      [CA02Status.WaitingForCommitteeApproval]: () => ToastHelper.sendApproveConfirmMessageToast(),
      [CA02Status.Edit]: () => ToastHelper.recallEditMessageToast(),
      [CA02Status.Cancelled]: () => ToastHelper.canceledMessageToast(),
    };

    const payload: CA02Body = {
      ...body.value,
      status: ca02Status ?? body.value.status,
    };

    const { status, data } = await CA02Service.onUpdateAsync(getContractVendorId(), id, payload);

    if (status === HttpStatusCode.Ok) {
      if (data?.newDocumentFileId) {
        body.value.documentId = data.newDocumentFileId;
      }
      ca02Status ? toast[ca02Status]() : ToastHelper.updatedMessageToast();

      isManualContractInfoEditing.value = false;
      await onGetByIdAsync(getContractVendorId(), body.value.id);

      return;
    }

    await onGetByIdAsync(getContractVendorId());
  };

  const onSendCommitteeApprovalAsync = async () => {
    if (!body.value.id || !await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

    await onUpdateAsync(body.value.id, CA02Status.WaitingForCommitteeApproval);
  };

  const onUpdateDutiesStatusAsync = async (isUnableDuties: boolean, acceptorId: string, remark?: string) => {
    if (!body.value.id) return;

    const { status } = await CA02Service.onSetDutiesStatusAsync(body.value.contractVendorInfo.id, body.value.id, acceptorId, { isUnableToPerformDuties: isUnableDuties, remark: remark });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
    }

    await onGetByIdAsync(getContractVendorId(), body.value.id);
  };

  const onRecallAsync = async () => {
    if (!body.value.id || !await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

    const { status } = await CA02Service.onRecallAsync(body.value.contractVendorInfo.id, body.value.id);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.recallEditMessageToast();
    }

    await onGetByIdAsync(getContractVendorId(), body.value.id);
  };

  const onSendApproveOrRejectAsync = async (type: 'Approve' | 'Reject') => {
    if (!body.value.id) return;

    const reasonDialogType = {
      'Approve': isBossCommitteeApproval.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted,
      'Reject': ReasonDialogType.NotAgree,
    }

    const resp = await showReasonDialogAsync(reasonDialogType[type]);

    if (!resp.isConfirm) return;

    const mapApi = {
      'Approve': async () => await CA02Service.onApproveAsync(body.value.contractVendorInfo.id, body.value.id!, { remark: resp.reason }),
      'Reject': async () => await CA02Service.onRejectAsync(body.value.contractVendorInfo.id, body.value.id!, { remark: resp.reason }),
    }

    const { status } = await mapApi[type]();

    if (status === HttpStatusCode.Ok) {
      type === 'Approve' ? ToastHelper.approvedMessageToast() : ToastHelper.notAgreeMessageToast();
    }

    await onGetByIdAsync(getContractVendorId(), body.value.id);
  };

  const isEdit = computed((): boolean => [CA02Status.Draft, CA02Status.Rejected, CA02Status.Edit].includes(body.value.status) &&
    body.value.acceptors.some((s): boolean => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isRecall = computed(() => {
    if (!body.value.acceptors) return false;

    const status = body.value.status === CA02Status.WaitingForCommitteeApproval;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status)
      && a.acceptorType === AcceptorType.AcceptanceCommittee
      && a.committeePositionsCode == CommitteePositions.PosBoard001);

    const isCommittee = body.value.acceptors.some(s => s.acceptorType === AcceptorType.AcceptanceCommittee && s.userId === auth.profile.id);

    return status && !isAnyApprovalAndRejected && isCommittee;
  });

  const isCommitteeApproval = computed(() => [CA02Status.WaitingForCommitteeApproval].includes(body.value.status) &&
    body.value.acceptors.some(s => s.acceptorType === AcceptorType.AcceptanceCommittee && s.userId === auth.profile.id));

  const isCommitteeCurrentApproval = computed(() => {
    if (!isCommitteeApproval.value) return false;

    if (body.value.acceptors.length === 1) {
      return true;
    }

    const isBoss = body.value.acceptors[0].userId === auth.profile.id;

    if (isBoss) {
      return body.value.acceptors
        .filter((value, index) => (index !== 0 && !value.isUnableToPerformDuties))
        .every(s => s.status !== AcceptorStatus.Pending);
    }

    return body.value.acceptors
      .some(s => s.userId === auth.profile.id && !s.isUnableToPerformDuties && s.isCurrent);
  });

  const isBossCommitteeApproval = computed(() => {
    if (!isCommitteeApproval.value) return false;

    if (body.value.acceptors.length === 1) {
      return true;
    }

    const isBoss = body.value.acceptors[0].userId === auth.profile.id;

    return isBoss;
  });

  const canRestoreVersion = computed(() => isEdit.value);

  const onUpsertAttachments = async () => {
    if (!body.value.id) return;

    const { status } = await CA02Service.attachmentsAsync(
      body.value.contractVendorInfo.id,
      body.value.id,
      body.value.attachments,
    );

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await onGetByIdAsync(getContractVendorId(), body.value.id);
    }
  };

  const resetDocumentAsync = async (): Promise<void> => {
    if (!body.value.id) return;

    const { status } = await CA02Service.resetDocumentAsync(body.value.contractVendorInfo.id, body.value.id);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
      await onGetByIdAsync(getContractVendorId(), body.value.id);
    }
  };

  const onGetReviewDocumentAsync = async (contractVendorInfoId: string, id: string): Promise<string> => {
    const { data, status } = await CA02Service.onGetReviewDocumentAsync(contractVendorInfoId, id);

    if (status !== HttpStatusCode.Ok) {
      ToastHelper.error('ดึงข้อมูลเอกสาร', 'ไม่พบเอกสารที่ต้องการ');

      return '';
    }

    return data;
  };

  return {
    body,
    isManualContractInfoEditing,
    supplyMethodCodeDDL,
    supplyMethodTypeCodeDDL,
    supplyMethodSpecialTypeCodeDDL,
    fn: {
      onGetByIdAsync,
      getSupplyMethodCodeDDLAsync,
      getSupplyMethodTypeCodeDDLAsync,
      getSupplyMethodSpecialTypeCodeDDLAsync,
      onUpdateDutiesStatusAsync,
      onSubmitAsync,
      onSendCommitteeApprovalAsync,
      onResetBody,
      onRecallAsync,
      onSendApproveOrRejectAsync,
      onGetReviewDocumentAsync,
      resetDocumentAsync,
      onUpsertAttachments,
    },
    states: {
      isEdit,
      isRecall,
      isCommitteeApproval,
      isCommitteeCurrentApproval,
      isBossCommitteeApproval,
      canRestoreVersion,
    },
  };
});
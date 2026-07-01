import type { RP002Body, RP002ContractComplete, RP002Summary } from "@/models/RP/rp002";
import type { ParticipantsAcceptor, ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia"
import { computed, ref } from "vue";
import { RP002Status } from "@/enums/RP/rp002";
import RP002Service from "@/services/RP/rp002";
import ToastHelper from "@/helpers/toast";
import router from "@/router";
import { AcceptorStatus, AcceptorType } from "@/enums/participants";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import { useAuthenticationStore } from "@/stores/authentication";
import type { Attachments } from "@/models/shared/uploadFile";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import operationService from "@/services/Shared/operations";
import { SectionProcessType } from "@/enums/operations";
import { SupplyMethodCode } from "@/enums/supplyMethod";

export const useRP002DetailStore = defineStore('rp002-detail-store', () => {
  const authStore = useAuthenticationStore();

  const body = ref<RP002Body>({
    status: RP002Status.Draft,
    acceptors: [] as ParticipantsAcceptor[],
    detail: [] as RP002ContractComplete[],
    attachments: [] as Attachments[],
  } as RP002Body);
  const dataTableDialog = ref<RP002Summary[]>([]);

  const canEdit = computed(() => {
    const status = [RP002Status.Draft, RP002Status.Edit, RP002Status.Rejected].includes(body.value.status);

    return status;
  });

  const canRecall = computed(() => {
    const status = body.value.status === RP002Status.WaitingApproval;
    const checkApproveReject = body.value.acceptors?.every(a => a.status === AcceptorStatus.Pending);

    return status && checkApproveReject;
  });

  const canApproveReject = computed(() => {
    if (!body.value.acceptors) return;

    const status = body.value.status === RP002Status.WaitingApproval;
    const checkQue = isCurrentPendingAcceptor(body.value.acceptors, authStore.profile.id, AcceptorType.Approver);

    return status && checkQue;
  });

  const isLastApproval = computed(() => {
    const pendingAcceptors = body.value.acceptors?.filter(a => a.status === AcceptorStatus.Pending) || [];
    return pendingAcceptors.length === 1;
  });

  const canRestoreVersion = computed(() => canEdit.value);

  const getByIdAsync = async (id: string) => {
    const { data, status } = await RP002Service.getByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;

      body.value.detail?.forEach(d => {
        if (d.contractTypeCode?.startsWith("CMRentalType")) {
          d.contractTypeCode = d.contractTypeCode.replace("CMRentalType", "CMType");
        }
      });
    }
  };

  const createAsync = async (rp002Status?: RP002Status) => {
    if (rp002Status) {
      body.value.status = rp002Status;
    }

    const { data, status } = await RP002Service.createAsync(body.value);

    if (status === HttpStatusCode.Created) {
      router.replace(`/rp/rp002/detail/${data}`);

      await getByIdAsync(data);

      if (RP002Status.WaitingApproval) {
        return ToastHelper.sendApproveConfirmMessageToast();
      }

      return ToastHelper.createdMessageToast();
    }
  };

  const updateAsync = async (id: string, rp002Status?: RP002Status) => {
    if (rp002Status) {
      body.value.status = rp002Status;
    }

    const { status, data } = await RP002Service.updateAsync(id, body.value);

    if (status === HttpStatusCode.Ok) {
      if (data?.newDocumentFileId) {
        body.value.documentId = data.newDocumentFileId;
      }
      await getByIdAsync(id);

      switch (body.value.status) {
        case RP002Status.Draft:
          return ToastHelper.updatedMessageToast();
        case RP002Status.Edit:
          return ToastHelper.recallEditMessageToast();
        case RP002Status.WaitingApproval:
          return ToastHelper.sendApproveConfirmMessageToast();
      }
    }
  };

  const getContractCompleteAsync = async (contractSignedStartDate?: Date, contractSignedEndDate?: Date) => {
    const { data, status } = await RP002Service.getContractCompleteAsync(contractSignedStartDate, contractSignedEndDate);

    if (status === HttpStatusCode.Ok) {
      if (!body.value.detail) {
        return;
      }

      const existingContractIds = new Set(body.value.detail.map(d => d.caContractDraftVendorId));

      const filteredContractData = data.filter((d: any) => !existingContractIds.has(d.id));

      const newDetail = filteredContractData.map((d: any) => ({
        ...d,
        caContractDraftVendorId: d.id,
        id: null,
      }));

      body.value.detail.push(...newDetail);

      body.value.detail = body.value.detail?.map((d, i) => ({
        ...d,
        sequence: i + 1,
      }));
    }
  };

  const getContractSummaryAsync = async (id: string) => {
    const { data, status } = await RP002Service.getContractSummaryAsync(id);

    if (status === HttpStatusCode.Ok) {
      dataTableDialog.value = data;
    }
  };

  const reSeq = () => {
    if (body.value.detail && body.value.detail.length > 0) {
      body.value.detail.forEach((d, i) => {
        d.sequence = i + 1;
      });
    }
  };

  const onRemoveDetail = async (index: number): Promise<void> => {
    if (!body.value.detail) return;
    const selectDetail = body.value.detail[index];

    if (!selectDetail) return;

    if (selectDetail.id) {
      if (!body.value.id) return;

      if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;

      await RP002Service.deleteDetailByIdAsync(body.value.id, selectDetail.id);
    }

    body.value.detail.splice(index, 1);

    reSeq();
  }


  const onResetData = () => {
    const currentMonth = new Date().getMonth() + 1;
    const quarter = Math.ceil(currentMonth / 3);

    body.value = {
      status: RP002Status.Draft,
      quarter,
      acceptors: [] as ParticipantsAcceptor[],
      detail: [] as RP002ContractComplete[],
      attachments: [] as Attachments[],
    } as RP002Body;
  };

  const approveAsync = async () => {
    const pendingAcceptors = body.value.acceptors?.filter(a => a.status === AcceptorStatus.Pending) || [];
    const isLastApprover = pendingAcceptors.length === 1;

    const dialogType = isLastApprover ? ReasonDialogType.Approve : ReasonDialogType.Accepted;
    const res = await showReasonDialogAsync(dialogType);

    if (res.isConfirm && body.value.id) {
      const { status } = await RP002Service.approveAsync(body.value.id, res.reason);

      if (status === HttpStatusCode.Ok) {
        await getByIdAsync(body.value.id);

        ToastHelper.approvedMessageToast();
      }
    }
  };

  const rejectAsync = async () => {
    const res = await showReasonDialogAsync(ReasonDialogType.Reject);

    if (res.isConfirm && body.value.id) {
      const { status } = await RP002Service.rejectAsync(body.value.id, res.reason);

      if (status === HttpStatusCode.Ok) {
        await getByIdAsync(body.value.id);

        ToastHelper.sendEditMessageToast();
      }
    }
  };

  const getReviewDocumentAsync = async (id: string, type: string): Promise<string> => {
    const { data, status } = await RP002Service.getReviewDocumentAsync(id, type);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };

  const onUpsertAttachments = async () => {
    if (!body.value.id) return;

    const { status } = await RP002Service.attachmentsAsync(body.value.id, body.value.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await getByIdAsync(body.value.id);
    }
  };

  const exportDetailAsync = async () => {
    if (!body.value.id) return;

    const { data, status } = await RP002Service.exportDetailAsync(body.value.id);

    if (status !== HttpStatusCode.Ok) {
      ToastHelper.error('ดึงข้อมูลเอกสาร', 'ไม่พบเอกสารที่ต้องการ');
      return;
    }

    const fileName = `รายงานสัญญาแล้วเสร็จ ไตรมาส ${body.value.quarter}-${body.value.year}.xlsx`
    const url = window.URL.createObjectURL(data);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    a.remove();
  }

  const getDefaultAcceptor = async (): Promise<void> => {
    const params = {
      processType: SectionProcessType.ContractAmendment,
      budget: 1,
      userId: authStore.profile.id,
      supplyMethodCode: SupplyMethodCode.eighty,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status === HttpStatusCode.Ok) {
      body.value.acceptors = [];

      data.forEach(item => {
        const acceptors = body.value.acceptors!;
        acceptors.push({
          acceptorType: AcceptorType.Approver,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: acceptors.length + 1,
          status: AcceptorStatus.Draft,
          userId: item.userId,
          departmentName: item.businessUnitName,
          isUnableToPerformDuties: false,
        } as ParticipantsCommitteeAcceptor);
      });
    }
  };

  const resetDocumentAsync = async (): Promise<void> => {
    if (!body.value.id) return;

    const { status } = await RP002Service.resetDocumentAsync(body.value.id);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('รีเซ็ตเอกสาร', 'รีเซ็ตเอกสารสำเร็จ');
      await getByIdAsync(body.value.id);
    }
  };

  return {
    body,
    dataTableDialog,
    status: {
      canEdit,
      canRecall,
      canApproveReject,
      canRestoreVersion,
      isLastApproval,
    },
    getByIdAsync,
    createAsync,
    updateAsync,
    getContractCompleteAsync,
    onRemoveDetail,
    onResetData,
    approveAsync,
    rejectAsync,
    getContractSummaryAsync,
    getReviewDocumentAsync,
    onUpsertAttachments,
    exportDetailAsync,
    resetDocumentAsync,
    getDefaultAcceptor,
  };
});
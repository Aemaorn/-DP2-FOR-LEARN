import type { AcceptorInvite, InvitedEntrepreneurs, PP006Detail } from "../../models/PP006/pp006Model";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { HttpStatusCode } from "axios";
import { PP006Status } from "../../enums/pp006";
import { useAuthenticationStore } from "@/stores/authentication";
import { usePPDetailStore } from "../../../../stores/PP/ppStore";
import { checkIsSixty } from "@/helpers/supplyMethod";
import { showReasonDialogAsync, showConfirmDialogAsync } from "@/helpers/dialog";
import { ReasonDialogType, ConfirmDialogType } from "@/enums/dialog";
import { AcceptorStatus, AcceptorType } from "@/enums/participants";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import PP006Service from "../../services/PP006/PP006Service";
import ToastHelper from "@/helpers/toast";
import { CommitteePositions } from "@/enums/PCM005/principle";
import operationService from "@/services/Shared/operations";
import { OrganizationLevel } from "@/enums/operations";
import type { OnlyFileAttachment } from "@/models/shared/uploadFile";

export const usePP006DetailStore = defineStore('PP-006-detail-store', () => {
  const procurementStore = usePPDetailStore();
  const authStore = useAuthenticationStore();

  const detail = ref<PP006Detail>({
    isInvite: undefined,
    acceptors: [] as AcceptorInvite[],
    status: PP006Status.Draft,
  } as PP006Detail);

  const isSixtyMorethan100k = computed(() => {
    const isSixty = checkIsSixty(procurementStore.procurementDetail.supplyMethodCode ?? '');
    const moreThan = procurementStore.procurementDetail.budget > 100000;

    return isSixty && moreThan;
  });

  const canEdit = computed(() => {

    return [PP006Status.Draft, PP006Status.Rejected, PP006Status.Edit].includes(detail.value.status) && detail.value.hasEditPermission;
  });

  const canAcceptAndRejectCommittee = computed(() => {
    const status = detail.value.status === PP006Status.WaitingApproval;
    const checkUser = detail.value.acceptors?.some(a => a.userId === authStore.profile.id && a.isCurrent && a.acceptorType === AcceptorType.ProcurementCommittee);

    return status && checkUser;
  });

  const canAcceptAndRejectApprover = computed(() => {
    if (!detail.value.acceptors) return false;
    const status = detail.value.status === PP006Status.WaitingApproval;
    const checkQue = isCurrentPendingAcceptor(detail.value.acceptors, authStore.profile.id, AcceptorType.Approver);
    return status && checkQue;
  });

  const isLastApprovalCommittee = computed(() => {
    const approvalUser = detail.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending && f.acceptorType === AcceptorType.ProcurementCommittee);

    if (!approvalUser) {
      return false;
    }

    return detail.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending && f.acceptorType === AcceptorType.ProcurementCommittee && !f.isUnableToPerformDuties).length === 1;
  });

  const isLastApprovalApprover = computed(() => {
    const approvalUser = detail.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending && f.acceptorType === AcceptorType.Approver);

    if (!approvalUser) {
      return false;
    }

    const current = approvalUser.find(f => f.isCurrent);

    if (!current) {
      return false;
    }

    return canAcceptAndRejectApprover.value && current.sequence === approvalUser[approvalUser.length - 1].sequence;
  });

  const canRecall = computed(() => {
    if (!detail.value.acceptors) return false;

    const status = detail.value.status === PP006Status.WaitingApproval;

    const canRecall = detail.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.ProcurementCommittee && a.committeePositionsCode == CommitteePositions.PosBoard001);

    const isCommittee = detail.value.acceptors.some(s => s.acceptorType === AcceptorType.ProcurementCommittee && s.userId === authStore.profile.id);

    return status && !canRecall && isCommittee;
  });

  const createAsync = async (statusInvite: PP006Status.Draft | PP006Status.WaitingApproval): Promise<void> => {
    if (!procurementStore.procurementDetail.id) return;

    detail.value.status = statusInvite;

    onClearWhenChangeInviteType();

    if (detail.value.submitProposalStartDate && detail.value.startTime) {
      detail.value.submitProposalStartTime = convertTimeToDateTime(detail.value.submitProposalStartDate, detail.value.startTime);
    }

    if (detail.value.submitProposalEndDate && detail.value.endTime) {
      detail.value.submitProposalEndTime = convertTimeToDateTime(detail.value.submitProposalEndDate, detail.value.endTime);
    }

    const { data, status } = await PP006Service.createAsync(procurementStore.procurementDetail.id, detail.value);

    if (status === HttpStatusCode.Created) {
      await procurementStore.onGetProcurementById(detail.value.procurementId);
      await getByIdAsync(data);
    }
  };

  const updateAsync = async (id: string, statusInvite: PP006Status.Draft | PP006Status.WaitingApproval | PP006Status.Edit, hiddenToast?: boolean): Promise<void> => {
    if (!procurementStore.procurementDetail.id) return;

    const toast: Record<PP006Status.Draft | PP006Status.WaitingApproval | PP006Status.Edit, () => void> = {
      [PP006Status.Draft]: () => ToastHelper.updatedMessageToast(),
      [PP006Status.WaitingApproval]: () => ToastHelper.sendApproveConfirmMessageToast(),
      [PP006Status.Edit]: () => ToastHelper.recallEditMessageToast(),
    }

    onClearWhenChangeInviteType();

    if (detail.value.startTime) {
      detail.value.submitProposalStartTime = convertTimeToDateTime(detail.value.submitProposalStartDate ?? new Date(), detail.value.startTime);
    }

    if (detail.value.endTime) {
      detail.value.submitProposalEndTime = convertTimeToDateTime(detail.value.submitProposalEndDate ?? new Date(), detail.value.endTime);
    }

    const mockData = {
      ...detail.value,
      status: statusInvite,
    }

    const { status } = await PP006Service.updateAsync(id, procurementStore.procurementDetail.id, mockData);

    if (status === HttpStatusCode.Ok) {
      if (!hiddenToast) {
        toast[statusInvite]();
      }

      await getByIdAsync(id);
    }
  };

  const getByIdAsync = async (id?: string): Promise<void> => {
    if (!procurementStore.procurementDetail.id) return;

    const { data, status } = await PP006Service.getByIdAsync(procurementStore.procurementDetail.id, id);

    if (status === HttpStatusCode.Ok) {
      const res = {
        ...data,
        startTime: data.submitProposalStartTime ? convertDateTimeToTime(data.submitProposalStartTime) : undefined,
        endTime: data.submitProposalEndTime ? convertDateTimeToTime(data.submitProposalEndTime) : undefined,
      };

      detail.value = res;
    }

    if (isSixtyMorethan100k.value && detail.value.acceptors.length === 0) {
      await getDefaultSegmentAsync(authStore.profile.id);
    }
  };

  const invitedEntreprenuerAsync = async (body: InvitedEntrepreneurs): Promise<void> => {
    if (!procurementStore.procurementDetail.id) return;

    if (!detail.value.id) {
      await createAsync(PP006Status.Draft);
    }

    if (!detail.value.id) return;

    const { status } = await PP006Service.invitedEntrepreneursAsync(detail.value.id, procurementStore.procurementDetail.id, body);

    if (status === HttpStatusCode.Created) {
      const { data, status } = await PP006Service.getByIdAsync(procurementStore.procurementDetail.id, detail.value.id);

      if (status === HttpStatusCode.Ok) {
        detail.value.invitedEntrepreneurs = data.invitedEntrepreneurs;

        await updateAsync(detail.value.id, PP006Status.Draft, true);
      }

      return ToastHelper.success('เพิ่มผู้ประกอบการ', 'เพิ่มผู้ประกอบการสำเร็จ');
    }
  };

  const updateEntrepreneurAsync = async (body: InvitedEntrepreneurs, message: string, messageDetail: string): Promise<boolean> => {
    if (!detail.value.id || !body.id || !procurementStore.procurementDetail.id) return false;

    const { status } = await PP006Service.updateEntrepreneurAsync(detail.value.id, body.id, procurementStore.procurementDetail.id, body);

    if (status === HttpStatusCode.Ok) {
      const { data, status } = await PP006Service.getByIdAsync(procurementStore.procurementDetail.id, detail.value.id);

      if (status === HttpStatusCode.Ok) {
        detail.value.invitedEntrepreneurs = data.invitedEntrepreneurs;

        await updateAsync(detail.value.id, PP006Status.Draft, true);
      }

      ToastHelper.success(message, messageDetail);

      return true;
    }

    return false;
  };

  const onUpdateDutieStatusAsync = async (acceptorId: string, dutieStatus: boolean, remark?: string): Promise<void> => {
    if (!detail.value.id || !procurementStore.procurementDetail.id) return;

    if (detail.value.status === PP006Status.WaitingApproval) {
      const { status } = await PP006Service.updateDutieStatusAsync(detail.value.id, procurementStore.procurementDetail.id, acceptorId, dutieStatus, remark);

      if (status === HttpStatusCode.Ok) {
        await getByIdAsync(detail.value.id);

        ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
      }

      return;
    }

    const payload = {
      ...detail.value,
      acceptors: detail.value.acceptors.map(m => m.id === acceptorId ? { ...m, isUnableToPerformDuties: dutieStatus, remark } : m),
    } as PP006Detail;

    const { status } = await PP006Service.updateAsync(detail.value.id, procurementStore.procurementDetail.id, payload);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(detail.value.id);

      ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
    }
  };

  const onClearWhenChangeInviteType = (): void => {
    if (!detail.value.isInvite) {
      detail.value = {
        procurementId: detail.value.procurementId,
        isInvite: detail.value.isInvite,
        status: detail.value.status,
        acceptors: detail.value.acceptors,
        invitedEntrepreneurs: detail.value.invitedEntrepreneurs,
      } as PP006Detail;
    }
  };

  const convertTimeToDateTime = (date: Date, time: string): Date => {
    const convertDate = new Date(date);

    const splitTime = time.split(':');
    convertDate.setHours(Number(splitTime[0]));
    convertDate.setMinutes(Number(splitTime[1]));

    return convertDate;
  };

  const convertDateTimeToTime = (dateTime: Date): string => {
    const convertDate = new Date(dateTime);

    const hour = convertDate.getHours().toString().padStart(2, "0");
    const minus = convertDate.getMinutes().toString().padStart(2, "0");

    return `${hour}:${minus}`;
  };

  const onApproveAsync = async (): Promise<void> => {
    if (!detail.value.id || !procurementStore.procurementDetail.id) return;

    const res = await showReasonDialogAsync(isLastApprovalApprover.value ? ReasonDialogType.Approve : ReasonDialogType.Accepted);

    if (res?.isConfirm) {
      const { status } = await PP006Service.approveAsync(detail.value.id, procurementStore.procurementDetail.id, res.reason);

      if (status === HttpStatusCode.Ok) {
        await getByIdAsync(detail.value.id);
        await procurementStore.onGetProcurementById(detail.value.procurementId);

        return ToastHelper.approvedMessageToast();
      }
    }
  };

  const onRejectAsync = async (isCommittee = false): Promise<void> => {
    if (!detail.value.id || !procurementStore.procurementDetail.id) return;

    const res = await showReasonDialogAsync(isCommittee ? ReasonDialogType.NotAgree : ReasonDialogType.Reject, true);

    if (res?.isConfirm) {
      const { status } = await PP006Service.rejectAsync(detail.value.id, procurementStore.procurementDetail.id, res.reason);

      if (status === HttpStatusCode.Ok) {
        await getByIdAsync(detail.value.id);

        return isCommittee ? ToastHelper.notAgreeMessageToast() : ToastHelper.sendEditMessageToast();
      }
    }
  };

  const getReviewDocumentAsync = async (id: string, procurementId: string): Promise<string> => {
    const { data, status } = await PP006Service.getReviewDocumentAsync(id, procurementId);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };

  const getReviewDocumentByEntrepreneurAsync = async (entrepreneurId: string): Promise<string> => {
    if (!detail.value.id || !procurementStore.procurementDetail.id) return '';

    const { data, status } = await PP006Service.getReviewDocumentByEntrepreneurAsync(
      procurementStore.procurementDetail.id,
      detail.value.id,
      entrepreneurId
    );
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    return '';
  };

  const setEntrepreneurDocumentId = (entrepreneurId: string, documentId: string): void => {
    const entrepreneur = detail.value.invitedEntrepreneurs?.find(e => e.id === entrepreneurId);
    if (entrepreneur) {
      entrepreneur.documentId = documentId;
      entrepreneur.isDocumentReplace = true;
    }
  };

  const onSendEmailInviteAsync = async (entrepreneursId: string, email: string, editorContent: string, attachments: OnlyFileAttachment[]): Promise<void> => {
    if (!detail.value.id || !procurementStore.procurementDetail.id) return;

    const { status } = await PP006Service.sendEmailInviteAsync(procurementStore.procurementDetail.id, detail.value.id, entrepreneursId, email, editorContent, attachments);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(detail.value.id);

      return ToastHelper.success('ส่งอีเมลเชิญ', 'ส่งอีเมลเชิญสำเร็จ');
    }
  };

  const onGetDefaultEmailTemplateAsync = async (entrepreneursId: string) => {
    if (!detail.value.id || !procurementStore.procurementDetail.id) return '';

    const { data, status } = await PP006Service.getDefaultEmailTemplateAsync(procurementStore.procurementDetail.id, detail.value.id, entrepreneursId);

    if (status === HttpStatusCode.Ok) {
      return data;
    }
  };

  const setDefaultApproverAsync = async () => {

    await getDefaultSegmentAsync(authStore.profile.id);

    return;
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
      detail.value.acceptors = [
        ...data.map((item, index) => ({
          acceptorType: AcceptorType.Approver,
          departmentName: item.businessUnitName,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: index + 1,
          userId: item.userId,
          status: AcceptorStatus.Draft,
          employeeCode: item.employeeCode,
          isUnableToPerformDuties: false,
        } as AcceptorInvite))];
    }
  };

  const isCanSetDefault = computed(() => {
    return [PP006Status.Draft, PP006Status.Edit].includes(detail.value.status);
  });

  const onNotInviteAsync = async (): Promise<void> => {
    if (!procurementStore.procurementDetail.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData)) return;

    if (!detail.value.id) {
      await createAsync(PP006Status.Draft);
    }

    if (!detail.value.id) return

    const { status } = await PP006Service.notInviteAsync(procurementStore.procurementDetail.id, detail.value.id, authStore.profile.id);

    if (status === HttpStatusCode.Ok) {
      await getByIdAsync(detail.value.id);
      await procurementStore.onGetProcurementById(detail.value.procurementId);

      return ToastHelper.success('ยืนยันดำเนินการ จพ.006', 'ยืนยันดำเนินการ จพ.006 สำเร็จ');
    }
  }

  const resetDocumentAsync = async (procurementId: string, inviteId: string): Promise<void> => {
    const { status } = await PP006Service.resetDocumentAsync(procurementId, inviteId);
    if (status === HttpStatusCode.Ok) {
      ToastHelper.success("สำเร็จ", "ทำการรีเซ็ตเอกสารสำเร็จ");
      return;
    }

    ToastHelper.error("ไม่สามารถรีเซ็ตเอกสารได้", "เกิดข้อผิดพลาดในการรีเซ็ตเอกสารการตรวจสอบ");
  };

  return {
    detail,
    createAsync,
    updateAsync,
    getByIdAsync,
    invitedEntreprenuerAsync,
    updateEntrepreneurAsync,
    onApproveAsync,
    onRejectAsync,
    getReviewDocumentAsync,
    getReviewDocumentByEntrepreneurAsync,
    setEntrepreneurDocumentId,
    onUpdateDutieStatusAsync,
    setDefaultApproverAsync,
    onNotInviteAsync,
    status: {
      isSixtyMorethan100k,
      canEdit,
      canAcceptAndRejectCommittee,
      canAcceptAndRejectApprover,
      isLastApprovalCommittee,
      isLastApprovalApprover,
      canRecall,
      isCanSetDefault,
    },
    onSendEmailInviteAsync,
    onGetDefaultEmailTemplateAsync,
    resetDocumentAsync
  }
});

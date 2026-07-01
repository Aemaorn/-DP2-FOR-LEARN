import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { PP007Detail, PP007Entrepreneurs, PP007PriceDetail } from "../../models/PP007/pp007Model";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import { PurchaseOrderStatus } from "../../enums/pp007";
import operationService from "@/services/Shared/operations";
import { useAuthenticationStore } from "@/stores/authentication";
import { HttpStatusCode } from "axios";
import { usePPDetailStore } from "../../../../stores/PP/ppStore";
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from "@/enums/participants";
import { isCurrentPendingAcceptor } from "@/helpers/participants";
import pp007service from "../../services/PP007/PP007Service";
import ToastHelper from "@/helpers/toast";
import { showConfirmDialogAsync, showReasonDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType, ReasonDialogType } from "@/enums/dialog";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import { OrganizationLevel, SectionProcessType } from "@/enums/operations";
import type { AcceptorInvite, Shareholder } from "../../models/PP006/pp006Model";
import type { EntrepreneurAttachments } from "@/models/shared/uploadFile";
import type { EntrepreneurType } from "@/enums/shared";
import { checkIsSixty } from "@/helpers/supplyMethod";
import { CommitteePositions } from "@/enums/PCM005/principle";
import { rp003SupplyMethod } from "@/enums/RP/rp003";

export const usePurchaseOrder = defineStore("purchaseOrder", () => {
  const procurementStore = usePPDetailStore();
  const auth = useAuthenticationStore();

  const body = ref<PP007Detail>({
    acceptors: [] as ParticipantsCommitteeAcceptor[],
    assignees: [] as ParticipantsAssignee[],
    entrepreneurs: [
      {
        sequence: 1,
        priceDetails: [] as PP007PriceDetail[],
        shareholder: [] as Shareholder[],
        attachments: [] as EntrepreneurAttachments[],
        isWinner: false,
      }
    ] as PP007Entrepreneurs[],
    status: PurchaseOrderStatus.Draft,
    isBp: false,
    procurement: {},
  } as PP007Detail);

  const getDefaultData = async (id?: string) => {
    const { data, status } = await pp007service.getJp006ByidAsync(procurementStore.procurementDetail.id!, id);

    if (status == HttpStatusCode.Ok) {
      body.value = data;
    };
  };

  const getDefaultJorporAsync = async (): Promise<void> => {
    const { data, status } = await operationService.getJorPorDirectorAsync();

    if (status === HttpStatusCode.Ok) {
      const jorPor = {
        assigneeGroup: AssigneeGroup.JorPor,
        assigneeType: AssigneeType.Director,
        departmentName: data.businessUnitName,
        fullName: data.fullName,
        positionName: data.fullPositionName,
        sequence: 1,
        status: AssigneeStatus.Pending,
        userId: data.userId,
      } as ParticipantsAssignee;

      body.value.assignees = [jorPor];
    }
  };

  const getDefaultAcceptor = async (): Promise<void> => {
    const detail = procurementStore.procurementDetail;

    if (!detail.supplyMethod) return;
    if (!body.value.entrepreneurs.some(x => x.isWinner)) return;

    const sumAgreedPriceDetails = (details?: PP007PriceDetail[]): number =>
      (details ?? []).reduce(
        (sum, p) => sum + (p.agreedPrice * p.parcelQuantity || 0),
        0
      );

    const calcTotalWinnerPrice = (entrepreneurs: PP007Entrepreneurs[]): number =>
      entrepreneurs
        .filter(x => x.isWinner)
        .reduce((sum, x) => sum + sumAgreedPriceDetails(x.priceDetails), 0);

    const budget = calcTotalWinnerPrice(body.value.entrepreneurs);

    body.value.acceptors = body.value.acceptors.filter(
      x => x.acceptorType === AcceptorType.ProcurementCommittee
    );

    const processType = detail.isCommercialMaterial
      ? SectionProcessType.PurchaseOrderCommercialParcel
      : SectionProcessType.PurchaseOrder;

    const lastOperator = body.value.operators.length > 0
      ? body.value.operators.reduce((prev, curr) => curr.sequence > prev.sequence ? curr : prev, body.value.operators[0])
      : undefined;

    if (!lastOperator) {
      return;
    }

    const list = body.value.acceptors.filter(x =>
      x.departmentCode === detail.departmentCode &&
      x.acceptorType === AcceptorType.ProcurementCommittee
    );

    const lastAcceptorsCommittee =
      list.length > 0
        ? list.reduce((a, b) => a.sequence > b.sequence ? a : b, list[0])
        : undefined;

    const createCriteria = (userId: string): defaultAcceptorCriteria => ({
      processType,
      supplyMethodCode: detail.supplyMethodCode,
      supplyMethodSpecialTypeCode: detail.supplyMethodSpecialTypeCode,
      budget,
      userId,
    });

    const appendAcceptors = (data: any[]) => {
      const newAcceptors = data.map((m, i) => ({
        sequence: i + 1,
        userId: m.userId,
        fullName: m.fullName,
        positionName: m.fullPositionName,
        departmentName: m.businessUnitName,
        acceptorType: AcceptorType.Approver,
        status: AcceptorStatus.Draft,
      })) as ParticipantsCommitteeAcceptor[];

      body.value.acceptors.push(...newAcceptors);
    };

    let { data, status } =
      await operationService.getOperationsDefaultAcceptorAsync(
        createCriteria(lastOperator?.userId)
      );

    const hasLevel300 = data.some(x => x.organizationLevel === 300);

    if (detail.isCommercialMaterial && !hasLevel300) {

      if (!lastAcceptorsCommittee) return;

      ({ data, status } =
        await operationService.getOperationsDefaultAcceptorAsync(
          createCriteria(lastAcceptorsCommittee?.userId)
        ));
    }

    if (status === HttpStatusCode.Ok) {
      appendAcceptors(data);
    }
  };

  const assigneeDefaultAcceptor = async (): Promise<void> => {
    const detail = procurementStore.procurementDetail;

    if (!detail.supplyMethod) return;
    if (!body.value.entrepreneurs.some(x => x.isWinner)) return;

    const assignees = body.value.assignees.filter(f => f.assigneeType === AssigneeType.Assignee);
    const lastAssignee = assignees[assignees.length - 1];

    if (!lastAssignee) return;

    const sumAgreedPriceDetails = (details?: PP007PriceDetail[]): number =>
      (details ?? []).reduce(
        (sum, p) => sum + (p.agreedPrice * p.parcelQuantity || 0),
        0
      );

    const calcTotalWinnerPrice = (entrepreneurs: PP007Entrepreneurs[]): number =>
      entrepreneurs
        .filter(x => x.isWinner)
        .reduce((sum, x) => sum + sumAgreedPriceDetails(x.priceDetails), 0);

    const budget = calcTotalWinnerPrice(body.value.entrepreneurs);

    body.value.acceptors = body.value.acceptors.filter(
      x => x.acceptorType === AcceptorType.ProcurementCommittee
    );

    const processType = detail.isCommercialMaterial
      ? SectionProcessType.PurchaseOrderCommercialParcel
      : SectionProcessType.PurchaseOrder;

    const params = {
      processType,
      supplyMethodCode: detail.supplyMethodCode,
      supplyMethodSpecialTypeCode: detail.supplyMethodSpecialTypeCode,
      budget,
      userId: lastAssignee.userId,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status === HttpStatusCode.Ok) {
      const newAcceptors = data.map((m, i) => ({
        sequence: i + 1,
        userId: m.userId,
        fullName: m.fullName,
        positionName: m.fullPositionName,
        departmentName: m.businessUnitName,
        acceptorType: AcceptorType.Approver,
        status: AcceptorStatus.Draft,
      })) as ParticipantsCommitteeAcceptor[];

      body.value.acceptors.push(...newAcceptors);
    }
  };

  const getDefaultAcceptorWithCondition = async (): Promise<void> => {
    if (body.value.assignees.filter(a => a.assigneeType === AssigneeType.Assignee).length > 0) {
      await assigneeDefaultAcceptor();
      return;
    }

    await getDefaultAcceptor();
  };

  const getDefaultSegmentAsync = async () => {
    let { data, status } = await operationService.getDefaultDepartmentApproverByUserIdAsync(
      auth.profile.id,
      OrganizationLevel.Segment,
      true);

    if (status === HttpStatusCode.Ok && data.length === 0) {
      const response = await operationService.getDefaultDepartmentApproverByUserIdAsync(
        auth.profile.id,
        OrganizationLevel.Segment,
        false);

      data = response.data;
      status = response.status;
    }

    if (status === HttpStatusCode.Ok) {
      body.value.acceptors = [
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

  const getJp006ByIdAsync = async () => {
    if (!procurementStore.procurementDetail.id || !body.value.jp006Id) return;

    const { data, status } = await pp007service.getJp006ByidAsync(procurementStore.procurementDetail.id, body.value.jp006Id);

    if (status == HttpStatusCode.Ok) {
      body.value = data;
      body.value.isJp006DocumentIdReplaced = false;
      body.value.isWinnerDocumentIdReplaced = false;
    }
  };

  const onCreateJp006Async = async (purchaseOrderStatus?: PurchaseOrderStatus.WaitingCommitteeApproval | PurchaseOrderStatus.WaitingApproval) => {
    if (!procurementStore.procurementDetail.id) return;

    body.value.status = purchaseOrderStatus ?? body.value.status;

    const { data, status } = await pp007service.createJp006Async(procurementStore.procurementDetail.id, body.value);

    if (status == HttpStatusCode.Created) {
      purchaseOrderStatus ? ToastHelper.sendApproveConfirmMessageToast() : ToastHelper.createdMessageToast();

      body.value.jp006Id = data;

      await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id);
      await getJp006ByIdAsync();
    }
  };

  const createEntreprenuerAsync = async (detail: PP007Entrepreneurs): Promise<void> => {

    if (body.value.jp006Id && procurementStore.procurementDetail.id) {
      const { status } = await pp007service.createEntrepreneursAsync(body.value.jp006Id!, procurementStore.procurementDetail.id, detail);

      if (status === HttpStatusCode.Created) {
        const { data, status } = await pp007service.getJp006ByidAsync(body.value.procurementId, body.value.jp006Id);

        if (status === HttpStatusCode.Ok) {
          body.value.entrepreneurs = data.invitedEntrepreneurs;
        }

        return ToastHelper.success('เพิ่มผู้ประกอบการ', 'เพิ่มผู้ประกอบการสำเร็จ');
      }
    }
  };

  const updateEntreprenuerAsync = async (detail: PP007Entrepreneurs): Promise<void> => {

    if (body.value.jp006Id && procurementStore.procurementDetail.id && detail.entrepreneurId) {
      const { status } = await pp007service.updateEntrepreneurCheckAsync(body.value.jp006Id!, procurementStore.procurementDetail.id, detail.entrepreneurId, detail);

      if (status === HttpStatusCode.Created) {
        const { data, status } = await pp007service.getJp006ByidAsync(body.value.procurementId, body.value.jp006Id);

        if (status === HttpStatusCode.Ok) {
          body.value.entrepreneurs = data.invitedEntrepreneurs;
        }

        return ToastHelper.success('แก้ไขผู้ประกอบการ', 'แก้ไขผู้ประกอบการสำเร็จ');
      }
    }
  };

  const onUpdateJp006Async = async (purchaseOrderStatus?: PurchaseOrderStatus.WaitingCommitteeApproval | PurchaseOrderStatus.WaitingApproval | PurchaseOrderStatus.WaitingComment | PurchaseOrderStatus.Edit, hiddenToast?: boolean) => {
    if (!procurementStore.procurementDetail.id || !body.value.jp006Id) return;

    const mockBody = {
      ...body.value,
      status: purchaseOrderStatus ?? body.value.status
    }

    const toast = {
      [PurchaseOrderStatus.WaitingCommitteeApproval]: () => ToastHelper.sendApproveConfirmMessageToast(),
      [PurchaseOrderStatus.WaitingApproval]: () => ToastHelper.sendApproveConfirmMessageToast(),
      [PurchaseOrderStatus.WaitingComment]: () => ToastHelper.assignedMessageToast(),
      [PurchaseOrderStatus.Edit]: () => ToastHelper.recallEditMessageToast(),
    }

    const { status, data } = await pp007service.updateJp006Async(procurementStore.procurementDetail.id, body.value.jp006Id, mockBody);

    if (status === HttpStatusCode.Ok) {
      if (data?.newJp006DocumentFileId) {
        body.value.jp006DocumentId = data.newJp006DocumentFileId;
      }
      if (data?.newWinnerDocumentFileId) {
        body.value.winnerDocumentId = data.newWinnerDocumentFileId;
      }

      if (!hiddenToast) {
        purchaseOrderStatus ? toast[purchaseOrderStatus]() : ToastHelper.updatedMessageToast();
      }

      await getJp006ByIdAsync();
    }
  };

  const onSendApproveAsync = async () => {
    if (!procurementStore.procurementDetail.id || !body.value.jp006Id) return;

    const { status } = await pp007service.sendApproveJp006Async(
      procurementStore.procurementDetail.id, body.value.jp006Id, body.value
    );

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast();
      await getJp006ByIdAsync();
    }
  };

  const onRecallToCommentAsync = async () => {
    if (!procurementStore.procurementDetail.id || !body.value.jp006Id) return;

    const { status } = await pp007service.recallToCommentJp006Async(
      procurementStore.procurementDetail.id, body.value.jp006Id, body.value
    );

    if (status === HttpStatusCode.Ok) {
      ToastHelper.assignedMessageToast();
      await getJp006ByIdAsync();
    }
  };

  const updateDutiesStatusAsync = async (acceptorId: string, dutieStatus: boolean, remark?: string) => {
    if (!body.value.jp006Id || !procurementStore.procurementDetail.id) return;

    if (body.value.status === PurchaseOrderStatus.WaitingApproval) {
      const { status } = await pp007service.updateDutiesStatusAsync(procurementStore.procurementDetail.id, body.value.jp006Id, acceptorId, dutieStatus, remark);

      if (status === HttpStatusCode.Ok) {
        await getJp006ByIdAsync();
        ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
      }

      return;
    }

    const payload = {
      ...body.value,
      acceptors: [
        ...body.value.acceptors.filter(f => f.acceptorType != AcceptorType.ProcurementCommittee),
        ...body.value.acceptors.filter(f => f.acceptorType == AcceptorType.ProcurementCommittee)
          .map(m => m.id === acceptorId ? { ...m, isUnableToPerformDuties: dutieStatus, remark: remark } : m)],
    };

    const { status } = await pp007service.updateJp006Async(procurementStore.procurementDetail.id, body.value.jp006Id, payload);

    if (status === HttpStatusCode.Ok) {
      await getJp006ByIdAsync();
      ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
    }
  };

  const onApproveAsync = async () => {
    if (!procurementStore.procurementDetail.id || !body.value.jp006Id) return;

    const reasonMap = (isLastAcceptorApproval.value) ? ReasonDialogType.Approve : ReasonDialogType.Accepted;

    const result = await showReasonDialogAsync(reasonMap);

    if (!result.isConfirm) return;

    const lastAssignee = body.value.operators.length > 0
      ? body.value.operators.reduce((prev, curr) => curr.sequence > prev.sequence ? curr : prev, body.value.operators[0])
      : undefined;

    const list = body.value.acceptors.filter(x =>
      x.departmentCode === procurementStore.procurementDetail.departmentCode &&
      x.acceptorType === AcceptorType.ProcurementCommittee
    );

    const lastAcceptorsCommittee =
      list.length > 0
        ? list.reduce((a, b) => a.sequence > b.sequence ? a : b, list[0])
        : undefined;

    const operationUserId = procurementStore.procurementDetail.isCommercialMaterial ?
      !lastAcceptorsCommittee
        ? lastAssignee?.userId
        : lastAcceptorsCommittee.userId
      : lastAssignee?.userId;

    if (!operationUserId) return;

    const { status } = await pp007service.approveJp006Async(procurementStore.procurementDetail.id, body.value.jp006Id, operationUserId, result.reason);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.approvedMessageToast();

      await getJp006ByIdAsync();
      await procurementStore.onGetProcurementById(body.value.procurementId);
    }
  };

  const onRejectAsync = async (isCommittee = false) => {
    if (!procurementStore.procurementDetail.id || !body.value.jp006Id) return;

    const result = await showReasonDialogAsync(isCommittee ? ReasonDialogType.NotAgree : ReasonDialogType.Reject, true);

    if (!result.isConfirm) return;

    const { status } = await pp007service.rejectJp006Async(procurementStore.procurementDetail.id, body.value.jp006Id, result.reason);

    if (status == HttpStatusCode.Ok) {
      isCommittee ? ToastHelper.notAgreeMessageToast() : ToastHelper.sendEditMessageToast();

      await getJp006ByIdAsync();
    }
  };

  const assigneeRejectAsync = async (): Promise<void> => {
    if (!procurementStore.procurementDetail.id || !body.value.jp006Id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendEdit)) return;

    const { status } = await pp007service.rejectJp006Async(procurementStore.procurementDetail.id, body.value.jp006Id);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();

      await getJp006ByIdAsync();
    }
  };

  const onCommentAsync = async (reason: string) => {
    if (!procurementStore.procurementDetail.id || !body.value.jp006Id) return;

    const { status } = await pp007service.assigneeCommentJp006Async(procurementStore.procurementDetail.id, body.value.jp006Id, reason);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.remarkOfficerMessageToast();

      await getJp006ByIdAsync();
    }
  };

  const onUpdateEntrepreneurCheck = async (enterpreneur: PP007Entrepreneurs) => {
    if (!body.value.jp006Id && !enterpreneur.entrepreneurId) return;

    const { status } = await pp007service.updateEntrepreneurCheckAsync(body.value.procurementId, body.value.jp006Id!, enterpreneur.entrepreneurId!, enterpreneur);

    if (status == HttpStatusCode.Ok) {
      const { data, status } = await pp007service.getJp006ByidAsync(body.value.procurementId, body.value.jp006Id);

      if (status === HttpStatusCode.Ok) {
        const mockBody = body.value.entrepreneurs;

        body.value.entrepreneurs = data.entrepreneurs.map((newItem: any) => {
          const oldItem = mockBody.find(x => x.entrepreneurId === newItem.entrepreneurId);

          if (!oldItem) return newItem;

          const { ...restOld } = oldItem;

          return {
            ...newItem,
            ...restOld,
            coi: newItem.coi,
            watchlist: newItem.watchlist,
            egp: newItem.egp,
          };
        });
        await onUpdateJp006Async(undefined, true);
      }

      ToastHelper.success("ตรวจสอบ", "ตรวจสอบสำเร็จ");

      return true;
    }

    return false;
  }

  const acceptorsByType = (acceptorType: AcceptorType) => body.value.acceptors.filter(f => f.acceptorType === acceptorType).sort(s => s.sequence);

  const canEdit = computed(() => {
    if (!body.value.acceptors) return false;

    const statusList = [PurchaseOrderStatus.Draft, PurchaseOrderStatus.Rejected, PurchaseOrderStatus.Edit];
    const canEditStatus = statusList.includes(body.value.status) || !body.value.status;
    const isProcurementCommittee =
      body.value.acceptors.some(a => a.acceptorType === AcceptorType.ProcurementCommittee &&
        a.userId === auth.profile.id);
    const isProcurementSuppliesDivision =
      body.value.procurementSuppliesDivision?.some(a => a.userId === auth.profile.id);

    const operatorId = body.value.operators?.some(a => a.userId === auth.profile.id); //มอบหมาย จพ.04

    return canEditStatus && (isProcurementCommittee || isProcurementSuppliesDivision || operatorId);
  });

  const jorporCanAssign = computed(() => {
    if (!body.value.assignees) return false;

    const status = [PurchaseOrderStatus.WaitingAssign, PurchaseOrderStatus.RejectToAssignee].includes(body.value.status)
    const checkUser = body.value.assignees.some(a =>
      (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id);

    return status && checkUser;
  });

  const canComment = computed(() => {
    if (!body.value.assignees) return false;

    const status = body.value.status == PurchaseOrderStatus.WaitingComment;
    const assignees = body.value.assignees.filter(r => r.assigneeType === AssigneeType.Assignee);

    if (assignees.length === 0) return false;

    const lastAssignee = assignees.reduce((prev, curr) => curr.sequence > prev.sequence ? curr : prev, assignees[0]);

    const hasPermissionUser = (lastAssignee.delegateeUserId ? lastAssignee.delegateeUserId : lastAssignee.userId) === auth.profile.id;

    return status && hasPermissionUser;
  });

  const isJorPorAssigned = computed(() =>
    [PurchaseOrderStatus.WaitingAssign, PurchaseOrderStatus.WaitingComment].includes(body.value.status) &&
    body.value.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isCommitteeApproval = computed(() => [PurchaseOrderStatus.WaitingCommitteeApproval].includes(body.value.status)
    && (acceptorsByType(AcceptorType.ProcurementCommittee).some(s => s.userId == auth.profile.id)));

  const isCommitteeCurrentApproval = computed(() => {
    if (!isCommitteeApproval.value) return false;

    if (acceptorsByType(AcceptorType.ProcurementCommittee).length === 1) {
      return true;
    }

    const isBoss = acceptorsByType(AcceptorType.ProcurementCommittee)[0].userId === auth.profile.id;

    if (isBoss) {
      return acceptorsByType(AcceptorType.ProcurementCommittee)
        .filter((value, index) => (index !== 0 && !value.isUnableToPerformDuties))
        .every(s => s.status !== AcceptorStatus.Pending);
    }

    return acceptorsByType(AcceptorType.ProcurementCommittee)
      .some(s => s.userId === auth.profile.id && !s.isUnableToPerformDuties && s.status === AcceptorStatus.Pending);
  });

  const canCommitteeRecall = computed(() => {
    if (!body.value.acceptors) return false;

    const status = body.value.status === PurchaseOrderStatus.WaitingCommitteeApproval;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.ProcurementCommittee && a.committeePositionsCode == CommitteePositions.PosBoard001);

    const isCommittee = body.value.acceptors.some(s => s.acceptorType === AcceptorType.ProcurementCommittee && s.userId === auth.profile.id);

    return status && !isAnyApprovalAndRejected && isCommittee;
  });

  const isCanRecall = computed(() => {
    if (!body.value.assignees) return false;

    const status = body.value.status === PurchaseOrderStatus.WaitingApproval;
    const checkUser = body.value.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id);

    if (!body.value.acceptors) return false;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.Approver);

    return status && checkUser && !isAnyApprovalAndRejected;
  });

  const isAcceptorApproval = computed(() => [PurchaseOrderStatus.WaitingApproval].includes(body.value.status) &&
    acceptorsByType(AcceptorType.Approver).some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) == auth.profile.id));

  const isCurrentAcceptorApproval = computed(() => {
    if (!isAcceptorApproval.value) return false;
    return isCurrentPendingAcceptor(body.value.acceptors, auth.profile.id, AcceptorType.Approver);
  });

  const isLastAcceptorApproval = computed(() => {
    if (acceptorsByType(AcceptorType.Approver).length === 0) return false;

    const acceptorData = acceptorsByType(AcceptorType.Approver)
      .filter(f => f.status === AcceptorStatus.Pending);

    const current = acceptorData
      .reduce((prev, curr) => curr.sequence < prev.sequence ? curr : prev,
        acceptorData[0]);

    return isCurrentAcceptorApproval.value && current.sequence === acceptorData[acceptorData.length - 1].sequence;
  });

  const isBossCommitteeApproval = computed(() => {
    if (!isCommitteeApproval.value) return false;

    if (acceptorsByType(AcceptorType.ProcurementCommittee).length === 1) {
      return true;
    }

    if (acceptorsByType(AcceptorType.ProcurementCommittee).length === 0) return false;
    const isBoss = acceptorsByType(AcceptorType.ProcurementCommittee)[0].userId === auth.profile.id;

    return isBoss;
  });

  const getReviewDocumentAsync = async (id: string, procurementId: string, documentType: string): Promise<string> => {
    const { data, status } = await pp007service.getReviewDocumentAsync(id, procurementId, documentType);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };

  const onUpsertAttachments = async (
    id: string,
    type: EntrepreneurType,
    attachments: EntrepreneurAttachments[]
  ) => {
    const entrepreneur = body.value.entrepreneurs.find(x => x.entrepreneurId === id);
    if (!entrepreneur?.entrepreneurId) return;

    const otherTypeAttachments =
      entrepreneur.attachments?.map(a => ({
        ...a,
        fileAttachments: a.fileAttachments.filter(f => f.type !== type)
      })).filter(a => a.fileAttachments.length > 0) ?? [];

    const newAttachments =
      attachments
        ?.map(att => ({
          ...att,
          fileAttachments: att.fileAttachments?.map(f => ({ ...f, type })) ?? []
        }))
        .filter(att => att.fileAttachments.length > 0) ?? [];

    entrepreneur.attachments = [...otherTypeAttachments, ...newAttachments];

    const { status } = await pp007service.onUpsertAttachmentsAsync(
      entrepreneur.entrepreneurId,
      entrepreneur.attachments
    );

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
    }
  };

  const isCheckPlanDepartmentCode = computed(() => {

    return procurementStore.procurementDetail.isCommercialMaterial && totalWinnerPrice.value <= 1000000 && procurementStore.procurementDetail.supplyMethodCode === rp003SupplyMethod.SMethod004;
  });

  const totalWinnerPrice = computed(() => {
    return body.value.entrepreneurs
      .filter(x => x.isWinner)
      .reduce((sum, x) => {
        const price = x.priceDetails?.reduce((s, p) => s + (p.agreedPrice * p.parcelQuantity || 0), 0) || 0;
        return sum + price;
      }, 0);
  });

  const isSixtyMorethan100k = computed(() => {
    const isSixty = checkIsSixty(procurementStore.procurementDetail.supplyMethodCode ?? '');
    const moreThan = procurementStore.procurementDetail.budget > 100000;

    return isSixty && moreThan;
  });

  return {
    body,
    getDefaultData,
    getDefaultJorporAsync,
    getJp006ByIdAsync,
    onCreateJp006Async,
    updateDutiesStatusAsync,
    canEdit,
    jorporCanAssign,
    onUpdateJp006Async,
    onSendApproveAsync,
    onRecallToCommentAsync,
    isCommitteeCurrentApproval,
    isCommitteeApproval,
    onApproveAsync,
    onRejectAsync,
    isJorPorAssigned,
    onCommentAsync,
    isAcceptorApproval,
    isCurrentAcceptorApproval,
    isLastAcceptorApproval,
    onUpdateEntrepreneurCheck,
    canComment,
    assigneeRejectAsync,
    getReviewDocumentAsync,
    isBossCommitteeApproval,
    getDefaultAcceptor,
    assigneeDefaultAcceptor,
    getDefaultAcceptorWithCondition,
    onUpsertAttachments,
    createEntreprenuerAsync,
    updateEntreprenuerAsync,
    canCommitteeRecall,
    isCanRecall,
    isCheckPlanDepartmentCode,
    isSixtyMorethan100k,
    getDefaultSegmentAsync,
  }
});
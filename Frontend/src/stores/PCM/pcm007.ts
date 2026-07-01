import Pcm007Constant from '@/constants/pcm007';
import { ConfirmDialogType } from '@/enums/dialog';
import { AcceptorStatus, AcceptorType } from '@/enums/participants';
import { Pcm007Status } from '@/enums/pcm007';
import { EGroupCode, OrganizationLevelEnum } from '@/enums/shared';
import { SectionProcessType } from '@/enums/operations';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { isCurrentPendingAcceptor } from '@/helpers/participants';
import ToastHelper from '@/helpers/toast';
import type {
  Pcm007ActionReq,
  Pcm007Committee,
  Pcm007Criteria,
  Pcm007Detail,
  Pcm007GlAccount,
  Pcm007ListResponse,
  Pcm007StatusCount,
  Pcm007Vendor,
  Pcm007VendorParcels,
} from '@/models/PCM/pcm007';
import type { Option, OptionBadge } from '@/models/shared/option';
import type { ParticipantsAcceptor } from '@/models/shared/participants';
import type { Attachments } from '@/models/shared/uploadFile';
import type { defaultAcceptorCriteria } from '@/models/shared/operations';
import Pcm007Service from '@/services/PCM/PCM007';
import SharedService from '@/services/Shared/dropdown';
import operationService from '@/services/Shared/operations';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { useAuthenticationStore } from '../authentication';

// ─── List Store ──────────────────────────────────────────────────────────────

export const usePcm007ListStore = defineStore('pcm007ListStore', () => {
  const initCriteria = {
    pageNumber: 1,
    pageSize: 10,
    sort: [],
    status: Pcm007Status.All,
    budgetYear: new Date().getFullYear() + 543,
  } as Pcm007Criteria;

  const criteria = ref<Pcm007Criteria>(structuredClone(initCriteria));
  const dataResponse = ref<Pcm007ListResponse>({} as Pcm007ListResponse);
  const statusOptionBadge = ref<OptionBadge[]>([]);
  const departmentDropdown = ref<Option[]>([]);

  const { Pcm007StatusName, Pcm007StatusColor } = Pcm007Constant;

  const getDepartmentDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);
    if (status === HttpStatusCode.Ok) {
      departmentDropdown.value = data;
    }
  };

  const getDataList = async (): Promise<void> => {
    const { data, status } = await Pcm007Service.getListAsync(criteria.value);
    if (status === HttpStatusCode.Ok) {
      dataResponse.value = data;
      buildStatusBadge(data.statusCount);
    }
  };

  const buildStatusBadge = (count: Pcm007StatusCount): void => {
    statusOptionBadge.value = Object.entries(Pcm007Status).map(([, value]) => ({
      label: Pcm007StatusName(value),
      value,
      bgColorClass: Pcm007StatusColor(value).bgColorClass,
      textColorClass: Pcm007StatusColor(value).textColorClass,
      count: getCount(count, value),
    } as OptionBadge));
  };

  const getCount = (countAll: Pcm007StatusCount, status: Pcm007Status): number => {
    const key = (status[0]?.toLowerCase() ?? '') + status.slice(1);
    return countAll[key as keyof Pcm007StatusCount] ?? 0;
  };

  const onDeleteAsync = async (id: string): Promise<void> => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

    const { status } = await Pcm007Service.deleteAsync(id);
    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();
      await getDataList();
    }
    if (status === HttpStatusCode.NotFound) {
      ToastHelper.notFoundMessageToast();
    }
  };

  const onResetCriteria = (): void => {
    criteria.value = structuredClone(initCriteria);
  };

  return {
    criteria,
    dataResponse,
    statusOptionBadge,
    departmentDropdown,
    getDepartmentDDLAsync,
    getDataList,
    onDeleteAsync,
    onResetCriteria,
  };
});

// ─── Detail Store ─────────────────────────────────────────────────────────────

export const usePcm007DetailStore = defineStore('pcm007DetailStore', () => {
  const authenStore = useAuthenticationStore();

  const initDetail = (): Pcm007Detail => ({
    pw184Number: '',
    pw184Date: new Date(),
    status: Pcm007Status.Draft,
    departmentCode: '',
    budgetYear: new Date().getFullYear() + 543,
    supplyMethodCode: '',
    subject: '',
    source: '',
    budget: 0,
    isAdvance: false,
    currentCommitteeSequence: 0,
    vendors: [{ vendorParcels: [{ sequence: 1 }], vendorType: '0' }] as Pcm007Vendor[],
    glAccounts: [{ sequence: 1 }] as Pcm007GlAccount[],
    committees: [] as Pcm007Committee[],
    acceptors: [] as ParticipantsAcceptor[],
    acceptanceConfirmers: [] as ParticipantsAcceptor[],
    attachments: [] as Attachments[],
  } as Pcm007Detail);

  const detail = ref<Pcm007Detail>(initDetail());

  // Dropdowns
  const departmentDropdown = ref<Option[]>([]);
  const supplyMethodDropdown = ref<Option[]>([]);
  const supplyMethodSpecialTypeDropdown = ref<Option[]>([]);
  const paymentMethodDropdown = ref<Option[]>([]);
  const bankDropdown = ref<Option[]>([]);
  const vatTypeDropdown = ref<Option[]>([]);
  const unitOfMeasureDropdown = ref<Option[]>([]);
  const solIdDropdown = ref<Option[]>([]);
  const budgetTypeDropdown = ref<Option[]>([]);
  const glAccountDropdown = ref<Option[]>([]);
  const billTypeDropdown = ref<Option[]>([]);
  const positionProcOptions = ref<Option[]>([]);
  const positionInspOptions = ref<Option[]>([]);

  // ── Dropdowns ────────────────────────────────────────────────────────────

  const getDepartmentDDLAsync = async () => {
    const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);
    if (status === HttpStatusCode.Ok) departmentDropdown.value = data;
  };

  const getSupplyMethodDDLAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, undefined, true);
    if (status === HttpStatusCode.Ok) supplyMethodDropdown.value = data;
  };

  const getSupplyMethodSpecialTypeDDLAsync = async (parentCode: string) => {
    if (!parentCode) return;
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, parentCode, true);
    if (status === HttpStatusCode.Ok) supplyMethodSpecialTypeDropdown.value = data;
  };

  const getPaymentMethodDDLAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PaymentMethod, undefined, true);
    if (status === HttpStatusCode.Ok) paymentMethodDropdown.value = data;
  };

  const getBankDDLAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.Bank, undefined, true);
    if (status === HttpStatusCode.Ok) bankDropdown.value = data;
  };

  const getVatTypeDDLAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.VATType, undefined, true);
    if (status === HttpStatusCode.Ok) vatTypeDropdown.value = data;
  };

  const getUnitOfMeasureDDLAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.UnitOfMea, undefined, true);
    if (status === HttpStatusCode.Ok) unitOfMeasureDropdown.value = data;
  };

  const getSolIdDDLAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SolId, undefined, true);
    if (status === HttpStatusCode.Ok) solIdDropdown.value = data;
  };

  const getBudgetTypeDDLAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.BudgetTyp, undefined, true);
    if (status === HttpStatusCode.Ok) budgetTypeDropdown.value = data;
  };

  const getGlAccountDDLAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.GLAcc, undefined, true);
    if (status === HttpStatusCode.Ok) glAccountDropdown.value = data;
  };

  const getBillTypeDDLAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.InvoiceDocType, undefined, true);
    if (status === HttpStatusCode.Ok) billTypeDropdown.value = data;
  };

  const getPositionProcOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoardProc);
    if (status === HttpStatusCode.Ok) positionProcOptions.value = data;
  };

  const getPositionInspOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoardInsp);
    if (status === HttpStatusCode.Ok) positionInspOptions.value = data;
  };

  // ── CRUD ─────────────────────────────────────────────────────────────────

  const getByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await Pcm007Service.getByIdAsync(id);
    if (status === HttpStatusCode.Ok) {
      detail.value = data;

      if (!detail.value.acceptanceConfirmers) {
        detail.value.acceptanceConfirmers = [];
      }

      if (
        detail.value.id &&
        detail.value.status === Pcm007Status.WaitingDisbursementDate &&
        detail.value.acceptanceConfirmers.length === 0 &&
        authenStore.profile
      ) {
        detail.value.acceptanceConfirmers = [{
          sequence: 1,
          userId: authenStore.profile.id,
          fullName: authenStore.profile.name,
          positionName: authenStore.profile.positionName,
          acceptorType: AcceptorType.AccountingConfirmer,
          status: AcceptorStatus.Draft,
        } as ParticipantsAcceptor];
      }
    }
  };

  const createAsync = async (): Promise<string | undefined> => {
    const { data, status } = await Pcm007Service.createAsync(detail.value);
    if (status === HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();

      await getByIdAsync(data);

      return data;
    }
  };

  const updateAsync = async (
    id: string,
    newStatus?: Pcm007Status,
    titleMessage?: string,
    detailMessage?: string,
  ): Promise<boolean> => {
    const previousStatus = detail.value.status;
    if (newStatus) {
      detail.value.status = newStatus;
    }

    const { status } = await Pcm007Service.updateAsync(id, detail.value);
    if (status === HttpStatusCode.Ok) {
      if (titleMessage && detailMessage) {
        ToastHelper.success(titleMessage, detailMessage);
      } else {
        ToastHelper.updatedMessageToast();
      }
      await getByIdAsync(id);
      return true;
    }

    if (newStatus) {
      detail.value.status = previousStatus;
    }
    return false;
  };

  const actionAsync = async (
    id: string,
    reqBody: Pcm007ActionReq,
    titleMessage: string,
    detailMessage: string,
  ): Promise<void> => {
    const { status } = await Pcm007Service.actionAsync(id, reqBody);
    if (status === HttpStatusCode.Ok) {
      ToastHelper.success(titleMessage, detailMessage);
      await getByIdAsync(id);
    }
    if (status === HttpStatusCode.NotFound) {
      ToastHelper.error('ดำเนินการล้มเหลว', 'ไม่สามารถดำเนินการได้');
    }
  };

  const onUpsertAttachments = async (): Promise<void> => {
    if (!detail.value.id) return;
    const { status } = await Pcm007Service.attachmentsAsync(detail.value.id, detail.value.attachments);
    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await getByIdAsync(detail.value.id);
    }
  };

  // ── Vendor helpers ────────────────────────────────────────────────────────

  const addVendor = (): void => {
    detail.value.vendors.push({
      sequence: detail.value.vendors.length + 1,
      vendorType: '0',
      vendorParcels: [{ sequence: 1 }],
    } as Pcm007Vendor);
  };

  const removeVendor = (index: number): void => {
    detail.value.vendors.splice(index, 1);
    detail.value.vendors.forEach((v, i) => { v.sequence = i + 1; });
  };

  const addParcelToVendor = (vendorIndex: number): void => {
    const vendor = detail.value.vendors[vendorIndex];
    if (!vendor) return;
    vendor.vendorParcels ??= [];
    vendor.vendorParcels.push({
      sequence: vendor.vendorParcels.length + 1,
    } as Pcm007VendorParcels);
  };

  const removeParcelFromVendor = (vendorIndex: number, parcelIndex: number): void => {
    const vendor = detail.value.vendors[vendorIndex];
    if (!vendor) return;
    vendor.vendorParcels.splice(parcelIndex, 1);
    vendor.vendorParcels.forEach((p, i) => { p.sequence = i + 1; });
  };

  // ── GL Account helpers ────────────────────────────────────────────────────

  const addGLAccount = (): void => {
    detail.value.glAccounts.push({ sequence: detail.value.glAccounts.length + 1 } as Pcm007GlAccount);
  };

  const removeGlAccount = (index: number): void => {
    detail.value.glAccounts.splice(index, 1);
    detail.value.glAccounts.forEach((g, i) => { g.sequence = i + 1; });
  };

  // ── Reset ─────────────────────────────────────────────────────────────────

  const onResetDetail = (): void => {
    detail.value = initDetail();
  };

  // ── Default acceptors ─────────────────────────────────────────────────────

  const getDefaultAccountingAcceptorAsync = async (): Promise<void> => {
    const { data: defaultExpense, status: defaultStatus } =
      await operationService.getDefaultExpenseDisbursementAsync();

    if (defaultStatus !== HttpStatusCode.Ok) return;

    const params: defaultAcceptorCriteria = {
      processType: SectionProcessType.ExpenseDisbursement,
      budget: detail.value.budget,
      userId: defaultExpense.userId,
      supplyMethodCode: 'SectionApprover001',
      skipCurrentEmployee: false,
    };

    const { data: acceptorList, status: acceptorStatus } =
      await operationService.getOperationsDefaultAcceptorAsync(params);

    if (acceptorStatus !== HttpStatusCode.Ok) return;

    const otherAcceptors = detail.value.acceptors?.filter(
      a => a.acceptorType !== AcceptorType.AccountingApprover,
    ) ?? [];

    const newAcceptors: ParticipantsAcceptor[] = acceptorList.map((item, index) => ({
      acceptorType: AcceptorType.AccountingApprover,
      fullName: item.fullName,
      positionName: item.fullPositionName,
      sequence: index + 1,
      status: AcceptorStatus.Pending,
      userId: item.userId,
      departmentName: item.businessUnitName,
    } as ParticipantsAcceptor));

    detail.value.acceptors = [...otherAcceptors, ...newAcceptors];
  };

  // ── Computed states ───────────────────────────────────────────────────────

  const isEdit = computed(() =>
    detail.value.status === Pcm007Status.Draft ||
    detail.value.status === Pcm007Status.Edit ||
    detail.value.status === Pcm007Status.Rejected,
  );

  const isMyForm = computed(() =>
    detail.value.createdBy === authenStore.profile.id,
  );

  const canRecall = computed(() =>
    detail.value.status === Pcm007Status.WaitingApproval && isMyForm.value,
  );

  const isWaitingApproval = computed(() => {
    if (!detail.value.acceptors) return false;
    return (
      detail.value.status === Pcm007Status.WaitingApproval &&
      isCurrentPendingAcceptor(detail.value.acceptors, authenStore.profile.id, AcceptorType.Approver)
    );
  });

  const isCommitteeApprove = computed(() => {
    if (detail.value.status !== Pcm007Status.WaitingCommitteeApprove) return false;
    const seq = detail.value.currentCommitteeSequence;
    const current = detail.value.committees?.find(
      c => c.sequence === seq,
    );
    return current?.userId === authenStore.profile.id;
  });

  const isAccounting = computed(() => {
    if (!detail.value.acceptors) return false;
    return (
      detail.value.status === Pcm007Status.WaitingAccounting &&
      isCurrentPendingAcceptor(detail.value.acceptors, authenStore.profile.id, AcceptorType.AccountingApprover)
    );
  });

  const canConfirmDisbursement = computed(() => {
    if (detail.value.status !== Pcm007Status.WaitingDisbursementDate) return false;
    return detail.value.acceptors?.some(
      a => a.acceptorType === AcceptorType.AccountingApprover &&
        (a.delegateeUserId ?? a.userId) === authenStore.profile.id,
    ) ?? false;
  });

  const isPaid = computed(() => detail.value.status === Pcm007Status.Paid);

  const isLastApprover = computed(() => {
    const pending = detail.value.acceptors?.filter(
      a =>
        a.acceptorType === AcceptorType.Approver &&
        a.status === AcceptorStatus.Pending,
    ) ?? [];
    return pending.length === 1;
  });

  const isLastAccountingApprover = computed(() => {
    const pending = detail.value.acceptors?.filter(
      a =>
        a.acceptorType === AcceptorType.AccountingApprover &&
        a.status === AcceptorStatus.Pending,
    ) ?? [];
    return pending.length === 1;
  });

  return {
    detail,
    // dropdowns
    departmentDropdown,
    supplyMethodDropdown,
    supplyMethodSpecialTypeDropdown,
    paymentMethodDropdown,
    bankDropdown,
    vatTypeDropdown,
    unitOfMeasureDropdown,
    solIdDropdown,
    budgetTypeDropdown,
    glAccountDropdown,
    billTypeDropdown,
    positionProcOptions,
    positionInspOptions,
    // dropdown loaders
    getDepartmentDDLAsync,
    getSupplyMethodDDLAsync,
    getSupplyMethodSpecialTypeDDLAsync,
    getPaymentMethodDDLAsync,
    getBankDDLAsync,
    getVatTypeDDLAsync,
    getUnitOfMeasureDDLAsync,
    getSolIdDDLAsync,
    getBudgetTypeDDLAsync,
    getGlAccountDDLAsync,
    getBillTypeDDLAsync,
    getPositionProcOptions,
    getPositionInspOptions,
    // crud
    getByIdAsync,
    createAsync,
    updateAsync,
    actionAsync,
    onUpsertAttachments,
    onResetDetail,
    // default acceptors
    getDefaultAccountingAcceptorAsync,
    // vendor helpers
    addVendor,
    removeVendor,
    addParcelToVendor,
    removeParcelFromVendor,
    // gl helpers
    addGLAccount,
    removeGlAccount,
    // computed
    isEdit,
    isMyForm,
    canRecall,
    isWaitingApproval,
    isCommitteeApprove,
    isAccounting,
    canConfirmDisbursement,
    isPaid,
    isLastApprover,
    isLastAccountingApprover,
  };
});

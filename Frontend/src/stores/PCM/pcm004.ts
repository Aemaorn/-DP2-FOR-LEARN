import Pcm004Constant from '@/constants/pcm004';
import { ConfirmDialogType } from '@/enums/dialog';
import { AcceptorStatus, AcceptorType, AssigneeType } from '@/enums/participants';
import { isCurrentPendingAcceptor } from '@/helpers/participants';
import { CashType, Pcm004Status } from '@/enums/pcm004';
import { EGroupCode, EWorkProcess, OrganizationLevelEnum } from '@/enums/shared';
import { isBranchOrganizationLevel } from '@/helpers/organization';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import procurementHelper from '@/helpers/procurement';
import ToastHelper from '@/helpers/toast';
import type { Pcm004ActionReq, Pcm004Advance, Pcm004Categories, Pcm004Criteria, Pcm004Detail, Pcm004GlAccount, Pcm004ListResponse, Pcm004StatusCount, Pcm004Vendor, Pcm004VendorParcels } from '@/models/PCM/pcm004';
import type { DefaultDepartmentDirectorCriteria } from '@/models/shared/operations';
import type { Option, OptionBadge } from '@/models/shared/option';
import type { ParticipantsAcceptor, ParticipantsAssignee } from '@/models/shared/participants';
import type { Attachments } from '@/models/shared/uploadFile';
import pcm004Service from '@/services/PCM/PCM004';
import SharedService, { type ParameterOptionWithChildren } from '@/services/Shared/dropdown';
import operationService from '@/services/Shared/operations';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { computed, ref, type Ref } from 'vue';
import { useAuthenticationStore } from '../authentication';
import { SupplyMethodCode } from '@/enums/supplyMethod';
;

const getDepartmentAsync = async (target: Ref<Option[]>): Promise<void> => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

export const usePcm004ListStore = defineStore('pcm004ListStore', () => {
  const initCriteria = {
    pageNumber: 1,
    pageSize: 10,
    sort: [],
    workProcess: EWorkProcess.InProcess,
    status: Pcm004Status.All,
    budgetYear: new Date().getFullYear() + 543,
  } as Pcm004Criteria;
  const criteria = ref<Pcm004Criteria>(structuredClone(initCriteria));
  const statusOptionBadge = ref([] as OptionBadge[]);
  const statusCount = ref<Pcm004StatusCount>({} as Pcm004StatusCount);
  const dataResponse = ref<Pcm004ListResponse>({} as Pcm004ListResponse);
  const { Pcm004StatusName, Pcm004StatusColor } = Pcm004Constant;
  const departmentDropdown = ref<Option[]>([] as Option[]);

  const getDepartmentDDLAsync = async (): Promise<void> => {
    await getDepartmentAsync(departmentDropdown);
  };

  const onInitStatusCriteriaPCM003 = () => {
    return procurementHelper.pcm004StatusAttributes(statusCount.value);
  };

  const getDataList = async (): Promise<void> => {
    const { data, status } = await pcm004Service.getListAsync(criteria.value);

    if (status === HttpStatusCode.Ok) {
      dataResponse.value = data;

      getStatusCount(data.statusCount);
    }
  };

  const onDeleteAsync = async (id: string) => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

    const { status } = await pcm004Service.deleteAsync(id);

    if (status == HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();

      await getDataList()
    }

    if (status == HttpStatusCode.NotFound) {
      ToastHelper.notFoundMessageToast();
    }
  }

  const getStatusCount = (count: Pcm004StatusCount): void => {
    const statusOptions = Object.entries(Pcm004Status).map(([, value]) => ({
      label: Pcm004StatusName(value),
      value: value,
      bgColorClass: Pcm004StatusColor(value).bgColorClass,
      textColorClass: Pcm004StatusColor(value).textColorClass,
      count: getCount(count, value),
    } as OptionBadge));

    statusOptionBadge.value = statusOptions;
  };

  const getCount = (countAll: Pcm004StatusCount, status: Pcm004Status): number => {
    let count = 0;
    const convertStatus = (status[0]?.toLowerCase() ?? '') + status.slice(1);
    count = countAll[convertStatus as keyof Pcm004StatusCount];

    return count;
  };

  const onResetCriteria = () => {
    criteria.value = structuredClone(initCriteria);
  };

  return {
    criteria,
    dataResponse,
    statusOptionBadge,
    departmentDropdown,
    onInitStatusCriteriaPCM003,
    getDataList,
    getDepartmentDDLAsync,
    onResetCriteria,
    onDeleteAsync
  };
});

export const usePcm004DetailStore = defineStore('pcm004DetailStore', () => {
  const authenStore = useAuthenticationStore();
  const detail = ref<Pcm004Detail>({
    pPettyCashDate: new Date(),
    supplyMethodCode: SupplyMethodCode.sixty,
    supplyMethodSpecialTypeCode: '',
    isAdvance: true,
    advance: {} as Pcm004Advance,
    categories: [] as Pcm004Categories[],
    vendors: [{ vendorParcels: [{ sequence: 1 }], vendorType: "0" }] as Pcm004Vendor[],
    attachments: [] as Attachments[],
    acceptors: [] as ParticipantsAcceptor[],
    assignees: [] as ParticipantsAssignee[],
    status: Pcm004Status.Draft,
    cashType: CashType.Standard,
    budgetYear: (new Date().getFullYear() + 543),
  } as Pcm004Detail);

  const departmentDropdown = ref<Option[]>([] as Option[]);
  const supplyMethodDropdown = ref<Option[]>([] as Option[]);
  const supplyMethodTypeDropdown = ref<Option[]>([] as Option[]);
  const supplyMethidSpecialTypeDropdown = ref<Option[]>([] as Option[]);
  const expenseItemW119DropDown = ref<Option[]>([] as Option[]);
  const paymentMethodDropDown = ref<Option[]>([] as Option[]);
  const bankDropDown = ref<Option[]>([] as Option[]);
  const vatTypeDropDown = ref<Option[]>([] as Option[]);
  const unitOfMeasureDropDown = ref<Option[]>([] as Option[]);
  const invoiceDocumentTypeDropDown = ref<Option[]>([] as Option[]);
  const solIdDropDown = ref<Option[]>([] as Option[]);
  const budgetTypeDropDown = ref<Option[]>([] as Option[]);
  const glAccountDropDown = ref<Option[]>([] as Option[]);
  const pettyCashStandardTypeOptions = ref<ParameterOptionWithChildren[]>([]);
  const pettyCashConvenienceTypeOptions = ref<ParameterOptionWithChildren[]>([]);
  const pettyCashWithoutForm001TypeOptions = ref<ParameterOptionWithChildren[]>([]);

  const invalidParentCategoryCodes = computed((): string[] => {
    const selected = new Set((detail.value.categories ?? []).map(c => String(c.categoryTypeCode)));
    const allParents = [
      ...pettyCashStandardTypeOptions.value,
      ...pettyCashConvenienceTypeOptions.value,
      ...pettyCashWithoutForm001TypeOptions.value,
    ];
    return allParents
      .filter(p =>
        selected.has(String(p.value)) &&
        !!p.children && p.children.length > 0 &&
        !p.children.some(c => selected.has(String(c.value))))
      .map(p => String(p.value));
  });
  const deliveryConditionOptions = ref<Option[]>([]);
  const deliveryPeriodTypeOptions = ref<Option[]>([]);
  const positionProcOptions = ref<Option[]>([]);
  const positionInspOptions = ref<Option[]>([]);

  const getPositionProcOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoardProc);
    if (status === HttpStatusCode.Ok) {
      positionProcOptions.value = data;
    }
  };

  const getPositionInspOptions = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoardInsp);
    if (status === HttpStatusCode.Ok) {
      positionInspOptions.value = data;
    }
  };

  const getSupplyMethodDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, undefined, true);

    if (status === HttpStatusCode.Ok) {
      supplyMethodDropdown.value = data;
    }
  };

  const getPettyCashStandardTypeAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeIncludeChildrenAsync(EGroupCode.PettyCashStandardType, true);

    if (status === HttpStatusCode.Ok) {
      pettyCashStandardTypeOptions.value = data;
    }
  };

  const getPettyCashConvenienceTypeAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeIncludeChildrenAsync(EGroupCode.PettyCashConvenienceType, true);

    if (status === HttpStatusCode.Ok) {
      pettyCashConvenienceTypeOptions.value = data;
    }
  };

  const getPettyCashWithoutForm001TypeAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeIncludeChildrenAsync(EGroupCode.PettyCashWithoutForm001Type, true);

    if (status === HttpStatusCode.Ok) {
      pettyCashWithoutForm001TypeOptions.value = data;
    }
  };

  const getDeliveryConditionOptionAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.DelvCUnit);
    if (status === 200) {
      deliveryConditionOptions.value = data;
    }
  };

  const getDeliveryPeriodTypeOptionsAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PeriodType);
    if (status === HttpStatusCode.Ok) {
      deliveryPeriodTypeOptions.value = data;
    }
  };

  const getBudgetTypeDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.BudgetTyp, undefined, true);

    if (status === HttpStatusCode.Ok) {
      budgetTypeDropDown.value = data;
    }
  };

  const getGlAccountDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.GLAcc, undefined, true);

    if (status === HttpStatusCode.Ok) {
      glAccountDropDown.value = data;
    }
  };

  const getSolIdDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SolId, undefined, true);

    if (status === HttpStatusCode.Ok) {
      solIdDropDown.value = data;
    }
  };

  const getInvoiceDocumentTypeDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.InvoiceDocType, undefined, true);

    if (status === HttpStatusCode.Ok) {
      invoiceDocumentTypeDropDown.value = data;
    }
  };

  const getSupplyMethodTypeDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethodType, undefined, true);

    if (status === HttpStatusCode.Ok) {
      supplyMethodTypeDropdown.value = data;
    }
  };

  const getSupplyMethodSpecialTypeDDLAsync = async (parentCode: string): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, parentCode, true);

    if (status === HttpStatusCode.Ok) {
      supplyMethidSpecialTypeDropdown.value = data;
    }
  };

  const getBankDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.Bank, undefined, true);

    if (status === HttpStatusCode.Ok) {
      bankDropDown.value = data;
    }
  };

  const getExpenseItemW119DDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.ExpenseItemW119, undefined, true);

    if (status === HttpStatusCode.Ok) {
      expenseItemW119DropDown.value = data;
    }
  };

  const getPaymentMethodDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PaymentMethod, undefined, true);

    if (status === HttpStatusCode.Ok) {
      paymentMethodDropDown.value = data;
    }
  };

  const getVatTypeDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.VATType, undefined, true);

    if (status === HttpStatusCode.Ok) {
      vatTypeDropDown.value = data;
    }
  };

  const getUnitOfMeasureDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.UnitOfMea, undefined, true);

    if (status === HttpStatusCode.Ok) {
      unitOfMeasureDropDown.value = data;
    }
  };

  const getByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await pcm004Service.getByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      detail.value = data;

      if (data.vendors && data.vendors.length === 0 && detail.value.status === Pcm004Status.WaitingForInspector) {
        detail.value.vendors = [{ vendorParcels: [{ sequence: 1 }], vendorType: "0" }] as Pcm004Vendor[];
      }

      if (data.advance) {
        data.advance = {
          advanceBankCode: data.advance.advanceBankCode ?? 'Bank014',
          advancePaymentMethodCode: data.advance.advancePaymentMethodCode ?? 'PaymentMethod002',
          advanceBankAccount: data.advance.advanceBankAccount,
          advanceBankAccountName: data.advance.advanceBankAccountName,
          advanceBankBranch: data.advance.advanceBankBranch,
          advanceDetail: data.advance.advanceDetail,
          advanceName: data.advance.advanceName,
          advancePaymentDate: data.advance.advancePaymentDate
        } as Pcm004Advance
      }

      return;
    }

    if (status === HttpStatusCode.NotFound) {
      return ToastHelper.error('ดึงข้อมูล', 'ไม่สามารถดึงข้อมูลได้');
    }
  };

  const onUpsertAttachments = async () => {
    if (!detail.value.id) return;

    const { status } = await pcm004Service.attachmentsAsync(detail.value.id, detail.value.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await getByIdAsync(detail.value.id);
    }
  };

  const onUpsertAttachmentsFromExpenseDisbursement = async (id: string, attachments: Attachments[]) => {
    const { status } = await pcm004Service.attachmentsAsync(id, attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
    }
  };

  const clearBody = (): void => {
    detail.value = {
      pPettyCashDate: new Date(),
      supplyMethodCode: SupplyMethodCode.sixty,
      supplyMethodSpecialTypeCode: '',
      isAdvance: true,
      advance: {} as Pcm004Advance,
      categories: [] as Pcm004Categories[],
      vendors: [{ vendorParcels: [{ sequence: 1 }], vendorType: "0" }] as Pcm004Vendor[],
      attachments: [] as Attachments[],
      acceptors: [] as ParticipantsAcceptor[],
      status: Pcm004Status.Draft,
      cashType: CashType.Standard,
      budgetYear: (new Date().getFullYear() + 543),
    } as Pcm004Detail;
  };

  const addVendors = () => {
    if (detail.value.vendors && Array.isArray(detail.value.vendors)) {
      detail.value.vendors.push(
        {
          sequence: detail.value.vendors.length + 1,
          vendorParcels: [{ sequence: 1 }]
        } as Pcm004Vendor);
    } else {
      detail.value.vendors = [{ vendorParcels: [{ sequence: 1 }] } as Pcm004Vendor];
    }
  };

  const addParcelToVendor = (vendorIndex: number) => {
    const vendor = detail.value.vendors?.[vendorIndex];
    if (!vendor) return;

    vendor.vendorParcels ??= [];
    vendor.vendorParcels.push({
      sequence: vendor.vendorParcels.length + 1,
      item: '',
      itemDetail: '',
      quantity: 0,
      unitCode: '',
      unitPrice: 0,
      totalPrice: 0,
      totalPriceVat: 0,
    } as Pcm004VendorParcels);
  };

  const addGLAccount = () => {
    if (detail.value.glAccounts && Array.isArray(detail.value.glAccounts)) {
      detail.value.glAccounts.push(
        {
          sequence: detail.value.glAccounts.length + 1,
        } as Pcm004GlAccount);
    } else {
      detail.value.glAccounts = [{sequence: 1} as Pcm004GlAccount];
    }
  };

  const getDepartmentDDLAsync = async (): Promise<void> => {
    await getDepartmentAsync(departmentDropdown);
  };

  function calculateTotalVendorParcelsPrice(): number {
    return detail.value.vendors.reduce((vendorAcc, vendor) => {
      const vendorTotal = vendor.vendorParcels.reduce((parcelAcc, parcel) => {
        return parcelAcc + (parcel.totalPriceVat || 0);
      }, 0);
      return vendorAcc + vendorTotal;
    }, 0);
  }

  function calculateTotalGlAccountAmount(): number {
    return detail.value.glAccounts.reduce((acc, glAccount) => {
      return acc + (glAccount.amount || 0);
    }, 0);
  }

  function validateGlAccountAmount(): boolean {

    if(detail.value.status !== Pcm004Status.WaitingForInspector){
      return true;
    }

    const totalGlAccounts = Math.round(calculateTotalGlAccountAmount() * 100) / 100;
    const totalVendorParcels = Math.round(calculateTotalVendorParcelsPrice() * 100) / 100;

    if (totalVendorParcels > detail.value.budget) {
      ToastHelper.errorDescription("จำนวนเงินรวมรายการพัสดุต้องไม่เกินวงเงินที่จะซื้อหรือจ้าง");
      return false;
    }

    if (totalGlAccounts !== totalVendorParcels) {
      ToastHelper.glAccountExceedsTotalPriceMessageToast();
      return false;
    }

    return true;
  }

  const createAsync = async (): Promise<string | undefined> => {

     if (!validateGlAccountAmount()) {
      return;
    }

    const { data, status } = await pcm004Service.createAsync(detail.value);

    if (status == HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();

      await getByIdAsync(data);

      return data;
    }
  };

  const updateAsync = async (id: string, newStatus?: Pcm004Status): Promise<void> => {
     if (!validateGlAccountAmount()) {
      return;
    }

    if (detail.value.status === Pcm004Status.WaitingForInspector && detail.value.categories.length < 1) {
      return ToastHelper.errorDescription("กรุณากรอกข้อมูลประเภทเงินสด หรือ ประเภทเงินสดย่อย-สะดวกใช้ อย่างน้อง 1 รายการ")
    }

    const totalAmount =
      detail.value.vendors?.reduce((sum, v) => {
        const parcelTotal =
        v.vendorParcels?.reduce((s, p) => s + (p.totalPrice ?? 0), 0) ?? 0;
        return sum + parcelTotal;
      }, 0) ?? 0;

    const budget = detail.value.budget ?? 0;

    const isOverBudget = totalAmount > budget;

    if (isOverBudget) {
      return ToastHelper.errorDescription(
          `จำนวนเงินรวมต้องไม่เกินวงเงินที่จะซื้อหรือจ้าง ${budget.toLocaleString(undefined, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,})}  บาท
           (ปัจจุบัน ${totalAmount.toLocaleString(undefined, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,})}  บาท)`);
    }

    const mockData = {
      ...detail.value,
      status: newStatus ?? detail.value.status
    }

    const { status, data } = await pcm004Service.updateAsync(id, mockData);

    if (status == HttpStatusCode.Ok) {
      if (newStatus === Pcm004Status.WaitingApproval) {
        ToastHelper.sendApproveConfirmMessageToast();

      } else {
        ToastHelper.updatedMessageToast();
      }

      if (data?.newApprovalRequestDocumentFileId) {
        detail.value.approvalRequestDocumentId = data.newApprovalRequestDocumentFileId;
      }

      await getByIdAsync(id);
    }
  }

  const onApproveUpdateAsync = async (id: string, newStatus?: Pcm004Status): Promise<void> => {
    const mockData = {
      ...detail.value,
      status: newStatus ?? detail.value.status
    }

    await pcm004Service.updateAsync(id, mockData);
  }

  const actionAsync = async (id: string, reqBody: Pcm004ActionReq, titleMessage: string, detailMessage: string): Promise<void> => {

    if (!validateGlAccountAmount()) {
      return;
    }

    const { status } = await pcm004Service.actionAsync(id, reqBody);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success(titleMessage, detailMessage);

      await getByIdAsync(id);

      return;
    }

    if (status === HttpStatusCode.NotFound) {
      return ToastHelper.error('ดำเนินการล้มเหลว', 'ไม่สามารถดำเนินการได้');
    }
  };

  const getDefaultDepartmentDirectorAsync = async (businessUnitId: string): Promise<void> => {
    const params = {
      businessUnitId: businessUnitId,

    } as DefaultDepartmentDirectorCriteria;
    const { data, status } = await operationService.getOperationsDefaultDepartmentDirectorAsync(params);

    if (status === HttpStatusCode.Ok) {
      detail.value.acceptors = [];
      const directors = Array.isArray(data) ? data : [data];

      detail.value.acceptors = directors.map((m, i) => ({
        sequence: i + 1,
        userId: m.userId,
        fullName: m.fullName,
        positionName: m.fullPositionName,
        departmentName: m.businessUnitName,
        acceptorType: AcceptorType.DepartmentDirectorAgree,
        status: AcceptorStatus.Draft,
      }));
    }
  };

  const removeVendorList = (index: number) => {
    detail.value.vendors.splice(index, 1);

    onResetOrderNoVendorList();
  };

  const onResetOrderNoVendorList = () => {
    if (detail.value.vendors && detail.value.vendors.length > 0) {
      detail.value.vendors.forEach((data, index) => {
        data.sequence = index + 1;
      });
    }
  };

  const removeGlAccount = (index: number) => {
    detail.value.glAccounts.splice(index, 1);

    onResetOrderNoGlAccountList();
  };

  const onResetOrderNoGlAccountList = () => {
    if (detail.value.glAccounts && detail.value.glAccounts.length > 0) {
      detail.value.glAccounts.forEach((data, index) => {
        data.sequence = index + 1;
      });
    }
  };

  const removeParcelFromVendor = (vendorIndex: number, parcelIndex: number) => {
    const vendor = detail.value.vendors[vendorIndex];
    if (!vendor) return;

    vendor.vendorParcels.splice(parcelIndex, 1);

    vendor.vendorParcels.forEach((item, idx) => {
      item.sequence = idx + 1;
    });
  };

  const isEdit = computed(() => {
    return detail.value.status === Pcm004Status.Draft ||
      detail.value.status === Pcm004Status.Edit ||
      detail.value.status === Pcm004Status.Rejected;
  });

  const isReject = computed(() => {
    const isAcceptor = detail.value.acceptors?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authenStore.profile.id);
    const status = detail.value.status == Pcm004Status.Rejected;

    return isAcceptor && status;
  });

  const isWaitingApproval = computed(() => {
    if (!detail.value.acceptors) return false;
    const status = detail.value.status == Pcm004Status.WaitingApproval;
    const checkQue = isCurrentPendingAcceptor(detail.value.acceptors, authenStore.profile.id, AcceptorType.DepartmentDirectorAgree);
    return status && checkQue;
  });

  const isJPApproval = computed(() => {
    const isAcceptor = detail.value.acceptors?.some(a => a.userId === authenStore.profile.id);
    const status = detail.value.status == Pcm004Status.WaitingForInspector;

    const pendingAcceptors = detail.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending && f.acceptorType === AcceptorType.InspectionCommittee);
    if (!pendingAcceptors || pendingAcceptors.length === 0) return false;

    const current = pendingAcceptors
      .reduce((prev, curr) => curr.sequence < prev.sequence ? curr : prev,
        pendingAcceptors[0]);

    return isAcceptor && status && current.userId == authenStore.profile.id;
  });

  const isWaitingForAssignment = computed(() => {
    return detail.value.status == Pcm004Status.WaitingForAssignment && detail.value.assignees.some(x => (x.delegateeUserId ? x.delegateeUserId : x.userId) == authenStore.profile.id);
  });

  const isWaitingForCompletion = computed(() => {
    return detail.value.status == Pcm004Status.WaitingForCompletion && detail.value.assignees.some(x => x.assigneeType === AssigneeType.Assignee && (x.delegateeUserId ? x.delegateeUserId : x.userId) == authenStore.profile.id);
  });

  const isCompleted = computed(() => {
    return detail.value.status == Pcm004Status.Completed;
  });

  const canReCall = computed(() => {
    return detail.value.hasPermission && detail.value.status === Pcm004Status.WaitingApproval
  });

  const isDepartmentLevel500Or601 = computed(() =>
    isBranchOrganizationLevel(detail.value.departmentOrganizationLevel)
  );

  const isNotFromJorPor001 = computed(() => detail.value.isFromJorPor001 === false);

  const getReviewDocumentAsync = async (id: string): Promise<string> => {
    const { data, status } = await pcm004Service.getReviewDocumentAsync(id);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };

  return {
    detail,
    departmentDropdown,
    supplyMethodDropdown,
    supplyMethodTypeDropdown,
    supplyMethidSpecialTypeDropdown,
    expenseItemW119DropDown,
    paymentMethodDropDown,
    vatTypeDropDown,
    unitOfMeasureDropDown,
    invoiceDocumentTypeDropDown,
    bankDropDown,
    solIdDropDown,
    budgetTypeDropDown,
    glAccountDropDown,
    pettyCashStandardTypeOptions,
    pettyCashConvenienceTypeOptions,
    deliveryConditionOptions,
    deliveryPeriodTypeOptions,
    positionProcOptions,
    positionInspOptions,
    onUpsertAttachmentsFromExpenseDisbursement,
    getByIdAsync,
    getDefaultDepartmentDirectorAsync,
    addVendors,
    addParcelToVendor,
    addGLAccount,
    removeVendorList,
    removeParcelFromVendor,
    removeGlAccount,
    clearBody,
    isEdit,
    isReject,
    isWaitingApproval,
    isWaitingForAssignment,
    isWaitingForCompletion,
    isCompleted,
    updateAsync,
    createAsync,
    actionAsync,
    getDepartmentDDLAsync,
    getSupplyMethodDDLAsync,
    getSupplyMethodTypeDDLAsync,
    getSupplyMethodSpecialTypeDDLAsync,
    getExpenseItemW119DDLAsync,
    getPaymentMethodDDLAsync,
    getVatTypeDDLAsync,
    getUnitOfMeasureDDLAsync,
    getBankDDLAsync,
    getInvoiceDocumentTypeDDLAsync,
    getSolIdDDLAsync,
    getBudgetTypeDDLAsync,
    getGlAccountDDLAsync,
    getPettyCashStandardTypeAsync,
    getPettyCashConvenienceTypeAsync,
    getPettyCashWithoutForm001TypeAsync,
    pettyCashWithoutForm001TypeOptions,
    invalidParentCategoryCodes,
    getDeliveryConditionOptionAsync,
    getDeliveryPeriodTypeOptionsAsync,
    getPositionProcOptions,
    getPositionInspOptions,
    canReCall,
    isJPApproval,
    isDepartmentLevel500Or601,
    isNotFromJorPor001,
    onUpsertAttachments,
    getReviewDocumentAsync,
    onApproveUpdateAsync
  };
});

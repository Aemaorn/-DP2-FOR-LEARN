import { defineStore } from 'pinia';
import { computed, ref, type Ref } from 'vue';
import {
  type Pcm002ActionReq,
  type Pcm002Advance,
  type Pcm002Criteria,
  type Pcm002Detail,
  type Pcm002GlAccount,
  type Pcm002ListResponse,
  type Pcm002StatusCount,
  type Pcm002Vendor,
  type Pcm002VendorParcels,
} from '@/models/PCM/pcm002';
import procurementHelper from '@/helpers/procurement';
import pcm002Service from '@/services/PCM/PCM002'
import { HttpStatusCode } from 'axios';
import Pw119Constant from '@/constants/pcm002';
import type { Option, OptionBadge } from '@/models/shared/option';
import SharedService from '@/services/Shared/dropdown';
import { AssignDepartmentCodeEnum, EGroupCode, EWorkProcess, OrganizationLevelEnum } from '@/enums/shared';
import { isBranchOrganizationLevel } from '@/helpers/organization';
import type { ParticipantsAcceptor } from '@/models/shared/participants';
import type { Attachments } from '@/models/shared/uploadFile';
import ToastHelper from '@/helpers/toast';
import type { defaultAcceptorCriteria, OperationBody } from '@/models/shared/operations';
import { OrganizationLevel, SectionProcessType } from '@/enums/operations';
import { AcceptorStatus, AcceptorType } from '@/enums/participants';
import { isCurrentPendingAcceptor } from '@/helpers/participants';
import { SupplyMethodCode } from '@/enums/supplyMethod';
import { useAuthenticationStore } from '../authentication';
import { Pcm002Action, Pcm002Status } from '@/enums/pcm002';
import { showConfirmDialogAsync, showReasonDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import operationService from '@/services/Shared/operations';
import { DepartmentId } from '@/enums/businessUnit';
import { PreProcurementStep } from '@/enums/preProcurement';

const getDepartmentAsync = async (target: Ref<Option[]>): Promise<void> => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getParameterByGroupCodeAsync = async (target: Ref<Option[]>, groupCode: EGroupCode, parentCode?: string) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(groupCode, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

export const usePcm002ListStore = defineStore('pcm002ListStore', () => {
  const initCriteria = {
    pageNumber: 1,
    pageSize: 10,
    sort: [],
    workProcess: EWorkProcess.InProcess,
    status: Pcm002Status.All,
    budgetYear: new Date().getFullYear() + 543,
  } as Pcm002Criteria;
  const criteria = ref<Pcm002Criteria>(structuredClone(initCriteria));
  const statusOptionBadge = ref([] as OptionBadge[]);
  const statusCount = ref<Pcm002StatusCount>({} as Pcm002StatusCount);
  const dataResponse = ref<Pcm002ListResponse>({} as Pcm002ListResponse);
  const { Pcm002StatusName, Pcm002StatusColor } = Pw119Constant;
  const departmentDropdown = ref<Option[]>([] as Option[]);

  const onDeleteAsync = async (id: string) => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

    const { status } = await pcm002Service.deleteAsync(id);

    if (status === HttpStatusCode.NoContent) {
      await getDataList();

      return ToastHelper.deletedMessageToast();
    }
  };

  const getDepartmentDDLAsync = async (): Promise<void> => {
    await getDepartmentAsync(departmentDropdown);
  };

  const onInitStatusCriteriaPCM002 = () => {
    return procurementHelper.pcm002StatusAttributes(statusCount.value);
  };

  const getDataList = async (): Promise<void> => {
    const { data, status } = await pcm002Service.getListAsync(criteria.value);

    if (status === HttpStatusCode.Ok) {
      dataResponse.value = data;

      getStatusCount(data.statusCount);
    }
  };

  const getStatusCount = (count: Pcm002StatusCount): void => {
    const statusOptions = Object.entries(Pcm002Status).map(([, value]) => ({
      label: Pcm002StatusName(value),
      value: value,
      bgColorClass: Pcm002StatusColor(value).bgColorClass,
      textColorClass: Pcm002StatusColor(value).textColorClass,
      count: getCount(count, value),
    } as OptionBadge));

    statusOptionBadge.value = statusOptions;
  };

  const onResetCriteria = () => {
    criteria.value = structuredClone(initCriteria);
  };

  const getCount = (countAll: Pcm002StatusCount, status: Pcm002Status): number => {
    let count = 0;
    const convertStatus = (status[0]?.toLowerCase() ?? '') + status.slice(1);
    count = countAll[convertStatus as keyof Pcm002StatusCount];

    return count;
  };

  const exportExcelAsync = async (): Promise<void> => {
    const { data, status } = await pcm002Service.exportExcelAsync(criteria.value);

    if (status !== HttpStatusCode.Ok) {
      ToastHelper.error('ดึงข้อมูลเอกสาร', 'ไม่พบเอกสารที่ต้องการ');
      return;
    }

    const monthRangeMap: Record<number, string> = { 1: 'ม.ค.-มี.ค.', 2: 'เม.ย.-มิ.ย.', 3: 'ก.ค.-ก.ย.', 4: 'ต.ค.-ธ.ค.' };
    const quarterLabel = criteria.value.quarter ? `ที่_${criteria.value.quarter}_(${monthRangeMap[criteria.value.quarter]})` : '';
    const yearLabel = criteria.value.budgetYear ? `_${criteria.value.budgetYear}` : '';
    const now = new Date();
    const dateStr = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}${String(now.getHours()).padStart(2, '0')}${String(now.getMinutes()).padStart(2, '0')}${String(now.getSeconds()).padStart(2, '0')}`;
    const fileName = `รายงานประกาศผู้ชนะรายไตรมาส${quarterLabel}${yearLabel}_${dateStr}.xlsx`;
    const url = window.URL.createObjectURL(data);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    a.remove();
  };

  return {
    criteria,
    dataResponse,
    statusOptionBadge,
    departmentDropdown,
    onInitStatusCriteriaPCM002,
    getDataList,
    getDepartmentDDLAsync,
    onResetCriteria,
    onDeleteAsync,
    exportExcelAsync,
  };
});

export const usePcm002DetailStore = defineStore('pcm002DetailStore', () => {
  const authenStore = useAuthenticationStore();
  const cloneAccountingAcceptors = ref<Array<ParticipantsAcceptor>>([]);
  const isCurrentUserAccountingSegmentMember = ref<boolean>(false);

  const initBody = {
    pw119Date: new Date(),
    supplyMethodCode: SupplyMethodCode.sixty,
    budgetYear: (new Date().getFullYear() + 543),
    advance: {
      isAdvance: true,
      advancePaymentMethodCode: 'PaymentMethod002',
    } as Pcm002Advance,
    vendors: [{
      sequence: 1,
      vendorType: '0',
      vendorParcels: [
        { sequence: 1 },
      ] as Pcm002VendorParcels[],
    }] as Pcm002Vendor[],
    glAccounts: [{
      sequence: 1,
    }] as Pcm002GlAccount[],
    attachments: [] as Attachments[],
    acceptors: [] as ParticipantsAcceptor[],
    acceptanceConfirmers: [] as ParticipantsAcceptor[],
    status: Pcm002Status.Draft,
    steps: [PreProcurementStep.W119],
    departmentCode: authenStore.profile.departmentCode,
    subject: 'ขอความเห็นชอบการจัด...................................................................',
    source: `ด้วย${authenStore.profile.departmentName} ได้ดำเนินการจัดซื้อ/จัดจ้าง.......................................จำนวน...............................รายการเป็นจำนวนเงินทั้งสิ้น.........................บาท (...............................) เพื่อใช้สำหรับ....................................... อันเป็นการ จัดซื้อจัดจ้างตามรายการลำดับที่......................ของหนังสือกรมบัญชีกลางที่ กค (กวจ) 0405.2/ 2119 ลงวันที่ 7 มีนาคม 2561และประสงค์จะรายงานขอความเห็นชอบในการดำเนินการจัดซื้อจัดจ้างดังกล่าวข้างต้น`,
  } as Pcm002Detail;

  const detail = ref<Pcm002Detail>(structuredClone(initBody));

  const departmentDropdown = ref<Option[]>([] as Option[]);
  const supplyMethodDropdown = ref<Option[]>([] as Option[]);
  const supplyMethidSpecialTypeDropDown = ref<Option[]>([] as Option[]);
  const expenseItemW119DropDown = ref<Option[]>([] as Option[]);
  const paymentMethodDropDown = ref<Option[]>([] as Option[]);
  const vatTypeDropDown = ref<Option[]>([] as Option[]);
  const unitOfMeasureDropDown = ref<Option[]>([] as Option[]);
  const bankDropDown = ref<Option[]>([] as Option[]);
  const invoiceDocumentTypeDropDown = ref<Option[]>([] as Option[]);
  const solIdDropDown = ref<Option[]>([] as Option[]);
  const budgetTypeDropDown = ref<Option[]>([] as Option[]);
  const glAccountDropDown = ref<Option[]>([] as Option[]);
  const assignDepartmentDDL = ref<Option[]>([]);

  const onResetDetail = () => {
    detail.value = {
      pw119Date: new Date(),
      supplyMethodCode: SupplyMethodCode.sixty,
      budgetYear: (new Date().getFullYear() + 543),
      advance: {
        isAdvance: true,
        advancePaymentMethodCode: 'PaymentMethod002',
      } as Pcm002Advance,
      vendors: [{
        sequence: 1,
        vendorType: '0',
        vendorParcels: [
          { sequence: 1 },
        ] as Pcm002VendorParcels[],
      }] as Pcm002Vendor[],
      glAccounts: [{
        sequence: 1,
      }] as Pcm002GlAccount[],
      attachments: [] as Attachments[],
      acceptors: [] as ParticipantsAcceptor[],
      status: Pcm002Status.Draft,
      steps: [PreProcurementStep.W119],
      departmentCode: authenStore.profile.departmentCode,
      subject: 'ขอความเห็นชอบการจัด...................................................................',
      source: `ด้วย${authenStore.profile.departmentName} ได้ดำเนินการจัดซื้อ/จัดจ้าง.......................................จำนวน...............................รายการเป็นจำนวนเงินทั้งสิ้น.........................บาท (...............................) เพื่อใช้สำหรับ....................................... อันเป็นการ จัดซื้อจัดจ้างตามรายการลำดับที่......................ของหนังสือกรมบัญชีกลางที่ กค (กวจ) 0405.2/ 2119 ลงวันที่ 7 มีนาคม 2561และประสงค์จะรายงานขอความเห็นชอบในการดำเนินการจัดซื้อจัดจ้างดังกล่าวข้างต้น`,
    } as Pcm002Detail;
  };

  const onGetDropdownAsync = async () => {
    await Promise.all([
      getParameterByGroupCodeAsync(supplyMethodDropdown, EGroupCode.SMethod),
      getParameterByGroupCodeAsync(budgetTypeDropDown, EGroupCode.BudgetTyp),
      getParameterByGroupCodeAsync(glAccountDropDown, EGroupCode.GLAcc),
      getParameterByGroupCodeAsync(solIdDropDown, EGroupCode.SolId),
      getParameterByGroupCodeAsync(expenseItemW119DropDown, EGroupCode.ExpenseItemW119),
      getParameterByGroupCodeAsync(paymentMethodDropDown, EGroupCode.PaymentMethod),
      getParameterByGroupCodeAsync(vatTypeDropDown, EGroupCode.VATType),
      getParameterByGroupCodeAsync(unitOfMeasureDropDown, EGroupCode.UnitOfMea),
      getParameterByGroupCodeAsync(bankDropDown, EGroupCode.Bank),
      getParameterByGroupCodeAsync(invoiceDocumentTypeDropDown, EGroupCode.InvoiceDocType),
    ]);

    expenseItemW119DropDown.value = expenseItemW119DropDown.value.map((item, index) => {
      const number = index + 1;
      const parts = item.label.split(' - ');

      if (parts.length > 1) {
        const formattedLabel = `ข้อ ${number}\n${parts.join('\n- ')}`;

        return {
          ...item,
          label: formattedLabel
        };
      }

      return {
        ...item,
        label: `ข้อ ${number}\n${item.label}`
      };
    });
  };

  const getSupplyMethodSpecialTypeDDLAsync = async (parentCode: string): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, parentCode, true);

    if (status === HttpStatusCode.Ok) {
      supplyMethidSpecialTypeDropDown.value = data;
    }
  };

  const getByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await pcm002Service.getByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      detail.value = data;

      if (!detail.value.acceptanceConfirmers) {
        detail.value.acceptanceConfirmers = [];
      }

      if (
        detail.value.id &&
        detail.value.status === Pcm002Status.WaitingDisbursementDate &&
        detail.value.acceptanceConfirmers.length === 0 &&
        !isBranchOrganizationLevel(detail.value.departmentOrganizationLevel)
      ) {
        const { data: members, status: membersStatus } = await operationService.getSegmentAccountingMembersAsync(true);
        if (membersStatus === HttpStatusCode.Ok && members.some((m): boolean => m.userId === authenStore.profile.id)) {
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

      isCurrentUserAccountingSegmentMember.value = false;

      if (
        detail.value.id &&
        [Pcm002Status.WaitingAccountingApproval, Pcm002Status.WaitingDisbursementDate].includes(detail.value.status) &&
        authenStore.profile &&
        !isBranchOrganizationLevel(detail.value.departmentOrganizationLevel)
      ) {
        const { data: accountingMembers, status: accountingStatus } =
          await operationService.getSegmentAccountingMembersAsync(true);

        if (accountingStatus === HttpStatusCode.Ok) {
          isCurrentUserAccountingSegmentMember.value =
            accountingMembers?.some((m): boolean => m.userId === authenStore.profile.id) ?? false;

          if (
            detail.value.status === Pcm002Status.WaitingAccountingApproval &&
            isCurrentUserAccountingSegmentMember.value &&
            !(detail.value.acceptors ?? []).some((a): boolean => a.acceptorType === AcceptorType.AccountingOperator) &&
            !(detail.value.acceptors ?? []).some((a): boolean => a.acceptorType === AcceptorType.AccountingApprover && a.userId === authenStore.profile.id)
          ) {
            if (!detail.value.acceptors) {
              detail.value.acceptors = [];
            }
            detail.value.acceptors.push({
              sequence: 1,
              userId: authenStore.profile.id,
              fullName: authenStore.profile.name,
              positionName: authenStore.profile.positionName,
              acceptorType: AcceptorType.AccountingOperator,
              status: AcceptorStatus.Draft,
            } as ParticipantsAcceptor);
          }
        }
      }

      cloneAccountingAcceptors.value = detail.value.acceptors?.filter(
        a => a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator
      ) || [];
    }
  };

  const addVendors = () => {
    if (detail.value.vendors && Array.isArray(detail.value.vendors)) {
      detail.value.vendors.push(
        {
          sequence: detail.value.vendors.length + 1,
          vendorType: '0',
          vendorParcels: [{
            sequence: 1,
          }] as Pcm002VendorParcels[],
        } as Pcm002Vendor);
    } else {
      detail.value.vendors = [{} as Pcm002Vendor];
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
    } as Pcm002VendorParcels);
  };

  const addGLAccount = () => {
    if (detail.value.glAccounts && Array.isArray(detail.value.glAccounts)) {
      detail.value.glAccounts.push(
        {
          sequence: detail.value.glAccounts.length + 1,
        } as Pcm002GlAccount);
    } else {
      detail.value.glAccounts = [{} as Pcm002GlAccount];
    }
  };

  const getDepartmentDDLAsync = async (): Promise<void> => {
    await getDepartmentAsync(departmentDropdown);
  };

  const limitBudget = 10000;

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
    const totalVendorParcels = Math.round(calculateTotalVendorParcelsPrice() * 100) / 100;
    const totalGlAccounts = Math.round(calculateTotalGlAccountAmount() * 100) / 100;

    if (totalGlAccounts !== totalVendorParcels) {
      ToastHelper.glAccountExceedsTotalPriceMessageToast();
      return false;
    }
    return true;
  }

  const createAsync = async (): Promise<string | undefined> => {
    detail.value.budget = calculateTotalPriceVat({ ...detail.value, budget: 0 });
    if (detail.value.budget > limitBudget) {
      ToastHelper.overBudgetW119MessageToast();
      return;
    }

    if (!validateGlAccountAmount()) {
      return;
    }

    const { data, status } = await pcm002Service.createAsync(detail.value);

    if (status == HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();

      return data;
    }
  };

  const updateAsync = async (id: string, showToast: boolean = true): Promise<boolean> => {

    if (!validateGlAccountAmount()) {
      return false;
    }

    detail.value.budget = calculateTotalPriceVat({ ...detail.value, budget: 0 });

    if (detail.value.budget > limitBudget) {
      ToastHelper.overBudgetW119MessageToast();
      return false;
    }

    const { status, data } = await pcm002Service.updateAsync(id, detail.value);

    if (status === HttpStatusCode.Ok) {
      if (data?.newApprovalRequestDocumentFileId) {
        detail.value.approvalRequestDocumentId = data.newApprovalRequestDocumentFileId;
      }
      if (data?.newWinnerAnnounceDocumentFileId) {
        detail.value.winnerAnnounceDocumentId = data.newWinnerAnnounceDocumentFileId;
      }

      if (showToast) {
        ToastHelper.updatedMessageToast();
      }

      // Refresh data to get updated document versions
      await getByIdAsync(id);

      return true;
    }

    return false;
  };

  const saveAcceptorsAsync = async (id: string): Promise<void> => {
    const { status } = await pcm002Service.updateAsync(id, detail.value);
    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await getByIdAsync(id);
    }
  };

  function calculateTotalPriceVat(detail: Pcm002Detail): number {
    return detail.vendors.reduce((vendorAcc, vendor) => {
      const vendorTotal = vendor.vendorParcels.reduce((parcelAcc, parcel) => {
        return parcelAcc + parcel.totalPriceVat;
      }, 0);
      return vendorAcc + vendorTotal;
    }, 0);
  }

  const actionAsync = async (id: string, reqBody: Pcm002ActionReq, titleMessage: string, detailMessage: string): Promise<void> => {
    const { status } = await pcm002Service.actionAsync(id, reqBody);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.success(titleMessage, detailMessage);

      await getByIdAsync(id);

      return;
    }

    if (status === HttpStatusCode.NotFound) {
      return ToastHelper.error('ดำเนินการล้มเหลว', 'ไม่สามารถดำเนินการได้');
    }
  };

  const getJorporSegmentManagerApproveAsync = async (assignSegmentCode?: string): Promise<OperationBody | null> => {
    const { data, status } = assignSegmentCode === AssignDepartmentCodeEnum.SegmentJorPorOther ?
      await operationService.getSegmentOtherManagerAsync()
      : await operationService.getSegmentITManagerAsync();

    if (status === HttpStatusCode.Ok && data) {
      return data;
    }

    return null;
  };

  const getMergedAcceptorsAsync = async (
    departmentId: string,
    budget: number,
    supplyMethodCode?: string,
    supplyMethodSpecialTypeCode?: string
  ): Promise<void> => {
    const hasAssignSegmentCode = !!detail.value.assignSegmentCode;

    const segmentManager = hasAssignSegmentCode
      ? await getJorporSegmentManagerApproveAsync(detail.value.assignSegmentCode)
      : null;

    // userId ที่ใช้ค้นหา: ใช้ของ segment manager ถ้ามี ถ้าไม่มีให้ใช้ user login
    const criteria: defaultAcceptorCriteria = {
      supplyMethodCode,
      supplyMethodSpecialTypeCode,
      processType: SectionProcessType.PurchaseOrder,
      budget,
      userId: segmentManager?.userId ?? authenStore.profile.id,
      skipCurrentEmployee: false,
    };

    let acceptors: any[] = [];
    const { status, data } = await pcm002Service.getOperationsDefaultAcceptorAsync(criteria);
    if (status === HttpStatusCode.Ok) {
      acceptors = data;
    }

    const toAcceptors = (items: any[]): ParticipantsAcceptor[] =>
      Array.from(new Map(items.map(item => [item.userId, item])).values())
        .map((acceptor, index) => ({
          sequence: index + 1,
          userId: acceptor.userId,
          fullName: acceptor.fullName,
          positionName: acceptor.fullPositionName,
          departmentName: acceptor.businessUnitName,
          acceptorType: AcceptorType.Approver,
          status: AcceptorStatus.Draft,
        } as ParticipantsAcceptor));

    const nonApproverAcceptors = (detail.value.acceptors ?? [])
      .filter((a): boolean => a.acceptorType !== AcceptorType.Approver);

    // 2. ไม่ระบุ assignSegmentCode → แสดงทุกคนจาก getOperationsDefaultAcceptorAsync
    if (!hasAssignSegmentCode) {
      detail.value.acceptors = [...toAcceptors(acceptors), ...nonApproverAcceptors];
      return;
    }

    // 1. ระบุ assignSegmentCode → เอาคนสุดท้าย 1 คนจาก getOperationsDefaultAcceptorAsync
    const lastAcceptor = acceptors[acceptors.length - 1];
    const mergedAcceptors: any[] = [];

    // 1.2 ไม่ใช่ จพ. → รวม default department approver ก่อน
    if (departmentId !== DepartmentId.JorPor) {
      const { status, data } = await operationService.getOperationsDefaultDepartmentAsync(OrganizationLevel.Department);
      if (status === HttpStatusCode.Ok) {
        mergedAcceptors.push(...data);
      }
    }

    if (lastAcceptor) {
      mergedAcceptors.push(lastAcceptor);
    }

    detail.value.acceptors = [...toAcceptors(mergedAcceptors), ...nonApproverAcceptors];
  };

  const getDefaultDisbursementAcceptor = async (budget: number): Promise<void> => {
    if (isBranchOrganizationLevel(detail.value.departmentOrganizationLevel)) return;

    const { data: defaultExpense, status: defaultStatus } =
      await operationService.getDefaultExpenseDisbursementAsync();

    if (defaultStatus === HttpStatusCode.Ok) {
      const params = {
        processType: SectionProcessType.ExpenseDisbursement,
        budget: budget,
        userId: defaultExpense.userId,
        supplyMethodCode: "SectionApprover001",
        skipCurrentEmployee: false,
      } as defaultAcceptorCriteria;

      const { data: acceptorList, status: acceptorStatus } =
        await operationService.getOperationsDefaultAcceptorAsync(params);

      if (acceptorStatus === HttpStatusCode.Ok) {
        if (detail.value.acceptors && detail.value.acceptors.length > 0) {
          detail.value.acceptors = detail.value.acceptors.filter(
            a => a.acceptorType !== AcceptorType.AccountingApprover
          );

          acceptorList.forEach(item =>
            detail.value.acceptors?.push({
              acceptorType: AcceptorType.AccountingApprover,
              fullName: item.fullName,
              positionName: item.fullPositionName,
              sequence: detail.value.acceptors?.length + 1,
              status: AcceptorStatus.Pending,
              userId: item.userId,
              departmentName: item.businessUnitName,
            })
          );

          // Update clone after adding new AccountingApprover
          if (cloneAccountingAcceptors.value.length == 0) {
            cloneAccountingAcceptors.value = detail.value.acceptors?.filter(
              a => a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator
            ) || [];
          }
        }
      }
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

  const removeParcelFromVendor = (vendorIndex: number, parcelIndex: number) => {
    const vendor = detail.value.vendors[vendorIndex];
    if (!vendor) return;

    vendor.vendorParcels.splice(parcelIndex, 1);

    vendor.vendorParcels.forEach((item, idx) => {
      item.sequence = idx + 1;
    });
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

  const onUpsertAttachments = async () => {
    if (!detail.value.id) return;

    const { status } = await pcm002Service.attachmentsAsync(detail.value.id, detail.value.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await getByIdAsync(detail.value.id);
    }
  };

  const onUpsertAttachmentsFromExpenseDisbursement = async (id: string, attachments: Attachments[]) => {
    const { status } = await pcm002Service.attachmentsAsync(id, attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
    }
  };

  const canConfirm = computed((): boolean => {
    const isWaitingDisbursement = detail.value.status == Pcm002Status.WaitingDisbursementDate;
    const isAcceptanceConfirmer = detail.value.acceptanceConfirmers?.some(
      (a): boolean => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authenStore.profile.id
    ) ?? false;

    return isWaitingDisbursement && isAcceptanceConfirmer;
  });

  const isEdit = computed(() => {
    return (detail.value.status === Pcm002Status.Draft ||
      detail.value.status === Pcm002Status.Edit ||
      detail.value.status === Pcm002Status.Rejected) && detail.value.departmentCode === authenStore.profile.departmentCode;
  });

  const isMyDepartment = computed(() => {
    return authenStore.profile.departmentCode === detail.value.departmentCode;
  });

  const isApproveReject = computed(() => {
    if (!detail.value.acceptors) return false;
    const status = detail.value.status == Pcm002Status.WaitingApproval;
    const checkQue = isCurrentPendingAcceptor(detail.value.acceptors, authenStore.profile.id, AcceptorType.Approver);
    return status && checkQue;
  });

  const isAccountingApproveReject = computed(() => {
    const isAcceptor = detail.value.acceptors?.some(a =>
      (a.delegateeUserId ? a.delegateeUserId : a.userId) === authenStore.profile.id &&
      (a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator)
    );
    const status = detail.value.status == Pcm002Status.WaitingAccountingApproval;

    return isAcceptor && status;
  });

  const onSendApprovalAsync = async () => {
    if (detail.value.acceptors?.length == 0) {
      ToastHelper.approvalAtLeastMessageToast();

      return;
    }

    if (!validateGlAccountAmount()) {
      return;
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApprove) || !detail.value.id) return;

    if (!detail.value.acceptors?.find(x => x.acceptorType === AcceptorType.AccountingApprover)) {
      await getDefaultDisbursementAcceptor(detail.value.budget)
    }

    const payload = {
      ...detail.value,
      status: Pcm002Status.WaitingApproval,
    }

    const resp = await pcm002Service.updateAsync(detail.value.id, payload);

    if (resp.status === HttpStatusCode.Ok) {
      const { status } = await pcm002Service.actionAsync(detail.value.id, {
        action: Pcm002Action.RequestApproval,
      });

      if (status === HttpStatusCode.Ok) {
        ToastHelper.sendApproveConfirmMessageToast();

        await getByIdAsync(detail.value.id);
      }
    }
  };

  const onConfirmDisbursementAsync = async () => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData) || !detail.value.id) return;

    const payload = {
      ...detail.value,
      status: Pcm002Status.Paid,
    }

    const resp = await pcm002Service.updateAsync(detail.value.id, payload);

    if (resp.status === HttpStatusCode.Ok) {
      const { status } = await pcm002Service.actionAsync(detail.value.id, {
        action: Pcm002Action.ConfirmDisbursement,
      });

      if (status === HttpStatusCode.Ok) {
        ToastHelper.confirmMessageToast();

        await getByIdAsync(detail.value.id);
      }
    }
  };

  const onRecallAsync = async () => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Edit) || !detail.value.id) return;

    const { status } = await pcm002Service.actionAsync(detail.value.id, {
      action: Pcm002Action.Recall,
    });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.recallEditMessageToast();

      await getByIdAsync(detail.value.id);
    }
  };

  const onRejectedAsync = async () => {
    const res = await showReasonDialogAsync(ReasonDialogType.Reject);

    if (!res.isConfirm || !detail.value.id) return;

    const { status } = await pcm002Service.actionAsync(detail.value.id, {
      action: Pcm002Action.RejectedAcceptor,
      remark: res.reason,
    });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();

      await getByIdAsync(detail.value.id);
    }
  };

  const isLastApprover = computed(() => detail.value.acceptors?.filter(f => f.acceptorType === AcceptorType.Approver && f.status === AcceptorStatus.Pending).length === 1);
  const isLastAccountingApprover = computed(() =>
    detail.value.acceptors?.filter(f =>
      (f.acceptorType === AcceptorType.AccountingApprover || f.acceptorType === AcceptorType.AccountingOperator) &&
      f.status === AcceptorStatus.Pending
    ).length === 1
  );

  const isAccountingCanAssign = computed(() => {
    const accountingAcceptors = detail.value.acceptors?.filter(a =>
      a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator
    );
    if (!accountingAcceptors || accountingAcceptors.length === 0) return false;

    const status = [Pcm002Status.WaitingAccountingApproval].includes(detail.value.status);
    const currentUser = accountingAcceptors.find(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authenStore.profile.id);

    if (!currentUser) return false;

    const firstPending = accountingAcceptors
      .filter(s => s.status === AcceptorStatus.Pending)
      .sort((a, b) => {
        const typeA = a.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;
        const typeB = b.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;
        if (typeA !== typeB) return typeA - typeB;
        return a.sequence - b.sequence;
      })[0];

    const isCurrentUserFirstPending = firstPending != null &&
      (firstPending.delegateeUserId ? firstPending.delegateeUserId : firstPending.userId) === authenStore.profile.id;
    const allAccountPending = accountingAcceptors.every(s => s.status === AcceptorStatus.Pending || s.status === AcceptorStatus.Draft);

    return status && (isCurrentUserFirstPending || allAccountPending);
  });

  const isAccountingMember = computed(() => {
    if (!detail.value.acceptors) return false;
    if (detail.value.status !== Pcm002Status.WaitingAccountingApproval) return false;

    if (
      isBranchOrganizationLevel(detail.value.departmentOrganizationLevel) &&
      authenStore.profile.departmentCode === detail.value.departmentCode
    ) {
      return true;
    }

    const accountingAcceptors = detail.value.acceptors.filter(a =>
      a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator
    );

    const currentUser = accountingAcceptors.find(a =>
      (a.delegateeUserId ? a.delegateeUserId : a.userId) === authenStore.profile.id
    );

    return currentUser != null;
  });

  const isAccountingBranch = computed(() => {
    return detail.value.status === Pcm002Status.WaitingAccountingApproval &&
      isBranchOrganizationLevel(detail.value.departmentOrganizationLevel) &&
      authenStore.profile.departmentCode === detail.value.departmentCode;
  });

  const isBranchOrZoneDepartment = computed((): boolean => {
    return isBranchOrganizationLevel(detail.value.departmentOrganizationLevel) ||
      isBranchOrganizationLevel(authenStore.profile.departmentOrganizationLevel);
  });

  const onApprovedAsync = async () => {
    let dialogType: ReasonDialogType;

    if (isAccountingApproveReject.value) {
      dialogType = ReasonDialogType.Confirm;
    } else if (isLastApprover.value && isLastAccountingApprover.value) {
      dialogType = ReasonDialogType.Approve;
    } else {
      dialogType = ReasonDialogType.Accepted;
    }

    const res = await showReasonDialogAsync(dialogType);

    if (!res.isConfirm || !detail.value.id) return;

    const { status } = await pcm002Service.actionAsync(detail.value.id, {
      action: Pcm002Action.ApprovedAcceptor,
      remark: res.reason,
    });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.approvedMessageToast();

      await getByIdAsync(detail.value.id);
    }
  };

  const isCanRecall = computed(() =>
    authenStore.profile.departmentCode === detail.value.departmentCode &&
    detail.value.acceptors?.filter(a => a.acceptorType === AcceptorType.Approver).every(a => a.status === AcceptorStatus.Pending) &&
    detail.value.status === Pcm002Status.WaitingApproval);

  const getReviewDocumentAsync = async (id: string, documentType: string): Promise<string> => {
    const { data, status } = await pcm002Service.getReviewDocumentAsync(id, documentType);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };

  const getAssignDepartmentDDLAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.AssignDept);

    if (status === HttpStatusCode.Ok) {
      assignDepartmentDDL.value = data;
    }
  };

  return {
    getAssignDepartmentDDLAsync,
    onUpsertAttachmentsFromExpenseDisbursement,
    onSendApprovalAsync,
    onRecallAsync,
    onRejectedAsync,
    onApprovedAsync,
    onConfirmDisbursementAsync,
    isCanRecall,
    isLastApprover,
    detail,
    cloneAccountingAcceptors,
    departmentDropdown,
    supplyMethodDropdown,
    supplyMethidSpecialTypeDropDown,
    expenseItemW119DropDown,
    paymentMethodDropDown,
    vatTypeDropDown,
    unitOfMeasureDropDown,
    bankDropDown,
    invoiceDocumentTypeDropDown,
    solIdDropDown,
    budgetTypeDropDown,
    glAccountDropDown,
    assignDepartmentDDL,
    canConfirm,
    onResetDetail,
    getByIdAsync,
    getMergedAcceptorsAsync,
    addVendors,
    addParcelToVendor,
    addGLAccount,
    removeVendorList,
    removeParcelFromVendor,
    removeGlAccount,
    isEdit,
    isApproveReject,
    isAccountingApproveReject,
    isLastAccountingApprover,
    isAccountingCanAssign,
    isAccountingMember,
    isAccountingBranch,
    isBranchOrZoneDepartment,
    isMyDepartment,
    updateAsync,
    createAsync,
    actionAsync,
    getDepartmentDDLAsync,
    getSupplyMethodSpecialTypeDDLAsync,
    onGetDropdownAsync,
    onUpsertAttachments,
    getReviewDocumentAsync,
    getDefaultDisbursementAcceptor,
    saveAcceptorsAsync,
    isCurrentUserAccountingSegmentMember,
  };
});

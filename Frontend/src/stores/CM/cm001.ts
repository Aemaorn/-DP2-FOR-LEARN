import { CM001Status, souceType } from "@/enums/CM/cm001";
import { EGroupCode, EWorkProcess, OrganizationLevelEnum } from "@/enums/shared";
import type { Option, OptionBadge } from '@/models/shared/option';
import type { CM001Criteria, CM001Detail, CM001Info, CM001Period, CM001Table, PlanAndContractVendorCriteria, PlanAndContractVendorData } from "@/models/CM/cm001";
import SharedService from "@/services/Shared/dropdown";
import { defineStore } from "pinia";
import { ref, type Ref } from "vue";
import { HttpStatusCode } from "axios";
import type { TDataTableResult } from "@/models/shared/paginated";
import CM001Service from "@/services/CM/cm001";
import CM001PeriodService from "@/services/CM/cm001Period";
import { CM001Helper } from "@/helpers/CM/cm001";
import { showConfirmDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType } from "@/enums/dialog";
import router from "@/router";
import ToastHelper from "@/helpers/toast";

const getSupplyMethodAsync = async (groupCode: EGroupCode, target: Ref<Option[]>, parentCode?: string) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(groupCode, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getDepartmentAsync = async (target: Ref<Option[]>): Promise<void> => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};


export const useCm001ListStore = defineStore('cm001-list-store', () => {
  const { MapCM001OptionBadgeStatus } = CM001Helper;

  const initCriteria: CM001Criteria = {
    workProcess: EWorkProcess.InProcess,
    pageNumber: 1,
    pageSize: 10,
    status: CM001Status.All,
  };
  const initTable: TDataTableResult<CM001Table> = {
    data: [],
    totalRecords: 0,
  };
  const initDropdown: Array<Option> = [];
  const initBadgeOptions = [] as Array<OptionBadge>;

  const searchCriteria = ref<CM001Criteria>(structuredClone(initCriteria));
  const statusOptionBadge = ref<Array<OptionBadge>>(structuredClone(initBadgeOptions));
  const departmentCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodTypeCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodSpecialTypeCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));

  const table = ref<TDataTableResult<CM001Table>>(structuredClone(initTable));

  const resetCriteriaAsync = async () => {
    searchCriteria.value = structuredClone(initCriteria);

    await getListAsync();
  };

  const getDefaultDropdownCriteriaAsync = async () => {
    await Promise.all([
      getSupplyMethodAsync(EGroupCode.SMethod, supplyMethodCodeDropdown),
      getSupplyMethodAsync(EGroupCode.SMethodType, supplyMethodTypeCodeDropdown),
      getDepartmentAsync(departmentCodeDropdown)
    ]);
  };

  const getSupplyMethodSpecialTypeCodeDropdownAsync = async (parentId: string) => {
    supplyMethodSpecialTypeCodeDropdown.value = [];
    searchCriteria.value = {
      ...searchCriteria.value,
      supplyMethodTypeCode: undefined,
      supplyMethodSpecialTypeCode: undefined,
    };

    await getSupplyMethodAsync(EGroupCode.SMethod, supplyMethodSpecialTypeCodeDropdown, parentId);
  };

  const getListAsync = async () => {
    const { data, status } = await CM001Service.onGetListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data.data;
      statusOptionBadge.value = MapCM001OptionBadgeStatus(data.statusCount);
    }
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  const onDeleteById = async (id: string) => {
    if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;


    const { status } = await CM001Service.onDeleteByIdAsync(id);

    if (status === HttpStatusCode.NoContent) {
      await getListAsync();

      ToastHelper.deletedMessageToast();
    }
  };

  return {
    table,
    searchCriteria,
    departmentCodeDropdown,
    supplyMethodCodeDropdown,
    supplyMethodTypeCodeDropdown,
    supplyMethodSpecialTypeCodeDropdown,
    statusOptionBadge,
    fn: {
      getListAsync,
      resetCriteriaAsync,
      getDefaultDropdownCriteriaAsync,
      getSupplyMethodSpecialTypeCodeDropdownAsync,
      onChangePageSize,
      onDeleteById,
    }
  }
});

export const useCm001DetailStore = defineStore('cm001-detail-store', () => {
  const initData = {
    periods: [] as Array<CM001Period>,
    cm001Info: {} as CM001Info,
    status: CM001Status.InProgress,
  } as CM001Detail;

  const body = ref<CM001Detail>(structuredClone(initData));

  const departmentDDL = ref<Array<Option>>([]);
  const supplyMethodCodeDDL = ref<Array<Option>>([]);
  const supplyMethodTypeCodeDDL = ref<Array<Option>>([]);
  const supplyMethodSpecialTypeCodeDDL = ref<Array<Option>>([]);

  const getDepartmentDDLAsync = async (): Promise<void> => {
    await getDepartmentAsync(departmentDDL);
  };

  const getSupplyMethodCodeDDLAsync = async (): Promise<void> => {
    await getSupplyMethodAsync(EGroupCode.SMethod, supplyMethodCodeDDL);
  };

  const getSupplyMethodTypeCodeDDLAsync = async (): Promise<void> => {
    await getSupplyMethodAsync(EGroupCode.SMethodType, supplyMethodTypeCodeDDL);
  };

  const getSupplyMethodSpecialTypeCodeDDLAsync = async (parentCode?: string): Promise<void> => {
    supplyMethodSpecialTypeCodeDDL.value = [];

    if (!parentCode) {
      return;
    }

    await getSupplyMethodAsync(EGroupCode.SMethod, supplyMethodSpecialTypeCodeDDL, parentCode);
  };

  const onGetByIdAsync = async (id: string) => {
    const { data, status } = await CM001Service.onGetByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;

      return;
    }

    router.back();
  };

  const onResetBody = () => {
    body.value = structuredClone(initData);
  }

  const onCreateAsync = async () => {
    if (!body.value.sourceType) return;
    if (body.value.sourceType !== souceType.Manual && !body.value.refId) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData, "ยืนยันสร้างข้อมูลส่งมอบตรวจรับ")) return;

    const { data, status } = await CM001Service.onCreateAsync({
      refId: body.value.refId,
      sourceType: body.value.sourceType,
      departmentId: body.value.departmentId,
      supplyMethodCode: body.value.supplyMethodCode,
      supplyMethodTypeCode: body.value.supplyMethodTypeCode,
      supplyMethodSpecialTypeCode: body.value.supplyMethodSpecialTypeCode,
      name: body.value.name,
      budget: body.value.budget,
      isCommercialMaterial: body.value.isCommercialMaterial,
    });

    if (status === HttpStatusCode.Ok) {
      body.value.id = data;

      await onGetByIdAsync(data);

      router.replace({ name: 'cm001Detail', params: { id: data } });
    }
  }

  const onUpdateManualAsync = async (): Promise<boolean> => {
    if (!body.value.id) return false;
    if (body.value.sourceType !== souceType.Manual) return false;

    if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData, "ยืนยันแก้ไขข้อมูล")) return false;

    const { status } = await CM001Service.onUpdateManualAsync(body.value.id, {
      departmentId: body.value.departmentId,
      supplyMethodCode: body.value.supplyMethodCode,
      supplyMethodTypeCode: body.value.supplyMethodTypeCode,
      supplyMethodSpecialTypeCode: body.value.supplyMethodSpecialTypeCode,
      name: body.value.name,
      budget: body.value.budget,
      isCommercialMaterial: body.value.isCommercialMaterial,
    });

    if (status === HttpStatusCode.NoContent || status === HttpStatusCode.Ok) {
      await onGetByIdAsync(body.value.id);
      ToastHelper.updatedMessageToast();
      return true;
    }

    return false;
  };

  const onDeletePeriodAsync = async (periodId: string) => {
    if (!body.value.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete, "ยืนยันการลบรายการส่งมอบตรวจรับ")) return;

    const { status } = await CM001PeriodService.onDeleteAsync(body.value.id, periodId);

    if (status === HttpStatusCode.NoContent) {
      await onGetByIdAsync(body.value.id);

      ToastHelper.deletedMessageToast();
    }
  }

  const onApproveDeliveryAcceptanceAsync = async () => {
    if (!body.value.id) return;

    if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData, "ยืนยันปิดการส่งมอบตรวจรับ")) return;

    const { status } = await CM001Service.onApproveAsync(body.value.id);

    if (status === HttpStatusCode.Ok) {
      await onGetByIdAsync(body.value.id);

      ToastHelper.approvedMessageToast();
    }
  }

  return {
    body,
    departmentDDL,
    supplyMethodCodeDDL,
    supplyMethodTypeCodeDDL,
    supplyMethodSpecialTypeCodeDDL,
    fn: {
      onGetByIdAsync,
      onCreateAsync,
      onUpdateManualAsync,
      onDeletePeriodAsync,
      onApproveDeliveryAcceptanceAsync,
      getDepartmentDDLAsync,
      getSupplyMethodCodeDDLAsync,
      getSupplyMethodTypeCodeDDLAsync,
      getSupplyMethodSpecialTypeCodeDDLAsync,
    },
    onResetBody,
  };
});

export const useCm001DialogStore = defineStore('cm001-dialog-store', () => {
  const detailStore = useCm001DetailStore();

  const sourceTypeDropdown: Array<Option> = [
    { label: 'แผนจัดซื้อจัดจ้าง', value: souceType.Plan },
    { label: 'ทำสัญญา (41 / 30)', value: souceType.ContractDraftVendor },
    { label: 'ไม่ทำสัญญา (40) / อื่นๆ', value: souceType.Procurement },
    { label: 'บันทึกต่อท้าย', value: souceType.ContractDraftVendorEdit },
  ];

  const initCriteria: PlanAndContractVendorCriteria = {
    pageNumber: 1,
    pageSize: 10,
  };

  const initTable: TDataTableResult<PlanAndContractVendorData> = {
    data: [],
    totalRecords: 0,
  };

  const initDropdown: Array<Option> = [];

  const searchCriteria = ref<PlanAndContractVendorCriteria>(structuredClone(initCriteria));
  const table = ref<TDataTableResult<PlanAndContractVendorData>>(structuredClone(initTable));
  const departmentDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodTypeCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodSpecialTypeCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));

  const getPlanAndContractVendorListAsync = async () => {
    const { data, status } = await CM001Service.onGetPlanAndContractVendorListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data;
    }
  };

  const getDepartmentDropdownAsync = async () => {
    await getDepartmentAsync(departmentDropdown);
  };

  const getSupplyMethodDropdownAsync = async () => {
    await getSupplyMethodAsync(EGroupCode.SMethod, supplyMethodCodeDropdown);
  };

  const getSupplyMethodTypeDropdownAsync = async (parentCode?: string) => {
    supplyMethodTypeCodeDropdown.value = [];
    searchCriteria.value.supplyMethodTypeCode = undefined;
    searchCriteria.value.supplyMethodSpecialTypeCode = undefined;

    await getSupplyMethodAsync(EGroupCode.SMethodType, supplyMethodTypeCodeDropdown, parentCode);
  };

  const getSupplyMethodSpecialTypeDropdownAsync = async (parentCode?: string) => {
    supplyMethodSpecialTypeCodeDropdown.value = [];
    searchCriteria.value.supplyMethodSpecialTypeCode = undefined;

    await getSupplyMethodAsync(EGroupCode.SMethod, supplyMethodSpecialTypeCodeDropdown, parentCode);
  };

  const resetCriteria = () => {
    searchCriteria.value = structuredClone(initCriteria);
    getPlanAndContractVendorListAsync();
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  const onSelectPlanAndContractVendor = (item: PlanAndContractVendorData) => {
    const isPlan = item.sourceType === souceType.Plan;

    detailStore.body = {
      ...detailStore.body,
      refId: item.id,
      refCode: isPlan ? item.planCode : item.contractNumber,
      sourceType: item.sourceType as souceType,
      cm001Info: {
        planCode: item.planCode,
        procurementNumber: item.contractNumber,
        procurementId: item.procurementId,
        departmentName: item.departmentName,
        planType: item.planType,
        vendorName: item.vendorName,
        vendorEmail: item.vendorEmail,
        contractNumber: item.contractNumber,
        poNumber: item.poNumber,
        contractBudget: item.contractBudget,
        name: item.name,
        contractTypeName: item.contractTypeName,
        templateName: item.templateName,
        contractDate: item.contractDate,
        period: item.period,
        deliveryDate: item.deliveryDate,
        supplyMethod: item.supplyMethod,
        supplyMethodType: item.supplyMethodType,
        supplyMethodSpecialType: item.supplyMethodSpecialType,
        budgetYear: item.budgetYear,
        budget: item.budget,
        sourceType: item.sourceType,
        createdAt: item.createdAt,
        isStock: item.isStock,
      },
    };
  };

  return {
    searchCriteria,
    table,
    departmentDropdown,
    supplyMethodCodeDropdown,
    supplyMethodTypeCodeDropdown,
    supplyMethodSpecialTypeCodeDropdown,
    sourceTypeDropdown,
    fn: {
      getPlanAndContractVendorListAsync,
      getDepartmentDropdownAsync,
      getSupplyMethodDropdownAsync,
      getSupplyMethodTypeDropdownAsync,
      getSupplyMethodSpecialTypeDropdownAsync,
      resetCriteria,
      onChangePageSize,
      onSelectPlanAndContractVendor,
    }
  };
});
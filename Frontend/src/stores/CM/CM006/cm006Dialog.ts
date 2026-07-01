import { EGroupCode, OrganizationLevelEnum } from "@/enums/shared";
import type { Cm006DialogCriteria, Cm006DialogItem } from "@/models/CM/cm006";
import type { Option } from "@/models/shared/option";
import type { TDataTableResult } from "@/models/shared/paginated";
import Cm006Service from "@/services/CM/cm006";
import SharedService from "@/services/Shared/dropdown";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref, type Ref } from "vue";
import { useCm006DetailStore } from "./cm006.detail";

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

export const useCm006DialogStore = defineStore('cm006Dialog', () => {
  const detailStore = useCm006DetailStore();

  const initCriteria: Cm006DialogCriteria = {
    pageNumber: 1,
    pageSize: 10,
  };

  const initTable: TDataTableResult<Cm006DialogItem> = {
    data: [],
    totalRecords: 0,
  };

  const initDropdown: Array<Option> = [];

  const searchCriteria = ref<Cm006DialogCriteria>(structuredClone(initCriteria));
  const table = ref<TDataTableResult<Cm006DialogItem>>(structuredClone(initTable));
  const departmentDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodTypeCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodSpecialTypeCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));

  const getDialogListAsync = async () => {
    const { data, status } = await Cm006Service.getDialogListAsync(searchCriteria.value);

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
    getDialogListAsync();
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  const onSelectDialogItem = async (item: Cm006DialogItem) => {
    detailStore.body = {
      ...detailStore.body,
      id: item.id,
      taxId: item.taxId,
      entrepreneurName: item.entrepreneurName,
      entrepreneurEmail: item.entrepreneurEmail,
      contractNumber: item.contractNumber,
      poNumber: item.poNumber,
      budget: item.budget,
      contractName: item.contractName,
      contractType: item.contractType ?? '',
      contractTemplate: item.contractTemplate ?? '',
      contractSignedDate: item.contractSignedDate ?? new Date(),
      deliveryLeadTime: item.deliveryLeadTime ?? 0,
      deliveryLeadTimeTypeCode: item.deliveryLeadTimeTypeCode ?? '',
      deliveryLeadTimeTypeLabel: item.deliveryLeadTimeTypeLabel ?? '',
      deliveryDate: item.deliveryDate ?? new Date(),
    };

    await detailStore.onGetById(item.id);
  };

  return {
    searchCriteria,
    table,
    departmentDropdown,
    supplyMethodCodeDropdown,
    supplyMethodTypeCodeDropdown,
    supplyMethodSpecialTypeCodeDropdown,
    fn: {
      getDialogListAsync,
      getDepartmentDropdownAsync,
      getSupplyMethodDropdownAsync,
      getSupplyMethodTypeDropdownAsync,
      getSupplyMethodSpecialTypeDropdownAsync,
      resetCriteria,
      onChangePageSize,
      onSelectDialogItem,
    }
  };
});

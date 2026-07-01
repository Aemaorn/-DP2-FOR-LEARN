import { EGroupCode, OrganizationLevelEnum } from "@/enums/shared";
import type { Cm007DialogCriteria, Cm007DialogItem } from "@/models/CM/cm007";
import type { Option } from "@/models/shared/option";
import type { TDataTableResult } from "@/models/shared/paginated";
import Cm007Service from "@/services/CM/cm007";
import SharedService from "@/services/Shared/dropdown";
import operationService from "@/services/Shared/operations";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref, type Ref } from "vue";
import router from "@/router";
import { useCm007DetailStore } from "./cm007.detail";

const store = useCm007DetailStore();

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

export const useCm007DialogStore = defineStore('cm007Dialog', () => {
  const initCriteria: Cm007DialogCriteria = {
    pageNumber: 1,
    pageSize: 10,
  };

  const initTable: TDataTableResult<Cm007DialogItem> = {
    data: [],
    totalRecords: 0,
  };

  const initDropdown: Array<Option> = [];

  const searchCriteria = ref<Cm007DialogCriteria>(structuredClone(initCriteria));
  const table = ref<TDataTableResult<Cm007DialogItem>>(structuredClone(initTable));
  const departmentDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodTypeCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const supplyMethodSpecialTypeCodeDropdown = ref<Array<Option>>(structuredClone(initDropdown));
  const getDialogListAsync = async () => {
    const { data, status } = await Cm007Service.getDialogListAsync(searchCriteria.value);

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

  const selectedItem = ref<Cm007DialogItem | null>(null);

  const onSelectDialogItem = (item: Cm007DialogItem) => {
    selectedItem.value = item;
  };

  const onConfirmCreateAsync = async () => {
    if (!selectedItem.value) return;

    const { data: directorAssignees } = await operationService.getContractDirectorAsync(true);

    const payload = {
      contractDraftVendorId: selectedItem.value.id,
      assignees: directorAssignees ?? [],
    };

    const { data, status } = await Cm007Service.onCreateAsync(payload);

    if (status === HttpStatusCode.Created || status === HttpStatusCode.Ok) {
      selectedItem.value = null;
      router.replace({ name: 'cm007Detail', params: { id: data } });

      await store.onGetById(data);
    }
  };

  const onClearSelected = () => {
    selectedItem.value = null;
  };

  return {
    searchCriteria,
    table,
    selectedItem,
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
      onConfirmCreateAsync,
      onClearSelected,
    }
  };
});

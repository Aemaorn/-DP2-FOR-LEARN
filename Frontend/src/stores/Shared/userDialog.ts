import { OrganizationLevelEnum } from "@/enums/shared";
import type { TDataTableResult } from "@/models/shared/paginated";
import type { TUserDialog, TUserDialogCriteria } from "@/models/ST/st005";
import type { Option } from "@/models/shared/option";
import SharedService from "@/services/Shared/dropdown";
import ST005Service from "@/services/ST/ST005";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref, type Ref } from "vue";

export const useUserDialogStore = defineStore('user-dialog-store', () => {
  const initCriteria: TUserDialogCriteria = {
    pageNumber: 1,
    pageSize: 10,
    isActive: true,
  };

  const initTable = {
    data: [] as TUserDialog[],
    totalRecords: 0,
  } as TDataTableResult<TUserDialog>;

  const searchCriteria = ref<TUserDialogCriteria>(structuredClone(initCriteria));
  const isShow = ref(false);
  const isByDepartment = ref(false);
  let resolvePromise: ((value: TUserDialog | undefined) => void) | undefined;

  const dropdowns = {
    group: ref<Option[]>([] as Option[]),
    line: ref<Option[]>([] as Option[]),
    department: ref<Option[]>([] as Option[]),
  };

  const table = ref(structuredClone(initTable));

  const onChangePageSize = async (pageNumber: number, pageSize: number): Promise<void> => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  const onOpenDialog = async (deptCode?: string): Promise<TUserDialog | undefined> => {
    if (deptCode) isByDepartment.value = true;

    searchCriteria.value.departmentCode = deptCode ?? searchCriteria.value.departmentCode;
    await Promise.all([onGetUserListDataAsync(), onGetAllDropdownDialogAsync()]);

    isShow.value = true;

    return new Promise((resolve) => {
      resolvePromise = resolve;
    });
  };

  const onSelected = (item: TUserDialog) => {
    if (resolvePromise) {
      resolvePromise(item);

      onClosed();
    }
  };

  const onResetCriteria = () => {
    if (isByDepartment.value) {
      searchCriteria.value = {
        pageNumber: 1,
        pageSize: 10,
        isActive: true,
        departmentCode: searchCriteria.value.departmentCode,
      };

      return;
    };

    searchCriteria.value = structuredClone(initCriteria);
  };

  const onClosed = () => {
    isShow.value = false;
    isByDepartment.value = false;
    searchCriteria.value = structuredClone(initCriteria);
    table.value = structuredClone(initTable);
  };

  const onGetUserListDataAsync = async (): Promise<void> => {
    const { data, status } = await ST005Service.onGetUserDialogAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data;
    }
  };

  const fetchDropdown = async (level: OrganizationLevelEnum, target: Ref<Option[]>) => {
    const { data, status } = await SharedService.onGetBusinessUnitAsync(level);

    if (status === HttpStatusCode.Ok) {
      target.value = data;
    }
  };

  const onGetAllDropdownDialogAsync = async () => {
    await Promise.all([getDropDownGroup(), getDropDownLine(), getDropDownDepartment()]);
  };

  const getDropDownGroup = async () => await fetchDropdown(OrganizationLevelEnum.Group, dropdowns.group);
  const getDropDownLine = async () => await fetchDropdown(OrganizationLevelEnum.Line, dropdowns.line);
  const getDropDownDepartment = async () => await fetchDropdown(OrganizationLevelEnum.Department, dropdowns.department);

  return {
    // Variables
    isShow,
    searchCriteria,
    table,
    dropdowns,
    isByDepartment,

    // Functions
    onChangePageSize,
    onGetUserListDataAsync,
    onResetCriteria,

    // States
    onOpenDialog,
    onSelected,
    onClosed,
  };
});
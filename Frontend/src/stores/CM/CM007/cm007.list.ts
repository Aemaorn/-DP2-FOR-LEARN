import { Cm007Status } from "@/enums/CM/cm007";
import { EGroupCode, EWorkProcess, OrganizationLevelEnum } from "@/enums/shared";
import type { Option } from "@/models/shared/option";
import type { Cm007Criteria, Cm007List } from "@/models/CM/cm007";
import type { TDataTableResult } from "@/models/shared/paginated";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref } from "vue";
import SharedService from "@/services/Shared/dropdown";
import Cm007Service from "@/services/CM/cm007";
import { showConfirmDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType } from "@/enums/dialog";
import ToastHelper from "@/helpers/toast";

export const useCm007ListStore = defineStore('cm007List', () => {
  const initCriteria: Cm007Criteria = {
    workProcess: EWorkProcess.InProcess,
    pageNumber: 1,
    pageSize: 10,
    status: Cm007Status.All,
  };

  const initList: TDataTableResult<Cm007List> = {
    data: [],
    totalRecords: 0
  };

  const searchCriteria = ref<Cm007Criteria>(structuredClone(initCriteria));
  const table = ref<TDataTableResult<Cm007List>>(structuredClone(initList));
  const options = ref<{ department: Array<Option>, contractType: Array<Option> }>({
    department: [] as Array<Option>,
    contractType: [] as Array<Option>,
  });

  const onResetCriteria = () => {
    searchCriteria.value = structuredClone(initCriteria);
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    searchCriteria.value.pageNumber = pageNumber;
    searchCriteria.value.pageSize = pageSize;
  };

  const onGetList = async () => {
    const { data, status } = await Cm007Service.getListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = structuredClone(data);
    }
  };

  const onDelete = async (id: string) => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

    const { status } = await Cm007Service.deleteAsync(id);

    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();

      onGetList();
    }
  }

  const onGetDropdown = async () => {
    const [department, contractType] = await Promise.all([
      SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department),
      SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CMRentalType, undefined, undefined, EGroupCode.CMType)]);

    if (department.status === HttpStatusCode.Ok) {
      options.value.department = department.data;
    }

    if (contractType.status === HttpStatusCode.Ok) {
      options.value.contractType = contractType.data;
    }
  };

  return {
    // Variables
    searchCriteria,
    table,
    options,

    // Functions
    onResetCriteria,
    onChangePageSize,
    onGetList,
    onGetDropdown,
    onDelete
  };
});

import { Cm006Status } from "@/enums/CM/cm006";
import { EGroupCode, EWorkProcess, OrganizationLevelEnum } from "@/enums/shared";
import type { Option } from "@/models/shared/option";
import type { Cm006Criteria, Cm006List } from "@/models/CM/cm006";
import type { TDataTableResult } from "@/models/shared/paginated";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref } from "vue";
import SharedService from "@/services/Shared/dropdown";
import Cm006Service from "@/services/CM/cm006";

export const useCm006ListStore = defineStore('cm006List', () => {
  const initCriteria: Cm006Criteria = {
    workProcess: EWorkProcess.InProcess,
    pageNumber: 1,
    pageSize: 10,
    status: Cm006Status.All,
  };

  const initList: TDataTableResult<Cm006List> = {
    data: [],
    totalRecords: 0
  };

  const searchCriteria = ref<Cm006Criteria>(structuredClone(initCriteria));
  const table = ref<TDataTableResult<Cm006List>>(structuredClone(initList));
  const options = ref<{ department: Array<Option>, rentalType: Array<Option> }>({
    department: [] as Array<Option>,
    rentalType: [] as Array<Option>,
  });

  const onResetCriteria = () => {
    searchCriteria.value = structuredClone(initCriteria);
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    searchCriteria.value.pageNumber = pageNumber;
    searchCriteria.value.pageSize = pageSize;
  };

  const onGetList = async () => {
    const { data, status } = await Cm006Service.getListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = structuredClone(data);
    }
  };

  const onGetDropdown = async () => {
    const [department, rentalType] = await Promise.all([
      SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department),
      SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CMRentalType, undefined, undefined, EGroupCode.CMType)]);

    if (department.status === HttpStatusCode.Ok) {
      options.value.department = department.data;
    }

    if (rentalType.status === HttpStatusCode.Ok) {
      options.value.rentalType = rentalType.data;
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
  };
});
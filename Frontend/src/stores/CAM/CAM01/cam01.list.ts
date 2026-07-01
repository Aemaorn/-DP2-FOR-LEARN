import Cam01Constants from "@/constants/CAM/CAM01/cam01";
import { Cam01Status } from "@/enums/CAM/CAM01/cam01";
import { EGroupCode, EWorkProcess } from "@/enums/shared";
import type { Cam01Criteria, Cam01List, Cam01StatusCount } from "@/models/CAM/CAM01/cam01";
import type { Option, OptionBadge } from "@/models/shared/option";
import type { TDataTableResult } from "@/models/shared/paginated";
import Cam01Service from "@/services/CAM/CAM01/cam01";
import SharedService from "@/services/Shared/dropdown";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref } from "vue";

export const useCam01ListStore = defineStore('cam01-list-store', () => {
  const { Cam01StatusName, Cam01ListStatusColor } = Cam01Constants;

  const initCriteria: Cam01Criteria = {
    pageNumber: 1,
    pageSize: 10,
    workProcess: EWorkProcess.InProcess,
    status: Cam01Status.All,
  };

  const initTable = {
    data: [],
    totalRecords: 0,
  } as TDataTableResult<Cam01List>;

  const criteria = ref<Cam01Criteria>(structuredClone(initCriteria));
  const table = ref<TDataTableResult<Cam01List>>(structuredClone(initTable));
  const options = ref<{ rentalType: Array<Option> }>({
    rentalType: [] as Array<Option>,
  });

  const statusOptionBadge = ref([] as OptionBadge[]);

  const mapStatusCount = (count: Cam01StatusCount) => {
    return Object.entries(Cam01Status).map(([, value]) => ({
      label: Cam01StatusName(value),
      value: value,
      bgColorClass: Cam01ListStatusColor(value).bgColorClass,
      textColorClass: Cam01ListStatusColor(value).textColorClass,
      count: getCount(count, value),
    } as OptionBadge));
  };

  const getCount = (countAll: Cam01StatusCount, status: Cam01Status): number => {
    let count = 0;
    const convertStatus = (status[0]?.toLowerCase() ?? '') + status.slice(1);
    count = countAll[convertStatus as keyof Cam01StatusCount];

    return count;
  };

  const onResetCriteria = () => {
    criteria.value = structuredClone(initCriteria);
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    criteria.value.pageNumber = pageNumber;
    criteria.value.pageSize = pageSize;
  };

  const onGetList = async () => {
    const { data, status } = await Cam01Service.onGetListAsync(criteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data.data;
      statusOptionBadge.value = mapStatusCount(data.statusCount);
    }
  };

  const onGetDropdown = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CMType);

    if (status === HttpStatusCode.Ok) {
      options.value.rentalType = data;
    }
  };

  return {
    // Variable
    criteria,
    table,
    options,
    statusOptionBadge,

    // Functions
    onResetCriteria,
    onChangePageSize,
    onGetList,
    onGetDropdown,
  };
});
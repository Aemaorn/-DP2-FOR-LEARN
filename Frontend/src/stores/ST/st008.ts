import type { TDataTableResult } from "@/models/shared/paginated";
import type { ST008Criteria, ST008List } from "@/models/ST/st008";
import ST008Service from "@/services/ST/ST008";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref } from "vue";

export const useST008Store = defineStore('st008-store', () => {
  const initCriteria: ST008Criteria = {
    pageNumber: 1,
    pageSize: 10,
  };

  const initTable: TDataTableResult<ST008List> = {
    data: [] as Array<ST008List>,
    totalRecords: 0,
  };

  const searchCriteria = ref<ST008Criteria>(structuredClone(initCriteria));
  const table = ref<TDataTableResult<ST008List>>(structuredClone(initTable));

  const onGetListAsync = async () => {
    const { data, status } = await ST008Service.onGetListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data;
    }
  };

  const onResetCriteria = () => {
    searchCriteria.value = structuredClone(initCriteria);
  };


  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  return {
    searchCriteria,
    table,

    onGetListAsync,
    onResetCriteria,
    onChangePageSize,
  };
});
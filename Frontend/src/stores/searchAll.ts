import type { TSearchAllCriteria, TSearchAllData } from "@/models/searchCriteria";
import type { TDataTableResult } from "@/models/shared/paginated";
import { searchAllService } from "@/services/searchAll";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref } from "vue";

export const useSearchAllStore = defineStore("search-all-store", () => {
  const searchCriteria = ref<TSearchAllCriteria>({
    pageNumber: 1,
    pageSize: 50,
  });

  const dateTableItems = ref<TDataTableResult<TSearchAllData>>({
    data: [],
    totalRecords: 0,
  });

  const searchAllAsync = async () => {
    const { data, status } = await searchAllService.getSearchAllAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      dateTableItems.value = data;
    }
  }

  return {
    searchCriteria,
    dateTableItems,
    searchAllAsync
  }
})
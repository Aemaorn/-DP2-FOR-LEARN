import type { TDataTableResult } from "@/models/shared/paginated";
import type { TSt003Criteria, TSt003List } from "@/models/ST/st003";
import type { Option } from '@/models/shared/option';
import ST003Service from "@/services/ST/ST003";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref } from "vue";
import SharedService from "@/services/Shared/dropdown";
import { EGroupCode } from "@/enums/shared";

export const usePartnerDialogStore = defineStore(
  'partner-dialog-store',
  () => {
    const entrepreneurTypeOptions = ref<Array<Option>>([]);

    const searchCriteria = ref<TSt003Criteria>({
      pageNumber: 1,
      pageSize: 10,
    });

    const table = ref({
      data: [] as TSt003List[],
      totalRecords: 0,
    } as TDataTableResult<TSt003List>);

    const onGetEntrepreneurTypeOptionsAsync = async (): Promise<void> => {
      const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.TraderType);

      if (status === HttpStatusCode.Ok) {
        entrepreneurTypeOptions.value = data;
      }
    };

    const onGetPartnerListDataAsync = async (): Promise<void> => {
      const { data, status } = await ST003Service.onGetLookupAsync(searchCriteria.value);

      if (status === HttpStatusCode.Ok) {
        table.value = data;
      }
    };

    const onChangePageSize = async (pageNumber: number, pageSize: number): Promise<void> => {
      searchCriteria.value = {
        ...searchCriteria.value,
        pageNumber,
        pageSize,
      };

      await onGetPartnerListDataAsync();
    };

    const onResetCriteriaAsync = async (): Promise<void> => {
      searchCriteria.value = {
        pageNumber: 1,
        pageSize: 10,
      } as TSt003Criteria;

      await onGetPartnerListDataAsync();
    };

    const onResetStore = (): void => {
      entrepreneurTypeOptions.value = [];

      searchCriteria.value = {
        pageNumber: 1,
        pageSize: 10,
      } as TSt003Criteria;

      table.value = {
        data: [] as TSt003List[],
        totalRecords: 0,
      } as TDataTableResult<TSt003List>;
    };

    return {
      searchCriteria,
      table,
      onGetEntrepreneurTypeOptionsAsync,
      onGetPartnerListDataAsync,
      onChangePageSize,
      onResetCriteriaAsync,
      onResetStore,
      entrepreneurTypeOptions,
    };
  });
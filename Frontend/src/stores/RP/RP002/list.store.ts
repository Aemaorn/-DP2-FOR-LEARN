import RP002Constants from "@/constants/RP/rp002";
import { ConfirmDialogType } from "@/enums/dialog";
import { RP002Status } from "@/enums/RP/rp002";
import { showConfirmDialogAsync } from "@/helpers/dialog";
import ToastHelper from "@/helpers/toast";
import { type RP002Criteria, type RP002List } from "@/models/RP/rp002";
import type { OptionBadge } from "@/models/shared/option";
import type { TDataTableResult } from "@/models/shared/paginated";
import RP002Service from "@/services/RP/rp002";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia"
import { ref } from "vue";

export const useRP002ListStore = defineStore('rp002-list-store', () => {
  const initRP002Criteria = (defaults?: Partial<RP002Criteria>): RP002Criteria => {
    return {
      pageNumber: 1,
      pageSize: 10,
      status: RP002Status.All,
      year: new Date().getFullYear() + 543,
      ...defaults,
    }
  };

  const { MapCountStatus } = RP002Constants;

  const initRP002Table = (): TDataTableResult<RP002List> => ({ data: [] as Array<RP002List>, totalRecords: 0 });

  const searchCriteria = ref<RP002Criteria>(initRP002Criteria());
  const table = ref<TDataTableResult<RP002List>>(initRP002Table());
  const statusOptionBadge = ref([] as OptionBadge[]);

  const onResetCriteria = () => {
    searchCriteria.value = initRP002Criteria();
  };

  const onGetListAsync = async () => {
    const { data, status } = await RP002Service.GetListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data.data;
      statusOptionBadge.value = MapCountStatus(data.statusCount);
    }
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  const onDeleteByIdAsync = async (id: string) => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

    const { status } = await RP002Service.DeleteByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.deletedMessageToast();

      await onGetListAsync();
    }
  }

  return {
    searchCriteria,
    table,
    statusOptionBadge,

    onResetCriteria,
    onGetListAsync,
    onChangePageSize,
    onDeleteByIdAsync,
  };
});
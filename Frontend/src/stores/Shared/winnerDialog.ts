import type { TDataTableResult } from "@/models/shared/paginated";
import type { PP007GetWinnerCriteria, PP007GetWinnerResponse } from "@/views/PP/models/PP007/pp007Model";
import { HttpStatusCode, type AxiosResponse } from "axios";
import { defineStore } from "pinia";
import { ref } from "vue";
import pp007service from "@/views/PP/services/PP007/PP007Service";
import principleApprovalRentalService from "@/services/PCM/PCM005/principleApprovalRental";

export const useWinnerDialogStore = defineStore('winner-dialog-store', () => {
  const criteria = ref<PP007GetWinnerCriteria>({} as PP007GetWinnerCriteria);
  const winnerListReponse = ref<TDataTableResult<PP007GetWinnerResponse>>({} as TDataTableResult<PP007GetWinnerResponse>);
  const isShow = ref<boolean>(false);

  let resolvePromise: ((value: PP007GetWinnerResponse | undefined) => void) | undefined;

  const getListAsync = async (): Promise<void> => {
    const mapApi: Record<string, (params: PP007GetWinnerCriteria) => Promise<AxiosResponse<any, any>>> = {
      'true': async (params) => await principleApprovalRentalService.getListRentalWinnerAsync(params),
      'false': async (params) => await pp007service.getListWinnerAsync(params),
    };

    const strRental = String(criteria.value.isRental);

    const { data, status } = await mapApi[strRental](criteria.value);

    if (status === HttpStatusCode.Ok) {
      winnerListReponse.value = data;
    }
  };

  const onClearCriteriaAsync = async (): Promise<void> => {
    criteria.value = {
      pageNumber: 1,
      pageSize: 10,
      jp006Id: criteria.value.jp006Id,
      procurementId: criteria.value.procurementId,
      isRental: criteria.value.isRental,
    } as PP007GetWinnerCriteria;

    await getListAsync();
  };

  const onChangePageSizeAsync = async (pageNumber: number, pageSize: number): Promise<void> => {
    criteria.value.pageNumber = pageNumber;
    criteria.value.pageSize = pageSize;

    await getListAsync();
  };

  const onOpenDialog = async (procurementId: string, jp006Id: string, keyword?: string, type?: string, isRental: boolean = false): Promise<PP007GetWinnerResponse | undefined> => {
    criteria.value = {
      pageNumber: 1,
      pageSize: 10,
      procurementId,
      jp006Id,
      keyword,
      type,
      isRental,
    } as PP007GetWinnerCriteria;

    await getListAsync();

    isShow.value = true;

    return new Promise((resolve) => {
      resolvePromise = resolve;
    });
  };

  const onSelected = (item: PP007GetWinnerResponse) => {
    if (resolvePromise) {
      resolvePromise(item);
    }

    isShow.value = false;
  };

  return {
    isShow,
    criteria,
    winnerListReponse,
    getListAsync,
    onClearCriteriaAsync,
    onChangePageSizeAsync,

    onOpenDialog,
    onSelected,
  }
});
import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt011Detail, TSt011Criteria, TSt011List } from '@/models/ST/st011';
import ST011Service from '@/services/ST/ST011';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';

// Exported so the st012 (district) and st013 (subdistrict) stores can build their "จังหวัด"
// dropdown/name lookups from the same live list.
export const getAllProvinces = async (): Promise<TSt011List[]> => {
  const { data, status } = await ST011Service.onGetListAsync({ pageNumber: 1, pageSize: 10000, sort: [] });

  return status === HttpStatusCode.Ok ? data.data : [];
};

export const useSt011ListStore = defineStore('st-011-list-store', () => {
  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  } as TSt011Criteria);

  const table = ref({
    data: [] as TSt011List[],
    totalRecords: 0,
  } as TDataTableResult<TSt011List>);

  const onGetListData = async (): Promise<void> => {
    const { data, status } = await ST011Service.onGetListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data;
    }
  };

  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  const onResetCriteria = (): void => {
    searchCriteria.value = {
      pageNumber: 1,
      pageSize: 10,
      sort: [],
    };
  };

  const onDeleteByIdAsync = async (id: string): Promise<void> => {
    const { status } = await ST011Service.onDeleteAsync(id);

    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();
      await onGetListData();
    }
  };

  return {
    searchCriteria,
    table,
    onGetListData,
    onChangePageSize,
    onResetCriteria,
    onDeleteByIdAsync,
  };
});

const generateNextCode = (allProvinces: TSt011List[]): string => {
  const maxCode = allProvinces.reduce((max, p) => Math.max(max, Number(p.code) || 0), 0);
  return String(maxCode + 1).padStart(2, '0');
};

export const useSt011DetailStore = defineStore('st-011-detail-store', () => {
  const router = useRouter();

  const body = ref({} as TSt011Detail);
  const isSubmitting = ref(false);

  const onResetBody = (): void => {
    body.value = {} as TSt011Detail;
  };

  const onInitCreate = async (): Promise<void> => {
    const allProvinces = await getAllProvinces();
    body.value = { code: generateNextCode(allProvinces), nameTh: '', nameEn: '' };
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await ST011Service.onGetByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = { ...data, nameEn: data.nameEn ?? '' };
    }
  };

  const onCreateAsync = async (): Promise<void> => {
    const { status } = await ST011Service.onCreateAsync(body.value);

    if (status === HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();
      router.replace({ name: 'st011' });
    }
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const { status } = await ST011Service.onUpdateAsync(id, body.value);

    if (status === HttpStatusCode.NoContent) {
      ToastHelper.updatedMessageToast();
    }
  };

  const onSubmitAsync = async (id?: string): Promise<void> => {
    if (isSubmitting.value) return;

    isSubmitting.value = true;

    try {
      if (id) {
        await onUpdateAsync(id);
        return;
      }

      await onCreateAsync();
    } finally {
      isSubmitting.value = false;
    }
  };

  return {
    body,
    isSubmitting,
    onResetBody,
    onInitCreate,
    onGetByIdAsync,
    onSubmitAsync,
  };
});

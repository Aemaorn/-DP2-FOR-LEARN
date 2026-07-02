import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TDistrictDetail, TDistrictCriteria, TDistrictList } from '@/models/ST/district';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';

// Demo data only — backend endpoint /api/st/district does not exist yet.
// Once it does, swap the bodies below for DistrictService calls (see ST010/ST004 stores for the pattern).
const mockDistricts: TDistrictList[] = [
  { id: '1001', code: '1001', nameTh: 'เขตพระนคร', nameEn: 'Phra Nakhon' },
  { id: '1002', code: '1002', nameTh: 'เขตดุสิต', nameEn: 'Dusit' },
  { id: '1003', code: '1003', nameTh: 'เขตหนองจอก', nameEn: 'Nong Chok' },
  { id: '1004', code: '1004', nameTh: 'เขตบางรัก', nameEn: 'Bang Rak' },
  { id: '1005', code: '1005', nameTh: 'เขตบางเขน', nameEn: 'Bang Khen' },
];

export const useDistrictListStore = defineStore('district-list-store', () => {
  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  } as TDistrictCriteria);

  const table = ref({
    data: [] as TDistrictList[],
    totalRecords: 0,
  } as TDataTableResult<TDistrictList>);

  const onGetListData = async (): Promise<void> => {
    const keyword = searchCriteria.value.keyword?.trim().toLowerCase();

    const filtered = keyword
      ? mockDistricts.filter((d) =>
        d.code.toLowerCase().includes(keyword) ||
        d.nameTh.toLowerCase().includes(keyword) ||
        d.nameEn.toLowerCase().includes(keyword)
      )
      : mockDistricts;

    const { pageNumber, pageSize } = searchCriteria.value;
    const start = (pageNumber - 1) * pageSize;

    table.value = {
      data: filtered.slice(start, start + pageSize),
      totalRecords: filtered.length,
    };
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
    const index = mockDistricts.findIndex((d) => d.id === id);

    if (index === -1) {
      ToastHelper.error('ไม่สำเร็จ', 'ลบไม่สำเร็จ');
      return;
    }

    mockDistricts.splice(index, 1);
    ToastHelper.deletedMessageToast();
    await onGetListData();
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

const generateNextCode = (): string => {
  const maxCode = mockDistricts.reduce((max, d) => Math.max(max, Number(d.code) || 0), 1000);
  return String(maxCode + 1);
};

export const useDistrictDetailStore = defineStore('district-detail-store', () => {
  const router = useRouter();

  const body = ref({} as TDistrictDetail);
  const isSubmitting = ref(false);

  const onResetBody = (): void => {
    body.value = {} as TDistrictDetail;
  };

  const onInitCreate = (): void => {
    body.value = { code: generateNextCode(), nameTh: '', nameEn: '' };
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const found = mockDistricts.find((d) => d.id === id);

    if (found) {
      body.value = { ...found };
    }
  };

  const onCreateAsync = async (): Promise<void> => {
    const id = crypto.randomUUID();

    mockDistricts.push({ ...body.value, id });
    ToastHelper.createdMessageToast();
    router.replace({ name: 'districtDetail', params: { id } });
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const index = mockDistricts.findIndex((d) => d.id === id);

    if (index === -1) return;

    mockDistricts[index] = { ...body.value, id };
    ToastHelper.updatedMessageToast();
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

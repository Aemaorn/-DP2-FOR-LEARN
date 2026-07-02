import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TProvinceDetail, TProvinceCriteria, TProvinceList } from '@/models/ST/province';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';

// Demo data only — backend endpoint /api/st/province does not exist yet.
// Once it does, swap the bodies below for ProvinceService calls (see ST010/ST004 stores for the pattern).
const mockProvinces: TProvinceList[] = [
  { id: '101', code: '101', nameTh: 'กรุงเทพมหานคร', nameEn: 'Bangkok' },
  { id: '102', code: '102', nameTh: 'สมุทรปราการ', nameEn: 'Samut Prakan' },
  { id: '103', code: '103', nameTh: 'นนทบุรี', nameEn: 'Nonthaburi' },
  { id: '104', code: '104', nameTh: 'ปทุมธานี', nameEn: 'Pathum Thani' },
  { id: '105', code: '105', nameTh: 'พระนครศรีอยุธยา', nameEn: 'Phra Nakhon Si Ayutthaya' },
];

export const useProvinceListStore = defineStore('province-list-store', () => {
  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  } as TProvinceCriteria);

  const table = ref({
    data: [] as TProvinceList[],
    totalRecords: 0,
  } as TDataTableResult<TProvinceList>);

  const onGetListData = async (): Promise<void> => {
    const keyword = searchCriteria.value.keyword?.trim().toLowerCase();

    const filtered = keyword
      ? mockProvinces.filter((p) =>
        p.code.toLowerCase().includes(keyword) ||
        p.nameTh.toLowerCase().includes(keyword) ||
        p.nameEn.toLowerCase().includes(keyword)
      )
      : mockProvinces;

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
    const index = mockProvinces.findIndex((p) => p.id === id);

    if (index === -1) {
      ToastHelper.error('ไม่สำเร็จ', 'ลบไม่สำเร็จ');
      return;
    }

    mockProvinces.splice(index, 1);
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
  const maxCode = mockProvinces.reduce((max, p) => Math.max(max, Number(p.code) || 0), 100);
  return String(maxCode + 1);
};

export const useProvinceDetailStore = defineStore('province-detail-store', () => {
  const router = useRouter();

  const body = ref({} as TProvinceDetail);
  const isSubmitting = ref(false);

  const onResetBody = (): void => {
    body.value = {} as TProvinceDetail;
  };

  const onInitCreate = (): void => {
    body.value = { code: generateNextCode(), nameTh: '', nameEn: '' };
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const found = mockProvinces.find((p) => p.id === id);

    if (found) {
      body.value = { ...found };
    }
  };

  const onCreateAsync = async (): Promise<void> => {
    const id = crypto.randomUUID();

    mockProvinces.push({ ...body.value, id });
    ToastHelper.createdMessageToast();
    router.replace({ name: 'provinceDetail', params: { id } });
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const index = mockProvinces.findIndex((p) => p.id === id);

    if (index === -1) return;

    mockProvinces[index] = { ...body.value, id };
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

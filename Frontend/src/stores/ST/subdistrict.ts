import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSubdistrictDetail, TSubdistrictCriteria, TSubdistrictList } from '@/models/ST/subdistrict';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';

// Demo data only — backend endpoint /api/st/subdistrict does not exist yet.
// Once it does, swap the bodies below for SubdistrictService calls (see ST010/ST004 stores for the pattern).
const mockSubdistricts: TSubdistrictList[] = [
  { id: '10001', code: '10001', nameTh: 'แขวงพระบรมมหาราชวัง', nameEn: 'Phra Borom Maha Ratchawang' },
  { id: '10002', code: '10002', nameTh: 'แขวงวังบูรพาภิรมย์', nameEn: 'Wang Burapha Phirom' },
  { id: '10003', code: '10003', nameTh: 'แขวงวัดราชบพิธ', nameEn: 'Wat Ratchabophit' },
  { id: '10004', code: '10004', nameTh: 'แขวงสำราญราษฎร์', nameEn: 'Samran Rat' },
  { id: '10005', code: '10005', nameTh: 'แขวงศาลเจ้าพ่อเสือ', nameEn: 'San Chao Pho Suea' },
];

export const useSubdistrictListStore = defineStore('subdistrict-list-store', () => {
  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  } as TSubdistrictCriteria);

  const table = ref({
    data: [] as TSubdistrictList[],
    totalRecords: 0,
  } as TDataTableResult<TSubdistrictList>);

  const onGetListData = async (): Promise<void> => {
    const keyword = searchCriteria.value.keyword?.trim().toLowerCase();

    const filtered = keyword
      ? mockSubdistricts.filter((s) =>
        s.code.toLowerCase().includes(keyword) ||
        s.nameTh.toLowerCase().includes(keyword) ||
        s.nameEn.toLowerCase().includes(keyword)
      )
      : mockSubdistricts;

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
    const index = mockSubdistricts.findIndex((s) => s.id === id);

    if (index === -1) {
      ToastHelper.error('ไม่สำเร็จ', 'ลบไม่สำเร็จ');
      return;
    }

    mockSubdistricts.splice(index, 1);
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
  const maxCode = mockSubdistricts.reduce((max, s) => Math.max(max, Number(s.code) || 0), 10000);
  return String(maxCode + 1);
};

export const useSubdistrictDetailStore = defineStore('subdistrict-detail-store', () => {
  const router = useRouter();

  const body = ref({} as TSubdistrictDetail);
  const isSubmitting = ref(false);

  const onResetBody = (): void => {
    body.value = {} as TSubdistrictDetail;
  };

  const onInitCreate = (): void => {
    body.value = { code: generateNextCode(), nameTh: '', nameEn: '' };
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const found = mockSubdistricts.find((s) => s.id === id);

    if (found) {
      body.value = { ...found };
    }
  };

  const onCreateAsync = async (): Promise<void> => {
    const id = crypto.randomUUID();

    mockSubdistricts.push({ ...body.value, id });
    ToastHelper.createdMessageToast();
    router.replace({ name: 'subdistrictDetail', params: { id } });
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const index = mockSubdistricts.findIndex((s) => s.id === id);

    if (index === -1) return;

    mockSubdistricts[index] = { ...body.value, id };
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

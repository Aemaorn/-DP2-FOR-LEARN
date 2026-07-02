import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt011Detail, TSt011Criteria, TSt011List } from '@/models/ST/st011';
import SharedService from '@/services/Shared/dropdown';
import { loadMockList, saveMockList } from './mockStorage';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';

// There is no create/update/delete endpoint for Raws.RawProvinces yet — only the read-only
// GET /api/dropdown/provinces used for dropdowns elsewhere. So "real" DB provinces are fetched
// for display/code-numbering, while additions made through this page are kept in localStorage
// (see mockStorage.ts) until a real write endpoint exists.
let realProvinces: TSt011List[] = [];

const fetchRealProvinces = async (): Promise<void> => {
  const { data, status } = await SharedService.onGetProvincesAsync();

  if (status === HttpStatusCode.Ok) {
    realProvinces = data.map((o) => ({
      id: String(o.id ?? o.value),
      code: String(o.value),
      nameTh: o.label,
      nameEn: '',
    }));
  }
};

export const mockProvinces: TSt011List[] = loadMockList('provinces-v2', []);
const persist = (): void => saveMockList('provinces-v2', mockProvinces);

// Exported so the st012 (district) store can build its "จังหวัด" dropdown from the same live list.
export const getAllProvinces = async (): Promise<TSt011List[]> => {
  await fetchRealProvinces();
  return [...realProvinces, ...mockProvinces];
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
    const allProvinces = await getAllProvinces();
    const keyword = searchCriteria.value.keyword?.trim().toLowerCase();

    const filtered = keyword
      ? allProvinces.filter((p) =>
        p.code.toLowerCase().includes(keyword) ||
        p.nameTh.toLowerCase().includes(keyword) ||
        p.nameEn.toLowerCase().includes(keyword)
      )
      : allProvinces;

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
      ToastHelper.error('ไม่สำเร็จ', 'ลบไม่สำเร็จ (รายการนี้มาจากฐานข้อมูลจริง ยังลบผ่านหน้านี้ไม่ได้)');
      return;
    }

    mockProvinces.splice(index, 1);
    persist();
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

const generateNextCode = (allProvinces: TSt011List[]): string => {
  const maxCode = allProvinces.reduce((max, p) => Math.max(max, Number(p.code) || 0), 0);
  return String(maxCode + 1);
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
    const allProvinces = await getAllProvinces();
    const found = allProvinces.find((p) => p.id === id);

    if (found) {
      body.value = { ...found };
    }
  };

  const onCreateAsync = async (): Promise<void> => {
    const id = crypto.randomUUID();

    mockProvinces.push({ ...body.value, id });
    persist();
    ToastHelper.createdMessageToast();
    router.replace({ name: 'st011Detail', params: { id } });
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const index = mockProvinces.findIndex((p) => p.id === id);

    if (index === -1) {
      ToastHelper.error('ไม่สำเร็จ', 'แก้ไขไม่ได้ (รายการนี้มาจากฐานข้อมูลจริง ยังแก้ไขผ่านหน้านี้ไม่ได้)');
      return;
    }

    mockProvinces[index] = { ...body.value, id };
    persist();
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

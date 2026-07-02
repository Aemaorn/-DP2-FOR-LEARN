import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt012Detail, TSt012Criteria, TSt012List } from '@/models/ST/st012';
import type { Option } from '@/models/shared/option';
import type { TSt011List } from '@/models/ST/st011';
import { getAllProvinces } from './st011';
import SharedService from '@/services/Shared/dropdown';
import { loadMockList, saveMockList } from './mockStorage';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';

// There is no create/update/delete endpoint for Raws.RawDistrict yet — only the read-only
// GET /api/dropdown/districts used for dropdowns elsewhere. So "real" DB districts are fetched
// for display/code-numbering, while additions made through this page are kept in localStorage
// (see mockStorage.ts) until a real write endpoint exists.
// Note: the unfiltered dropdown endpoint doesn't return which province each district belongs to,
// so real (non-mock) rows show a blank "จังหวัด" column — only districts added on this page track it.
let realDistricts: TSt012List[] = [];

const fetchRealDistricts = async (): Promise<void> => {
  const { data, status } = await SharedService.onGetDistrictsAsync();

  if (status === HttpStatusCode.Ok) {
    realDistricts = data.map((o) => ({
      id: String(o.id ?? o.value),
      code: String(o.value),
      nameTh: o.label,
      nameEn: '',
      provinceId: '',
    }));
  }
};

export const mockDistricts: TSt012List[] = loadMockList('districts-v2', []);
const persist = (): void => saveMockList('districts-v2', mockDistricts);

// Exported so the st013 (subdistrict) store can build its "อำเภอ/เขต" dropdown from the same live list.
export const getAllDistricts = async (): Promise<TSt012List[]> => {
  await fetchRealDistricts();
  return [...realDistricts, ...mockDistricts];
};

const provinceName = (provinceId: string, provinces: TSt011List[]): string | undefined =>
  provinces.find((p) => p.id === provinceId)?.nameTh;

export const useSt012ListStore = defineStore('st-012-list-store', () => {
  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  } as TSt012Criteria);

  const table = ref({
    data: [] as TSt012List[],
    totalRecords: 0,
  } as TDataTableResult<TSt012List>);

  const onGetListData = async (): Promise<void> => {
    const [allDistricts, allProvinces] = await Promise.all([getAllDistricts(), getAllProvinces()]);
    const keyword = searchCriteria.value.keyword?.trim().toLowerCase();

    const filtered = keyword
      ? allDistricts.filter((d) =>
        d.code.toLowerCase().includes(keyword) ||
        d.nameTh.toLowerCase().includes(keyword) ||
        d.nameEn.toLowerCase().includes(keyword)
      )
      : allDistricts;

    const { pageNumber, pageSize } = searchCriteria.value;
    const start = (pageNumber - 1) * pageSize;

    table.value = {
      data: filtered.slice(start, start + pageSize).map((d) => ({ ...d, provinceName: provinceName(d.provinceId, allProvinces) })),
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
      ToastHelper.error('ไม่สำเร็จ', 'ลบไม่สำเร็จ (รายการนี้มาจากฐานข้อมูลจริง ยังลบผ่านหน้านี้ไม่ได้)');
      return;
    }

    mockDistricts.splice(index, 1);
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

const generateNextCode = (allDistricts: TSt012List[]): string => {
  const maxCode = allDistricts.reduce((max, d) => Math.max(max, Number(d.code) || 0), 0);
  return String(maxCode + 1);
};

export const useSt012DetailStore = defineStore('st-012-detail-store', () => {
  const router = useRouter();

  const body = ref({} as TSt012Detail);
  const isSubmitting = ref(false);
  const provinceOptions = ref<Option[]>([]);

  const onResetBody = (): void => {
    body.value = {} as TSt012Detail;
  };

  const onFetchProvinceOptions = async (): Promise<void> => {
    const allProvinces = await getAllProvinces();
    provinceOptions.value = allProvinces.map((p) => ({ value: p.id, label: p.nameTh }));
  };

  const onInitCreate = async (): Promise<void> => {
    const allDistricts = await getAllDistricts();
    body.value = { code: generateNextCode(allDistricts), nameTh: '', nameEn: '', provinceId: '' };
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const allDistricts = await getAllDistricts();
    const found = allDistricts.find((d) => d.id === id);

    if (found) {
      body.value = { ...found };
    }
  };

  const onCreateAsync = async (): Promise<void> => {
    const id = crypto.randomUUID();

    mockDistricts.push({ ...body.value, id });
    persist();
    ToastHelper.createdMessageToast();
    router.replace({ name: 'st012Detail', params: { id } });
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const index = mockDistricts.findIndex((d) => d.id === id);

    if (index === -1) {
      ToastHelper.error('ไม่สำเร็จ', 'แก้ไขไม่ได้ (รายการนี้มาจากฐานข้อมูลจริง ยังแก้ไขผ่านหน้านี้ไม่ได้)');
      return;
    }

    // Province is locked once created — only the district's own name fields can change.
    mockDistricts[index] = {
      ...mockDistricts[index],
      nameTh: body.value.nameTh,
      nameEn: body.value.nameEn,
    };
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
    provinceOptions,
    onResetBody,
    onFetchProvinceOptions,
    onInitCreate,
    onGetByIdAsync,
    onSubmitAsync,
  };
});

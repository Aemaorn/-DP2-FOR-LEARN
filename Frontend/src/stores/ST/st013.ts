import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt013Detail, TSt013Criteria, TSt013List } from '@/models/ST/st013';
import type { Option } from '@/models/shared/option';
import type { TSt011List } from '@/models/ST/st011';
import type { TSt012List } from '@/models/ST/st012';
import { getAllProvinces } from './st011';
import { getAllDistricts, mockDistricts } from './st012';
import SharedService from '@/services/Shared/dropdown';
import { loadMockList, saveMockList } from './mockStorage';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';

// There is no create/update/delete endpoint for Raws.RawSubDistrict yet — only the read-only
// GET /api/dropdown/subDistricts used for dropdowns elsewhere. So "real" DB subdistricts are
// fetched for display/code-numbering, while additions made through this page are kept in
// localStorage (see mockStorage.ts) until a real write endpoint exists.
// Note: the unfiltered dropdown endpoint doesn't return province/district/zip code, so real
// (non-mock) rows show blank "จังหวัด"/"อำเภอ/เขต" columns — only subdistricts added on this
// page track those.
let realSubdistricts: TSt013List[] = [];

const fetchRealSubdistricts = async (): Promise<void> => {
  const { data, status } = await SharedService.onGetSubDistrictsAsync();

  if (status === HttpStatusCode.Ok) {
    realSubdistricts = data.map((o) => ({
      id: String(o.id ?? o.value),
      code: String(o.value),
      nameTh: o.label,
      nameEn: '',
      postalCode: '',
      provinceId: '',
      districtId: '',
    }));
  }
};

const mockSubdistricts: TSt013List[] = loadMockList('subdistricts-v2', []);
const persist = (): void => saveMockList('subdistricts-v2', mockSubdistricts);

const getAllSubdistricts = async (): Promise<TSt013List[]> => {
  await fetchRealSubdistricts();
  return [...realSubdistricts, ...mockSubdistricts];
};

const provinceName = (provinceId: string, provinces: TSt011List[]): string | undefined =>
  provinces.find((p) => p.id === provinceId)?.nameTh;

const districtName = (districtId: string, districts: TSt012List[]): string | undefined =>
  districts.find((d) => d.id === districtId)?.nameTh;

export const useSt013ListStore = defineStore('st-013-list-store', () => {
  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  } as TSt013Criteria);

  const table = ref({
    data: [] as TSt013List[],
    totalRecords: 0,
  } as TDataTableResult<TSt013List>);

  const onGetListData = async (): Promise<void> => {
    const [allSubdistricts, allProvinces, allDistricts] = await Promise.all([
      getAllSubdistricts(),
      getAllProvinces(),
      getAllDistricts(),
    ]);
    const keyword = searchCriteria.value.keyword?.trim().toLowerCase();

    const filtered = keyword
      ? allSubdistricts.filter((s) =>
        s.code.toLowerCase().includes(keyword) ||
        s.nameTh.toLowerCase().includes(keyword) ||
        s.nameEn.toLowerCase().includes(keyword)
      )
      : allSubdistricts;

    const { pageNumber, pageSize } = searchCriteria.value;
    const start = (pageNumber - 1) * pageSize;

    table.value = {
      data: filtered.slice(start, start + pageSize).map((s) => ({
        ...s,
        provinceName: provinceName(s.provinceId, allProvinces),
        districtName: districtName(s.districtId, allDistricts),
      })),
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
      ToastHelper.error('ไม่สำเร็จ', 'ลบไม่สำเร็จ (รายการนี้มาจากฐานข้อมูลจริง ยังลบผ่านหน้านี้ไม่ได้)');
      return;
    }

    mockSubdistricts.splice(index, 1);
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

const generateNextCode = (allSubdistricts: TSt013List[]): string => {
  const maxCode = allSubdistricts.reduce((max, s) => Math.max(max, Number(s.code) || 0), 0);
  return String(maxCode + 1);
};

export const useSt013DetailStore = defineStore('st-013-detail-store', () => {
  const router = useRouter();

  const body = ref({} as TSt013Detail);
  const isSubmitting = ref(false);
  const provinceOptions = ref<Option[]>([]);
  const districtOptions = ref<Option[]>([]);

  const onResetBody = (): void => {
    body.value = {} as TSt013Detail;
  };

  const onFetchProvinceOptions = async (): Promise<void> => {
    const allProvinces = await getAllProvinces();
    provinceOptions.value = allProvinces.map((p) => ({ value: p.id, label: p.nameTh }));
  };

  const onFetchDistrictOptions = async (): Promise<void> => {
    if (!body.value.provinceId) {
      districtOptions.value = [];
      return;
    }

    const { data, status } = await SharedService.onGetDistrictsAsync(body.value.provinceId);
    const real = status === HttpStatusCode.Ok ? data : [];

    const mockFiltered = mockDistricts
      .filter((d) => d.provinceId === body.value.provinceId)
      .map((d) => ({ value: d.id, label: d.nameTh }));

    districtOptions.value = [...real, ...mockFiltered];
  };

  const onChangeProvince = async (): Promise<void> => {
    body.value.districtId = '';
    await onFetchDistrictOptions();
  };

  const onInitCreate = async (): Promise<void> => {
    const allSubdistricts = await getAllSubdistricts();
    body.value = { code: generateNextCode(allSubdistricts), nameTh: '', nameEn: '', postalCode: '', provinceId: '', districtId: '' };
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const allSubdistricts = await getAllSubdistricts();
    const found = allSubdistricts.find((s) => s.id === id);

    if (found) {
      body.value = { ...found };
      await onFetchDistrictOptions();
    }
  };

  const onCreateAsync = async (): Promise<void> => {
    const id = crypto.randomUUID();

    mockSubdistricts.push({ ...body.value, id });
    persist();
    ToastHelper.createdMessageToast();
    router.replace({ name: 'st013Detail', params: { id } });
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const index = mockSubdistricts.findIndex((s) => s.id === id);

    if (index === -1) {
      ToastHelper.error('ไม่สำเร็จ', 'แก้ไขไม่ได้ (รายการนี้มาจากฐานข้อมูลจริง ยังแก้ไขผ่านหน้านี้ไม่ได้)');
      return;
    }

    // Province and district are locked once created — only the subdistrict's own name/postal code fields can change.
    mockSubdistricts[index] = {
      ...mockSubdistricts[index],
      nameTh: body.value.nameTh,
      nameEn: body.value.nameEn,
      postalCode: body.value.postalCode,
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
    districtOptions,
    onResetBody,
    onFetchProvinceOptions,
    onChangeProvince,
    onInitCreate,
    onGetByIdAsync,
    onSubmitAsync,
  };
});

import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt013Detail, TSt013Criteria, TSt013List } from '@/models/ST/st013';
import type { Option } from '@/models/shared/option';
import type { TSt011List } from '@/models/ST/st011';
import type { TSt012List } from '@/models/ST/st012';
import { getAllProvinces } from './st011';
import { getAllDistricts } from './st012';
import ST013Service from '@/services/ST/ST013';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';

const getAllSubdistricts = async (): Promise<TSt013List[]> => {
  const { data, status } = await ST013Service.onGetListAsync({ pageNumber: 1, pageSize: 10000, sort: [] });

  return status === HttpStatusCode.Ok ? data.data : [];
};

const provinceName = (provinceCode: string, provinces: TSt011List[]): string | undefined =>
  provinces.find((p) => p.code === provinceCode)?.nameTh;

const districtName = (districtCode: string, districts: TSt012List[]): string | undefined =>
  districts.find((d) => d.code === districtCode)?.nameTh;

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
    const [{ data, status }, allProvinces, allDistricts] = await Promise.all([
      ST013Service.onGetListAsync(searchCriteria.value),
      getAllProvinces(),
      getAllDistricts(),
    ]);

    if (status === HttpStatusCode.Ok) {
      table.value = {
        data: data.data.map((s) => {
          const district = allDistricts.find((d) => d.code === s.districtCode);

          return {
            ...s,
            provinceName: district && provinceName(district.provinceCode, allProvinces),
            districtName: districtName(s.districtCode, allDistricts),
          };
        }),
        totalRecords: data.totalRecords,
      };
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
    const { status } = await ST013Service.onDeleteAsync(id);

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

// Subdistrict codes follow the Thai standard geographic code (TIS 1099): districtCode (4 digits)
// + a running sequence within that district, zero-padded to 2 digits — e.g. district "7801" has
// subdistricts "780101", "780102", ... So the next code must be scoped to the selected district,
// not the highest code across every subdistrict nationwide.
const generateNextCode = (allSubdistricts: TSt013List[], districtCode: string): string => {
  if (!districtCode) return '';

  const maxSeq = allSubdistricts
    .filter((s) => s.districtCode === districtCode)
    .reduce((max, s) => Math.max(max, Number(s.code.slice(districtCode.length)) || 0), 0);

  return districtCode + String(maxSeq + 1).padStart(2, '0');
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
    provinceOptions.value = allProvinces.map((p) => ({ value: p.code, label: p.nameTh }));
  };

  const onFetchDistrictOptions = async (): Promise<void> => {
    if (!body.value.provinceCode) {
      districtOptions.value = [];
      return;
    }

    const allDistricts = await getAllDistricts();
    districtOptions.value = allDistricts
      .filter((d) => d.provinceCode === body.value.provinceCode)
      .map((d) => ({ value: d.code, label: d.nameTh }));
  };

  const onChangeProvince = async (): Promise<void> => {
    body.value.districtCode = '';
    body.value.code = '';
    await onFetchDistrictOptions();
  };

  const onChangeDistrict = async (): Promise<void> => {
    const allSubdistricts = await getAllSubdistricts();
    body.value.code = generateNextCode(allSubdistricts, body.value.districtCode);
  };

  const onInitCreate = async (): Promise<void> => {
    body.value = { code: '', nameTh: '', nameEn: '', postalCode: '', provinceCode: '', districtCode: '' };
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const [{ data, status }, allDistricts] = await Promise.all([
      ST013Service.onGetByIdAsync(id),
      getAllDistricts(),
    ]);

    if (status !== HttpStatusCode.Ok) return;

    const district = allDistricts.find((d) => d.code === data.districtCode);

    body.value = { ...data, nameEn: data.nameEn ?? '', provinceCode: district?.provinceCode ?? '' };
    await onFetchDistrictOptions();
  };

  const onCreateAsync = async (): Promise<void> => {
    const { status } = await ST013Service.onCreateAsync(body.value);

    if (status === HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();
      router.replace({ name: 'st013' });
    }
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const { status } = await ST013Service.onUpdateAsync(id, body.value);

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
    provinceOptions,
    districtOptions,
    onResetBody,
    onFetchProvinceOptions,
    onChangeProvince,
    onChangeDistrict,
    onInitCreate,
    onGetByIdAsync,
    onSubmitAsync,
  };
});

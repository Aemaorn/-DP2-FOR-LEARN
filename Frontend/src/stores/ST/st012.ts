import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt012Detail, TSt012Criteria, TSt012List } from '@/models/ST/st012';
import type { Option } from '@/models/shared/option';
import type { TSt011List } from '@/models/ST/st011';
import { getAllProvinces } from './st011';
import ST012Service from '@/services/ST/ST012';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';

// Exported so the st013 (subdistrict) store can build its "อำเภอ/เขต" dropdown/name lookups
// from the same live list.
export const getAllDistricts = async (): Promise<TSt012List[]> => {
  const { data, status } = await ST012Service.onGetListAsync({ pageNumber: 1, pageSize: 10000, sort: [] });

  return status === HttpStatusCode.Ok ? data.data : [];
};

const provinceName = (provinceCode: string, provinces: TSt011List[]): string | undefined =>
  provinces.find((p) => p.code === provinceCode)?.nameTh;

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
    const [{ data, status }, allProvinces] = await Promise.all([
      ST012Service.onGetListAsync(searchCriteria.value),
      getAllProvinces(),
    ]);

    if (status === HttpStatusCode.Ok) {
      table.value = {
        data: data.data.map((d) => ({ ...d, provinceName: provinceName(d.provinceCode, allProvinces) })),
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
    const { status } = await ST012Service.onDeleteAsync(id);

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

// District codes are 4-digit real Thai geographic codes — a 2-digit province prefix + a running
// 2-digit sequence within that province (e.g. "1101", "1102" for Samut Prakan). Note this prefix
// is NOT the same as RawProvinces.Code/provinceCode (that's just an internal row id — e.g.
// province internal code "2" is Samut Prakan, whose real geo prefix is "11"). So the prefix has
// to be read off an existing sibling district's own code rather than assumed from provinceCode.
const generateNextCode = (allDistricts: TSt012List[], provinceCode: string): string => {
  if (!provinceCode) return '';

  const siblings = allDistricts.filter((d) => d.provinceCode === provinceCode);
  if (siblings.length === 0) return '';

  const prefix = siblings[0].code.slice(0, -2);
  const maxSeq = siblings.reduce((max, d) => Math.max(max, Number(d.code.slice(-2)) || 0), 0);

  return prefix + String(maxSeq + 1).padStart(2, '0');
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
    provinceOptions.value = allProvinces.map((p) => ({ value: p.code, label: p.nameTh }));
  };

  const onInitCreate = async (): Promise<void> => {
    body.value = { code: '', nameTh: '', nameEn: '', provinceCode: '' };
  };

  const onChangeProvince = async (): Promise<void> => {
    const allDistricts = await getAllDistricts();
    body.value.code = generateNextCode(allDistricts, body.value.provinceCode);
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await ST012Service.onGetByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = { ...data, nameEn: data.nameEn ?? '', provinceCode: data.provinceCode ?? '' };
    }
  };

  const onCreateAsync = async (): Promise<void> => {
    const { status } = await ST012Service.onCreateAsync(body.value);

    if (status === HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();
      router.replace({ name: 'st012' });
    }
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const { status } = await ST012Service.onUpdateAsync(id, body.value);

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
    onResetBody,
    onFetchProvinceOptions,
    onChangeProvince,
    onInitCreate,
    onGetByIdAsync,
    onSubmitAsync,
  };
});

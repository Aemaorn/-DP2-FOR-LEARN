import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type {
  TSt003Criteria,
  TSt003Detail,
  TSt003AddressDetail,
  CustomFileSt003,
  TSt003List,
} from '@/models/ST/st003';
import type { Option } from '@/models/shared/option';
import ST003Service from '@/services/ST/ST003';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import { errorMessageHandler } from '@/helpers/error';

export const useSt003ListStore = defineStore('st-003-list-store', () => {
  const entrepreneurTypeOptions = ref<Array<Option>>([]);
  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  } as TSt003Criteria);

  const table = ref({
    data: [] as TSt003List[],
    totalRecords: 0,
  } as TDataTableResult<TSt003List>);


  const onGetEntrepreneurTypeOptionsAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.TraderType);

    if (status === HttpStatusCode.Ok) {
      entrepreneurTypeOptions.value = data;
    }
  };

  const onGetListData = async () => {
    const { data, status } = await ST003Service.onGetListAsync(searchCriteria.value);

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
    const { status } = await ST003Service.onDeleteAsync(id);

    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();
      await onGetListData();
    }
  };

  return {
    entrepreneurTypeOptions,
    searchCriteria,
    table,
    onChangePageSize,
    onGetListData,
    onResetCriteria,
    onDeleteByIdAsync,
    onGetEntrepreneurTypeOptionsAsync,
  };
});

export const useSt003DetailStore = defineStore('st-003-detail-store', () => {
  const router = useRouter();
  const body = ref({
    nationality: 'TH', // TODO: Change this to a default value or fetch from an API
    type: 'Individual', // TODO: Change this to a default value or fetch from an API
    entrepreneurType: '', // TODO: Change this to a default value or fetch from an API
    taxpayerIdentificationNo: '',
    establishmentName: '',
    placeName: '',
    address: {
      houseNumber: '',
      roomNumber: '',
      floor: '',
      villageName: '',
      moo: '',
      allay: '',
      road: '',
      rawProvinceCode: '',
      rawDistrictCode: '',
      rawSubDistrictCode: '',
      postalCode: '',
    } as TSt003AddressDetail,
    tel: '',
    fax: '',
    sapVendorNumber: '',
    sapBranchNumber: '',
    email: '',
    attachments: [] as CustomFileSt003[],
  } as TSt003Detail);

  const onResetBody = (): void => {
    body.value = {
      nationality: 'TH', // TODO: Change this to a default value or fetch from an API
      type: 'Individual', // TODO: Change this to a default value or fetch from an API
      entrepreneurType: '', // TODO: Change this to a default value or fetch from an API
      taxpayerIdentificationNo: '',
      establishmentName: '',
      placeName: '',
      address: {
        houseNumber: '',
        roomNumber: '',
        floor: '',
        villageName: '',
        moo: '',
        allay: '',
        road: '',
        rawProvinceCode: '',
        rawDistrictCode: '',
        rawSubDistrictCode: '',
        postalCode: '',
      } as TSt003AddressDetail,
      tel: '',
      fax: '',
      sapVendorNumber: '',
      sapBranchNumber: '',
      email: '',
      attachments: [] as CustomFileSt003[],
    } as TSt003Detail;
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await ST003Service.onGetByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = {
        ...data,
      } as TSt003Detail;
    }
  };

  const onSubmitAsync = async (id?: string): Promise<void> => {
    if (id) {
      await onUpdateAsync(id);
      return;
    }

    await onCreateAsync();
  };

  const onCreateAsync = async (): Promise<void> => {
    const { data, status } = await ST003Service.onCreateAsync(body.value);

    if (status > 299) {
      ToastHelper.errorDescription(errorMessageHandler(data));

      return;
    }

    router.replace({ name: 'st003Detail', params: { id: data.id } });
    await onGetByIdAsync(data.id);
    ToastHelper.createdMessageToast();
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const { status } = await ST003Service.onUpdateAsync(id, body.value);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await onGetByIdAsync(id);
    }
  };

  const onUploadFileAsync = async (id: string, file: CustomFileSt003[]): Promise<void> => {
    const onlyFile = file.filter((f) => f.file);
    const { status } = await ST003Service.onUploadFileAsync(id, onlyFile);

    if (status !== HttpStatusCode.Created && status !== HttpStatusCode.Ok) {
      ToastHelper.error('ไม่สำเร็จ', 'อัปโหลดไฟล์ไม่สำเร็จ');

      return;
    }

    ToastHelper.success('สำเร็จ', 'อัปโหลดไฟล์สำเร็จ');
    onGetByIdAsync(id);
  };

  const onUpdateFileSequenceAsync = async (
    id: string,
    attachment: { fileId: string; sequence: number; isPrivate: boolean }[]
  ): Promise<void> => {
    const { status } = await ST003Service.onUpdateFileSequenceAsync(id, attachment);

    if (status !== HttpStatusCode.NoContent && status !== HttpStatusCode.Ok) {
      ToastHelper.error('ไม่สำเร็จ', 'อัพเดทลำดับไฟล์ไม่สำเร็จ');

      return;
    }

    ToastHelper.success('สำเร็จ', 'อัพเดทลำดับไฟล์สำเร็จ');
    onGetByIdAsync(id);
  };

  const onUpdatIsPrivateAsync = async (
    id: string,
    attachment: { fileId: string; sequence: number; isPrivate: boolean }[]
  ): Promise<void> => {
    const { status } = await ST003Service.onUpdateFileSequenceAsync(id, attachment);

    if (status !== HttpStatusCode.NoContent && status !== HttpStatusCode.Ok) {
      ToastHelper.error('ไม่สำเร็จ', 'อัพเดทไฟล์ไม่สำเร็จ');

      return;
    }

    ToastHelper.success('สำเร็จ', 'อัพเดทสิทธิ์ไฟล์สำเร็จ');
    onGetByIdAsync(id);
  };

  const onDeleteFileAsync = async (id: string, attachmentId: string, fileId: string): Promise<void> => {
    const { status } = await ST003Service.onDeleteFileAsync(id, attachmentId, fileId);

    if (status !== HttpStatusCode.NoContent && status !== HttpStatusCode.Ok) {
      ToastHelper.error('ไม่สำเร็จ', 'ลบไฟล์ไม่สำเร็จ');

      return;
    }

    ToastHelper.success('สำเร็จ', 'ลบไฟล์สำเร็จ');
    onGetByIdAsync(id);
  };

  return {
    body,
    onResetBody,
    onGetByIdAsync,
    onSubmitAsync,
    onCreateAsync,
    onUpdateAsync,
    onUploadFileAsync,
    onUpdateFileSequenceAsync,
    onUpdatIsPrivateAsync,
    onDeleteFileAsync,
  };
});

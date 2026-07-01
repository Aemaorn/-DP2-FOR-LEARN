import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type {
  TSuDelegateCreateUserRequestType,
  TSuDelegateeCreateUserRequestType,
  TGetListRawEmpPosition,
  TSt001Criteria,
  TSt001Detail,
  TSt001List,
  TUser,
} from '@/models/ST/st001';
import type { Option } from '@/models/shared/option';
import ST001Service from '@/services/ST/ST001';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { computed, ref, type Ref } from 'vue';
import { useRouter } from 'vue-router';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode, OrganizationLevelEnum } from '@/enums/shared';
import { errorMessageHandler } from '@/helpers/error';
import { ConvertStartToEndDate } from '@/helpers/dateTime';

export const useSt001ListStore = defineStore('st-001-list-store', () => {
  const entrepreneurTypeOptions = ref<Array<Option>>([]);
  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  } as TSt001Criteria);

  const table = ref({
    data: [] as TSt001List[],
    totalRecords: 0,
  } as TDataTableResult<TSt001List>);

  const fetchDropdown = async (level: OrganizationLevelEnum, target: Ref<Option[]>) => {
    const { data, status } = await SharedService.onGetBusinessUnitAsync(level);
    if (status === HttpStatusCode.Ok) {
      target.value = data;
    }
  };

  const departmentDropdown = ref<Option[]>([] as Option[]);
  const groupDropdown = ref<Option[]>([] as Option[]);
  const lineDropdown = ref<Option[]>([] as Option[]);

  const businessUnitDropdown = computed<Option[]>(() => [
    ...groupDropdown.value,
    ...lineDropdown.value,
    ...departmentDropdown.value,
  ]);

  const getDropDownDepartment = async () => await fetchDropdown(OrganizationLevelEnum.Department, departmentDropdown);
  const getDropDownGroup = async () => await fetchDropdown(OrganizationLevelEnum.Group, groupDropdown);
  const getDropDownLine = async () => await fetchDropdown(OrganizationLevelEnum.Line, lineDropdown);

  const onGetEntrepreneurTypeOptionsAsync = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.TraderType);

    if (status === HttpStatusCode.Ok) {
      entrepreneurTypeOptions.value = data;
    }
  };

  const onGetListData = async () => {
    const { data, status } = await ST001Service.onGetListAsync(searchCriteria.value);

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
    const { status } = await ST001Service.onDeleteAsync(id);

    if (status !== HttpStatusCode.NoContent && status !== HttpStatusCode.Ok) {
      ToastHelper.error('ไม่สำเร็จ', 'ลบไม่สำเร็จ');

      return;
    }

    ToastHelper.deletedMessageToast();
    onGetListData();
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
    departmentDropdown,
    groupDropdown,
    lineDropdown,
    businessUnitDropdown,
    getDropDownDepartment,
    getDropDownGroup,
    getDropDownLine,
  };
});

export const useSt001DetailStore = defineStore('st-001-detail-store', () => {
  const router = useRouter();
  const user = ref({} as TUser);
  const delegateeListSelection = ref<TGetListRawEmpPosition[]>([] as TGetListRawEmpPosition[]);
  const body = ref({
    delegator: {
    } as TSuDelegateCreateUserRequestType,
    delegatees: [] as TSuDelegateeCreateUserRequestType[],
  } as TSt001Detail);

  const onResetBody = (): void => {
    body.value = {
      delegator: {
      } as TSuDelegateCreateUserRequestType,
      delegatees: [] as TSuDelegateeCreateUserRequestType[],
    } as TSt001Detail;
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await ST001Service.onGetByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = {
        ...data,
        delegatees: data.delegatees.map((item: TSuDelegateeCreateUserRequestType) => (
          {
            ...item,
            levelOneBusinessUnitId: item.businessUnitId,
          } as TSuDelegateeCreateUserRequestType)),
      } as TSt001Detail;

      onGetBuPositionByEmpCodeAsync(data.delegator.employeeCode);
    }
  };

  const onGetBuPositionByEmpCodeAsync = async (employeeCode: string): Promise<void> => {
    const { data, status } = await ST001Service.onGetBuPositionByEmpCodeAsync(employeeCode);

    if (status === HttpStatusCode.Ok) {
      delegateeListSelection.value = data;

      return;
    }

    ToastHelper.error('ไม่สำเร็จ', 'ไม่พบข้อมูลตำแหน่งงาน');
  };

  const onGetSuUserByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await ST001Service.onGetUserByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      user.value = {
        ...data,
      } as TUser;

      return;
    }

    ToastHelper.error('ไม่สำเร็จ', 'ไม่พบข้อมูลผู้ใช้งาน');
  };

  const onSubmitAsync = async (id?: string): Promise<void> => {
    if (id) {
      await onUpdateAsync(id);

      return;
    }

    await onCreateAsync();
  };

  const onCreateAsync = async (): Promise<void> => {
    const payload = mapPayload(body.value);

    const { data, status } = await ST001Service.onCreateAsync(payload);

    if (status !== HttpStatusCode.Created && status !== HttpStatusCode.Ok) {
      ToastHelper.error('ไม่สำเร็จ', 'บันทึกข้อมูลไม่สำเร็จ');

      return;
    }

    router.replace({ name: 'st001Detail', params: { id: data.id } });
    await onGetByIdAsync(data.id);
    ToastHelper.createdMessageToast();
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const payload = mapPayload(body.value);

    const { data, status } = await ST001Service.onUpdateAsync(id, payload);

    if (status !== HttpStatusCode.Accepted && status !== HttpStatusCode.Ok) {
      ToastHelper.errorDescription(errorMessageHandler(data));

      await onGetByIdAsync(id);
      return;
    }

    ToastHelper.updatedMessageToast();
  };

  const mapPayload = (data: TSt001Detail) => {
    const delegator = data.delegator;

    const [start, end] = ConvertStartToEndDate(data.delegator.delegationStartDate, data.delegator.delegationEndDate);

    return {
      ...data,
      delegator: {
        ...delegator,
        delegationStartDate: start,
        delegationEndDate: end,
      } as TSuDelegateCreateUserRequestType,
    } as TSt001Detail;
  }

  return {
    body,
    user,
    delegateeListSelection,
    onResetBody,
    onGetByIdAsync,
    onSubmitAsync,
    onCreateAsync,
    onUpdateAsync,
    onGetSuUserByIdAsync,
    onGetBuPositionByEmpCodeAsync,
  };
});

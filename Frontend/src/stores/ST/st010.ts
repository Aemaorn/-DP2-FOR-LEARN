import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type {
  TSt010Criteria,
  TSt010Detail,
  TSt010List,
  TSt010SuSecretaryOwner,
  TSt010Secretary,
} from '@/models/ST/st010';
import type { Option } from '@/models/shared/option';
import type { Attachments } from '@/models/shared/uploadFile';
import ST010Service from '@/services/ST/ST010';
import SharedService from '@/services/Shared/dropdown';
import { OrganizationLevelEnum } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { computed, ref, type Ref } from 'vue';
import { useRouter } from 'vue-router';
import { errorMessageHandler } from '@/helpers/error';

export const useSt010ListStore = defineStore('st-010-list-store', () => {
  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  } as TSt010Criteria);

  const table = ref({
    data: [] as TSt010List[],
    totalRecords: 0,
  } as TDataTableResult<TSt010List>);

  const fetchDropdown = async (level: OrganizationLevelEnum, target: Ref<Option[]>) => {
    const { data, status } = await SharedService.onGetBusinessUnitAsync(level);
    if (status === HttpStatusCode.Ok) {
      target.value = data;
    }
  };

  const departmentDropdown = ref<Option[]>([]);
  const groupDropdown = ref<Option[]>([]);
  const lineDropdown = ref<Option[]>([]);

  const businessUnitDropdown = computed<Option[]>(() => [
    ...groupDropdown.value,
    ...lineDropdown.value,
    ...departmentDropdown.value,
  ]);

  const getDropDownDepartment = async () => fetchDropdown(OrganizationLevelEnum.Department, departmentDropdown);
  const getDropDownGroup = async () => fetchDropdown(OrganizationLevelEnum.Group, groupDropdown);
  const getDropDownLine = async () => fetchDropdown(OrganizationLevelEnum.Line, lineDropdown);

  const onGetListData = async (): Promise<void> => {
    const { data, status } = await ST010Service.onGetListAsync(searchCriteria.value);

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
    const { status } = await ST010Service.onDeleteAsync(id);

    if (status !== HttpStatusCode.NoContent && status !== HttpStatusCode.Ok) {
      ToastHelper.error('ไม่สำเร็จ', 'ลบไม่สำเร็จ');
      return;
    }

    ToastHelper.deletedMessageToast();
    onGetListData();
  };

  return {
    searchCriteria,
    table,
    businessUnitDropdown,
    onGetListData,
    onChangePageSize,
    onResetCriteria,
    onDeleteByIdAsync,
    getDropDownDepartment,
    getDropDownGroup,
    getDropDownLine,
  };
});

export const useSt010DetailStore = defineStore('st-010-detail-store', () => {
  const router = useRouter();

  const defaultOwner = (): TSt010SuSecretaryOwner => ({});

  const body = ref({
    suSecretaryOwner: defaultOwner(),
    secretaries: [] as TSt010Secretary[],
    attachments: [] as Attachments[],
  } as TSt010Detail);

  const departmentDropdown = ref<Option[]>([]);
  const groupDropdown = ref<Option[]>([]);
  const lineDropdown = ref<Option[]>([]);

  const businessUnitDropdown = computed<Option[]>(() => [
    ...groupDropdown.value,
    ...lineDropdown.value,
    ...departmentDropdown.value,
  ]);

  const fetchDropdown = async (level: OrganizationLevelEnum, target: Ref<Option[]>): Promise<void> => {
    const { data, status } = await SharedService.onGetBusinessUnitAsync(level);
    if (status === HttpStatusCode.Ok) target.value = data;
  };

  const getDropDownGroup = async (): Promise<void> => fetchDropdown(OrganizationLevelEnum.Group, groupDropdown);
  const getDropDownLine = async (): Promise<void> => fetchDropdown(OrganizationLevelEnum.Line, lineDropdown);
  const getDropDownDepartment = async (): Promise<void> => fetchDropdown(OrganizationLevelEnum.Department, departmentDropdown);

  const onFetchBusinessUnitDropdowns = async (): Promise<void> => {
    await Promise.all([getDropDownGroup(), getDropDownLine(), getDropDownDepartment()]);
  };

  type PositionOption = { businessUnitId: string; positionId: string; label: string };
  const allPositions = ref<PositionOption[]>([]);

  const positionDropdown = computed<Option[]>((): Option[] => {
    const buId = body.value.suSecretaryOwner.businessUnitId;
    if (!buId) return [];
    return allPositions.value
      .filter((p): boolean => p.businessUnitId === buId)
      .map((p): Option => ({ value: p.positionId, label: p.label }));
  });

  const onFetchAllPositions = async (): Promise<void> => {
    const { data, status } = await ST010Service.onGetAllPositionsByBusinessUnitAsync();
    if (status === HttpStatusCode.Ok) allPositions.value = data;
  };

  const onResetBody = (): void => {
    body.value = {
      suSecretaryOwner: defaultOwner(),
      secretaries: [] as TSt010Secretary[],
      attachments: [] as Attachments[],
    };
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await ST010Service.onGetByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = {
        ...data,
        secretaries: data.secretaries.map((s) => ({
          ...s,
          effectiveStartDate: s.effectiveStartDate ? new Date(s.effectiveStartDate as unknown as string) : undefined,
          effectiveEndDate: s.effectiveEndDate ? new Date(s.effectiveEndDate as unknown as string) : undefined,
        })),
        attachments: data.attachments ?? [],
      };
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
    const { data, status } = await ST010Service.onCreateAsync(body.value);

    if (status !== HttpStatusCode.Created && status !== HttpStatusCode.Ok) {
      if (status !== HttpStatusCode.Conflict) {
        ToastHelper.errorDescription(errorMessageHandler(data));
      }
      return;
    }

    router.replace({ name: 'st010Detail', params: { id: data.id } });
    await onGetByIdAsync(data.id);
    ToastHelper.createdMessageToast();
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const { data, status } = await ST010Service.onUpdateAsync(id, body.value);

    if (status !== HttpStatusCode.Accepted && status !== HttpStatusCode.Ok) {
      ToastHelper.errorDescription(errorMessageHandler(data));
      await onGetByIdAsync(id);
      return;
    }

    ToastHelper.updatedMessageToast();
  };

  const onDeleteSecretaryAsync = async (id: string, secretaryId: string): Promise<void> => {
    const { status } = await ST010Service.onDeleteSecretaryAsync(id, secretaryId);

    if (status !== HttpStatusCode.NoContent && status !== HttpStatusCode.Ok) {
      ToastHelper.error('ไม่สำเร็จ', 'ลบเลขาไม่สำเร็จ');
      return;
    }

    body.value.secretaries = body.value.secretaries.filter((s) => s.id !== secretaryId);
    body.value.secretaries.forEach((s, i) => {
      s.sequence = i + 1;
    });

    ToastHelper.deletedMessageToast();
  };

  const onUpsertAttachments = async (): Promise<void> => {
    if (!body.value.suSecretaryOwner.id) return;
    const id = body.value.suSecretaryOwner.id;

    const { status } = await ST010Service.onUpsertAttachmentsAsync(id, body.value.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await onGetByIdAsync(id);
    }
  };

  return {
    body,
    businessUnitDropdown,
    positionDropdown,
    onFetchBusinessUnitDropdowns,
    onFetchAllPositions,
    onResetBody,
    onGetByIdAsync,
    onSubmitAsync,
    onCreateAsync,
    onUpdateAsync,
    onDeleteSecretaryAsync,
    onUpsertAttachments,
  };
});

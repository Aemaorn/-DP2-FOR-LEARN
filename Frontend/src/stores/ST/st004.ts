import { ConfirmDialogType } from '@/enums/dialog';
import { PermissionStrType, PermissionType } from '@/enums/role';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { Permission, TSt004Criteria, TSt004Detail, TSt004List } from '@/models/ST/st004';
import ST004Service from '@/services/ST/ST004';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useMenuStore } from '../menu';
import { errorMessageHandler } from '@/helpers/error';

export const useSt004ListStore = defineStore('st-004-list-store', () => {
  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  } as TSt004Criteria);

  const table = ref({
    data: [] as TSt004List[],
    totalRecords: 0,
  } as TDataTableResult<TSt004List>);

  const onGetPermissionListData = async (): Promise<void> => {
    try {
      const { data, status } = await ST004Service.onGetListAsync(searchCriteria.value);

      if (status === HttpStatusCode.Ok) {
        table.value = data;
      }
    } catch {
      // Error handling is done in axios interceptor (redirects to forbidden page)
      // Just prevent further execution here
      return;
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

  const onDeleteRoleByCode = async (code: string): Promise<void> => {
    if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) {
      return;
    }

    const { data, status } = await ST004Service.onDeleteAsync(code);

    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();
    }

    if ([HttpStatusCode.Conflict].includes(status)) {
      ToastHelper.errorDescription(errorMessageHandler(data));
    }

    await onGetPermissionListData();
  };

  return {
    searchCriteria,
    table,
    onChangePageSize,
    onGetPermissionListData,
    onResetCriteria,
    onDeleteRoleByCode,
  };
});

export const useSt004DetailStore = defineStore('st-004-detail-store', () => {
  const menuStore = useMenuStore();

  const router = useRouter();
  const body = ref({
    isActive: true,
    programPermissions: [] as Permission[],
  } as TSt004Detail);

  const onGetByCode = async (code?: string): Promise<void> => {
    if (code) {
      await onGetRoleByCodeAsync(code);
      return;
    }

    await onGetProgram();
  };

  const onGetProgram = async (): Promise<void> => {
    const { data, status } = await ST004Service.onGetProgramPermissionAsync();

    if (status === HttpStatusCode.Ok) {
      body.value = {
        ...body.value,
        programPermissions: data as Permission[],
      } as TSt004Detail;
    }
  };

  const onGetRoleByCodeAsync = async (code: string): Promise<void> => {
    const { data, status } = await ST004Service.onGetByCodeAsync(code);

    if (status === HttpStatusCode.Ok) {
      body.value = {
        ...data,
        programPermissions: mappingResponsePermission(data.programPermissions),
      };
    }
  };

  const mappingPermission = (): void => {
    const setPermission = (view: boolean, manage: boolean): PermissionType => {
      if (manage) {
        return PermissionType.isManage;
      }

      if (view) {
        return PermissionType.isView;
      }

      return PermissionType.none;
    };

    body.value.programPermissions = body.value.programPermissions.map(
      (item): Permission =>
        ({
          ...item,
          permission: setPermission(item.isView, item.isManage),
        }) as Permission
    );
  };

  const mappingResponsePermission = (permission: Permission[]): Permission[] => {
    return permission.map(
      (item: Permission): Permission => {
        const tempPermission = item.permission as unknown as PermissionStrType;

        const isView = tempPermission === PermissionStrType.Manage ||
          tempPermission === PermissionStrType.View;

        return {
          ...item,
          isView,
          isManage: tempPermission === PermissionStrType.Manage,
        } as Permission;
      }
    );
  };

  const onSubmitAsync = async (code?: string): Promise<void> => {
    if (body.value.programPermissions.every(e => !(e.isView || e.isManage))) return ToastHelper.errorDescription('กรุณากำหนดสิทธิ์อย่างน้อยหนึ่งรายการ');
    mappingPermission();

    if (code) {
      await onUpdateAsync(code);

      return;
    }

    await onCreateAsync();


  };

  const onCreateAsync = async (): Promise<void> => {
    const { data, status } = await ST004Service.onCreateAsync(body.value);

    if (status === HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();
      await menuStore.getMenuAsyncAsync();
      router.replace({ name: 'st004Detail', params: { code: data } });
    }

    if (status === HttpStatusCode.Conflict) {
      ToastHelper.errorDescription(errorMessageHandler(data));
    }
  };

  const onUpdateAsync = async (code: string): Promise<void> => {
    const { data, status } = await ST004Service.onUpdateAsync(code, body.value);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await menuStore.getMenuAsyncAsync();
    }

    if (status === HttpStatusCode.Conflict) {
      ToastHelper.errorDescription(errorMessageHandler(data));
    }
  };

  const onClearBody = (): void => {
    body.value = {
      isActive: true,
      programPermissions: [] as Permission[],
    } as TSt004Detail;
  };

  return { body, onGetByCode, onSubmitAsync, onClearBody };
});

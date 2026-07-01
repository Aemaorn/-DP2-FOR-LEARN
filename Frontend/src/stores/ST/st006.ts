import type { TDataTableResult } from '@/models/shared/paginated';
import type {
  St006Criteria,
  St006Detail,
  St006GroupDDL,
  St006ListResponse,
  St006Option,
  St006Parameter,
  St006SubGroupDDL,
} from '@/models/ST/st006';
import type { Option } from '@/models/shared/option';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import ST006Service from '@/services/ST/ST006';
import ToastHelper from '@/helpers/toast';
import router from '@/router';

export const useSt006ListStore = defineStore('st006ListStore', () => {
  const criteria = ref<St006Criteria>({
    pageNumber: 1,
    pageSize: 10,
  } as St006Criteria);
  const groupDropdown = ref<St006Option[]>([]);
  const subGroupDropdown = ref<St006Option[]>([]);
  const parentDropdown = ref<Option[]>([]);
  const dataList = ref<TDataTableResult<St006ListResponse>>(
    {} as TDataTableResult<St006ListResponse>
  );

  const onGetGroupDropdownAsync = async () => {
    const { data, status } = await ST006Service.getGroupDropdownAsync();

    if (status === HttpStatusCode.Ok) {
      groupDropdown.value = data.map((d: St006GroupDDL) => ({
        id: d.id,
        label: d.label,
        value: d.group,
      }));
    }
  };

  const onGetSubGroupDropdownAsync = async (id: string) => {
    const { data, status } = await ST006Service.getSubGroupDropdownAsync(id);

    if (status === HttpStatusCode.Ok) {
      subGroupDropdown.value = data.map((d: St006SubGroupDDL) => ({
        id: d.id,
        label: d.label,
        value: d.code,
      }));
    }
  };

  const onGetParentDropdownAsync = async (group: string, excludeId?: string) => {
    const { data, status } = await ST006Service.getListAsync({
      group,
      pageNumber: 1,
      pageSize: 1000,
    } as St006Criteria);

    if (status === HttpStatusCode.Ok) {
      parentDropdown.value = data.data
        .filter((d: St006ListResponse) => d.id !== excludeId)
        .map((d: St006ListResponse) => ({
          label: d.parameter,
          value: d.id,
        }));
    }
  };

  const onGetListAsync = async () => {
    const { data, status } = await ST006Service.getListAsync(criteria.value);

    if (status === HttpStatusCode.Ok) {
      dataList.value = data;
    }
  };

  const onDeleteAsync = async (id: string) => {
    if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;

    const { status } = await ST006Service.deleteAsync(id);

    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();

      return await onGetListAsync();
    }
  };

  const onChangePageSize = async (pageNumber: number, pageSize: number) => {
    criteria.value = {
      ...criteria.value,
      pageNumber: pageNumber,
      pageSize: pageSize,
    };

    await onGetListAsync();
  };

  const onClearCriteriaSearch = async () => {
    criteria.value = {
      pageNumber: 1,
      pageSize: 10,
    } as St006Criteria;

    await onGetListAsync();
  };

  return {
    criteria,
    dataList,
    groupDropdown,
    subGroupDropdown,
    parentDropdown,
    onGetGroupDropdownAsync,
    onGetSubGroupDropdownAsync,
    onGetParentDropdownAsync,
    onGetListAsync,
    onDeleteAsync,
    onChangePageSize,
    onClearCriteriaSearch,
  };
});

export const useSt006DetailStore = defineStore('st006DetailStore', () => {
  const body = ref<St006Detail>({
    isActive: true,
    parentId: null,
    parameters: [
      {
        key: '-',
        value: { sequence: 1, value: '-' },
      },
    ],
  } as St006Detail);

  const addParameterValue = () => {
    if (body.value.parameters && body.value.parameters.length > 0) {
      return body.value.parameters.push({
        key: '-',
        value: { sequence: body.value.parameters.length + 1, value: '-' },
      } as St006Parameter);
    }
  };

  const removeParameterValue = (index: number) => {
    body.value.parameters.splice(index, 1);
  };

  const getDetailAsync = async (id: string): Promise<St006Detail | void> => {
    const { data, status } = await ST006Service.getDetailAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = {
        code: data.code,
        group: data.group,
        isActive: data.isActive,
        name: data.name,
        sequence: data.sequence,
        subGroup: data.subGroup,
        parentId: data.parentId ?? null,
        parameters: data.values,
      };

      return body.value;
    }
  };

  const createAsync = async (): Promise<void> => {
    const { data, status } = await ST006Service.createAsync(body.value);

    if (status === HttpStatusCode.Ok) {
      router.replace(`/st/st006/detail/${data}`);

      await getDetailAsync(data);

      ToastHelper.createdMessageToast();
    }
  };

  const updateAsync = async (id: string): Promise<void> => {
    const { status } = await ST006Service.updateAsync(id, body.value);

    if (status === HttpStatusCode.Ok) {
      await getDetailAsync(id);
      return ToastHelper.updatedMessageToast();
    }
  };

  const setDefaultsAsync = async (groupId: string, parentId?: string | null): Promise<void> => {
    const { data, status } = await ST006Service.getDefaultAsync(groupId, parentId);

    if (status === HttpStatusCode.Ok) {
      body.value.sequence = data.nextSequence;

      if (data.nextCode) {
        body.value.code = data.nextCode;
      }
    }
  };

  const clearData = () => {
    body.value = {
      isActive: true,
      parentId: null,
      parameters: [
        {
          key: '-',
          value: { sequence: 1, value: '-' },
        },
      ],
    } as St006Detail;
  };

  return {
    body,
    addParameterValue,
    removeParameterValue,
    getDetailAsync,
    createAsync,
    updateAsync,
    clearData,
    setDefaultsAsync,
  };
});

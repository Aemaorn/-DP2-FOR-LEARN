import ToastHelper from "@/helpers/toast";
import type { TDataTableResult } from "@/models/shared/paginated";
import type { ListSuSection, SuSection, SuSectionCriteria } from "@/models/ST/st099";
import ST099Service from "@/services/ST/ST099";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref } from "vue";
import { useRouter } from "vue-router";

export const useST099ListStore = defineStore('st099-list-store', () => {
  const initCriteria: SuSectionCriteria = {
    pageNumber: 1,
    pageSize: 10,
  };

  const initTable: TDataTableResult<ListSuSection> = {
    data: [] as Array<ListSuSection>,
    totalRecords: 0,
  };

  const searchCriteria = ref<SuSectionCriteria>(structuredClone(initCriteria));
  const table = ref<TDataTableResult<ListSuSection>>(structuredClone(initTable));

  const onGetListAsync = async () => {
    const { data, status } = await ST099Service.onGetListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data;
    }
  };

  const onResetCriteria = () => {
    searchCriteria.value = structuredClone(initCriteria);
  };


  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  const onDeleteByIdAsync = async (id: string) => {
    const { status } = await ST099Service.onDeleteByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.deletedMessageToast();
      onGetListAsync();
    }
  };

  return {
    searchCriteria,
    table,

    onGetListAsync,
    onResetCriteria,
    onChangePageSize,
    onDeleteByIdAsync,
  };
});

export const useST099Store = defineStore('st099-store', () => {
  const router = useRouter();
  const initBody: SuSection = {
    approvers: [],
    id: "",
    newId: "",
    refBankOrder: "",
    maximumBudget: 0
  };

  const body = ref<SuSection>(initBody);

  const onGeByIdAsync = async (id: string) => {
    const { data, status } = await ST099Service.onGetByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;
    }
  };

  const onCreate = async () => {
    const { data, status } = await ST099Service.onCreateAsync(body.value);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.createdMessageToast();

      router.push({ name: 'st099Detail', params: { id: data } });
      return;
    }

    ToastHelper.error('ไม่สำเร็จ', 'อัพเดทไม่สำเร็จ');
  };

  const onUpdate = async (id: string) => {
    const { status } = await ST099Service.onUpdateAsync(id, body.value);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      onGeByIdAsync(id);

      return;
    }

    ToastHelper.error('ไม่สำเร็จ', 'อัพเดทไม่สำเร็จ');
  };

  const onResetBody = () => body.value = initBody;

  return {
    body,
    onResetBody,
    onGeByIdAsync,
    onCreate,
    onUpdate
  };
});
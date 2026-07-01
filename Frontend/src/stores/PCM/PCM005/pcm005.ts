import type { TPreProcurement, TPreProcurementCriteria, TPreProcurementDetail, TPreProcurementGroupStepCount } from "@/models/PP/ppModel";
import type { Option, OptionBadge } from "@/models/shared/option";
import type { Attachments } from "@/models/shared/uploadFile";
import { computed, ref, type Ref } from "vue";
import { defineStore } from "pinia";
import { EGroupCode, EWorkProcess, OrganizationLevelEnum } from "@/enums/shared";
import { HttpStatusCode } from "axios";
import { ProcurementStatus, ProcurementStep, ProcurementType } from "@/enums/procurement";
import SharedService from "@/services/Shared/dropdown";
import PreProcurementService from "@/services/PP/ppService";
import ToastHelper from "@/helpers/toast";
import router from "@/router";
import { PreProcurementGroupStep, PreProcurementStep } from "@/enums/preProcurement";
import type { TDataTableResult } from "@/models/shared/paginated";
import preProcurementHelper from "@/helpers/preProcurement";
import { showConfirmDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType } from "@/enums/dialog";
import { useAuthenticationStore } from "@/stores/authentication";

const getDepartmentAsync = async (target: Ref<Option[]>): Promise<void> => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getSupplyMethodAsync = async (target: Ref<Option[]>, groupCode: EGroupCode, parentCode?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(groupCode, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

export const usePcm005ListStore = defineStore("pcm005-list-store", () => {
  const initCriteria = {
    pageNumber: 1,
    pageSize: 10,
    sort: [],
    workProcess: EWorkProcess.InProcess,
    step: PreProcurementGroupStep.All,
    procurementType: ProcurementType.Rent,
    budgetYear: new Date().getFullYear() + 543,
  } as TPreProcurementCriteria;

  const searchCriteria = ref(structuredClone(initCriteria));

  const departmentDropdown = ref<Array<Option>>([]);
  const smCodeDropdown = ref<Array<Option>>([]);
  const smTypeCodeDropdown = ref<Array<Option>>([]);
  const smSpecialTypeCodeDropdown = ref<Array<Option>>([]);

  const table = ref({
    data: [] as TPreProcurement[],
    totalRecords: 0,
  } as TDataTableResult<TPreProcurement>);
  const countData = ref({} as TPreProcurementGroupStepCount);

  const statusOptionBadge = ref([] as OptionBadge[]);

  const onGetListAsync = async () => {
    const { data, status } = await PreProcurementService.getProcurementListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      countData.value = {
        all: data.all,
        contractAgreement: data.contractAgreement,
        contractManagement: data.contractManagement,
        preProcurement: data.preProcurement,
        procurement: data.procurement,
      };

      table.value = data.data;

      statusOptionBadge.value = preProcurementHelper.PcmGroupStatusOptions(countData.value);
    }
  }

  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    } as TPreProcurementCriteria;
  };

  const onResetCriteria = (): void => {
    searchCriteria.value = structuredClone(initCriteria);
  };

  const getDropdownAsync = async (): Promise<void> => {
    await Promise.all([getDepartmentAsync(departmentDropdown), getSupplyMethodAsync(smCodeDropdown, EGroupCode.SMethod), getSupplyMethodAsync(smTypeCodeDropdown, EGroupCode.SMethodType)]);
  };

  const getSmSpecialTypeCodeDropdownAsync = async (smCode: string): Promise<void> => {
    await getSupplyMethodAsync(smSpecialTypeCodeDropdown, EGroupCode.SMethod, smCode);
  };

  const onDeleteAsync = async (id: string) => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

    const { status } = await PreProcurementService.deleteAsync(id);

    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();

      await onGetListAsync();
    };
  };

  return {
    searchCriteria,
    table,
    statusOptionBadge,
    countData,
    dropdown: {
      departmentDropdown,
      smCodeDropdown,
      smTypeCodeDropdown,
      smSpecialTypeCodeDropdown,
    },
    fn: {
      onGetListAsync,
      onChangePageSize,
      onResetCriteria,
      getDropdownAsync,
      getSmSpecialTypeCodeDropdownAsync,
      onDeleteAsync,
    },
  };
});

export const usePcm005DetailStore = defineStore('pcm005-detail-store', () => {
  const authStore = useAuthenticationStore();

  const initData = {
    status: ProcurementStatus.Draft,
    steps: [] as PreProcurementStep[],
    budgetYear: (new Date().getFullYear() + 543),
    departmentCode: authStore.profile.departmentCode,
    supplyMethodCode: "SMethod004",
    supplyMethodTypeCode: "SMethodType003",
    supplyMethodSpecialTypeCode: "SMethod003"
  } as TPreProcurementDetail;

  const body = ref<TPreProcurementDetail>(structuredClone(initData));
  const departmentDDL = ref<Option[]>([]);
  const smCodeDDL = ref<Option[]>([]);
  const smTypeCodeDDL = ref<Option[]>([]);
  const smSpTypeCodeDDL = ref<Option[]>([]);

  const getDDLAsync = async (): Promise<void> => {
    await Promise.all([getDepartmentAsync(departmentDDL), getSupplyMethodAsync(smCodeDDL, EGroupCode.SMethod), getSupplyMethodAsync(smTypeCodeDDL, EGroupCode.SMethodType)]);
  };

  const getsmSpTypeCodeDDLAsync = async (smCode: string): Promise<void> => {
    await getSupplyMethodAsync(smSpTypeCodeDDL, EGroupCode.SMethod, smCode);
  };

  const createAsync = async (procurementStatus?: ProcurementStatus): Promise<void> => {
    body.value = {
      ...body.value,
      procurementType: ProcurementType.Rent,
      procurementStep: ProcurementStep.Procurement,
      status: procurementStatus ?? body.value.status,
    };

    const { data, status } = await PreProcurementService.createProcurementAsync(body.value);

    if (status === HttpStatusCode.Created) {
      router.replace({ name: 'pcm005Detail', params: { id: data } });

      await getDetailAsync(data);


      return ToastHelper.createdMessageToast();
    }
  };

  const updateAsync = async (id: string, procurementStatus?: ProcurementStatus) => {
    const payload = {
      ...body.value,
      status: procurementStatus ?? body.value.status,
    };

    const { status } = await PreProcurementService.updateAsync(id, payload);

    if (status === HttpStatusCode.Ok) {
      await getDetailAsync(id);

      return procurementStatus ? ToastHelper.confirmMessageToast() : ToastHelper.updatedMessageToast();
    }
  };

  const getDetailAsync = async (id: string): Promise<void> => {
    const { data, status } = await PreProcurementService.getProcurementByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;
    }
  };

  const onResetBody = (): void => {
    body.value = structuredClone(initData);
  };

  const onUpsertAttachments = async () => {
    if (!body.value.id) return;

    const { status } = await PreProcurementService.onUpsertAttachmentsAsync(body.value.id, body.value.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await getDetailAsync(body.value.id);
    }
  };

  const isDraft = computed(() => body.value.status === ProcurementStatus.Draft);

  const canCloseProcurement = computed(() =>
    authStore.profile.isJorPor &&
    !!body.value.id &&
    body.value.status !== ProcurementStatus.Cancelled
  );

  const canCancelCloseProcurement = computed(() =>
    authStore.profile.isJorPor &&
    !!body.value.id &&
    body.value.status === ProcurementStatus.Cancelled
  );

  const onCloseAsync = async (remark: string, attachments: Attachments[] = []): Promise<void> => {
    if (!body.value.id) return;

    const { status } = await PreProcurementService.updateAsync(
      body.value.id,
      { ...body.value, status: ProcurementStatus.Cancelled, remarkClosed: remark, attachments }
    );

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await getDetailAsync(body.value.id);
    }
  };

  const onCancelCloseAsync = async (): Promise<void> => {
    if (!body.value.id) return;

    const { status } = await PreProcurementService.updateAsync(
      body.value.id,
      { ...body.value, status: ProcurementStatus.InProgress, remarkClosed: null, lastStatusBeforeClosed: null }
    );

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await getDetailAsync(body.value.id);
    }
  };

  return {
    body,
    departmentDDL,
    smCodeDDL,
    smTypeCodeDDL,
    smSpTypeCodeDDL,
    getDDLAsync,
    getsmSpTypeCodeDDLAsync,
    createAsync,
    updateAsync,
    getDetailAsync,
    onResetBody,
    onUpsertAttachments,
    canCloseProcurement,
    canCancelCloseProcurement,
    onCloseAsync,
    onCancelCloseAsync,
    states: {
      isDraft,
    }
  }
});

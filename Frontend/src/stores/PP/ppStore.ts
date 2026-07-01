import {
  PreProcurementGroupStep,
  PreProcurementType,
  PreProcurementDialogGroupStep,
  PreProcurementStep,
} from '@/enums/preProcurement';
import type {
  TPreProcurementCriteria,
  TPreProcurement,
  TPreProcurementGroupStepCount,
  TPreProcurementDetail,
  TPreProcurementDialogCriteria,
  TPreProcurementDialog,
  TPreProcurementDialogGroupStepCount,
} from '../../models/PP/ppModel';
import type { TDataTableResult } from '@/models/shared/paginated';
import { defineStore } from 'pinia';
import { computed, ref, type Ref } from 'vue';
import { useAuthenticationStore } from '@/stores/authentication';
import preProcurementHelper from '@/helpers/preProcurement';
import type { Option, OptionBadge } from '@/models/shared/option';
import { HttpStatusCode } from 'axios';
import { EGroupCode, EWorkProcess, OrganizationLevelEnum } from '@/enums/shared';
import SharedService from '@/services/Shared/dropdown';
import { ProcurementStatus, ProcurementStep, ProcurementType } from '@/enums/procurement';
import ToastHelper from '@/helpers/toast';
import PreProcurementService from '@/services/PP/ppService';
import type { Attachments } from '@/models/shared/uploadFile';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';

const getDepartmentAsync = async (target: Ref<Option[]>): Promise<void> => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getSupplyMethodAsync = async (target: Ref<Option[]>, parentCode?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getSupplyMethodTypeAsync = async (target: Ref<Option[]>, parentCode?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethodType, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

export const usePPListStore = defineStore(
  'PP-list-store',
  () => {
    const initCriteria = {
      pageNumber: 1,
      pageSize: 10,
      sort: [],
      workProcess: EWorkProcess.InProcess,
      procurementType: ProcurementType.Procurement,
      step: PreProcurementGroupStep.All,
      budgetYear: new Date().getFullYear() + 543,
    } as TPreProcurementCriteria;

    const searchCriteria = ref(structuredClone(initCriteria));

    const table = ref({
      data: [] as TPreProcurement[],
      totalRecords: 0,
    } as TDataTableResult<TPreProcurement>);

    const statusOptionBadge = ref([] as OptionBadge[]);
    const departmentDropdown = ref<Option[]>([] as Option[]);
    const supplyMethodDropdown = ref<Option[]>([] as Option[]);
    const supplyMethodTypeDropdown = ref<Option[]>([] as Option[]);
    const supplyMethidSpecialTypeDropDown = ref<Option[]>([] as Option[]);

    const getDepartmentDDLAsync = async (): Promise<void> => {
      await getDepartmentAsync(departmentDropdown);
    };

    const getSupplyMethodDDLAsync = async (): Promise<void> => {
      await getSupplyMethodAsync(supplyMethodDropdown);
    };

    const getSupplyMethodTypeDDLAsync = async (): Promise<void> => {
      await getSupplyMethodTypeAsync(supplyMethodTypeDropdown);
    };

    const getSupplyMethodSpecialTypeDDLAsync = async (parentCode: string): Promise<void> => {
      await getSupplyMethodAsync(supplyMethidSpecialTypeDropDown, parentCode);
    };
    //TODO: Delete After fetch API
    const countData = ref({} as TPreProcurementGroupStepCount);

    const onGetPreProcurementListDataAsync = async (): Promise<void> => {
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
      }

      statusOptionBadge.value = preProcurementHelper.AssignPreProcurementGroupStatusAttributes(
        countData.value
      );
    };

    const onChangePageSizePPList = (pageNumber: number, pageSize: number): void => {
      searchCriteria.value = {
        ...searchCriteria.value,
        pageNumber,
        pageSize,
      } as TPreProcurementCriteria;
    };

    const onResetCriteria = (): void => {
      searchCriteria.value = structuredClone(initCriteria);

      onGetPreProcurementListDataAsync();
    };

    const onDeleteAsync = async (id: string) => {
      if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

      const { status } = await PreProcurementService.deleteAsync(id);

      if (status === HttpStatusCode.NoContent) {
        ToastHelper.deletedMessageToast();

        await onGetPreProcurementListDataAsync();
      };
    };


    const exportExcelProcurementAsync = async (): Promise<void> => {
      const { data, status } = await PreProcurementService.exportExcelProcurementAsync(
        searchCriteria.value
      );

      if (status !== HttpStatusCode.Ok) {
        ToastHelper.error('ดึงข้อมูลเอกสาร', 'ไม่พบเอกสารที่ต้องการ');
        return;
      }

      const now = new Date();
      const year = now.getFullYear();
      const month = String(now.getMonth() + 1).padStart(2, '0');
      const day = String(now.getDate()).padStart(2, '0');
      const dateStr = `${year}${month}${day}`;

      const fileName = `รายงานรายการจัดซื้อจัดจ้าง_${dateStr}.xlsx`;
      const url = window.URL.createObjectURL(data);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      a.remove();
    };

    return {
      searchCriteria,
      table,
      onGetPreProcurementListDataAsync,
      onChangePageSizePPList,
      onResetCriteria,
      statusOptionBadge,
      departmentDropdown,
      supplyMethodDropdown,
      supplyMethodTypeDropdown,
      supplyMethidSpecialTypeDropDown,
      getDepartmentDDLAsync,
      getSupplyMethodDDLAsync,
      getSupplyMethodTypeDDLAsync,
      getSupplyMethodSpecialTypeDDLAsync,
      onDeleteAsync,
      exportExcelProcurementAsync
    };
  }
);

export const usePPDetailStore = defineStore(
  'PP-detail-store',
  () => {
    const initData = {
      status: ProcurementStatus.Draft,
      procurementStatus: ProcurementStatus.Draft,
      attachments: [] as Array<Attachments>,
    } as TPreProcurementDetail;

    const procurementDetail = ref(structuredClone(initData));

    const resetPreProcurementDetail = (): void => {
      procurementDetail.value = {
        status: ProcurementStatus.Draft,
        procurementStatus: ProcurementStatus.Draft,
        attachments: [] as Array<Attachments>,
      } as TPreProcurementDetail;
    };

    const onCreateProcurement = async (): Promise<string | undefined> => {

      const { data, status } = await PreProcurementService.createProcurementAsync(procurementDetail.value);

      if (status == HttpStatusCode.Created) {
        ToastHelper.success("ข้อมูลรายการจัดซื้อจัดจ้าง", "บันทึกข้อมูลสำเร็จ")

        return data;
      }
    };

    const onGetProcurementById = async (id: string): Promise<void> => {
      const { data, status } = await PreProcurementService.getProcurementByIdAsync(id);

      if (status == HttpStatusCode.Ok) {
        procurementDetail.value = data;
      }
    };

    const onUpsertAttachments = async () => {
      if (!procurementDetail.value.id) return;

      const { status } = await PreProcurementService.onUpsertAttachmentsAsync(procurementDetail.value.id, procurementDetail.value.attachments);

      if (status === HttpStatusCode.Ok) {
        ToastHelper.updatedMessageToast();

        await onGetProcurementById(procurementDetail.value.id);
      }
    };

    const authenStore = useAuthenticationStore();

    const canCloseProcurement = computed(() =>
      authenStore.profile.isJorPor &&
      !!procurementDetail.value.id &&
      procurementDetail.value.procurementStatus !== ProcurementStatus.Cancelled
    );

    const canCancelCloseProcurement = computed(() =>
      authenStore.profile.isJorPor &&
      !!procurementDetail.value.id &&
      procurementDetail.value.procurementStatus === ProcurementStatus.Cancelled
    );

    const onCloseProcurementAsync = async (remark: string, attachments: Attachments[] = []): Promise<void> => {
      if (!procurementDetail.value.id) return;

      const { status } = await PreProcurementService.updateAsync(
        procurementDetail.value.id,
        { ...procurementDetail.value, status: ProcurementStatus.Cancelled, remarkClosed: remark, attachments }
      );

      if (status === HttpStatusCode.Ok) {
        ToastHelper.updatedMessageToast();
        await onGetProcurementById(procurementDetail.value.id);
      }
    };

    const onCancelCloseProcurementAsync = async (): Promise<void> => {
      if (!procurementDetail.value.id) return;

      const { status } = await PreProcurementService.updateAsync(
        procurementDetail.value.id,
        { ...procurementDetail.value, status: ProcurementStatus.InProgress, remarkClosed: null, lastStatusBeforeClosed: null }
      );

      if (status === HttpStatusCode.Ok) {
        ToastHelper.updatedMessageToast();
        await onGetProcurementById(procurementDetail.value.id);
      }
    };

    return {
      procurementDetail,
      resetPreProcurementDetail,
      onCreateProcurement,
      onGetProcurementById,
      onUpsertAttachments,
      canCloseProcurement,
      canCancelCloseProcurement,
      onCloseProcurementAsync,
      onCancelCloseProcurementAsync,
    };
  }
);

export const usePPListDialogStore = defineStore('PP-list-dialog-store', () => {
  const detailStore = usePPDetailStore();

  const initDialogCriteria = {
    pageNumber: 1,
    pageSize: 10,
    sort: [],
    type: PreProcurementType.All,
    groupStep: PreProcurementDialogGroupStep.SupplyMethodCode60,
    budgetYear: new Date().getFullYear() + 543,
  } as TPreProcurementDialogCriteria;
  const searchCriteria = ref(structuredClone(initDialogCriteria));

  const table = ref({
    data: [] as TPreProcurementDialog[],
    totalRecords: 0,
  } as TDataTableResult<TPreProcurementDialog>);
  const statusOptionBadge = ref([] as OptionBadge[]);

  const countData = ref({} as TPreProcurementDialogGroupStepCount);

  const departmentDropdown = ref<Option[]>([] as Option[]);
  const supplyMethodDropdown = ref<Option[]>([] as Option[]);
  const supplyMethodTypeDropdown = ref<Option[]>([] as Option[]);
  const supplyMethidSpecialTypeDropDown = ref<Option[]>([] as Option[]);

  const getDepartmentDDLAsync = async (): Promise<void> => {
    await getDepartmentAsync(departmentDropdown);
  };

  const getSupplyMethodDDLAsync = async (): Promise<void> => {
    await getSupplyMethodAsync(supplyMethodDropdown);
  };

  const getSupplyMethodTypeDDLAsync = async (): Promise<void> => {
    await getSupplyMethodTypeAsync(supplyMethodTypeDropdown);
  };

  const getSupplyMethodSpecialTypeDDLAsync = async (parentCode: string): Promise<void> => {
    await getSupplyMethodAsync(supplyMethidSpecialTypeDropDown, parentCode);
  };

  const onGetProcurementDialogListDataAsync = async (): Promise<void> => {
    const { data, status } = await PreProcurementService.getPlanDialogAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data.data;
      countData.value = data.groupType;
    }
  };

  const onGetPreProcurementDialogListDataAsync = async (): Promise<void> => {
    const { data, status } = await PreProcurementService.getProcurementDialogAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data.data;
      countData.value = data.groupType;
    }
  };

  const onChangePageSizePPListDialog = (pageNumber: number, pageSize: number): void => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    } as TPreProcurementDialogCriteria;
  };

  const onResetCriteriaDialog = (): void => {
    searchCriteria.value = {
      ...structuredClone(initDialogCriteria),
      departmentCode: searchCriteria.value.departmentCode,
    };

    onGetProcurementDialogListDataAsync();
  };

  const onSelectData = (index: number, isJorPorDepartmentCode?: boolean) => {
    const plan = table.value.data[index];

    if (!plan) return;

    detailStore.procurementDetail = {
      ...plan,
      id: plan.procurementId,
      planId: plan.id ?? plan.procurementId,
      planNumber: plan.planNumber,
      procurementNumber: plan.procurementNumber,
      procurementStep: ProcurementStep.PreProcurement,
      procurementType: (plan.procurementType as ProcurementType) ?? ProcurementType.Procurement,
      planType: plan.type,
      currentStep: PreProcurementStep.Appoint,
      steps: [],
      planbudget: plan.budget,
      status: ProcurementStatus.Draft,
      procurementStatus: ProcurementStatus.Draft,
      hasMd: false,
      attachments: [],
      isStock: isJorPorDepartmentCode ? plan.isStock : false,
      isCommercialMaterial: plan.isCommercialMaterial,
    };
  }

  return {
    searchCriteria,
    table,
    onGetProcurementDialogListDataAsync,
    onGetPreProcurementDialogListDataAsync,
    onChangePageSizePPListDialog,
    onResetCriteriaDialog,
    statusOptionBadge,
    departmentDropdown,
    supplyMethodDropdown,
    supplyMethodTypeDropdown,
    supplyMethidSpecialTypeDropDown,
    getDepartmentDDLAsync,
    getSupplyMethodDDLAsync,
    getSupplyMethodTypeDDLAsync,
    getSupplyMethodSpecialTypeDDLAsync,
    onSelectData
  };
}
);
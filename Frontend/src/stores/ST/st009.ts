import type { TDataTableResult } from '@/models/shared/paginated';
import type { St009Criteria, St009Detail, St009EditSectionBody, St009ListItem, St009UpdateApproverBody } from '@/models/ST/st009';
import ST009Service from '@/services/ST/ST009';
import SharedService from '@/services/Shared/dropdown';
import ToastHelper from '@/helpers/toast';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { Option } from '@/models/shared/option';

export const useSt009ListStore = defineStore('st009ListStore', () => {
  const initCriteria: St009Criteria = {
    pageNumber: 1,
    pageSize: 10,
  };

  const initTable: TDataTableResult<St009ListItem> = {
    data: [],
    totalRecords: 0,
  };

  const searchCriteria = ref<St009Criteria>(structuredClone(initCriteria));
  const table = ref<TDataTableResult<St009ListItem>>(structuredClone(initTable));
  const supplyMethodOptions = ref<Option[]>([]);
  const supplyMethodSpecialTypeOptions = ref<Option[]>([]);

  const onGetDropdownOptionsAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod);
    if (status === HttpStatusCode.Ok) {
      supplyMethodOptions.value = data;
    }
  };

  const onGetSupplyMethodSpecialTypeOptionsAsync = async (parentCode?: string): Promise<void> => {
    supplyMethodSpecialTypeOptions.value = [];
    if (!parentCode) return;
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, parentCode);
    if (status === HttpStatusCode.Ok) {
      supplyMethodSpecialTypeOptions.value = data;
    }
  };

  const onGetListAsync = async (): Promise<void> => {
    const { data, status } = await ST009Service.getListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data;
    }
  };

  const onResetCriteria = (): void => {
    searchCriteria.value = structuredClone(initCriteria);
    supplyMethodSpecialTypeOptions.value = [];
  };

  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  const onDeleteAsync = async (id: string): Promise<void> => {
    const { status } = await ST009Service.deleteAsync(id);

    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();
      await onGetListAsync();
    }
  };

  return {
    searchCriteria,
    table,
    supplyMethodOptions,
    supplyMethodSpecialTypeOptions,
    onGetListAsync,
    onGetDropdownOptionsAsync,
    onGetSupplyMethodSpecialTypeOptionsAsync,
    onResetCriteria,
    onChangePageSize,
    onDeleteAsync,
  };
});

export const useSt009DetailStore = defineStore('st009DetailStore', () => {
  const detail = ref<St009Detail | null>(null);
  const editableApprovers = ref<St009UpdateApproverBody[]>([]);
  const editableSection = ref<St009EditSectionBody>({
    refBankOrder: '',
    maximumBudget: 0,
  });
  const supplyMethodOptions = ref<Option[]>([]);
  const supplyMethodSpecialTypeOptions = ref<Option[]>([]);

  const onGetDropdownOptionsAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod);
    if (status === HttpStatusCode.Ok) {
      supplyMethodOptions.value = data;
    }
  };

  const onGetSupplyMethodSpecialTypeOptionsAsync = async (parentCode?: string): Promise<void> => {
    supplyMethodSpecialTypeOptions.value = [];
    if (!parentCode) return;
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, parentCode);
    if (status === HttpStatusCode.Ok) {
      supplyMethodSpecialTypeOptions.value = data;
    }
  };

  const onGetBySuSectionIdAsync = async (suSectionId: string): Promise<void> => {
    const { data, status } = await ST009Service.getBySuSectionIdAsync(suSectionId);

    if (status === HttpStatusCode.Ok) {
      detail.value = data;
      editableSection.value = {
        refBankOrder: data.refBankOrder,
        maximumBudget: data.maximumBudget,
        remark: data.remark,
        supplyMethodCode: data.supplyMethodCode,
        supplyMethodSpecialTypeCode: data.supplyMethodSpecialTypeCode,
      };
      editableApprovers.value = data.approvers.map(a => ({
        id: a.id,
        inRefCode: a.inRefCode,
        positionName: a.positionName,
        shortPosition: a.shortPosition,
        budget: a.budget,
        processType: a.processType,
        commandText: a.commandText,
        commandBudget: a.commandBudget,
      }));
    }
  };

  const onUpdateApproversAsync = async (suSectionId: string): Promise<void> => {
    const { status } = await ST009Service.updateApproversAsync(suSectionId, {
      approvers: editableApprovers.value,
    });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await onGetBySuSectionIdAsync(suSectionId);
    }
  };

  const onCreateApproverAsync = async (suSectionId: string, approver: St009UpdateApproverBody): Promise<string | null> => {
    const { status, data } = await ST009Service.createApproverAsync({
      suSectionId,
      inRefCode: approver.inRefCode,
      positionName: approver.positionName,
      shortPosition: approver.shortPosition,
      budget: approver.budget,
      processType: approver.processType,
      commandText: approver.commandText,
      commandBudget: approver.commandBudget,
    });

    if (status === HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();
      return data;
    }

    return null;
  };

  const onUpdateSingleApproverAsync = async (suSectionId: string, approver: St009UpdateApproverBody): Promise<void> => {
    if (!approver.id) return;

    const { status } = await ST009Service.updateApproverAsync(suSectionId, approver.id, {
      inRefCode: approver.inRefCode,
      positionName: approver.positionName,
      shortPosition: approver.shortPosition,
      budget: approver.budget,
      processType: approver.processType,
      commandText: approver.commandText,
      commandBudget: approver.commandBudget,
    });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
    }
  };

  const onCreateSectionAsync = async (newId: string): Promise<string | null> => {
    const { status, data } = await ST009Service.createSectionAsync({
      newId,
      ...editableSection.value,
    });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.createdMessageToast();
      return data;
    }

    return null;
  };

  const onUpdateSectionAsync = async (suSectionId: string): Promise<void> => {
    const { status } = await ST009Service.updateSectionAsync({
      id: suSectionId,
      ...editableSection.value,
    });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
      await onGetBySuSectionIdAsync(suSectionId);
    }
  };

  const onDeleteAsync = async (id: string): Promise<boolean> => {
    const { status } = await ST009Service.deleteAsync(id);
    return status === HttpStatusCode.NoContent;
  };

  const onDeleteApproverAsync = async (suSectionId: string, approverId: string): Promise<boolean> => {
    const { status } = await ST009Service.deleteApproverAsync(suSectionId, approverId);
    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();
      await onGetBySuSectionIdAsync(suSectionId);
      return true;
    }
    return false;
  };

  const onClear = (): void => {
    detail.value = null;
    editableApprovers.value = [];
    editableSection.value = { refBankOrder: '', maximumBudget: 0 };
  };

  return {
    detail,
    editableApprovers,
    editableSection,
    supplyMethodOptions,
    supplyMethodSpecialTypeOptions,
    onGetBySuSectionIdAsync,
    onGetDropdownOptionsAsync,
    onGetSupplyMethodSpecialTypeOptionsAsync,
    onUpdateApproversAsync,
    onCreateSectionAsync,
    onUpdateSectionAsync,
    onCreateApproverAsync,
    onUpdateSingleApproverAsync,
    onDeleteAsync,
    onDeleteApproverAsync,
    onClear,
  };
});

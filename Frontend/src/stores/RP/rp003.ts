import { defineStore } from 'pinia';
import { ref, type Ref } from 'vue';
import type { TRP003Criteria, TRResponseTable } from '@/models/RP/rp003';
import type { Option, OptionBadge } from '@/models/shared/option';
import { EWorkProcess, OrganizationLevelEnum } from '@/enums/shared';
import { rp003SupplyMethod } from '@/enums/RP/rp003';
import rp003Service from '@/services/RP/rp003';
import { HttpStatusCode } from 'axios';
import SharedService from '@/services/Shared/dropdown';
import RP003Constants from '@/constants/RP/rp003';
import ToastHelper from '@/helpers/toast';

const onGetDepartmentDropdown = async (target: Ref<Array<Option>>) => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const { MapCountStatus } = RP003Constants;

const deparmentDropdown = ref<Array<Option>>([]);

const onGetDropdownAsync = async () => {
  await Promise.all([onGetDepartmentDropdown(deparmentDropdown)]);
};

export const useRP003ListStore = defineStore('rp003-list-store', () => {
  const searchCriteria = ref<TRP003Criteria>({
    supplyMethodCode: rp003SupplyMethod.ALL,
    pageNumber: 1,
    pageSize: 10,
    workProcess: EWorkProcess.InProcess,
    budgetYear: new Date().getFullYear() + 543,
  });

  const statusOptions = ref<OptionBadge[]>([]);

  const procurementListTable = ref<TRResponseTable>({
    data: {}
  } as TRResponseTable);

  const onGetProcurementListData = async (): Promise<void> => {
    const { status, data } = await rp003Service.getListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      procurementListTable.value = data as TRResponseTable;

      statusOptions.value = MapCountStatus(data.counts);
    }
  };

  const onResetCriteria = async () => {
    searchCriteria.value = {
      pageNumber: 1,
      pageSize: 10,
      workProcess: EWorkProcess.InProcess,
      supplyMethodCode: rp003SupplyMethod.ALL,
      budgetYear: new Date().getFullYear() + 543,
    }

    await onGetProcurementListData();
  };

  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    }
  };

  const exporeExcelAsync = async (columns: number[]): Promise<void> => {
    const { data, status } = await rp003Service.exporeExcelAsync(
      searchCriteria.value,
      columns
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

    const fileName = `รายงานบริหารสัญญา_${dateStr}.xlsx`;
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
    statusOptions,
    procurementListTable,
    onGetProcurementListData,
    onResetCriteria,
    onChangePageSize,
    deparmentDropdown,
    onGetDropdownAsync,
    exporeExcelAsync
  }
});
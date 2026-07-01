import type { Option } from '@/models/shared/option';
import type { WorklistCriteria, WorklistRes } from '@/models/WorkList/worklist';
import { EGroupCode, OrganizationLevelEnum } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref, type Ref } from 'vue';
import SharedService from '@/services/Shared/dropdown';
import worklistService from '@/services/WorkList/worklist';

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

export const useWlStore = defineStore('wl-store', () => {
  const criteria = ref<WorklistCriteria>({
    pageNumber: 1,
    pageSize: 10,
    includeAll: false,
    includeAnnouncements: false,
    includeContractAgreement: false,
    includePlans: false,
    includePreProcurement: false,
    includeProcurement: false,
    includeContractManagement: false,
    includeContractAmendment: false,
    includeExpenseDisbursement: false,
  } as WorklistCriteria);
  const worklistRes = ref<WorklistRes>({} as WorklistRes);
  const departmentDDL = ref<Option[]>([]);
  const supplyMethodCodeDDL = ref<Option[]>([]);
  const supplyMethodTypeCodeDDL = ref<Option[]>([]);
  const supplyMethodSpecialTypeCodeDDL = ref<Option[]>([]);

  const getDepartmentDDLAsync = async (): Promise<void> => {
    await getDepartmentAsync(departmentDDL);
  };

  const getSupplyMethodDDLAsync = async (): Promise<void> => {
    await getSupplyMethodAsync(supplyMethodCodeDDL);
  };

  const getSupplyMethodTypeDDLAsync = async (): Promise<void> => {
    await getSupplyMethodTypeAsync(supplyMethodTypeCodeDDL);
  };

  const getSupplyMethodSpecialTypeDDlAsync = async (parentCode: string): Promise<void> => {
    await getSupplyMethodAsync(supplyMethodSpecialTypeCodeDDL, parentCode);
  };

  const onResetWorklistType = () => {
    criteria.value = {
      ...criteria.value,
      pageNumber: 1,
      pageSize: 10,
      includeAll: false,
      includeAnnouncements: false,
      includeContractAgreement: false,
      includePlans: false,
      includePreProcurement: false,
      includeProcurement: false,
      includeContractManagement: false,
      includeContractAmendment: false,
      includeExpenseDisbursement: false,
    };
  };

  const onClearCriteriaAsync = async () => {
    criteria.value = {
      ...criteria.value,
      keyword: undefined,
      departmentCode: undefined,
      budgetYear: undefined,
      supplyMethodCode: undefined,
      supplyMethodTypeCode: undefined,
      supplyMethodSpecialTypeCode: undefined,
      isPendingDepartment: false,
      pageNumber: 1,
      pageSize: 10,
    };

    await getListAsync();
  };

  const onChangePageSizeAsync = async (pageNumber: number, pageSize: number) => {
    criteria.value.pageNumber = pageNumber;
    criteria.value.pageSize = pageSize;

    await getListAsync();
  };

  const getListAsync = async () => {
    const { data, status } = await worklistService.getListAsync(criteria.value);

    if (status === HttpStatusCode.Ok) {
      worklistRes.value = data;
    }
  };

  return {
    criteria,
    departmentDDL,
    supplyMethodCodeDDL,
    supplyMethodTypeCodeDDL,
    supplyMethodSpecialTypeCodeDDL,
    worklistRes,
    onClearCriteriaAsync,
    getDepartmentDDLAsync,
    getSupplyMethodDDLAsync,
    getSupplyMethodTypeDDLAsync,
    getSupplyMethodSpecialTypeDDlAsync,
    onResetWorklistType,
    onChangePageSizeAsync,
    getListAsync,
  };
});

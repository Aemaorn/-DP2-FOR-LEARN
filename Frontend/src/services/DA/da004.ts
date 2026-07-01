import http from '@/configs/axios';
import type { AxiosResponse } from 'axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { Da004BarChartItem, Da004Criteria, Da004DepartmentSummary, Da004PriceSummary, Da004SpecialTypeChartItem } from '@/models/DA/da004';

const getListAsync = async (criteria: Da004Criteria): Promise<AxiosResponse<TDataTableResult<Da004PriceSummary>>> => {
  return await http.get<TDataTableResult<Da004PriceSummary>>('api/dashboard/price-summary', { params: criteria });
};

const exportExcelAsync = async (criteria: Da004Criteria): Promise<AxiosResponse<Blob>> => {
  return await http.get<Blob>('api/dashboard/price-summary/export-excel', {
    params: {
      budgetYear: criteria.budgetYear,
      keyword: criteria.keyword,
      departmentId: criteria.departmentId,
      supplyMethodCode: criteria.supplyMethodCode,
      supplyMethodSpecialTypeCode: criteria.supplyMethodSpecialTypeCode,
      month: criteria.month,
      quarter: criteria.quarter,
    },
    responseType: 'blob',
    headers: {
      Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, */*',
    },
  });
};

const getBarChartAsync = async (criteria: Da004Criteria): Promise<AxiosResponse<Da004BarChartItem[]>> => {
  return await http.get<Da004BarChartItem[]>('api/dashboard/price-summary/bar-chart', {
    params: {
      budgetYear: criteria.budgetYear,
      keyword: criteria.keyword,
      departmentId: criteria.departmentId,
      supplyMethodCode: criteria.supplyMethodCode,
      supplyMethodSpecialTypeCode: criteria.supplyMethodSpecialTypeCode,
      month: criteria.month,
      quarter: criteria.quarter,
    },
  });
};

const getSpecialTypeChartAsync = async (criteria: Da004Criteria): Promise<AxiosResponse<Da004SpecialTypeChartItem[]>> => {
  return await http.get<Da004SpecialTypeChartItem[]>('api/dashboard/price-summary/special-type-chart', {
    params: {
      budgetYear: criteria.budgetYear,
      keyword: criteria.keyword,
      departmentId: criteria.departmentId,
      supplyMethodCode: criteria.supplyMethodCode,
      supplyMethodSpecialTypeCode: criteria.supplyMethodSpecialTypeCode,
      month: criteria.month,
      quarter: criteria.quarter,
    },
  });
};

const getDepartmentSummaryAsync = async (criteria: Da004Criteria): Promise<AxiosResponse<Da004DepartmentSummary[]>> => {
  return await http.get<Da004DepartmentSummary[]>('api/dashboard/price-summary/department', {
    params: {
      budgetYear: criteria.budgetYear,
      keyword: criteria.keyword,
      departmentId: criteria.departmentId,
      supplyMethodCode: criteria.supplyMethodCode,
      supplyMethodSpecialTypeCode: criteria.supplyMethodSpecialTypeCode,
      month: criteria.month,
      quarter: criteria.quarter,
    },
  });
};

const exportDepartmentExcelAsync = async (criteria: Da004Criteria): Promise<AxiosResponse<Blob>> => {
  return await http.get<Blob>('api/dashboard/price-summary/department/export-excel', {
    params: {
      budgetYear: criteria.budgetYear,
      keyword: criteria.keyword,
      departmentId: criteria.departmentId,
      supplyMethodCode: criteria.supplyMethodCode,
      supplyMethodSpecialTypeCode: criteria.supplyMethodSpecialTypeCode,
      month: criteria.month,
      quarter: criteria.quarter,
    },
    responseType: 'blob',
    headers: {
      Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, */*',
    },
  });
};

export const da004Service = {
  getListAsync,
  exportExcelAsync,
  exportDepartmentExcelAsync,
  getBarChartAsync,
  getSpecialTypeChartAsync,
  getDepartmentSummaryAsync,
};

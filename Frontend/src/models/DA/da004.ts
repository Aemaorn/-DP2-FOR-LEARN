import type { TPaginated } from '@/models/shared/paginated';

export type Da004Criteria = {
  budgetYear?: number;
  keyword?: string;
  departmentId?: string;
  supplyMethodCode?: string;
  supplyMethodSpecialTypeCode?: string;
  month?: number;
  quarter?: number;
} & TPaginated;

export type Da004BarChartItem = {
  period: string;
  totalBudget: number;
  totalMedianPrice: number;
  totalOfferedPrice: number;
  totalAgreedPrice: number;
};

export type Da004SpecialTypeChartItem = {
  specialTypeName: string;
  projectCount: number;
  totalAgreedPrice: number;
};

export type Da004DepartmentSummary = {
  departmentName: string;
  projectCount: number;
  totalBudget: number;
  totalMedianPrice: number;
  totalAgreedPrice: number;
};

export type Da004PriceSummary = {
  procurementId: string;
  jp006Id: string;
  procurementNumber: string;
  projectName: string;
  supplyMethodName: string;
  supplyMethodSpecialTypeName?: string;
  approvedDate?: string;
  vendorName: string;
  budget: number;
  medianPrice: number;
  totalOfferedPrice: number;
  totalAgreedPrice: number;
  budgetDiff: number;
  budgetDiffPercent: number;
  isUnderBudget: boolean;
  medianPriceDiff: number;
  medianPriceDiffPercent: number;
  isUnderMedianPrice: boolean;
  offeredPriceDiff: number;
  offeredPriceDiffPercent: number;
  isUnderOfferedPrice: boolean;
  selectionReasonName?: string;
  remark?: string;
};

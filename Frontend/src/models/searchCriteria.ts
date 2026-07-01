import type { TPaginated } from "./shared/paginated";

export type TSearchAllCriteria = {
  searchText?: string;
} & TPaginated;

export type TWorkflowStep = {
  name: string;
  refNumber: string;
  url: string;
};

export type TSearchAllData = {
  id: string;
  programNumber?: string;
  procurementNumber?: string;
  contractNumber?: string;
  programName?: string;
  budget?: number;
  departmentName?: string;
  budgetYear?: number;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
  supplyMethodName?: string;
  supplyMethodTypeName?: string;
  supplyMethodSpecialTypeName?: string;
  vendorName?: string;
  type?: string;
  createdDate: Date;
  lastModifiedDate: Date;
  steps: TWorkflowStep[];
}
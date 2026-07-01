import type { TDataTableResult, TPaginated } from "../shared/paginated";
import type { rp003SupplyMethod } from "@/enums/RP/rp003";
import type { EWorkProcess } from "@/enums/shared";

export type TRP003Criteria = {
  keyword?: string;
  departmentCode?: string;
  budgetYear?: number;
  supplyMethodCode?: rp003SupplyMethod;
  workProcess: EWorkProcess;
} & TPaginated;

export type TRP003List = {
  id: string;
  contractDraftNumber: string;
  poNumber?: string;
  supplyMethodName: string;
  departmentName: string;
  budgetYear?: number;
  contractSignedDate?: Date;
  contractTypeName: string;
  contractName: string;
  vendorName: string;
  budget: number;
};

export type Rp003Counts = {
  allCount: number;
  sixtyCount: number;
  eightyCount: number;
}

export type TRResponseTable = {
  counts: Rp003Counts,
  data: TDataTableResult<TRP003List[]>
};

export interface TRP003ProcurementItem {
  procurementNo: string;
  procurementMethod: string;
  department: string;
  contractDate: Date;
  contractType: string;
  contractName: string;
  contractorCompany: string;
  contractValue: number;
};
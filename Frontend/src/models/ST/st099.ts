import type { TPaginated } from "../shared/paginated";

export type SuSectionCriteria = {
  keyword?: string;
} & TPaginated;

export type ListSuSection = {
  id: string;
  refBankOrder: string;
  maximumBudget: number;
  remark?: string | null;
  supplyMethodCode?: string | null;
  supplyMethodSpecialTypeCode?: string | null;
};

export type SuSection = {
  id: string;
  newId: string;
  refBankOrder: string;
  maximumBudget: number;
  remark?: string;
  supplyMethodCode?: string;
  supplyMethodSpecialTypeCode?: string;
  approvers: SuSectionApprover[];
};

export type SuSectionApprover = {
  id?: string;
  processType: string;
  positionName: string;
  shortPositionName: string;
  inRefCode: string;
  budget: number;
  sectionId?: string;
  commandText: string;
};
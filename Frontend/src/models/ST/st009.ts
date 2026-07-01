import type { TPaginated } from '../shared/paginated';

export type St009Criteria = {
  keyword?: string;
  supplyMethodCode?: string;
  supplyMethodSpecialTypeCode?: string;
} & TPaginated;

export type St009ListItem = {
  id: string;
  refBankOrder: string;
  maximumBudget: number;
  remark?: string;
  supplyMethodCode?: string;
  supplyMethod?: string;
  supplyMethodSpecialTypeCode?: string;
  supplyMethodSpecialType?: string;
};

export type St009ApproverItem = {
  id: string;
  inRefCode: string;
  positionName: string;
  shortPosition: string;
  budget: number;
  processType: string;
  commandText: string;
  suSectionId: string;
  commandBudget?: number;
};

export type St009UpdateApproverBody = {
  id?: string;
  inRefCode: string;
  positionName: string;
  shortPosition: string;
  budget: number;
  processType: string;
  commandText: string;
  commandBudget?: number;
};

export type St009CreateApproverRequest = {
  suSectionId: string;
  inRefCode: string;
  positionName: string;
  shortPosition: string;
  budget: number;
  processType: string;
  commandText: string;
  commandBudget?: number;
};

export type St009UpdateSingleApproverRequest = {
  inRefCode: string;
  positionName: string;
  shortPosition: string;
  budget: number;
  processType: string;
  commandText: string;
  commandBudget?: number;
};

export type St009UpdateApproversRequest = {
  approvers: St009UpdateApproverBody[];
};

export type St009EditSectionBody = {
  refBankOrder: string;
  maximumBudget: number;
  remark?: string;
  supplyMethodCode?: string;
  supplyMethodSpecialTypeCode?: string;
};

export type St009UpdateSectionRequest = St009EditSectionBody & {
  id: string;
};

export type St009CreateSectionRequest = St009EditSectionBody & {
  newId: string;
};

export type St009Detail = {
  id: string;
  refBankOrder: string;
  maximumBudget: number;
  remark?: string;
  supplyMethodCode?: string;
  supplyMethod?: string;
  supplyMethodSpecialTypeCode?: string;
  supplyMethodSpecialType?: string;
  approvers: St009ApproverItem[];
};

import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import type { PP003Status } from "../../enums/pp003";
import type { DocumentVersion } from "@/models/shared/document";


export type TPP003Body = {
  budgetAllocations: TPP003BudgetAllocations;
  staff?: TPP003Staff;
  acceptors: ParticipantsCommitteeAcceptor[];
  assignees: ParticipantsAssignee[];
  expenseDescription?: TP003MedianPriceExpenseDescriptionInfo;
  status: PP003Status;
  medianPriceDocumentId?: string;
  isMedianPriceDocumentIdReplaced?: boolean;
  cancelReason?: string;
  changeReason?: string;
  isChange: boolean;
  isCancel: boolean;
  isActive: boolean;
  torTemplate?: string;
  documentVersions?: DocumentVersion[];
} & TPP003Proposal;

export type TPP003Proposal = {
  id?: string;
  procurementId?: string;
  referenceNumber: string;
  telephone?: string;
  documentDate?: Date;
  medianPriceDocumentTemplateCode?: string;
  object: string;
  reason: string;
  specialDescription: string;
  jobDescription?: string;
  priceReasonablenessInfo: string;
};

export type TPP003BudgetAllocations = {
  id?: string;
  referenceDate: Date;
  budget: number;
  referenceMedianPrice: number;
  details: TPP003BudgetAllocationsDetail[] | TPP003BudgetAllocationsDetailRefBudget[];
}

export type TPP003BudgetAllocationsDetail = {
  type: 'Without' | 'With';
  id?: string;
  sequence: number;
  source: string;
}

export type TPP003BudgetAllocationsDetailRefBudget = {
  referenceBudge: number;
} & TPP003BudgetAllocationsDetail;

export type TPP003Staff = {
  id?: string;
  personnelCompensation: number;
  personnelCount: number;
  details: TPP003StaffDetail[] | TPP003StaffDetailPersonal[];
};

// Personal => Template 2 & 3
// ConsultantTypes => Template 4 Type รายการประเภทที่ปรึกษา
// ConsultantQualifications => Template 4 Type คุณสมบัติที่ปรึกษา
export type TPP003StaffDetail = {
  type: 'Personal' | 'ConsultantTypes' | 'ConsultantQualifications';
  id?: string;
  sequence: number;
  description: string;
};

export type TPP003StaffDetailPersonal = {
  personnelCount: number;
} & TPP003StaffDetail;

export type TP003MedianPriceExpenseDescriptionInfo = {
  materialCost?: number;
  overseasTravelCost?: number;
  otherExpenses?: number;
  hardwareCost?: number;
  softwareCost?: number;
  systemDevelopmentCost?: number;
};
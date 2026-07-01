import type { CommitteeGroupType, PrincipleStatus, RentalAnalysisType } from "@/enums/PCM005/principle";
import type { ParticipantsAcceptor, ParticipantsAssignee } from "@/models/shared/participants";
import type { OnlyFileAttachment } from "@/models/shared/uploadFile";
import type { DocumentVersion } from "@/models/shared/document";

export type MockBudget = {
  no?: string;
  email?: string;
  contactNo?: string;
  price?: number;
  po?: string;
  isEdit?: boolean;
}

export type Mocking = {
  type: string;
  taxId: string;
  name: string;
  price: number;
}

export type MockContract = {
  name: string;
  message: string;
  count: number;
}

export type PrincipleBody = {
  id?: string;
  documentDate?: Date;
  branchLocation: string;
  documentTemplateCode?: string;
  documentTemplateId?: string;
  isDocumentTemplateIdReplaced?: boolean;
  documentVersions?: DocumentVersion[];
  rentTypeCode: string;
  rentalStartDate: Date;
  rentalEndDate: Date | undefined;
  rentalDurationYear?: number;
  rentalDurationMonth?: number;
  rentalDurationDay?: number;
  maxMonthlyRent: number;
  totalRentalAmount?: number;
  expectedContractDate?: Date;
  rentalLocationDetails: string;
  subDistrictCode: string | undefined;
  subDistrictName: string;
  districtCode: string | undefined;
  districtName: string;
  provinceCode: string;
  provinceName: string;
  referencePriceAmount?: number;
  operationExpense?: number;
  analysisSummaryNpv?: number;
  analysisSummaryPaybackYearPeriod?: number;
  analysisSummaryDiscountedPaybackYearPeriod?: number;
  status: PrincipleStatus;
  acceptors: ParticipantsAcceptor[];
  assignees: ParticipantsAssignee[];
  committees: Committee[];
  perfSupportData?: PerfSupportData;
  perfSupportDataDetails: PerfSupportDataDetail[];
  roiLoanAndDepositSummaries: RoiLoanAndDepositSummary[];
  roiPerfResults: RoiPerfResult[];
  budgets: Budget[];
  rentalAnalyses: RentalAnalyses[];
  isRentCommittee: boolean;
  isAcceptanceCommittee: boolean;
  attachments: OnlyFileAttachment[];
  phoneNumber: string;
};

export type PrincipleConsiderationImportResp = {
  perfSupportDataDetails: PerfSupportDataDetail[];
  roiLoanAndDepositSummaries: RoiLoanAndDepositSummary[];
  roiPerfResults: RoiPerfResult[];
};

export type PrincipleAnalysisImportResp = {
  rentalAnalyses: RentalAnalyses[];
  analysisSummaryNpv: number;
  analysisSummaryPaybackYearPeriod: number;
  analysisSummaryDiscountedPaybackYearPeriod: number;
};

export type RentalAnalyses = {
  id?: string;
  sequence: number;
  type: RentalAnalysisType;
  description: string;
  details?: RentalAnalysisDetail[];
}

export type RentalAnalysisDetail = {
  id: string;
  year: number;
  amount: number;
}

export type Budget = {
  id?: string;
  sequence: number;
  description: string;
  budgetAmount: number;
  details?: BudgetDetail[];
}

export type BudgetDetail = {
  id?: string;
  sequence: number;
  department: string;
  budgetType: string;
  projectCode?: string;
  accountNo: string;
  budget: number;
}

export type RoiPerfResult = {
  id?: string;
  sequence: number;
  performanceResultGroup: string;
  year: number;
  accountActual: number;
  accountGrowth: number;
  amountTarget: number;
  amountActual: number;
  amountRate: number;
  amountGrowth: number;
}

export type RoiLoanAndDepositSummary = {
  id?: string;
  sequence: number;
  activityDescription: string;
  amountYear1: number;
  amountYear2: number;
  amountYear3: number;
}

export type PerfSupportDataDetail = {
  id?: string;
  sequence: number;
  activityDescription: string;
  accountCountYear1: number;
  amountYear1: number;
  accountCountYear2: number;
  amountYear2: number;
}

export type PerfSupportData = {
  id?: string;
  transactionVolume?: number;
  activityDescription?: string;
  periodYear?: number;
  startMonth?: number;
  endMonth?: number;
}

export type Committee = {
  userId: string;
  groupType: CommitteeGroupType;
  committeePositionsCode?: string;
  committeePositionsName?: string;
  sequence: number;
  fullName?: string;
  fullPositionName?: string;
}

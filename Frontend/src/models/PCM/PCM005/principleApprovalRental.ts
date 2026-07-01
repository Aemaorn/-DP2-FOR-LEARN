import type { PerformanceResultGroup, RentalAnalysisType } from "@/enums/PCM005/principle";
import type { PrincipleApprovalRentalStatus, UseContractType } from "@/enums/PCM005/principleApprovalRental";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import type { comparingAttachments, EntrepreneurAttachments } from "@/models/shared/uploadFile";
import type { EnterpreneurShowData, QualificationResultDto } from "@/views/PP/models/PP006/pp006Model";
import type { DocumentVersion } from "@/models/shared/document";

export type PrincipleApprovalRentalBody = {
  id?: string;
  useContract: UseContractType;
  status: PrincipleApprovalRentalStatus;
  documentId?: string;
  isDocumentIdReplaced?: boolean;
  documentVersions?: DocumentVersion[];
  winnerDocumentId?: string;
  isWinnerDocumentIdReplaced?: boolean;
  isWinnerDocumentReplace: boolean;
  isDocumentReplace: boolean;
  winnerDocumentVersions?: DocumentVersion[];
  documentDate?: Date;
  branchLocation: string;
  rentTypeCode: string;
  rentTypeName: string;
  rentalStartDate: Date;
  rentalEndDate: Date;
  rentalDurationYear: number;
  rentalDurationMonth: number;
  rentalDurationDay: number;
  maxMonthlyRent: number;
  totalRentalAmount: number;
  expectedContractDate: Date;
  rentalLocationDetails: string;
  subDistrictCode: string;
  subDistrictName: string;
  districtCode: string;
  districtName: string;
  provinceCode: string;
  provinceName: string;
  referencePriceAmount?: number;
  analysisSummaryNpv?: number;
  analysisSummaryPaybackYearPeriod?: number;
  analysisSummaryDiscountedPaybackYearPeriod?: number;
  acceptors: ParticipantsCommitteeAcceptor[];
  assignees: ParticipantsAssignee[];
  perfSupportData?: PerfSupportData;
  perfSupportDataDetails?: PerfSupportDataDetails[];
  roiLoanAndDepositSummaries?: RoiLoanAndDepositSummaries[];
  roiPerfResults?: RoiPerfResults[];
  budgets: Budget[];
  rentalAnalyses?: RentalAnalyses[];
  entrepreneurs: Entrepreneurs[];
  hasPermission: boolean;
  comparingAttachments: comparingAttachments[];
  phoneNumber: string;
}

export type PerfSupportData = {
  id?: string;
  transactionVolume?: number;
  activityDescription?: string;
  periodYear?: number;
  startMonth?: number;
  endMonth?: number;
}

export type PerfSupportDataDetails = {
  id?: string;
  sequence: number;
  activityDescription: string;
  accountCountYear1: number;
  amountYear1: number;
  accountCountYear2: number;
  amountYear2: number;
}

export type RoiLoanAndDepositSummaries = {
  id?: string;
  sequence: number;
  activityDescription: string;
  amountYear1: number;
  amountYear2: number;
  amountYear3: number;
}

export type RoiPerfResults = {
  id?: string;
  sequence: number;
  performanceResultGroup: PerformanceResultGroup;
  year: number;
  accountActual: number;
  accountGrowth: number;
  amountTarget: number;
  amountActual: number;
  amountRate: number;
  amountGrowth: number;
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

export type Entrepreneurs = {
  id?: string;
  procurementId?: string;
  principleApprovalRentalId?: string;
  vendorId: string;
  sequence: number;
  emailSend: boolean;
  watchlistResult?: boolean;
  watchlistResultRemark?: string;
  watchlistResultAt?: Date;
  coiResult?: boolean;
  coiResultRemark?: string;
  coiResultAt?: Date;
  egpResult?: boolean;
  egpResultRemark?: string;
  egpResultAt?: Date;
  entrepreneurTaxId?: string;
  entrepreneurType?: string;
  entrepreneurName?: string;
  entrepreneurEmail?: string;
  sapBranchNumber?: string;
  shareholders?: ShareHolder[];
  details: EntrepreneursPriceDetail[];
  coiCheckerResult?: QualificationResultDto;
  watchlistCheckerResult?: QualificationResultDto;
  attachments: EntrepreneurAttachments[];
} & EnterpreneurShowData;

export type ShareHolder = {
  id?: string;
  sequence: number;
  taxId: string;
  firstName: string;
  lastName: string;
  checkType?: string;
  isDirectorOr20PctShareholder?: boolean;
  isDirector?: boolean;
  isShareholder?: boolean;
  isJuristic?: boolean;
  watchlistResult?: boolean;
  watchlistResultRemark?: string;
  watchlistResultAt?: Date;
  coiResult?: boolean;
  coiResultRemark?: string;
  coiResultAt?: Date;
  egpResult?: boolean;
  egpRemark?: string;
  egpResultAt?: Date;
  coiCheckerResult?: QualificationResultDto;
  watchlistCheckerResult?: QualificationResultDto;
  coiCheckerResults?: QualificationResultDto[];
  watchlistCheckerResults?: QualificationResultDto[];
}

export type EntrepreneursPriceDetailBody = {
  id: string;
  entrepreneursPriceDetails: EntrepreneursPriceDetail[];
} & Entrepreneurs

export type EntrepreneursPriceDetail = {
  id: string;
  sequence: number;
  parcelName: string;
  description: string;
  parcelQuantity: number;
  parcelUnitCode: string;
  vatTypeCode: string;
  offeredPrice: number;
  agreedPrice: number;
}

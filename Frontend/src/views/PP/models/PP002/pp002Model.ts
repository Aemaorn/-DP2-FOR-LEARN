import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from '@/models/shared/participants';
import type { PP002Status } from '../../enums/pp002';
import type { AcceptorType } from '@/enums/participants';
import type { DocumentVersion } from '@/models/shared/document';

export type PP002Detail = {
  id?: string;
  status: PP002Status;
  procurementId: string;
  referenceNumber: string;
  telephoneNumber: string;
  bidGuarantee: boolean;
  isStock: boolean;
  reason: string;
  torDocumentTemplateCode: string;
  evaluationCriteria?: string;
  objects?: SequenceDescription[];
  qualifications?: SequenceDescription[];
  technicalSpecifications?: TechnicalSpecifications[];
  technicalPeriods?: TechnicalPeriods[];
  budgets?: Budgets[];
  paymentTerms?: PaymentTerms[];
  warranties?: Warranties[];
  fineRates?: FineRates[];
  acceptors?: AcceptorTorDraft[];
  assignees?: ParticipantsAssignee[];
  torDraftDocumentId?: string;
  isTorDraftDocumentIdReplaced?: boolean;
  torDraftApprovalDocumentId?: string;
  isTorDraftApprovalDocumentIdReplaced?: boolean;
  isChange?: boolean;
  isCancel?: boolean;
  cancelReason?: string;
  changeReason?: string;
  isContractGuarantee: boolean;
  percentageContract?: number;
  supplyMethodTypeCode?: string;
  documentDescription?: string;
  manuelDescription?: string;
  preventiveMaintenance?: TorPreventiveMaintenanceModel;
  correctiveMaintenance?: TorCorrectiveMaintenanceModel;
  training?: TorTrainingModel;
  trainingItems?: TorTrainingItemModel[];
  impediments?: TorImpedimentModel[];
  torDocumentVersions: DocumentVersion[];
  approvalDocumentVersions: DocumentVersion[];
  documentDate?: Date;
  isMigration?: boolean;
  isMA?: boolean;
  isCM?: boolean;
  isPM?: boolean;
  isTraining?: boolean;
  isImpediment?: boolean;
  paymentTermPeriods?: TorPaymentTermPeriods[];
}

export type TorPaymentTermPeriods = {
  id?: string;
  sequence: number;
  description?: string;
  quantity?: number;
  periodTypeCode?: string;
  totalQuantity?: number;
  totalPeriodTypeCode?: string;
}

export interface TorTemplateComputerModel {
  documentDescription?: string;
  manuelDescription?: string;
  preventiveMaintenance?: TorPreventiveMaintenanceModel;
  correctiveMaintenance?: TorCorrectiveMaintenanceModel;
  training?: TorTrainingModel;
  impediments?: TorImpedimentModel[];
}

export interface TorTrainingModel {
  trainingCount?: number;
  trainingCountUnit?: string;
  trainingUnitId?: string;
}

export interface TorTrainingItemModel {
  id?: string;
  termOfRefId?: string;
  sequence: number;
  courseName?: string;
  periodDay?: number;
  place?: string;
  trainingCount?: number;
  totalPersonPerTime?: number;
}

export interface TorPreventiveMaintenanceModel {
  pmProductName?: string;
  pmCount?: number;
  pmUnit?: string;
  pmFinePct?: number;
  pmFineAmount?: number;
  condition?: string;
  disruptedCount?: number;
  disruptedCountUnit?: string;
  disruptedPercent?: number;
  disruptedFinePercent?: number;
  disruptedFineAmount?: number;
  pmFinePctUnit?: string;
}

export interface TorCorrectiveMaintenanceModel {
  cmProductName?: string;
  startDate?: Date;
  endDate?: Date;
  cmCount?: number;
  cmUnit?: string;
  cmCompleteCount?: number;
  cmCompleteUnit?: string;
  cmFinePercent?: number;
  cmDisruptedFinePercent?: number;
  dayStart?: string;
  dayEnd?: string;
  startTime?: string;
  endTime?: string;
  cmFinePercentUnit?: string;
}

export interface TorImpedimentModel {
  id?: string;
  termOfRefId?: string;
  sequence: number;
  description?: string;
  impedimentValue?: number;
}

export type SequenceDescription = {
  id?: string;
  sequence: number;
  description: string;
  isDefault?: boolean;
}

export type TechnicalSpecifications = {
  id?: string;
  sequence: number;
  name: string;
  description: string;
  quantity: number;
  unitCode: string;
}

export type TechnicalPeriods = {
  id?: string;
  deliveryConditionCode?: string;
  deliveryDate?: Date;
  period?: number;
  periodTypeCode?: string;
  periodConditionCode?: string;
  startDate: Date;
  endDate: Date;
  details: TechnicalPeriodsDetail[];
}

export type TechnicalPeriodsDetail = {
  id?: string;
  branch: string;
  personalCount: string;
  startDate: Date;
  endDate: Date;
}

export type Budgets = {
  id?: string;
  sequence: number;
  description: string;
  budgetAmount: number;
  details: BudgetsDetail[];
}

export type BudgetsDetail = {
  id?: string;
  sequence: number;
  department: string;
  budgetType: string;
  projectCode?: string;
  accountNo: string;
  budget: number;
}

export type PaymentTerms = {
  id?: string;
  proRateTypeCode?: string;
  paymentPercent?: number;
  period?: number;
  description?: string;
  periodTypeCode?: string;
  totalPeriod?: number;
  totalPeriodTypeCode?: string;
  isMA: boolean;
  details: PaymentTermsDetail[];
}

export type PaymentTermsDetail = {
  id?: string;
  termNumber: number;
  percent: number;
  period: number;
  description: string;
}

export type Warranties = {
  id?: string;
  hasWarranty: boolean;
  period?: number;
  periodTypeCode?: string;
  conditionOther?: string;
}

export type FineRates = {
  id?: string;
  sequence: number;
  description: string;
  rate: number;
  periodTypeCode: string;
  conditionCode?: string;
  conditionOther?: string;
}

export type AcceptorTorDraft = {
  employeeCode: string;
} & ParticipantsCommitteeAcceptor

export type ActionTorDraft = {
  isChange?: boolean;
  isCancel?: boolean;
  reason: string;
}

export type ApproveTor = {
  torDraftId: string;
  group: AcceptorType;
  remark: string;
}

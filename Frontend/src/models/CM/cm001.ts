import type { EWorkProcess } from "@/enums/shared";
import type { TDataTableResult, TPaginated } from "../shared/paginated";
import type { CM001PeriodStatus, CM001Status, souceType, CmDeliveryAcceptancePeriodAccountStatus } from "@/enums/CM/cm001";
import type { ParticipantsAcceptor, ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "../shared/participants";
import type { BudgetsDetail } from "@/views/PP/models/PP002/pp002Model";
import type { DocumentVersion } from "../shared/document";
import type { CommitteeInfo } from "@/views/PP/models/PP005/pp005Model";
import type { ProcurementType } from "@/enums/procurement";
import type { Attachments } from "../shared/uploadFile";
import type { TRentalDurationInfo } from "@/views/PP/models/PP0010/ContractDraft";

export type CommitteeSection = {
  committees: CommitteeInfo[];
  isCommittee: boolean;
};

export type CM001Criteria = {
  workProcess: EWorkProcess;
  keyword?: string;
  departmentCode?: string;
  contractDateStart?: Date;
  contractDateEnd?: Date;
  contractType?: Date;
  contractTypeCode?: string;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
  status?: CM001Status;
} & TPaginated;

export type PeriodRef = {
  periodId: string;
  acceptanceNumber?: string;
};

export type CM001Table = {
  id: string;
  status: CM001Status;
  refNumber?: string;
  planNumber?: string;
  planId?: string;
  refId?: string;
  poNumber?: string;
  name: string;
  vendorName?: string;
  budget: number;
  departmentName: string;
  supplyMethod: string;
  supplyMethodType?: string;
  supplyMethodSpecialType?: string;
  periodRefs: PeriodRef[];
  sourceType: souceType;
  processType?: ProcurementType;
  isCanDelete: boolean;
  glAccounts?: string[];
};

export interface CM001StatusCount {
  all: number;
  inProgress: number;
  completed: number;
};

export type CMTableResponse = {
  data: TDataTableResult<CM001Table>,
  statusCount: CM001StatusCount,
};

export type CM001Detail = {
  id?: string;
  refId?: string;
  refCode?: string;
  sourceType?: souceType;
  status: CM001Status;
  contractType?: string;
  cm001Info: CM001Info;
  periods: CM001Period[];
  canApproveDeliveryAcceptance?: boolean;
  number?: string;
  departmentId?: string;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
  name?: string;
  budget?: number;
  isCommercialMaterial?: boolean;
};

export type WarrantyInfo = {
  hasWarranty: boolean;
  warrantyConditionCode?: string;
  warrantyPeriod?: TRentalDurationInfo;
  fixingDeadlinePeriod?: TRentalDurationInfo;
  warrantyMonthlyAllowedDowntimeHours?: number;
  warrantyDowntimePercentPerMonth?: number;
  warrantyPenaltyPerHour?: number;
  downtimeResolutionHours?: number;
  downtimeResolutionDay?: number;
  repairCompletionHours?: number;
  repairCompletionDay?: number;
  repairDelayPenaltyPercentPerHour?: number;
  maxMonthlyMalfunction?: number;
  maxMonthlyMalfunctionTypeCode?: string;
  maxMonthlyMalfunctionRate?: number;
  maxMonthlyMalfunctionPenaltyPercentageRate?: number;
  maxMonthlyMalfunctionPenaltyPerHour?: number;
  maxMonthlyMalfunctionPenaltyDueDays?: number;
  warrantyStartDate?: Date;
  warrantyEndDate?: Date;
  warrantyMaintenanceCount?: number;
  warrantyMaintenanceTypeCode?: string;
  lastAcceptanceDate?: Date;
};

export interface CM001Info {
  planCode: string;
  planId?: string;
  procurementNumber?: string;
  procurementId?: string;
  departmentName: string;
  planType: string;
  vendorName?: string;
  establishmentName?: string;
  vendorEmail?: string;
  contractNumber?: string;
  poNumber?: string;
  contractBudget?: number;
  name?: string;
  contractTypeName?: string;
  templateName?: string;
  contractDate?: Date;
  period?: string;
  deliveryDate?: Date;
  supplyMethod?: string;
  supplyMethodCode?: string;
  supplyMethodType?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialType?: string;
  supplyMethodSpecialTypeCode?: string;
  budgetYear?: number;
  budget?: number;
  sourceType: string;
  createdAt: Date;
  isStock?: boolean;
  warranty?: WarrantyInfo;
};

export interface CM001Period {
  id: string;
  status: CM001PeriodStatus;
  acceptanceNumber: string;
  paymentTermNo: string;
  description: string;
  accountStatus: CmDeliveryAcceptancePeriodAccountStatus;
};

export type CM001PeriodAcceptanceInfo = {
  acceptanceDate?: Date;
  acceptanceNumber?: string;
  acceptedAmount?: number;
  description?: string;
  totalDeductions: number;
};

export type CM001PaymentTermInfo = {
  paymentTermNo?: number;
  leadTime?: number;
  deliveryDate?: Date;
  installmentPercentage?: number;
  amount?: number;
  advanceDeductionAmount?: number;
  performanceDeductionAmount?: number;
  description?: string;
};

/**
 * โมเดล สำหรับโปรแกรม ส่งมอบและตรวจรับงาน
 */
export type CM001PeriodBody = {
  id?: string;
  status: CM001PeriodStatus;
  acceptanceNumber?: string;
  documentDate?: Date;
  description?: string;
  phoneNumber?: string;
  contractBudgetAmount?: number;
  objectiveDescription?: string;
  hasDeduction: boolean;
  deductionDescription?: string;
  deductionAmount?: number;
  hasInvoiceSlip: boolean;
  invoiceSlipDescription?: string;
  invoiceSlipAmount?: number;
  paymentTerms: Cm001PaymentTerm[];
  hasJorPorAssign: boolean;
  hasEditPermission: boolean;
  acceptanceCommittees: ParticipantsCommitteeAcceptor[];
  assignees: ParticipantsAssignee[];
  acceptors: ParticipantsAcceptor[];
  documentId?: string;
  isDocumentReplaced?: boolean;
  cm001Info?: CM001Info;
  supplyMethod?: string;
  supplyMethodCode?: string;
  supplyMethodType?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialType?: string;
  supplyMethodSpecialTypeCode?: string;
  budgetDetails: BudgetsDetail[];
  isCommercialMaterial: boolean;
  departmentCode: string;
  documentVersions?: DocumentVersion[];
  inspectionCommittees: CommitteeSection;
  accountStatus: CmDeliveryAcceptancePeriodAccountStatus;
  disbursementDate?: Date;
  disbursementAmount?: number;
  disbursementRemark?: string;
  departmentOrganizationLevel?: string;
  acceptanceOfAccounting: ParticipantsAcceptor[];
  acceptanceConfirmers: ParticipantsAcceptor[];
  paymentTermOnUser: number[];
  totalPaymentOnUser: number;
  attachments: Attachments[];
}

export type Cm001PaymentTerm = {
  id: string;
  sequence: number;
  paymentTerm: number;
  description: string;
  amount: number;
}

/**
 * โมเดลส่วนบันทึกส่งมอบงานส่่วนต้น
 *
 * **ข้อสำคัญ**
 *  - วันที่ (deliveryDate) - ในแต่่ละขุดจะต้องไม่ใช่วันเดียวกัน
 */
export type Delivery = {
  id?: string;
  sequence: number;
  deliveryDate: Date;
  considerationResult: string;
  deliveryItems: Array<DeliveryItem>;
}

/**
 * โมเดลส่วนบันทึกส่งมอบงานส่วนรายละเอียด
 */
export type DeliveryItem = {
  id?: string;
  sequence: number;
  description: string;
  quantity: number;
  price: number;
  total: number;
}

export type PeriodAcceptanceInfo = {
  acceptanceDate?: Date;
  acceptanceNumber?: string;
  description?: string;
  acceptedAmount?: number;
  hasDeduction: boolean;
  acceptanceDeductionItems: AcceptanceDeductionItem[];
  totalDeductions: number;
}

export type AcceptanceDeductionItem = {
  id?: string;
  sequence: number;
  description: string;
  amount: number;
}

export type PlanAndContractVendorData = {
  id: string;
  planCode: string;
  procurementId?: string;
  departmentName: string;
  planType: string;
  vendorName?: string;
  vendorEmail?: string;
  contractNumber?: string;
  poNumber?: string;
  contractBudget?: number;
  name?: string;
  contractTypeName?: string;
  templateName?: string;
  contractDate?: Date;
  period?: string;
  deliveryDate?: Date;
  supplyMethod?: string;
  supplyMethodType?: string;
  supplyMethodSpecialType?: string;
  budgetYear?: number;
  budget?: number;
  sourceType: string;
  createdAt: Date;
  isStock?: boolean;
}

export type PlanAndContractVendorCriteria = {
  keyword?: string;
  budgetYear?: number;
  departmentCode?: string;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
  sourceType?: string;
} & TPaginated;


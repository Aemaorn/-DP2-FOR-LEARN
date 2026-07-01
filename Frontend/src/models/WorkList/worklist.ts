import type { PlanStatus } from "@/enums/plan";
import type { TDataTableResult, TPaginated } from "../shared/paginated";
import type { ProcurementPlanType, ProcurementStep } from "@/enums/procurement";
import type { PlanAnnouncementStatus } from "@/enums/planAnnouncement";
import type { PreProcurementStep } from "@/enums/preProcurement";

export type WorklistCriteria = {
  includeAll: boolean;
  includePlans: boolean;
  includeAnnouncements: boolean;
  includePreProcurement: boolean;
  includeProcurement: boolean;
  includeContractAgreement: boolean;
  includeContractManagement: boolean;
  includeContractAmendment: boolean;
  includeExpenseDisbursement: boolean;
  keyword?: string;
  departmentCode?: string;
  budgetYear?: number;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
  isPendingDepartment?: boolean;
} & TPaginated;

export type WorklistRes = {
  combined: {
    count: number;
    data: CombinedWorklistItem[];
    totalRecords: number
  },
  plans: {
    count: number;
    page: TDataTableResult<WorklistPlanItemDTO>;
  },
  planAnnouncements: {
    count: number;
    page: TDataTableResult<WorklistPlanAnnouncementItemDTO>;
  },
  preProcurement: {
    count: number;
    page: TDataTableResult<WorklistProcurementItemDTO>;
  },
  procurement: {
    count: number;
    page: TDataTableResult<WorklistProcurementItemDTO>;
  },
  contractAgreement: {
    count: number;
    page: TDataTableResult<WorklistProcurementItemDTO>;
  },
  contractManagement: {
    page: TDataTableResult<WorklistContractManagementItemDTO>;
  },
  contractAmendments: {
    page: TDataTableResult<WorklistContractAmendmentItemDTO>;
  },
  expenseDisbursements: {
    page: TDataTableResult<WorklistExpenseDisbursementItemDTO>;
  },
  counts: {
    plans: number;
    planAnnouncements: number;
    preProcurement: number;
    procurement: number;
    contractAgreement: number;
    combined: number;
    contractManagement: number;
    contractAmendments: number;
    expenseDisbursement: number;
  }
};

export type CombinedWorklistItem = {
  source: string;
  type?: string;
  id: string;
  detailId?: string;
  number: string;
  planNumber?: string;
  name?: string;
  year?: number;
  summaryBudget: number;
  planCount?: number;
  departmentName?: string;
  supplyMethodName: string;
  statusCode: string;
  step?: string;
  sortAt?: Date;
  processType?: string;
  contractDraftVendorId?: string;
  vendorName?: string;
  glAccounts?: string[];
};

export type WorklistPlanItemDTO = {
  id: string;
  planNumber: string;
  name: string;
  budgetYear: number;
  budget: number;
  type: ProcurementPlanType;
  departmentName: string;
  supplyMethod: string;
  isChange: boolean;
  isCancel: boolean;
  status: PlanStatus;
};

export type WorklistPlanAnnouncementItemDTO = {
  id: string;
  announcementNumber: string;
  announcementName?: string;
  year: number;
  planCount: number;
  summaryBudget: number;
  supplyMethodName: string;
  announcementDate?: string;
  status: PlanAnnouncementStatus;
};

export type WorklistProcurementItemDTO = {
  id: string;
  planNumber?: string;
  procurementNumber?: string;
  name: string;
  budget?: number;
  planTypeName?: string;
  departmentName: string;
  supplyMethod: string;
  supplyMethodType: string;
  supplyMethodSpecialType: string;
  rentTypeName?: string;
  step: ProcurementStep;
  processType: PreProcurementStep;
  status: string;
  period?: string;
  contractDraftVendorId?: string;
  contractName?: string;
  vendorName?: string;
};


export type WorklistContractManagementItemDTO = {
  id: string;
  detailId: string;
  refId?: string;
  contractNumber: string;
  procurementNumber?: string;
  periodNumber?: string;
  poNumber?: string;
  contractName: string;
  vendorName?: string;
  contractSignedDate?: Date;
  budget: number;
  departmentName: string;
  supplyMethod: string;
  supplyMethodType?: string;
  supplyMethodSpecialType?: string;
  status?: string;
  sourceType?: string;
  contractTypeLabel?: string;
  processType?: string;
}

export type WorklistContractAmendmentItemDTO = {
  id: string;
  camContractAmendmentNumber: string;
  type: string;
  status: string;
  remark?: string;
  contractDraftVendorId?: string;
  contractNumber: string;
  contractName: string;
  poNumber: string;
  contractSignedDate?: Date;
  budget: number;
  department: string;
  contractTypeLabel?: string;
}

export type WorklistExpenseDisbursementItemDTO = {
  id: string;
  status: string;
  sourceType: string;
  sourceId: string;
  createdDate: Date;
  date: Date;
  budget?: number;
}

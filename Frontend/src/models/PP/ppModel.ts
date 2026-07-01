import type {
  PreProcurementGroupStep,
  PreProcurementType,
  PreProcurementDialogGroupStep,
  PreProcurementStep,
} from '@/enums/preProcurement';
import type { ProcurementStatus, ProcurementStep, ProcurementType } from '@/enums/procurement';
import type { EWorkProcess } from '@/enums/shared';
import type { TDataTableResult, TPaginated } from '@/models/shared/paginated';
import type { Attachments } from '../shared/uploadFile';
import type { pp008CommitteeType } from '@/views/PP/enums/pp008';

export type TPreProcurementTypeBadge = {
  value: string;
  bgColorClass: string;
  textColorClass: string;
};

// #region Commitee Section Using in PP001 & PP004 && PP005 && PP008
export type TCommitteeSection = {
  id?: string,
  fullName?: string,
  positionCode?: string,
  fullPositionName?: string,
  departmentCode?: string,
  departmentName?: string,
  userId: string,
  committeePositionsCode: string,
  sequence: number,
};

export type TDutySection = {
  id?: string,
  description: string,
  sequence: number,
};

export type TCommitteeDutyList = {
  committeeSection: TCommitteeSection[];
  dutySection: TDutySection[];
};
// #endreion

// #region Shared
export type TBudgetProcurement = {
  summaryBudget: number;
  groupDetail: TBudgetProcurementGroupDetail[];
};

export type TBudgetProcurementGroupDetail = {
  sequence: number;
  description: string;
  budget: number;
  detail: TBudgetProcurementDetail[];
};

export type TBudgetProcurementDetail = {
  sequence: number;
  agency: string;
  budgetType: string;
  projectCode?: string;
  accNo: string;
  budget: number;
};

export type PPContractInfo = {
  establishmentName: string;
  email: string;
  contractNumber: string;
  pONumber: string;
  contractAmount: number;
  contractName: string;
  contractType: string;
  contractTemplate: string;
  contractDate: Date;
  deliveryDueDays: number;
  deliveryDueDate: Date;
  warrantyDate: string;
}
// #endregion

// #region PreProcurement
export type TPreProcurementCriteria = {
  workProcess?: EWorkProcess;
  keyword?: string;
  departmentCode?: string;
  budgetYear?: number;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
  step?: PreProcurementGroupStep;
  procurementType: ProcurementType;
  statusCode?: string;
  planId?: string;
} & TPaginated;

export type TPreProcurement = {
  id: string;
  planId?: string;
  procurementNumber?: string;
  planNumber?: string;
  name?: string;
  budget: number;
  type: PreProcurementType;
  departmentName: string;
  departmentCode: string;
  supplyMethod: string;
  supplyMethodType: string;
  supplyMethodSpecialType: string;
  rentTypeName?: string;
  groupStep?: PreProcurementGroupStep;
  step: PreProcurementStep;
  status: string;
  procurementStatus: ProcurementStatus;
  period?: string;
  isCancel: boolean;
  isChange: boolean;
  canDelete: boolean;
  processType: PreProcurementStep;
  budgetYear?: number;
};

export type TPreProcurementList = {
  all: number,
  preProcurement: number,
  procurement: number,
  contractAgreement: number,
  contractManagement: number,
  data: TDataTableResult<TPreProcurement>,
}

export type TPreProcurementGroupStepCount = {
  all: number;
  preProcurement: number;
  procurement: number;
  contractAgreement: number;
  contractManagement: number;
};

export type PurchaseOrderApprovalCommittee = {
  id?: string;
  groupType: pp008CommitteeType;
  departmentCode: string;
  suUserId: string;
  fullName: string;
  committeePositionsCode: string;
  committeePositionsName: string;
  fullPositionName: string;
  sequence: number;
};

export type TPreProcurementDetail = {
  id?: string;
  planId?: string;
  planNumber?: string;
  procurementType: ProcurementType;
  procurementStep: ProcurementStep;
  departmentName: string;
  departmentCode: string;
  departmentOrganizationLevel?: string;
  planName?: string;
  planType?: string;
  planbudget: number;
  budget: number;
  budgetYear?: number;
  supplyMethod?: string;
  supplyMethodCode?: string;
  supplyMethodType?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialType?: string;
  supplyMethodSpecialTypeCode?: string;
  status: ProcurementStatus;
  procurementStatus: ProcurementStatus;
  expectingProcurementAt: Date;
  isStock: boolean;
  isCommercialMaterial: boolean;
  appoint?: ProcessDto;
  torDraft?: ProcessDto;
  medianPrice?: ProcessDto;
  invite?: ProcessDto;
  jp005?: ProcessDto;
  purchaseRequisition?: ProcessDto;
  purchaseOrder?: ProcessDto;
  purchaseOrderApproval?: ProcessDto;
  principleApproval?: ProcessDto;
  principleApprovalRental?: ProcessDto;
  contractInvitation?: ProcessDto;
  contractDraft?: ProcessDto;
  currentStep: PreProcurementStep;
  steps: Array<PreProcurementStep>;
  hasMd: boolean;
  procurementNumber?: string;
  createdBy?: string;
  attachments: Array<Attachments>;
  appointNumber?: string;
  contractType?: string;
  remarkClosed?: string | null;
  lastStatusBeforeClosed?: string | null;
};

export type ProcessDto = {
  id: string;
  status: string;
}
// #endregion

// #region PreProcurementDialog
export type TPreProcurementDialogCriteria = {
  keyword?: string;
  departmentCode?: string;
  budgetYear?: number;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
  type: PreProcurementType;
  groupStep?: PreProcurementDialogGroupStep;
} & TPaginated;

export type TPreProcurementDialog = {
  procurementId?: string;
  id: string;
  planNumber: string;
  planName?: string;
  procurementNumber?: string;
  procurementType?: string;
  processType?: PreProcurementStep;
  budget: number;
  budgetYear: number;
  type: PreProcurementType;
  departmentName: string;
  departmentCode: string;
  supplyMethod: string;
  supplyMethodCode: string;
  supplyMethodType: string;
  supplyMethodTypeCode: string;
  supplyMethodSpecialType: string;
  supplyMethodSpecialTypeCode: string;
  status: ProcurementStatus;
  expectingProcurementAt: Date;
  isStock: boolean;
  isCommercialMaterial: boolean;
};

export type TPreProcurementDialogGroupStepCount = {
  all: number;
  supplyMethodCode60: number;
  supplyMethodCode80: number;
};
// #endregion
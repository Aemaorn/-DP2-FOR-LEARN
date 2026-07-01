import type { ConsiderOverPriceType, PreProcurementCommitteeType } from "@/enums/preProcurement";
import type { EDateType } from "@/enums/shared";
import type { Budgets, SequenceDescription, TechnicalSpecifications } from "../PP002/pp002Model";
import type { pp004CommitteeType, pp004status } from "../../enums/pp004";
import type { ParticipantsAcceptor, ParticipantsAssignee } from "@/models/shared/participants";
import type { DocumentVersion } from "@/models/shared/document";

export type TP004TorInfo = {
  sequence: number;
  description: string;
};

export type TDeliverPaymentCondition = {
  budget: number;
  date: Date;
  startType: string;
}

export type TPaymentCondition = {
  sequence: number;
  periodNo: number;
  amountPercentage: number;
  amountDate: number;
  remark: string;
};

export type TProcessDeliverPeriod = {
  delivery: number;
  dateType: EDateType;
  startType: string;
};

export type TFineRate = {
  isFineRate: boolean;
  detail: TFineRateDetail[];
};

export type TFineRateDetail = {
  sequence: number;
  exampleDescription: string;
  description: string;
  percentage: number;
  dateType: EDateType;
};

export type TWarrantyDefect = {
  isWarranty: boolean;
  period: number;
  dateType: EDateType;
  startType: string;
};

export type TPP004Procurement = {
  committeeType: PreProcurementCommitteeType;
  detail: TPP004UserListDetail[];
};

export type TPP004UserList = {
  detail: TPP004UserListDetail[];
};

export type TPP004UserListCondition = {
  isHas: boolean;
} & TPP004UserList;

export type TPP004UserListDetail = {
  sequence: number;
  name: string;
  position: string;
  positionProcurement: string;
};

export type JorPor04Request = {
  id?: string;
  procurementId: string;
  torDraftId?: string;
  userId?: string;
  requisition: JorPor04Requisition;
  budgets: Budgets[];
  warranties: JorPor04Warranty[];
  paymentTerms: JorPor04PaymentTerm[];
  paymentTypeCode?: string;
  fineRates: JorPor04FineRate[];
  committees: JorPor04Committee[];
  acceptors: ParticipantsAcceptor[];
  assignees: ParticipantsAssignee[];
  scopeOfWorks: TechnicalSpecifications[];
  isProcurementCommittee: boolean;
  isInspectCommittee: boolean;
  isMaCommittee: boolean;
  isSupCommittee: boolean;
  reason?: string;
  torObjectResponses: SequenceDescription[];
  hasPermission: boolean;
  departmentCode?: string;
  supplyMethodCode?: string;
  isCommercialMaterial?: boolean;
  budget?: number;
  documentVersions?: DocumentVersion[];
  torTemplateCode?: string;
};

export type JorPor04Requisition = {
  purchaseRequisitionNumber: string;
  documentDate?: Date;
  egpNumber: string;
  prNumber: string;
  telephone: string;
  description: string;
  priceReasonablenessInfo: ConsiderOverPriceType;
  medianPriceAmount: number;
  evaluationCriteriaCode: string;
  deliveryPeriod: number;
  deliveryDate: Date;
  deliveryPeriodTypeCode: string;
  deliveryConditionCode: string;
  hasFineRate: boolean;
  hasWarranty: boolean;
  warrantyPeriod: number;
  warrantyPeriodCode: string;
  warrantyConditionCode: string;
  hasContractGuarantee: boolean;
  hasInspectionCommittee: boolean;
  hasConstructionSupervisor: boolean;
  status: pp004status;
  purchaseRequisitionDocumentId?: string;
  isPurchaseRequisitionDocumentIdReplaced?: boolean;
};

export type JorPor04Warranty = {
  id?: string;
  hasWarranty: boolean;
  period: number;
  periodTypeCode: string;
  conditionOther: string;
};

export type JorPor04PaymentTerm = {
  id?: string;
  termNumber: number;
  percent: number;
  period: number;
  description: string;
  sequence: number;
  periodTypeCode?: string;
  totalPeriod?: number;
  totalPeriodTypeCode?: string;
  isMA: boolean;
  paymentTypeCode: string;
};

export type JorPor04FineRate = {
  id?: string;
  sequence: number;
  percentage: number;
  periodTypeCode: string;
  conditionCode: string;
  conditionOther: string;
};

export type JorPor04Committee = {
  id?: string;
  groupType: pp004CommitteeType;
  departmentCode: string;
  suUserId: string;
  fullName: string;
  committeePositionsCode: string;
  committeePositionsName: string;
  positionName: string;
  sequence: number;
};

export type JorPor04Acceptor = {
  type: number;
  userId: string;
  employeeCode: string;
  fullName: string;
  positionName: string;
  businessUnitName: string;
  sequence: number;
  status: number;
};

export type JorPor04Assignee = {
  userId: string;
};

export type JorPor04SendAction = {
  id: string;
  remarks: string;
};
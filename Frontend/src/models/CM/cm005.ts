import type { CmContractTerminationStatus } from "@/enums/CM/cm005";
import type { souceType } from "@/enums/CM/cm001";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "../shared/participants";
import type { TDataTableResult, TPaginated } from "../shared/paginated";
import type { EWorkProcess } from "@/enums/shared";
import type { DocumentVersion } from "../shared/document";
import type { Attachments } from "../shared/uploadFile";

export type Cm005ListCriteria = {
  keyword?: string;
  contractSignedDate?: Date;
  contractType?: string;
  status?: CmContractTerminationStatus;
  workProcess?: EWorkProcess;
} & TPaginated

export type GetCm005ListResponse = {
  data: TDataTableResult<Cm005ListResponse>;
  statusCount: StatusCount;
}

export type Cm005ListResponse = {
  procurementId: string;
  contractId: string;
  id: string;
  procurementType: string;
  contractNumber: string;
  poNumber: string;
  terminateTypeName: string;
  contractSignedDate: string;
  entrepreneurName: string;
  contractName: string;
  budget: number;
  contractTypeName?: string;
  status: CmContractTerminationStatus;
};

export type StatusCount = {
  all: number;
  draft: number;
  waitingCommitteeApproval: number;
  assigned: number;
  waitingAcceptance: number;
  approved: number;
  rejected: number;
}

export type Cm005Detail = {
  id?: string;
  taxId: string;
  entrepreneurName: string;
  entrepreneurEmail: string;
  contractNumber: string;
  poNumber: string;
  budget: number;
  contractName: string;
  contractType: string;
  contractTemplate: string;
  contractSignedDate: string;
  deliveryLeadTime: number;
  deliveryLeadTimeTypeCode: string;
  deliveryLeadTimeTypeLabel: string;
  deliveryDate: Date;
  supplyMethodCode: string;
  contractTermination: ContractTermination;
}

export type ContractVendorListCriteria = {
  keyword?: string;
  budgetYear?: number;
  departmentCode?: string;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
  sourceType?: number;
} & TPaginated

export type ContractVendorData = {
  id: string;
  planCode: string;
  departmentName: string;
  planType?: string;
  vendorName?: string;
  vendorEmail?: string;
  contractNumber?: string;
  poNumber?: string;
  contractBudget?: number;
  name?: string;
  contractTypeName?: string;
  templateName?: string;
  contractDate?: string;
  period?: string;
  deliveryDate?: string;
  supplyMethod?: string;
  supplyMethodType?: string;
  supplyMethodSpecialType?: string;
  budgetYear?: number;
  budget?: number;
  sourceType: souceType;
  isStock: boolean;
  createdAt: string;
  procurementId?: string;
}

export type ContractTermination = {
  id?: string;
  terminationDate?: Date;
  terminateType?: string;
  terminateReason?: string;
  terminateReasonOther?: string;
  terminateReasonDetail?: string;
  isProposedApprover: boolean;
  status: CmContractTerminationStatus;
  contractTerminationDocumentId?: string;
  isContractTerminationDocumentIdReplace?: boolean;
  acceptors: ParticipantsCommitteeAcceptor[];
  assignees: ParticipantsAssignee[];
  documentVersions?: DocumentVersion[];
  attachments: Attachments[];
}

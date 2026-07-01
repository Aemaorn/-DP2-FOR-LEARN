import type { ParticipantsAcceptor, ParticipantsAssignee } from "@/models/shared/participants";
import type { PP008Status } from "../../enums/pp008";
import type { TNationalityType, TVendorType } from "@/models/ST/st003";
import type { PurchaseOrderApprovalCommittee } from "@/models/PP/ppModel";

export type PP008Detail = {
  id?: string;
  contractType: string;
  status: PP008Status;
  acceptors: ParticipantsAcceptor[];
  assignees: ParticipantsAssignee[];
  contractBudgetGroups?: ContractGroup[];
  contracts?: ContractCreate[];
  hasPermission?: boolean;
  entrepreneurs?: Entrepreneurs[];
  budgets: Budgets[];
  purchaseRequisitionId?: string;
  procurementBudget: number;
  committees: PurchaseOrderApprovalCommittee[];
  isInspectCommittee: boolean;
  purchaseRequisitionAssignees?: ParticipantsAssignee[];
}

export type ContractGroup = {
  budgetId: string;
  budgetSequence: number;
  budgetDescription: string;
  budget: number;
  contracts: Contract[];
}

export type Contract = {
  id?: string;
  sequence: number;
  purchaseOrderEntrepreneurId?: string;
  principleApprovalRentalEntrepreneursId?: string;
  purchaseOrderApprovalEntrepreneursId?: string;
  purchaseOrderEntrepreneurName?: string;
  purchaseOrderEntrepreneurEmail?: string;
  contractNumber?: string;
  hasEditContractNumber?: boolean;
  agreedPrice?: number;
  poNumber?: string;
  committeeType?: string;
  entrepreneurs?: Entrepreneurs;
  vendorId?: string;
  budget: number;
}

export type ContractCreate = {
  torDraftBudgetId?: string;
  principleApprovalRentalBudgetId?: string;
  purchaseOrderApprovalBudgetId?: string;
} & Contract

export type Entrepreneurs = {
  id?: string;
  vendorId?: string;
  sequence: number;
  emailSend: boolean;
}

export type Shareholder = {
  id: string;
  sequence: number;
  taxId: string;
  firstName: string;
  lastName: string;
  isDirectorOr20PctShareholder: boolean;
  watchlistResult?: boolean;
  watchlistResultRemark: string;
  watchlistResultAt: Date;
  coiResult?: boolean;
  coiResultRemark: string;
  coiResultAt: Date;
  egpResult?: boolean;
  egpRemark: string;
  egpResultAt: Date;
};

export type EnterpreneurShowData = {
  type: TVendorType;
  entrepreneurType?: string;
  entrepreneurTypeLabel?: string;
  sapVendorNumber: string;
  sapBranchNumber: string;
  nationality: TNationalityType;
  tel?: string;
}

export type Budgets = {
  id?: string;
  sequence: number;
  description: string;
  budgetAmount: number;
}
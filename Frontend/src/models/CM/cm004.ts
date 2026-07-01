import type { CmDisbursementApprovalStatus } from "@/enums/CM/cm004";
import type { ParticipantsAcceptor, ParticipantsAssignee } from "../shared/participants";
import type { TPaginated } from "../shared/paginated";
import type { EWorkProcess } from "@/enums/shared";
import type { Attachments } from "../shared/uploadFile";
import type { BudgetsDetail } from "@/views/PP/models/PP002/pp002Model";
import type { DocumentVersion } from "../shared/document";

export type Cm004Criteria = {
  keyword?: string;
  departmentCode?: string;
  startContractSignedDate?: Date;
  endContractSignedDate?: Date;
  status?: string;
  workProcess?: EWorkProcess;
} & TPaginated;

export type Cm004Detail = {
  id: string;
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
  deliveryDate: string;
  disbursementApprovals: Cm004DisbursementApprovals[];
  isCompleted: boolean;
}

export type Cm004DisbursementApprovals = {
  id: string;
  contractDraftVendorId: string;
  requestDate: Date;
  subject: string;
  description?: string;
  status: CmDisbursementApprovalStatus;
  netClaimAmount: number;
}

export type Cm004DisbursementBody = {
  id?: string;
  requestDate: Date;
  subject: string;
  description: string;
  netClaimAmount: number;
  status: CmDisbursementApprovalStatus;
  documentId?: string;
  isDocumentReplaced?: boolean;
  acceptors: ParticipantsAcceptor[];
  assignees: ParticipantsAssignee[];
  attachments: Attachments[];
  installments?: InstallmentRequest[];
  budgetDetails: BudgetsDetail[];
  documentVersions?: DocumentVersion[];
}

export type InstallmentRequest = {
  id?: string;
  cmDeliveryAcceptancePeriodId?: string;
  installmentNo?: number;
  migoNumber?: string;
  receiveDate?: Date;
  amount?: number;
  deliveryDetail?: DeliveryItemDto[];
  isSelected?: boolean;
}

export type DeliveryItemDto = {
  deliveryDate: Date;
  considerationResult: string;
  items: Items;
}

export type Items = {
  description: string;
  quantity: number;
  price: number;
  total: number;
}

export type Calculate = {
  amount: number;
  warranty: number;
  advanceBudget: number;
  fine: number;
}

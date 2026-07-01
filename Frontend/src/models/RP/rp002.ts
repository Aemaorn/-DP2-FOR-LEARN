import type { RP002Status } from "@/enums/RP/rp002";
import type { TDataTableResult, TPaginated } from "../shared/paginated";
import type { ParticipantsCommitteeAcceptor } from "../shared/participants";
import type { Attachments } from "../shared/uploadFile";
import type { DocumentVersion } from "../shared/document";

export type RP002Criteria = {
  keyword?: string;
  year?: number;
  quarter?: number;
  status?: RP002Status;
} & TPaginated;

export type RP002List = {
  id: string;
  documentNumber: string;
  year: number;
  quarter: number;
  documentDate: Date;
  signStartDate: Date;
  signEndDate: Date;
  status: RP002Status;
  detailCount: number;
  totalAmount: number;
};

export type RP002StatusCount = {
  all: number;
  draft: number;
  edit: number;
  waitingApproval: number;
  approved: number;
  rejected: number;
};

export interface RP002ListResponse {
  statusCount: RP002StatusCount,
  data: TDataTableResult<RP002List>;
};

export type RP002Body = {
  id?: string;
  status: RP002Status;
  documentDate: Date;
  year: number;
  quarter: number;
  signStartDate: Date;
  signEndDate: Date;
  detail?: RP002ContractComplete[];
  acceptors?: ParticipantsCommitteeAcceptor[];
  documentId?: string;
  isDocumentReplace?: boolean;
  attachments: Attachments[];
  documentVersions?: DocumentVersion[];
}

export type RP002ContractComplete = {
  id?: string;
  sequence?: number;
  contractTypeCode: string;
  contractTypeName: string;
  contractNumber: string;
  contractName: string;
  budget: number;
  entrepreneurName: string;
  contractSignedDate: Date;
  caContractDraftVendorId?: string;
  procurementId?: string;
  description?: string;
  overdue?: boolean;
}

export type RP002Summary = {
  quarter: number;
  contractCount: number;
  contractTypeCode: string;
  contractTypeName: string;
  percentComplete: number;
}


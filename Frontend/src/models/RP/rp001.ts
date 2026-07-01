import type { ContractType, rp001Status } from "@/enums/RP/rp001";
import type { ParticipantsAcceptor } from "../shared/participants";
import type { TDataTableResult, TPaginated } from "../shared/paginated";
import type { StatusCount } from "../shared/status";
import type { ProcurementWorkProcess } from "@/enums/procurement";
import type { Attachments } from "../shared/uploadFile";
import type { DocumentVersion } from "../shared/document";

export type rp001Body = {
  id?: string;
  documentNumber?: string;
  auditReportDocumentId?: string,
  isAuditReportDocumentIdReplaced?: boolean,
  auditGeneralReportDocumentId?: string,
  isAuditGeneralReportDocumentIdReplaced?: boolean,
  revenueReportDocumentId?: string,
  isRevenueReportDocumentIdReplaced?: boolean,
  documentDate: Date;
  signStartDate: Date;
  signEndDate: Date;
  deliveryDate: Date;
  status: rp001Status;
  details: rp001Detail[];
  approvalAcceptors: ParticipantsAcceptor[];
  hasPermission: boolean;
  attachments: Attachments[];
  auditReportDocumentVersions?: DocumentVersion[];
  auditGeneralReportDocumentVersions?: DocumentVersion[];
  revenueReportDocumentVersions?: DocumentVersion[];
};

export type rp001List = {
  id: string;
  docurementNumber: string;
  docurementDate: Date;
  signStartDate: Date;
  signEndDate: Date;
  deliveryDate: Date;
  status: rp001Status;
  detailCount: number;
  totalAmount: number;
}

export type rp001ListResponse = {
  statusCount: StatusCount;
  data: TDataTableResult<rp001List>;
};

export type rp001Criteria = {
  keyword?: string;
  documentDate?: Date;
  signStartDate?: Date;
  signEndDate?: Date;
  status?: rp001Status;
  workProcess: ProcurementWorkProcess;
} & TPaginated;

export type rp001Detail = {
  id?: string;
  caContractDraftVendorId: string;
  contractTypeCode: ContractType;
  contractTypeName: string;
  contractNumber: string;
  contractName: string;
  ContractSignedDate: Date;
  entrepreneurName: string;
  budget: number;
  overdue: boolean;
  sequence: number;
  description?: string;
  procurementId?: string
};
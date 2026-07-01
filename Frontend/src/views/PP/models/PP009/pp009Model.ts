import type { TNationalityType, TVendorType } from "@/models/ST/st003";
import type { PP009Status } from "../../enums/pp009";
import type { ParticipantsAcceptor } from "@/models/shared/participants";
import type { QualificationResultDto, Shareholder } from "../PP006/pp006Model";
import type { EntrepreneurAttachments, OnlyFileAttachment } from "@/models/shared/uploadFile";
import type { DocumentVersion } from "@/models/shared/document";

export type PP009Detail = {
  id?: string;
  procurementId: string;
  vendors: Array<VendorInfo>;
  status: PP009Status;
  acceptors: Array<ParticipantsAcceptor>;
  hasEditPermission: boolean;
  hasPermissionUserId?: string;
  isDocumentReplace?: boolean;
};

export type VendorInfo = {
  id?: string;
  purchaseOrderApprovalContractId: string;
  documentId?: string;
  isDocumentIdReplace?: boolean;
  documentVersions?: DocumentVersion[];
  vendorName: string;
  documentDate?: Date;
  email: string;
  contractName: string;
  poNumber: string;
  contractNumber?: string;
  agreedPrice: number;
  hasContractGuarantee: boolean;
  contractGuaranteePercent?: number;
  guaranteeAmount?: number;
  documentTemplateCode: string;
  contractOfficerName: string;
  contractOfficerPhone: string;
  contractOfficerEmail: string;
  egpResult?: boolean;
  egpRemark?: string;
  egpDate?: Date;
  coiResult?: boolean;
  coiRemark?: string;
  coiDate?: Date;
  watchlistResult?: boolean;
  watchlistRemark?: string;
  watchlistDate?: Date;
  entrepreneur?: Entrepreneur;
  coiCheckerResult?: QualificationResultDto;
  watchlistCheckerResult?: QualificationResultDto;
  shareholder: Shareholder[];
  attachments: EntrepreneurAttachments[];
  emailSend?: string;
  emailTemplate?: string;
  emailAttachments?: OnlyFileAttachment[];
  budgetDetail: string;
};

interface Entrepreneur {
  id?: string;
  nationality: TNationalityType;
  type: TVendorType;
  entrepreneurType: string;
  entrepreneurTypeName: string;
  taxpayerIdentificationNo: string;
  establishmentName: string;
  tel?: string;
  fax?: string;
  sapVendorNumber: string;
  sapBranchNumber: string;
  email: string;
};

export type VenderEgp = {
  egpResult: boolean;
  egpRemark?: string;
  egpDate?: Date;
}
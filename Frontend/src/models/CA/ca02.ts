import type { CA02Status } from "@/enums/CA/ca02";
import type { TDataTableResult, TPaginated } from "../shared/paginated";
import type { EWorkProcess } from "@/enums/shared";
import type { ParticipantsCommitteeAcceptor } from "../shared/participants";
import type { DocumentVersion } from "../shared/document";
import type { Attachments } from "../shared/uploadFile";

export type InspectionCommitteeSection = {
  committees: ParticipantsCommitteeAcceptor[];
  isCommittee: boolean;
};

// #region List Page
export type TCA02Criteria = {
  keyword?: string;
  status?: CA02Status;
  workProcess: EWorkProcess;
} & TPaginated;

export type TCA02List = {
  id: string;
  contractDraftVendorId: string;
  certificateNo: string;
  poNumber: string;
  contractName: string;
  budget: number;
  contractSignedDate: Date;
  vendorCode: string;
  vendorName: string;
  status: CA02Status;
};

export type TCA02StatusCount = {
  all: number;
  draft: number;
  waitingForCommitteeApproval: number;
  approved: number;
  rejected: number;
  edit: number;
  cancelled: number;
};

export type GetCA02ListResponse = {
  data: TDataTableResult<TCA02List>;
  statusCount: TCA02StatusCount;
};

// #endregion

// #region Detail Page

// #region Dialog
export type TCA02DialogCriteria = {
  keyword?: string;
} & TPaginated;

export type TCA02DialogTable = {
  id: string;
  planId: string;
  dpNumber: string;
  contractDraftVendorId: string;
  contractNumber: string;
  poNumber: string;
  contractSignedDate: Date;
  entrepreneurCode: string;
  entrepreneurName: string;
  contractName: string;
  budget: number;
  contractTypeCode: string;
  contractTypeLabel: string;
};

// #endregion

// #region Detail
export interface CA02Body {
  id?: string;
  status: CA02Status;
  isManual?: boolean;
  certificateNo?: string;
  receiveDate?: Date;
  sbsDocumentNo?: string;
  documentDate?: Date;
  issuedDate?: Date;
  requestReason?: string;
  acceptors: Array<ParticipantsCommitteeAcceptor>;
  inspectionCommittees: InspectionCommitteeSection;
  contractVendorInfo: CA02ContractVendorInfo;
  deliveryAcceptancePeriodInfo: Array<CA02DeliveryAcceptancePeriodInfo>;
  documentId: string | undefined;
  isReplace: boolean | undefined;
  documentVersions?: DocumentVersion[];
  attachments: Attachments[];
  isResetDocument: boolean | undefined;
}

export type CA02DeliveryAcceptancePeriodInfo = {
  id: string;
  periodId: string;
  status: string;
  sequence: number;
  deliveryDate: string;
  leadTime: number;
  installmentPercentage: number;
  amount: number;
  deliveryAcceptanceDate: string;
  deliveries: Array<CA02Delivery>;
}

export type CA02Delivery = {
  sequence: number;
  deliveryDate: string;
  deliveryItems: Array<CA02DeliveryItem>;
}

export type CA02DeliveryItem = {
  sequence: number;
  description: string;
  quantity: number;
  price: number;
  total: number;
}

export type CA02ContractVendorInfo = {
  id: string;
  entrepreneur: CA02Entrepreneur;
  contractNumber: string;
  poNumber: string;
  budget: number;
  contractName: string;
  contractTypeCode: string;
  contractTypeLabel: string;
  templateCode: string;
  templateLabel: string;
  contractSignedDate: string;
  deliveryLeadTime: number;
  deliveryLeadTimeTypeCode: string;
  deliveryLeadTimeTypeLabel: string;
  deliveryDate: string;
  contractEndDate: string;
  isManual: boolean;
  entrepreneurCode: string;
  entrepreneurName: string;
  entrepreneurId: string;
  entrepreneurEmail: string;
  supplyMethodCode?: string;
  supplyMethodLabel?: string;
  supplyMethodTypeCode?: string;
  supplyMethodTypeLabel?: string;
  supplyMethodSpecialTypeCode?: string;
  supplyMethodSpecialTypeLabel?: string;
}

export type CA02Entrepreneur = {
  code: string;
  name: string;
  email: string;
}
// #endregion

// #endregion
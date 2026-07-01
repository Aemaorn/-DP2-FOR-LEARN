import type { EWorkProcess } from "@/enums/shared";
import type { TPaginated } from "../shared/paginated";
import type { Cm007Status } from "@/enums/CM/cm007";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "../shared/participants";
import type { DocumentVersion } from "../shared/document";
import type { Attachments } from "../shared/uploadFile";
import type {
  TBuyerInfo,
  TVendor,
  TAgreementBase,
  TPaymentBase,
  TDeliveryInfo,
  TTerminationInfo,
  TGuaranteeInfo,
  TPenaltyInfo,
  TWarrantyInfo,
  TAdvancePayment,
  TRetentionPayment,
  TRedelivery,
  TCopierLeaseInfo,
  TComputerLeaseInfo,
  TCarLeaseInfo,
} from "@/views/PP/models/PP0010/ContractDraft";

export type Cm007DialogCriteria = {
  keyword?: string;
  budgetYear?: number;
  departmentCode?: string;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
} & TPaginated;

export type Cm007DialogItem = {
  id: string;
  contractDraftNumber: string;
  contractNumber: string;
  poNumber: string;
  contractName: string;
  entrepreneurName: string;
  taxId: string;
  budget: number;
  contractSignedDate?: Date;
  contractEndDate?: Date;
  templateCode?: string;
  supplyMethodName?: string;
  departmentName?: string;
};

export type Cm007Criteria = {
  workProcess: EWorkProcess;
  keyword?: string;
  departmentCode?: string;
  signedDate?: Date;
  contractTypeCode?: string;
  status?: Cm007Status;
} & TPaginated;

export type Cm007List = {
  id: string;
  contractDraftVendorId: string;
  contractNumber: string;
  poNumber: string;
  contractSignedDate?: Date;
  contractName: string;
  budget: number;
  contractTypeLabel?: string;
  status: Cm007Status;
  departmentName: string;
  supplyMethodName?: string;
  isCanDelete: boolean;
};

export type Cm007Component = {
  id?: string;
  componentCode: string;
  componentName: string;
  isEdited: boolean;
};

export type Cm007AttachmentFile = {
  id?: string;
  fileId: string;
  fileName: string;
  fileType?: string;
  sequence: number;
};

export type Cm007Attachment = {
  id?: string;
  typeCode?: string;
  description?: string;
  pageNumber?: number;
  sequence: number;
  formatOtherName?: string;
  files: Cm007AttachmentFile[];
};

export type Cm007Shareholder = {
  id?: string;
  sequence: number;
  taxId?: string;
  firstName?: string;
  lastName?: string;
  isDirectorOr20PctShareholder: boolean;
};

// Old data contains only section-level data (mirrors ContractDraftDetailBase)
export type Cm007OldData = {
  poNumber?: string;
  budget?: number;
  contractSignedDate?: Date;
  contractEndDate?: Date;
  buyer?: TBuyerInfo;
  vendor?: TVendor;
  agreement?: TAgreementBase;
  payment?: TPaymentBase;
  delivery?: TDeliveryInfo;
  termination?: TTerminationInfo;
  defectWarrantyTypeCode?: string;
  warranty?: TWarrantyInfo;
  penalty?: TPenaltyInfo;
  guarantee?: TGuaranteeInfo;
  advancePayment?: TAdvancePayment;
  retentionPayment?: TRetentionPayment;
  redelivery?: TRedelivery;
  copierLease?: TCopierLeaseInfo;
  computerLease?: TComputerLeaseInfo;
  carLease?: TCarLeaseInfo;
};

export type Cm007CreateRequest = {
  contractDraftVendorId: string;
  components?: Cm007Component[];
  acceptors?: any[];
  assignees?: any[];
};

export type Cm007Detail = {
  id: string;
  contractDraftVendorId: string;
  procurementId: string;
  status: Cm007Status;
  documentDate?: Date;
  // Contract info
  email: string;
  title?: string;
  description?: string;
  contractName: string;
  poNumber: string;
  contractDraftNumber: string;
  contractNumber: string;
  budget: number;
  contractSignedDate?: Date;
  contractEndDate?: Date;
  supplyMethodCode?: string;
  supplyMethodSpecialTypeCode?: string;
  contractTypeCode?: string;
  contractTypeLabel?: string;
  templateCode?: string;
  templateLabel?: string;
  templateText?: string;
  subTemplateCode?: string;
  subTemplateText?: string;
  startDate?: Date;
  endDate?: Date;
  isWorkingDayOnly: boolean;
  vendorAppointmentMemoDate?: Date;
  periodConditionTypeCode?: string;

  // Section data (editable, mirrors ContractDraftDetailBase)
  buyer?: TBuyerInfo;
  vendor?: TVendor;
  agreement?: TAgreementBase;
  payment?: TPaymentBase;
  delivery?: TDeliveryInfo;
  termination?: TTerminationInfo;
  defectWarrantyTypeCode?: string;
  warranty?: TWarrantyInfo;
  penalty?: TPenaltyInfo;
  guarantee?: TGuaranteeInfo;
  advancePayment?: TAdvancePayment;
  retentionPayment?: TRetentionPayment;
  redelivery?: TRedelivery;
  copierLease?: TCopierLeaseInfo;
  computerLease?: TComputerLeaseInfo;
  carLease?: TCarLeaseInfo;

  // Qualification check
  egpResult?: boolean;
  egpRemark?: string;
  egpDate?: Date;
  coiResult?: boolean;
  coiRemark?: string;
  coiDate?: Date;
  watchlistResult?: boolean;
  watchlistRemark?: string;
  watchlistDate?: Date;
  contractStatus: string;

  // Participants
  assignees: ParticipantsAssignee[];
  acceptors: ParticipantsCommitteeAcceptor[];

  // Collections
  components: Cm007Component[];
  attachments: Cm007Attachment[];
  shareholders: Cm007Shareholder[];
  fileAttachments: Attachments[];

  // Flags
  isPurchaseOrderApprovalAssignee?: boolean;

  // Old data
  oldData?: Cm007OldData;

  // Documents
  amendmentDocumentId?: string;
  isAmendmentDocumentIdReplaced?: boolean;
  amendmentApprovalRequestDocumentId?: string;
  isAmendmentApprovalRequestDocumentIdReplaced?: boolean;
  amendmentDocumentVersions?: DocumentVersion[];
  approvalRequestDocumentVersions?: DocumentVersion[];
};

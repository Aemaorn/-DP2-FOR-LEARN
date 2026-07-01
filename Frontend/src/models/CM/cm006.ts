import type { EWorkProcess } from "@/enums/shared";
import type { TPaginated } from "../shared/paginated";
import type { Cm006Status } from "@/enums/CM/cm006";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "../shared/participants";
import type { Attachments, OnlyFileAttachment } from "../shared/uploadFile";
import type { DocumentVersion } from "../shared/document";

export type Cm006DialogCriteria = {
  keyword?: string;
  budgetYear?: number;
  departmentCode?: string;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
} & TPaginated;

export type Cm006DialogItem = {
  id: string;
  taxId: string;
  entrepreneurName: string;
  entrepreneurEmail: string;
  contractNumber: string;
  poNumber: string;
  budget: number;
  contractName: string;
  contractType?: string;
  contractTemplate?: string;
  contractSignedDate?: Date;
  deliveryLeadTime?: number;
  deliveryLeadTimeTypeCode?: string;
  deliveryLeadTimeTypeLabel?: string;
  deliveryDate?: Date;
};

export type Cm006Criteria = {
  workProcess: EWorkProcess;
  keyword?: string;
  departmentCode?: string;
  signedDate?: Date;
  contractTypeCode?: string;
  status?: Cm006Status;
} & TPaginated;

export type Cm006List = {
  contractDraftVendorId: string;
  contractNumber: string;
  poNumber: string;
  contractSignedDate: Date;
  entrepreneurCode: string;
  entrepreneurName: string;
  contractName: string;
  budget: number;
  contractTypeCode?: string;
  contractTypeLabel?: string;
  contractGuaranteeReturnId?: string;
  contractGuaranteeReturnStatus?: Cm006Status;
  department: string;
};

export type Cm006Detail = {
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
  contractSignedDate: Date;
  deliveryLeadTime: number;
  deliveryLeadTimeTypeCode: string;
  deliveryLeadTimeTypeLabel: string;
  deliveryDate: Date;
  guaranteeBankName?: string;
  guaranteeBankBranch?: string;
  contractEndDate?: Date;
  guaranteeReturn: Cm006GuaranteeReturn;
};

export type Cm006GuaranteeReturn = {
  id: string | undefined;
  guaranteeTypeCode?: string;
  guaranteeReturnDescription?: string;
  contractDescription?: string;
  proofOfPaymentDescription?: string;
  guranteeDescription?: string;
  guaranteeReturnDate?: Date;
  returnAmount: number | undefined;
  isDeducted: boolean;
  deductedAmount: number | undefined;
  netReturnAmount: number | undefined;
  additionalComment: string | undefined;
  documentDate?: Date;
  disbursementDate?: Date;
  disbursementAmount?: number;
  disbursementRemark?: string;
  status: Cm006Status;
  approvalCmContractGuaranteeReturnDocumentId: string | undefined;
  isApprovalCmContractGuaranteeReturnDocumentIdReplaced?: boolean;
  contractGuaranteeReturnResultDocumentId: string | undefined;
  isContractGuaranteeReturnResultDocumentIdReplaced?: boolean;
  assignees: ParticipantsAssignee[];
  acceptors: ParticipantsCommitteeAcceptor[];
  conditions: Cm006GuaranteeReturnCondition[];
  requiredDocuments: Cm006GuaranteeReturnRequiredDocument[];
  attachments: Attachments[];
  approvalDocumentVersions?: DocumentVersion[];
  resultDocumentVersions?: DocumentVersion[];
  isSendMail?: boolean;
  emailSend?: string;
  emailTemplate?: string;
  emailAttachments?: OnlyFileAttachment[];
};

export type Cm006GuaranteeReturnCondition = {
  id: string | undefined;
  sequence: number;
  description: string | undefined;
  isSatisfied: boolean;
};

export type Cm006GuaranteeReturnRequiredDocument = {
  id: string | undefined;
  sequence: number;
  documentName: string | undefined;
  isSubmitted: boolean;
};
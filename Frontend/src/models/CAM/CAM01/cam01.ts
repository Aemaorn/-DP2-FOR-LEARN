import type { EWorkProcess } from "@/enums/shared";
import type { TPaginated } from "../../shared/paginated";
import type { Cam01PoStep, Cam01Status, Cam01Type } from "@/enums/CAM/CAM01/cam01";
import type { Attachments } from "../../shared/uploadFile";
import type { DocumentVersion } from "../../shared/document";

export type Cam01Criteria = {
  workProcess: EWorkProcess;
  keyword?: string;
  signedDate?: Date;
  contractTypeCode?: string;
  status?: Cam01Status;
} & TPaginated;

export type Cam01List = {
  id?: string;
  procurementId?: string;
  procurementType: string;
  contractDraftVendorId: string;
  contractNumber: string;
  poNumber: string;
  contractSignedDate: Date;
  entrepreneurTax: string;
  entrepreneurName: string;
  contractName: string;
  budget: number;
  contractTypeCode?: string;
  contractTypeLabel?: string;
  status: Cam01Status;
  type: Cam01Type;
};

export type Cam01StatusCount = {
  all: number;
  draft: number;
  inProgress: number;
  completed: number;
};


export type Cam001ContractCriteria = {
  keyword?: string;
} & TPaginated;


export type Cam001ContractDialog = {
  contractDraftVendorId: string;
  contractNumber: string;
  poNumber: string;
  contractName: string;
  entrepreneurCode: string;
  entrepreneurName: string;
  entrepreneurEmail?: string | null;
  contractSignedDate?: Date;
  budget: number;
  contractTypeCode?: string | null;
  contractTypeLabel?: string | null;
  status: any;
  contractTemplate?: string | null;
  deliveryLeadTime?: number | null;
  deliveryLeadTimeTypeCode?: string;
  deliveryLeadTimeTypeLabel?: string | null;
  deliveryDate?: Date;
};

export type Cam01Body = {
  id?: string;
  contractDraftVendorId: string;
  contractInfo?: Cam001ContractDialog;
  type: Cam01Type;
  remark: string;
  currentStep?: Cam01PoStep;
  steps: Array<Cam01PoStep>;
  status: Cam01Status;
  poAddendum?: ProcessInfo;
  poSap?: ProcessInfo;
  waiveOrReducePenalty?: ProcessInfo;
  adjustContractDuration?: ProcessInfo;
  attachments: Array<Attachments>;
};

export type ProcessInfo = {
  id: string;
  status: string;
};

export type Cam01PoHeader = {
  contractNo: string;
  vendorId: string;
  vendorName: string;
  sapNumber: string;
  poNumber: string;
};

export type Cam01PoPayment = {
  id?: string;
  paymentTermNo: number;
  leadTime: number;
  deliveryDate: Date;
  installmentPercentage: number;
  amount: number;
  advanceDeductionAmount: number;
  performanceDeductionAmount: number;
  title: string;
  description: string;
  sequence: number;
};

export type Cam01DocInfo = {
  contractAddendumDocumentId?: string;
  isContractAddendumDocumentIdReplaced?: boolean;
  contractAmendmentRequestDocumentId?: string;
  isContractAmendmentRequestDocumentIdReplaced?: boolean;
  contractAddendumDocumentVersions?: DocumentVersion[];
  contractAmendmentRequestDocumentVersions?: DocumentVersion[];
};
import { AC01Status, sourceType } from "@/enums/AC/ac01";
import type { TDataTableResult, TPaginated } from "../shared/paginated";
import type { EWorkProcess } from "@/enums/shared";
import type { ParticipantsAcceptor, ParticipantsAssignee } from "../shared/participants";
import type { Attachments } from "../shared/uploadFile";

export type TAC01Criteria = {
  workProcess: EWorkProcess;
  status?: AC01Status;
  keyword?: string;
  departmentCode?: string;
  sourceType?: string;
  dateFrom?: Date;
  dateTo?: Date;
  advancePaymentDateFrom?: Date;
  advancePaymentDateTo?: Date;
} & TPaginated;

export type TAC01List = {
  id: string;
  sourceType: string;
  sourceId: string;
  sourceCode: string;
  sourceName: string;
  departmentName: string;
  advancePaymentDate: Date;
  date: Date;
  status: AC01Status;
  budget: number;
}

export type TAC01StatusCount = {
  all: number;
  draft: number;
  edit: number;
  rejected: number;
  waitingApproval: number;
  approved: number;
};

export type TAC01ListResponse = {
  statusCount: TAC01StatusCount;
  data: TDataTableResult<TAC01List>;
};

export type TA01Detail = {
  id: string;
  status: AC01Status;
  sourceType: sourceType;
  sourceId: string;
  date: Date;
  description?: string;
  isAdvance: boolean;
  advanceName?: string;
  advancePaymentMethodCode?: string;
  advancePaymentDate?: Date;
  advanceBankCode?: string;
  isInvoiceAmount?: boolean;
  invoiceAmount?: number;
  advanceBankAccount?: string;
  advanceBankBranch?: string;
  advanceBankAccountName?: string;
  advanceDetail?: string;
  acceptors: ParticipantsAcceptor[];
  assignees: ParticipantsAssignee[];
  glAccounts: GlAccounts[];
  attachments: Attachments[];
  source: Source;
  hasPermission: boolean;
  remarks?: string;
}

export type GlAccounts = {
  id: string;
  sequence: number;
  soId: string;
  budgetTypeCode: string;
  plAccountCode: string;
  projectNumber?: string;
  amount: number;
}

export type SourceDataDisbursement = {
  id: string;
  cm004Id: string;
  subject: string;
  requestDate: Date;
  description: string;
  netClaimAmount: number;
  status: string;
  contractDraftVendor: ContractDraftVendor;
  installments: Installment[];
};

export type ContractDraftVendor = {
  id: string;
  contractDraftNumber: string;
  contractNumber: string;
  contractName: string;
  poNumber: string;
  budget: number;
  contractSignedDate: Date;
  status: string;
  email: string;
  contractTypeLabel: string;
  templateLabel: string;
  deliveryLeadTime: number;
  deliveryDate: Date;
  deliveryLeadTimeTypeLabel: string;
};

export type Installment = {
  id: string;
  installmentNo: number;
  migoNumber: string;
  receiveDate: Date;
  amount: number;
  deliveryDetail: DeliveryDetail[];
  fineAmount: number;
};

export type DeliveryDetail = {
  deliveryDate: Date;
  items: DeliveryItem[];
};

export type DeliveryItem = {
  description: string;
  quantity: number;
  price: number;
  total: number;
};

export type SourceDataClause79_2 = {
  id: string;
  p79Clause2Number: { value: string };
  departmentName: string;
  p79Clause2Date: Date;
  subject: string;
  budget: number;
  status: string;
  budgetYear: number;
  supplyMethod: string;
  supplyMethodType: string;
  supplyMethodSpecialType: string;
  source: string;
  reasonItem1: string;
  reasonItem2: string;
  reasonItem3: string;
  vendors: Vendor[];
};

export type Vendor = {
  id: string;
  vendorType: string;
  suVendorId: string | null;
  vendorName: string;
  sequence: number;
  taxNumber: string;
  vendorBranchNumber: string;
  vatIncludeTypeCode?: string;
  vatIncludeTypeLabel?: string;
  billTypeCode: string;
  billTypeLabel: string;
  billTypeOther?: string;
  billBookNo: string;
  billDate?: Date;
  billDetail: string;
  parcels: Parcel[];
};

export type Parcel = {
  id: string;
  sequence: number;
  item: string;
  itemDetail: string;
  quantity: number;
  unitCode: string;
  unitLabel: string;
  unitPrice: number;
  totalPrice: number;
  totalPriceVat: number;
};

export type SourceDataW119 = {
  id: string;
  pw119Number: { value: string };
  departmentName: string;
  pw119Date: Date;
  subject: string;
  budget: number;
  status: string;
  budgetYear: number;
  supplyMethod: string;
  supplyMethodSpecialType: string;
  reason: string;
  vendors: Vendor[];
  source: string;
  w119Categories: string;
};

export type SourceDataPettyCashReimbursement = {
  id: string;
  number: string;
  status: string;
  reimbursementDate: Date;
  subject: string;
  description: string;
  referredTo: string;
  bankAccountName: string;
  bankAccountNumber: string;
  items: PettyCashItem[];
};

export type PettyCashItem = {
  id: string;
  sequence: number;
  pettyCashDate: Date;
  pettyCashNumber: string;
  soId: string;
  budgetTypeCode: string;
  budgetTypeLabel: string;
  glAccountCode: string;
  glAccountLabel: string;
  projectNumber?: string | null;
  amount: number;
  departmentName: string;
  subject: string;
};

export type SourceDataContractGuaranteeReturn = {
  id: string;
  guaranteeReturnDate: Date;
  returnAmount: number;
  isDeducted: boolean;
  deductedAmount: number;
  netReturnAmount: number;
  additionalComment: string;
  status: string;
  departmentName: string;
  contractDraftVendor: ContractDraftVendor;
  conditions: GuaranteeCondition[];
};

export type GuaranteeCondition = {
  id: string;
  sequence: number;
  description: string;
  isSatisfied: boolean;
};

export type SourceData =
  | SourceDataDisbursement
  | SourceDataClause79_2
  | SourceDataW119
  | SourceDataPettyCashReimbursement
  | SourceDataContractGuaranteeReturn;

export type Source = {
  sourceType: sourceType;
  data: SourceData;
  refCode?: string;
  departmentName?: string;
};
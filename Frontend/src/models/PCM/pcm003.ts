import type { Pcm003Action, Pcm003Status } from '@/enums/pcm003';
import type { TDataTableResult, TPaginated } from '../shared/paginated';
import type { ParticipantsAcceptor } from '../shared/participants';
import type { Attachments } from '../shared/uploadFile';
import type { EWorkProcess } from '@/enums/shared';
import type { DocumentVersion } from '../shared/document';
import type { PreProcurementStep } from '@/enums/preProcurement';
import type { ProcurementStep } from '@/enums/procurement';

export type Pcm003Criteria = {
  workProcess?: EWorkProcess;
  keyword?: string;
  departmentCode?: string;
  budgetYear?: number;
  quarter?: number;
  supplyMethodCode?: string;
  supplyMethodSpecialTypeCode?: string;
  status?: Pcm003Status;
  actionAtFrom?: Date;
  actionAtTo?: Date;
} & TPaginated;

export type Pcm003StatusCount = {
  all: number;
  draft: number;
  edit: number;
  rejected: number;
  waitingApproval: number;
  approved: number;
  cancelled: number;
};

export type Pcm003List = {
  id: string;
  p79Clause2Number: string;
  subject: string;
  budget: number;
  departmentName: string;
  supplyMethodName: string;
  supplyMethodTypeName: string;
  SupplyMethodSpecialName?: string;
  status: string;
  glAccounts?: string[];
};

export type Pcm003ListResponse = {
  statusCount: Pcm003StatusCount;
  data: TDataTableResult<Pcm003List>;
};

export type Pcm003Detail = {
  id?: string;
  p79Clause2Number: string;
  telephone?: string;
  status: Pcm003Status;
  p79Clause2Date: Date;
  departmentCode: string;
  departmentOrganizationLevel?: string;
  budgetYear: number;
  supplyMethodCode: string;
  supplyMethodTypeCode: string;
  supplyMethodSpecialTypeCode: string;
  assignSegmentCode?: string;
  subject: string;
  source: string;
  budget: number;
  medianPrice: number;
  deliveryDate: Date;
  procurementReasonItem1?: string;
  procurementReasonItem2?: string;
  reasonItem1: string;
  reasonItem2: string;
  reasonItem3: string;
  isAdvance: boolean;
  advance: Pcm003Advance;
  vendors: Pcm003Vendor[];
  glAccounts: Pcm003GlAccount[];
  acceptors?: ParticipantsAcceptor[];
  acceptanceConfirmers: ParticipantsAcceptor[],
  attachments: Attachments[];
  approvalRequestDocumentId?: string;
  isApprovalRequestDocumentReplace?: boolean;
  winnerAnnounceDocumentId?: string;
  isWinnerAnnounceDocumentReplace?: boolean;
  approvalRequestDocumentVersions?: DocumentVersion[];
  winnerAnnounceDocumentVersions?: DocumentVersion[];
  currentStep: PreProcurementStep;
  steps: Array<PreProcurementStep>;
  procurementStep: ProcurementStep;
  disbursementDate?: Date;
  disbursementAmount?: number;
  disbursementDescription?: string;
  userId?: string;
};

export type Pcm003Expenses = {
  order: number;
  reason: string;
};

export type Pcm003Advance = {
  advanceName: string;
  advancePaymentMethodCode: string;
  advancePaymentDate: Date;
  advanceBankCode: string;
  advanceBankAccount: string,
  advanceBankBranch: string,
  advanceBankAccountName: string,
  advanceDetail: string
};

export type Pcm003Vendor = {
  id?: string;
  vendorType: string;
  suVendorId: string;
  vendorName: string;
  sequence: number;
  taxNumber?: string;
  vendorBranchNumber?: string;
  vatIncludeTypeCode?: string;
  billTypeCode: string;
  billTypeOther?: string;
  billBookNo?: string;
  billDate: Date;
  billDetail?: string;
  vendorParcels: Pcm003VendorParcels[];
};

export type Pcm003VendorParcels = {
  id?: string;
  sequence: number;
  item: string;
  itemDetail: string;
  quantity: number;
  unitCode: string;
  unitPrice: number;
  totalPrice: number;
  totalPriceVat: number;
  vatIncludeTypeCode?: string;
};

export type Pcm003GlAccount = {
  id?: string;
  sequence: number;
  solId: string;
  budgetTypeCode: string;
  glAccountCode: string;
  projectNumber?: string;
  amount: number;
};

export type Pcm003PartnerDialogCriteria = {
  keyword: string;
  type: string;
  entrepreneurType: string;
};


export type Pcm003ActionReq = {
  action: Pcm003Action;
  remark?: string;
  jorporId?: string;
  acceptors?: ParticipantsAcceptor[];
}
import type { Pcm002Action, Pcm002Status } from '@/enums/pcm002';
import type { TDataTableResult, TPaginated } from '../shared/paginated';
import type { ParticipantsAcceptor } from '../shared/participants';
import type { Attachments } from '../shared/uploadFile';
import type { EWorkProcess } from '@/enums/shared';
import type { DocumentVersion } from '../shared/document';
import type { PreProcurementStep } from '@/enums/preProcurement';
import type { ProcurementStep } from '@/enums/procurement';

export type Pcm002Criteria = {
  workProcess?: EWorkProcess;
  keyword?: string;
  departmentCode?: string;
  budgetYear?: number;
  quarter?: number;
  supplyMethod?: string;
  SupplyMethodSpecialType?: string;
  status?: Pcm002Status;
  actionAtFrom?: Date;
  actionAtTo?: Date;
} & TPaginated;

export type Pcm002StatusCount = {
  all: number;
  draft: number;
  edit: number;
  rejected: number;
  waitingApproval: number;
  approved: number;
  cancelled: number;
};

export type Pcm002List = {
  id: string;
  pw119Number: string;
  subject: string;
  budget: number;
  departmentName: string;
  supplyMethodName: string;
  SupplyMethodSpecialName?: string;
  status: string;
  glAccounts?: string[];
};

export type Pcm002ListResponse = {
  statusCount: Pcm002StatusCount;
  data: TDataTableResult<Pcm002List>;
};

export type Pcm002Detail = {
  id?: string;
  pw119Number: string;
  status: Pcm002Status;
  pw119Date: Date;
  departmentCode: string;
  departmentOrganizationLevel?: string;
  budgetYear: number;
  supplyMethodCode: string;
  supplyMethodSpecialTypeCode: string;
  assignSegmentCode?: string;
  subject: string;
  source: string;
  budget: number;
  medianPrice: number;
  w119CategoriesCode: string;
  reason: string;
  advance: Pcm002Advance;
  vendors: Pcm002Vendor[];
  glAccounts: Pcm002GlAccount[];
  acceptors?: ParticipantsAcceptor[];
  acceptanceConfirmers: ParticipantsAcceptor[],
  attachments: Attachments[];
  approvalRequestDocumentId?: string;
  isApprovalRequestDocumentReplace?: boolean;
  winnerAnnounceDocumentId?: string;
  isWinnerAnnounceDocumentReplace?: boolean;
  approvalRequestDocumentVersions?: DocumentVersion[];
  winnerAnnounceDocumentVersions?: DocumentVersion[];
  telephone?: string;
  currentStep: PreProcurementStep;
  steps: Array<PreProcurementStep>;
  procurementStep: ProcurementStep;
  disbursementDate?: Date;
  disbursementAmount?: number;
  disbursementDescription?: string;
};

export type Pcm002Expenses = {
  order: number;
  reason: string;
};

export type Pcm002Advance = {
  isAdvance: boolean;
  advanceName: string;
  advancePaymentMethodCode: string;
  advancePaymentDate: Date;
  advanceBankCode: string;
  advanceBankAccount: string,
  advanceBankBranch: string,
  advanceBankAccountName: string,
  advanceDetail: string
};

export type Pcm002Vendor = {
  id?: string;
  vendorType: string;
  suVendorId: string;
  vendorName: string;
  sequence: number;
  taxNumber?: string;
  vendorBranchNumber?: string;
  vatIncludeTypeCode?: string;
  billTypeCode: string;
  billTypeOther: string;
  billBookNo: string;
  billDate: Date;
  billDetail?: string;
  vendorParcels: Pcm002VendorParcels[];
};

export type Pcm002VendorParcels = {
  id?: string;
  sequence: number;
  item: string;
  itemDetail: string;
  quantity: number;
  unitCode: string;
  unitLabel: string;
  unitPrice: number;
  totalPrice: number;
  totalPriceVat: number;
  vatIncludeTypeCode?: string;
};

export type Pcm002GlAccount = {
  id?: string;
  sequence: number;
  solId: string;
  budgetTypeCode: string;
  glAccountCode: string;
  projectNumber?: string;
  amount: number;
};

export type Pcm002PartnerDialogCriteria = {
  keyword: string;
  type: string;
  entrepreneurType: string;
};


export type Pcm002ActionReq = {
  action: Pcm002Action;
  remark?: string;
  jorporId?: string;
  acceptors?: ParticipantsAcceptor[];
}
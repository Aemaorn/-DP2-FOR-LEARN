import type { Pcm007Action, Pcm007CommitteeType, Pcm007Status } from '@/enums/pcm007';
import type { TDataTableResult, TPaginated } from '../shared/paginated';
import type { ParticipantsAcceptor } from '../shared/participants';
import type { Attachments } from '../shared/uploadFile';

export type Pcm007Criteria = {
  keyword?: string;
  departmentCode?: string;
  budgetYear?: number;
  supplyMethodCode?: string;
  status?: Pcm007Status;
} & TPaginated;

export type Pcm007StatusCount = {
  all: number;
  draft: number;
  edit: number;
  waitingApproval: number;
  waitingCommitteeApprove: number;
  waitingAccounting: number;
  waitingDisbursementDate: number;
  paid: number;
  rejected: number;
};

export type Pcm007List = {
  id: string;
  pw184Number: string;
  subject: string;
  budget: number;
  departmentName: string;
  supplyMethodName: string;
  supplyMethodSpecialName?: string;
  status: Pcm007Status;
};

export type Pcm007ListResponse = {
  statusCount: Pcm007StatusCount;
  data: TDataTableResult<Pcm007List>;
};

export type Pcm007Detail = {
  id?: string;
  pw184Number: string;
  status: Pcm007Status;
  pw184Date: Date;
  departmentCode: string;
  budgetYear: number;
  supplyMethodCode: string;
  supplyMethodSpecialTypeCode?: string;
  subject: string;
  source: string;
  reason?: string;
  budget: number;
  isAdvance: boolean;
  advanceName?: string;
  advancePaymentMethodCode?: string;
  advancePaymentDate?: Date;
  advanceBankCode?: string;
  advanceBankAccount?: string;
  advanceBankBranch?: string;
  advanceBankAccountName?: string;
  advanceDetail?: string;
  disbursementDate?: Date;
  disbursementAmount?: number;
  disbursementDescription?: string;
  currentCommitteeSequence: number;
  createdBy?: string;
  vendors: Pcm007Vendor[];
  glAccounts: Pcm007GlAccount[];
  committees: Pcm007Committee[];
  acceptors?: ParticipantsAcceptor[];
  acceptanceConfirmers: ParticipantsAcceptor[];
  attachments: Attachments[];
};

export type Pcm007Vendor = {
  id?: string;
  vendorType: string;
  suVendorId?: string;
  vendorName: string;
  sequence: number;
  taxNumber?: string;
  vendorBranchNumber?: string;
  vatIncludeTypeCode?: string;
  billTypeCode: string;
  billTypeOther?: string;
  billBookNo?: string;
  billDate?: Date;
  billDetail?: string;
  vendorParcels: Pcm007VendorParcels[];
};

export type Pcm007VendorParcels = {
  id?: string;
  sequence: number;
  item: string;
  itemDetail?: string;
  quantity: number;
  unitCode: string;
  unitPrice: number;
  totalPrice: number;
  totalPriceVat: number;
  vatIncludeTypeCode?: string;
};

export type Pcm007GlAccount = {
  id?: string;
  sequence: number;
  solId: string;
  budgetTypeCode: string;
  glAccountCode: string;
  projectNumber?: string;
  amount: number;
};

export type Pcm007Committee = {
  id?: string;
  groupType: Pcm007CommitteeType;
  userId: string;
  fullName: string;
  fullPositionName: string;
  committeePositionsCode: string;
  committeePositionsName: string;
  sequence: number;
};

export type Pcm007ActionReq = {
  action: Pcm007Action;
  remark?: string;
};

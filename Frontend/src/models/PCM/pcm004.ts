import type { CashType, Pcm004Action, Pcm004CommitteeType, Pcm004Status } from '@/enums/pcm004';
import type { TDataTableResult, TPaginated } from '../shared/paginated';
import type { ParticipantsAcceptor, ParticipantsAssignee } from '../shared/participants';
import type { Attachments } from '../shared/uploadFile';
import type { EWorkProcess } from '@/enums/shared';
import type { DocumentVersion } from '../shared/document';

export type Pcm004Criteria = {
  workProcess?: EWorkProcess;
  keyword?: string;
  departmentCode?: string;
  budgetYear?: number;
  supplyMethod?: string;
  SupplyMethodSpecialType?: string;
  status?: Pcm004Status;
} & TPaginated;

export type Pcm004StatusCount = {
  all: number;
  draft: number;
  edit: number;
  rejected: number;
  waitingApproval: number;
  waitingForInspector: number;
  waitingForAssignment: number;
  waitingForCompletion: number;
  approved: number;
  cancelled: number;
  completed: number;
};

export type Pcm004List = {
  id: string;
  pPettyCashNumber: string;
  subject: string;
  budget: number;
  departmentName: string;
  supplyMethodName: string;
  supplyMethodTypeName: string;
  SupplyMethodSpecialName?: string;
  status: string;
};

export type Pcm004ListResponse = {
  statusCount: Pcm004StatusCount;
  data: TDataTableResult<Pcm004List>;
};

export type Pcm004Detail = {
  id?: string;
  pPettyCashNumber: string;
  status: Pcm004Status;
  pPettyCashDate: Date;
  departmentCode: string;
  budgetYear: number;
  supplyMethodCode: string;
  supplyMethodTypeCode: string;
  supplyMethodSpecialTypeCode: string | undefined;
  subject: string;
  reasons: string;
  source: string;
  budget: number;
  deliveryDate?: Date;
  deliveryPeriod?: number;
  pettyCaseDepartmentCode?: string;
  deliveryPeriodTypeCode?: string;
  deliveryConditionCode?: string;
  disbursementDate?: Date;
  isAdvance: boolean;
  isProcurementCommittee: boolean;
  isInspectCommittee: boolean;
  advance: Pcm004Advance;
  categories: Pcm004Categories[];
  vendors: Pcm004Vendor[];
  glAccounts: Pcm004GlAccount[];
  committees: Pcm004Committee[];
  acceptors?: ParticipantsAcceptor[];
  assignees: ParticipantsAssignee[];
  attachments: Attachments[];
  hasPermission: boolean;
  cashType: CashType;
  approvalRequestDocumentId?: string;
  isApprovalRequestDocumentReplace?: boolean;
  approvalRequestDocumentVersions?: DocumentVersion[];
  departmentOrganizationLevel?: string;
  isFromJorPor001?: boolean | null;
};

export type Pcm004Categories = {
  id?: string;
  categoryTypeCode: string;
};

export type Pcm004Expenses = {
  order: number;
  reason: string;
};

export type Pcm004Advance = {
  advanceName: string;
  advancePaymentMethodCode: string;
  advancePaymentDate: Date;
  advanceBankCode: string;
  advanceBankAccount: string,
  advanceBankBranch: string,
  advanceBankAccountName: string,
  advanceDetail: string
};

export type Pcm004Vendor = {
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
  billBookNo: string;
  billDate: Date;
  billDetail?: string;
  vendorParcels: Pcm004VendorParcels[];
};

export type Pcm004VendorParcels = {
  id?: string;
  sequence: number;
  item: string;
  itemDetail: string;
  quantity: number;
  unitCode: string;
  unitPrice: number;
  totalPrice: number;
  totalPriceVat: number;
};

export type Pcm004GlAccount = {
  id?: string;
  sequence: number;
  solId: string;
  budgetTypeCode: string;
  glAccountCode: string;
  projectNumber?: string;
  amount: number;
};

export type Pcm004PartnerDialogCriteria = {
  keyword: string;
  type: string;
  entrepreneurType: string;
};

export type Pcm004Committee = {
  id?: string;
  groupType: Pcm004CommitteeType;
  userId: string;
  fullName: string;
  committeePositionCode: string;
  committeePositionName: string;
  positionName: string;
  sequence: number;
};


export type Pcm004ActionReq = {
  action: Pcm004Action;
  remark?: string;
  jorporId?: string;
  disbursementDate?: Date;
  acceptors?: ParticipantsAcceptor[];
  assignees?: ParticipantsAssignee[];
}
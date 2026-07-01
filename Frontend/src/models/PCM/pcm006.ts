import type { EWorkProcess } from "@/enums/shared"
import type { TDataTableResult, TPaginated } from "../shared/paginated";
import type { EPcm006Status } from "@/enums/pcm006";
import type { ParticipantsAcceptor } from "../shared/participants";
import type { Attachments } from "../shared/uploadFile";
import type { PreProcurementStep } from "@/enums/preProcurement";
import type { ProcurementStep } from "@/enums/procurement";

export type TPcm006Criteria = {
  workProcess?: EWorkProcess;
  keyword?: string;
  departmentCode?: string;
  status?: string;
} & TPaginated;

export type TPcm006StatusCount = {
  all: number;
  draft: number;
  edit: number;
  waitingApproval: number;
  approved: number;
  rejected: number;
  cancelled: number;
}

export type TPcm006ListItems = {
  id: string;
  number: string;
  status: EPcm006Status,
  reimbursementDate: Date,
  subject: string;
  totalAmount: number;
  departmentName?: string;
  createData: Date;
}

export type TPcm006ListResponse = {
  statusCount: TPcm006StatusCount;
  data: TDataTableResult<TPcm006ListItems>;
};

export type TPcm006Detail = {
  id?: string;
  number: string;
  status: EPcm006Status;
  reimbursementDate: Date;
  departmentId: string;
  departmentOrganizationLevel?: string;
  subject: string;
  description?: string;
  referredTo?: string;
  bankAccountName?: string;
  bankAccountNumber: string;
  acceptors: ParticipantsAcceptor[];
  items: ReimbursementItem[];
  hasPermission: boolean;
  attachments: Attachments[];
  currentStep: PreProcurementStep;
  steps: Array<PreProcurementStep>;
  procurementStep: ProcurementStep;
  disbursementDate?: Date;
  disbursementAmount?: number;
  disbursementDescription?: string;
  acceptanceConfirmers: ParticipantsAcceptor[];
}

export type ReimbursementItem = {
  id?: string;
  pettyCashGlAccountId: string;
  sequence: number;
  pettyCashDate: Date;
  pettyCashNumber: string;
  pettyCashSubject: string;
  soId: string;
  budgetTypeCode: string;
  budgetTypeLabel?: string;
  glAccounneCode: string;
  glAccountLabel?: string;
  projectNumber?: string;
  amount: number;
};
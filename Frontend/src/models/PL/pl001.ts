import type {
  ProcurementType,
  ProcurementProcess,
  ProcurementPlanType,
} from '@/enums/procurement';
import type { TDataTableResult, TPaginated } from '../shared/paginated';
import type { StatusCount } from '../shared/status';
import type { Attachments } from '../shared/uploadFile';
import type { PlanAction, PlanStatus } from '@/enums/plan';
import type { ParticipantsAcceptor, ParticipantsAssignee } from '../shared/participants';
import type { EWorkProcess } from '@/enums/shared';
import type { DocumentVersion } from '../shared/document';

export type TPL001Criteria = {
  workProcess?: EWorkProcess;
  type?: ProcurementPlanType;
  keyword?: string;
  departmentCode?: string;
  budgetYear?: number;
  method?: string;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  SupplyMethodSpecialTypeCode?: string;
  process?: ProcurementProcess;
  status?: PlanStatus;
  isChange?: boolean;
  isCancel?: boolean;
} & TPaginated;

export type TPL001List = {
  id: string;
  planNumber: string;
  name: string;
  budgetYear: number;
  budget: number;
  type: ProcurementType;
  departmentName: string;
  departmentCode: string;
  supplyMethodName: string;
  status: string;
  isChange: boolean;
  isCancel: boolean;
  oldData: boolean;
};

export type TPL001ListResponse = {
  statusCount: StatusCount;
  data: TDataTableResult<TPL001List>;
};

export type PlanBody = {
  id?: string;
  status: PlanStatus;
  planNumber?: string;
  departmentCode: string;
  type: string;
  supplyMethodCode: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
  budgetYear: number;
  name: string;
  budget: number;
  expectingProcurementAt?: Date;
  documentDate?: Date;
  remark: string;
  telephone: string;
  isStock: boolean;
  assignSegmentCode: string;
  groupEgpNumber?: string;
  egpNumber?: string;
  isCommercialMaterial: boolean | null;
  planDocumentId?: string;
  isPlanDocumentIdReplace?: boolean;
  planAnnouncementDocumentId?: string;
  isPlanAnnouncementDocumentIdReplace?: boolean;
  planDocumentVersions?: DocumentVersion[];
  planAnnouncementDocumentVersions?: DocumentVersion[];
  acceptors: ParticipantsAcceptor[];
  assignees: ParticipantsAssignee[];
  attachments: Attachments[];
  createdBy: string;
  assigneeAnnouncement?: ParticipantsAssignee;
  isChange?: boolean;
  isCancel?: boolean;
  changeReason?: string;
  cancelReason?: string;
  isCurrentCancelOrChange: boolean;
  relatedPlanIds?: string[];
  isProcurement: boolean;
  lastModifiedAt?: string;
  remarkClosed?: string;
};

export type PlanActionReq = {
  action: PlanAction;
  remark?: string;
  attachments?: Attachments[];
  jorporId?: string;
  assignees?: ParticipantsAssignee[];
  acceptors?: ParticipantsAcceptor[];
  assignSegmentCode?: string;
  groupEgpNumber?: string;
  egpNumber?: string;
  isPlanAnnouncementDocumentIdReplace?: boolean;
  isPlanDocumentIdReplace?: boolean;
  documentDate?: Date;
}
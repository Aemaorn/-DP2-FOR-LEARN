import type { TDataTableResult, TPaginated } from '../shared/paginated';
import type { Attachments } from '../shared/uploadFile';
import type { ParticipantsAcceptor, ParticipantsAssignee } from '../shared/participants';
import type { PlanAnnouncementStatus } from '@/enums/planAnnouncement';
import type { EWorkProcess } from '@/enums/shared';
import type { DocumentVersion } from '../shared/document';

export type pl002Criteria = {
  workProcess: EWorkProcess;
  searchText?: string;
  supplyMethodCode?: string;
  fromBudgetYear?: number;
  toBudgetYear?: number;
  fromAnnouncementDate?: Date;
  toAnnouncementDate?: Date;
  status?: PlanAnnouncementStatus;
} & TPaginated;

export type pl002Table = {
  id: string;
  announcementNumber: string;
  announcementName: string;
  year: number;
  planCount: number;
  summaryBudget: number;
  supplyMethodName: string;
  announcementDate: Date;
  status: PlanAnnouncementStatus;
};

export type TPL002StatusCount = {
  all: number;
  draft: number;
  assigned: number;
  waitingAcceptor: number;
  approved: number;
  announcement: number;
  rejected: number;
};

export type tableCountResponse = {
  data: TDataTableResult<pl002Table>;
  counts: TPL002StatusCount;
};

type FileInformation = {
  order: number;
  file?: File;
  isPrivated: boolean;
};

export type pl002DocumentList = {
  id?: string;
  order: number;
  FileList: FileInformation[];
};

export type PlanAnnouncementBody = {
  planAnnouncementId?: string;
  planAnnouncementNumber?: string;
  groupEgpNumber?: string;
  year: number;
  supplyMethodCode: string;
  remark?: string;
  telephone?: string;
  announcementTitle?: string;
  announcementDate?: Date;
  documentDate?: Date;
  isAnnouncementDocumentIdReplace?: boolean;
  announcementDocumentId?: string;
  isApproveDocumentIdReplace?: boolean;
  approveDocumentId?: string;
  approveDocumentVersions?: DocumentVersion[];
  announcementDocumentVersions?: DocumentVersion[];
  planSelected: planSelected[];
  attachments: Attachments[];
  assignees: ParticipantsAssignee[];
  acceptors: ParticipantsAcceptor[];
  assigneeAnnouncement?: ParticipantsAssignee;
  status: PlanAnnouncementStatus;
  lastModifiedAt?: string;
};

export type planSelected = {
  id?: string;
  refId?: string;
  planId: string;
  planNumber: string;
  planTitle: string;
  budget: number;
  departmentName: string;
  supplyMethodName: string;
  supplyMethodTypeName: string;
  egpNumber: string;
  isCancel: boolean;
  isChange: boolean;
};
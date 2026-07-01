import type { EWorkProcess } from '@/enums/shared';
import type { TDataTableResult, TPaginated } from '../../shared/paginated';
import type { Cam02Status } from '@/enums/CAM/CAM02/cam02';
import type { Attachments } from '../../shared/uploadFile';
import type { ProcurementProcess, ProcurementType } from '@/enums/procurement';
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from '@/models/shared/participants';
import type { DocumentVersion } from '@/models/shared/document';

export type SourceTypeList = {
  sourceType: string;
  sourceId: string;
  committeeGroupType: string;
};

export enum SourceType {
  Appoint = 'Appoint',
  PurchaseRequisition = 'PurchaseRequisition',
  Jp005 = 'Jp005',
  PurchaseOrderApproval = 'PurchaseOrderApproval',
  PrincipleApproval = 'PrincipleApproval',
}

export enum CommitteeType {
  TOR = 'TOR',
  MedianPrice = 'MedianPrice',
  ProcurementCommittee = 'ProcurementCommittee',
  InspectionCommittee = 'InspectionCommittee',
  MaintenanceInspectionCommittee = 'MaintenanceInspectionCommittee',
  ConstructionSupervisor = 'ConstructionSupervisor',
  AcceptanceCommittee = 'AcceptanceCommittee',
}

export type TCam02StatusCount = {
  all: number;
  draft: number;
  edit: number;
  waitingApproval: number;
  approved: number;
  rejected: number;
  cancelled: number;
};

export type TCam02Criteria = {
  workProcess?: EWorkProcess;
  committeeGroupType?: string;
  keyword?: string;
  departmentCode?: string;
  budgetYear?: string;
  method?: string;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  SupplyMethodSpecialTypeCode?: string;
  process?: ProcurementProcess;
  status?: Cam02Status;
} & TPaginated;

export type TCam02List = {
  all: number;
  draft: number;
  edit: number;
  waitingApproval: number;
  approved: number;
  rejected: number;
  cancelled: number;
  data: TDataTableResult<TCam02ListData>;
};

export type TCam02ListData = {
  id?: string;
  procurementId: string;
  procurementNumber: string;
  procurementName: string;
  planName: string;
  planNumber: string;
  committeeType: CommitteeType;
  departmentName?: string;
  supplyMethod?: string;
  status: Cam02Status;
  remark?: string;
};

export type TCam02Body = {
  id?: string;
  procurementType?: string;
  procurementId: string;
  procurement: TChangeCommitteeProcurement;
  sourceType: string;
  sourceId: string;
  committeeType: string;
  status?: Cam02Status;
  documentDate?: Date;
  remark?: string;
  oldCommittees: TListCam02Committee[];
  newCommittees: TListCam02Committee[];
  acceptors: Array<ParticipantsCommitteeAcceptor>;
  assignees: Array<ParticipantsAssignee>;
  attachments: Array<Attachments>;
  documentId?: string;
  lastedDocumentId?: string;
  documentVersions?: DocumentVersion[];
  isResetTemplace?: boolean;
  isJorPorComment?: boolean;
};

export type TListCam02Committee = {
  suUserId: string;
  fullName: string;
  fullPositionName: string;
  committeePositionsCode: string;
  committeePositionsName: string;
  sequence: number;
};

export type TChangeCommitteeProcurement = {
  planId?: string;
  planNumber?: string;
  procurementNumber?: string;
  planType?: string;
  departmentName?: string;
  planName?: string;
  budget?: number;
  budgetYear?: number;
  procurementType?: ProcurementType;
  supplyMethod?: string;
  supplyMethodCode?: string;
  supplyMethodType?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialType?: string;
  supplyMethodSpecialTypeCode?: string;
  isStock: boolean;
  isCommercialMaterial: boolean;
};

export type TChangeCommitteeSendAction = {
  acceptorId: string;
  remark?: string;
};

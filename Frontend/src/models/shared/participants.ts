import type { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from "@/enums/participants";

export type ParticipantsData = {
  id?: string;
  userId: string;
  fullName: string;
  positionName: string;
  departmentCode?: string;
  departmentName: string;
  sequence: number;
  remark?: string;
  actionAt?: Date;
  isCurrent?: boolean;
  delegateId?: string;
  delegateeUserId?: string;
  organizationLevel?: number;
};

export type ParticipantsAssignee = {
  assigneeGroup: AssigneeGroup;
  assigneeType: AssigneeType;
  status: AssigneeStatus;
} & ParticipantsData;

export type ParticipantsAcceptor = {
  acceptorType: AcceptorType;
  status: AcceptorStatus;
} & ParticipantsData;

export type ParticipantsCommitteeAcceptor = {
  committeePositionsCode?: string;
  committeePositionName?: string;
  isUnableToPerformDuties: boolean;
} & ParticipantsAcceptor;

export type SectionType = 'Committee' | 'Duty';
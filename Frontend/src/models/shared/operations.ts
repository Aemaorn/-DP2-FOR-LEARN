import type { SectionProcessType } from "@/enums/operations";
import type { ParticipantsCommitteeAcceptor } from "./participants";
import type { AcceptorTorDraft } from "@/views/PP/models/PP002/pp002Model";

export type OperationBody = {
  userId: string;
  fullName: string;
  positionId: string;
  fullPositionName: string;
  organizationLevel: number;
  businessUnitId: string;
  businessUnitName: string;
  employeeCode: string;
}

export type defaultAcceptorCriteria = {
  processType: SectionProcessType;
  supplyMethodCode?: string;
  supplyMethodSpecialTypeCode?: string;
  id?: string;
  budget: number;
  userId?: string;
  skipCurrentEmployee?: boolean;
}

export type CommitteeAcceptor = {
  sequence: number;
} & ParticipantsCommitteeAcceptor | AcceptorTorDraft;

export type ObjectNReason = {
  objective: string;
  reason: string;
  specificDescription: string;
  torTemplate?: string;
};

export type CommitteeTypeWithReason = {
  committees: CommitteeAcceptor[];
  objAndReason: ObjectNReason;
}

export type DefaultDepartmentDirectorCriteria = {
  businessUnitId: string;
}
import type { Cam01FineStatus } from "@/enums/CAM/CAM01/cam01.fine";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "../../shared/participants";
import type { Cam01DocInfo } from "./cam01";

export type Cam01FineBody = {
  id?: string;
  camContractAmendmentId: string;
  waiveAll: boolean;
  penaltyOld: Cam01FineHeader;
  penaltyNew: Cam01FineHeader;
  acceptors: Array<ParticipantsCommitteeAcceptor>;
  assignees: Array<ParticipantsAssignee>;
  status: Cam01FineStatus;
} & Cam01DocInfo;

export type Cam01FineHeader = {
  penaltyTypeCode?: string;
  rate?: number;
  amount?: number;
  rateTypeCode?: string;
};
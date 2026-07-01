import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "../../shared/participants";
import type { Cam01DocInfo } from "./cam01";
import type { Cam01AdjustContractStatus } from "@/enums/CAM/CAM01/cam01.adjustContract";

export type Cam01AdjustContractBody = {
  id?: string;
  camContractAmendmentId: string;
  adjustContractDurationOld: Cam01AdjustContractInfo;
  adjustContractDurationNew: Cam01AdjustContractInfo;
  acceptors: Array<ParticipantsCommitteeAcceptor>;
  assignees: Array<ParticipantsAssignee>;
  status: Cam01AdjustContractStatus;
} & Cam01DocInfo;

export type Cam01AdjustContractInfo = {
  adjustContractDurationId?: string;
  changeType: 'Extend' | 'Change';
  workStartDate: Date;
  newEndDate: Date;
  paymentTypeCode?: string;
  paymentTerms: Array<Cam01AdjustContractPayment>;
};


export type Cam01AdjustContractPayment = {
  paymentTermId?: string;
  paymentTermNo: number;
  leadTime: number;
  deliveryDate: Date;
  installmentPercent: number;
  amount: number;
  advanceDeductionAmount: number;
  performanceDeductionAmount: number;
  description: string;
  IsDelivery: boolean;
};
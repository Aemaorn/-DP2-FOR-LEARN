import type { Cam01PoAddendumStatus } from "@/enums/CAM/CAM01/cam01.poAddendum";
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "../../shared/participants";
import type { Cam01DocInfo, Cam01PoHeader, Cam01PoPayment } from "./cam01";

export type Cam01PoAddendumBody = {
  id?: string;
  camContractAmendmentId: string;
  oldContract: Cam01PoHeader;
  newContract: Cam01PoHeader;
  vendor: Cam01PoAddendumVendor;
  status: Cam01PoAddendumStatus;
  acceptors: Array<ParticipantsCommitteeAcceptor>;
  assignees: Array<ParticipantsAssignee>;
  oldPaymentTerms: Array<Cam01PoPayment>;
  newPaymentTerms: Array<Cam01PoPayment>;
} & Cam01DocInfo;


export type Cam01PoAddendumVendor = {
  taxpayerIdentificationNo: string;
  establishmentName: string;
  email?: string;
};

export type Cam01PoAddendumPayload = {
  camContractAmendmentId: string;
  contractNumber: string;
  sapNumber: string;
  poNumber?: string;
  vendorId: string;
  status: Cam01PoAddendumStatus;
  acceptors: Array<ParticipantsCommitteeAcceptor>;
  assignees: Array<ParticipantsAssignee>;
  paymentTerms: Array<Cam01PoPayment>;
};
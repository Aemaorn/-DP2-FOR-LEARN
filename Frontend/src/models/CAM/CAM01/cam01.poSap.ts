import type { Cam01PoSapStatus } from "@/enums/CAM/CAM01/cam01.poSap";
import type { ParticipantsAcceptor } from "../../shared/participants";
import type { Cam01PoHeader, Cam01PoPayment } from "./cam01";

export type Cam01PoSapBody = {
  id?: string;
  camContractAmendmentId: string;
  oldContract: Cam01PoHeader;
  newContract: Cam01PoHeader;
  oldPaymentTerms: Array<Cam01PoPayment>;
  paymentTerms: Array<Cam01PoPayment>;
  acceptors: Array<ParticipantsAcceptor>;
  status: Cam01PoSapStatus;
  hasPermission: boolean;
};

export type Cam01PoSapPayload = {
  contractAmendmentId: string;
  id?: string;
  poSapNumber: string;
  acceptors: Array<ParticipantsAcceptor>;
  status: Cam01PoSapStatus;
};

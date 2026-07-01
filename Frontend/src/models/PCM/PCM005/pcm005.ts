import type { PreProcurementStep } from "@/enums/preProcurement";

export type StatusType = 'Success' | 'Progress' | 'Waiting';

export type ProgramType = {
  menu: string;
  status: StatusType;
  name: PreProcurementStep;
}

export type ProgramMenuType = {
  procurement: ProgramType[];
  contractAgreement?: ProgramType[];
  accounting?: ProgramType[];
}

export type PCM005StatusGroupCount = {
  all: number;
  procurement: number;
  contractAgreement: number;
};
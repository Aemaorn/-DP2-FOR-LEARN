import type { CmStatus } from "@/enums/CM/cm";
import type { TDataTableResult, TPaginated } from "../shared/paginated";

export type CmCriteria = {
  keyword?: string;
  departmentCode?: string;
  contractSignedDateFrom?: Date;
  contractSignedDateTo?: Date;
  status?: CmStatus;
} & TPaginated

export type GetListResponse = {
  data: TDataTableResult<CmData>;
  statusCount: StatusCount;
}

export type StatusCount = {
  all: number;
  inProgress: number;
  completed: number;
}

export type CmData = {
  id?: string;
  procurementId: string;
  contractDraftVendorId: string;
  contractNumber: string;
  procurementType: string;
  poNumber: string;
  contractName: string;
  entrepreneurCode: string;
  entrepreneurName: string;
  contractSignedDate: string;
  budget: number;
  contractTypeCode: string;
  contractTypeLabel: string;
  entrepreneurEmail: string;
  status: string;
  contractTemplate: string;
  deliveryLeadTime: number;
  deliveryLeadTimeTypeCode: string;
  deliveryLeadTimeTypeLabel: string;
  deliveryDate: Date;
}

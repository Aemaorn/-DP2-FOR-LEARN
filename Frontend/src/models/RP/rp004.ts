import type { checkType } from "@/enums/RP/rp004";
import type { TNationalityType, TVendorType } from "../ST/st003";

export type TRp004Body = {
  vendorId: string;
  checkType: checkType;
  isJuristic: boolean;
  result: boolean;
  remark?: string;
  nationality?: TNationalityType;
  type?: TVendorType;
  sapVendorNumber?: string;
  sapBranchNumber?: string;
  resultDate?: Date;
  entrepreneurType?: string;
  taxpayerIdentificationNo?: string;
  placeName?: string;
  tel?: string;
  email?: string;
  vendorName?: string;
};

export type TCheckHistoryItem = {
  vendorName?: string;
  taxpayerIdentificationNo?: string;
  firstName?: string;
  lastName?: string;
  isJuristic?: boolean;
};

export type TCheckHistoryRequest = {
  vendorId?: string;
  checkType: checkType;
  items: TCheckHistoryItem[];
};

export type TCheckHistoryItemResponse = {
  name: string;
  taxpayerIdentificationNo?: string;
  result: boolean | 'UnKnow';
  remark: string;
  checkTime?: string;
  checkerEmployeeCode?: string;
  employeeName?: string;
  position?: string;
};
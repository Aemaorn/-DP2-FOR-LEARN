import type { TPaginated } from '../shared/paginated';
import type { Attachments } from '../shared/uploadFile';

export type TSt010Criteria = {
  businessUnitIds?: string[];
  keyword?: string;
  effectiveStartDate?: Date;
  effectiveEndDate?: Date;
} & TPaginated;

export type TSt010List = {
  id: string;
  isPositionType: boolean;
  userFullName?: string;
  fullPositionName?: string;
  businessUnitName?: string;
  secretaryCount: number;
  secretaryNames?: string;
  updatedAt?: Date;
};

export type TSt010Detail = {
  suSecretaryOwner: TSt010SuSecretaryOwner;
  secretaries: TSt010Secretary[];
  attachments: Attachments[];
};

export type TSt010SuSecretaryOwner = {
  id?: string;
  isPositionType?: boolean;
  suUserId?: string;
  businessUnitId?: string;
  businessUnitName?: string;
  userFullName?: string;
  employeeCode?: string;
  positionId?: string;
  fullPositionName?: string;
  email?: string;
};

export type TSt010Secretary = {
  id?: string;
  suUserId: string;
  userFullName: string;
  positionId?: string;
  fullPositionName?: string;
  email?: string;
  active?: boolean;
  sequence: number;
  effectiveStartDate?: Date;
  effectiveEndDate?: Date;
};

import type { TPaginated } from '../shared/paginated';

export type TSt005Criteria = {
  searchText?: string;
  departmentCode?: string;
  isActive: boolean;
} & TPaginated;

export type TSt005List = {
  id: string;
  name: string;
  positionCode?: string;
  positionName?: string;
  departmentCode?: string;
  departmentName?: string;
  email?: string;
  lastModifiedByName?: string;
  lastModifiedAt?: Date
  isActive?: boolean;
  isLockedOut?: boolean;
};

export type TSt005Detail = {
  id?: string;
  employeeCode?: string;
  name: string;
  email: string;
  departmentName: string;
  positionName: string;
  isActive: boolean;
  isLockedOut?: boolean;
  signatureImage?: File | null;
  signatureImageId?: string;
  role: TSt005Role[];
};

export type TSt005Role = {
  roleCode?: string;
};

export type TUserDialogCriteria = {
  searchText?: string;
  groupCode?: string;
  lineCode?: string;
  departmentCode?: string;
  isActive?: boolean;
} & TPaginated;

export type TUserDialog = {
  id: string;
  name: string;
  positionCode?: string;
  positionName?: string;
  departmentCode?: string;
  departmentName?: string;
  employeeCode: string;
  email?: string;
  keyword?: string;
  groupWork?: string;
  lineWork?: string;
  department?: string;
  delegateeId?: string;
  organizationLevel?: string;
} & TPaginated;

export type TRawEmployeeDialogCriteria = {
  keyword?: string;
  groupWork?: string;
  lineWork?: string;
  department?: string;
} & TPaginated;

export type TRawEmployeeDialog = {
  employeeCode: string;
  fullName: string;
  email: string;
  departmentName: string;
  fullPositionName: string;
  departmentCode: string;
};
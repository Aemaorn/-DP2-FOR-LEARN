import type { TPaginated } from '../shared/paginated';

export type TUser = {
  id?: string;
  employeeCode?: string;
  name: string;
  email: string;
  departmentName: string;
  positionName: string;
  isActive: boolean;
};

export type TSt001Criteria = {
  businessUnitIds?: string[];
  delegatorStartDate?: Date;
  delegatorEndDate?: Date;
  delegatorName?: string;
  delegatorPositionName?: string;
  delegateeName?: string;
  delegateePositionName?: string;
} & TPaginated;

export type TSt001List = {
  id: string;
  delegatorName?: string;
  delegatorPositionName?: string;
  delegatorStartDate?: Date;
  delegatorEndDate?: Date;
  delegateeName?: string;
  delegateePositionName?: string;
  updatedAt?: Date;
};

export type TSt001Detail = {
  delegator: TSuDelegateCreateUserRequestType;
  delegatees: TSuDelegateeCreateUserRequestType[];
};

export type TSuDelegateCreateUserRequestType = {
  id?: string;
  suUserId: string;
  userFullName: string;
  employeeCode: string;
  positionId: string;
  fullPositionName?: string;
  email?: string;
  delegationStartDate: Date;
  delegationEndDate: Date;
  annotation: string;
};

export type TSuDelegateeCreateUserRequestType = {
  id?: string;
  delegatorPositionId: string;
  delegatorBusinessUnitId: string;
  delegatorPosuitionName?: string;
  suUserId: string;
  userFullName: string;
  positionId: string;
  email?: string;
  fullPositionName?: string;
  parentBusinessUnitId?: string;
  levelOneBusinessUnitId?: string;
  businessUnitId?: string;
  subBusinessUnitId?: string;
  active: boolean;
  sequence: number;
};

export type TGetListRawEmpPosition = {
  positionId: string;
  businessUnitId: string;
  label: string;
  levelOnes: TRawEmpPositionLevelOne[];
};

export type TRawEmpPositionLevelOne = TRawEmpPosition & {
  levelTwos: TRawEmpPositionLevelTwo[];
};

export type TRawEmpPositionLevelTwo = TRawEmpPosition & {
  levelThrees: TRawEmpPosition[];
}

export type TRawEmpPosition = {
  parentBusinessUnitId?: string;
  businessUnitId: string;
  label: string;
}

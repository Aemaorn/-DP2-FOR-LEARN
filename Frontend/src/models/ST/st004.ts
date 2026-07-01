import type { PermissionType } from '@/enums/role';
import type { TPaginated } from '../shared/paginated';

export type TSt004Criteria = {
  keyword?: string;
} & TPaginated;

export type TSt004List = {
  code: string;
  name: string;
};

export type TSt004Detail = {
  code?: string;
  name: string;
  isActive: boolean;
  programPermissions: Permission[];
};

export type Permission = {
  programId: string;
  groupName: string;
  code: string;
  name: string;
  permission: PermissionType;
  isView: boolean;
  isManage: boolean;
};

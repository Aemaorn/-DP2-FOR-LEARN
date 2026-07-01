export enum PermissionType {
  none = 0b0000,
  isView = 0b0001,
  isManage = isView | 0b0010,
}

export enum PermissionStrType {
  None = 'None',
  View = 'View',
  Manage = 'Manage'
}

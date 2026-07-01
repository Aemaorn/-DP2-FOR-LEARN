import type { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import type { Attachments } from './uploadFile';

export type TConfirmSeverity = 'secondary' | 'success' | 'info' | 'warn' | 'help' | 'danger' | 'contrast';

export type TConfirmDialog = {
  dialogType?: ConfirmDialogType;
  title?: string;
  description?: string;
  cancelText?: string;
  confirmText?: string;
  hideCancel?: boolean;
  confirmSeverity?: TConfirmSeverity;
};

export type TReasonDialog = {
  dialogType: ReasonDialogType;
  title?: string;
  cancelText?: string;
  confirmText?: string;
  isRequired?: boolean;
  oldRemark?: string;
};

export type TDialogResult = {
  isConfirm: boolean;
  reason?: string;
  attachments?: Attachments[];
};

export type TPartnerDialog = {
  searchText?: string;
};


export interface ActivityHistory {
  showExpand?: boolean;
  groupName: string;
  lastedActivity: LastedActivity;
  activityLogs: LastedActivity[];
}

export interface LastedActivity {
  id: string;
  createdAt: Date;
  createdByName: string;
  activityAction: string;
  activityStatus: string;
  activityType: string;
  activityRemark?: string;
}
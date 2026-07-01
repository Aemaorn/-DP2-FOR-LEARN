import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import type { TConfirmSeverity, TDialogResult, TPartnerDialog } from '@/models/shared/dialog';
import type { TSt003List } from '@/models/ST/st003';
import type { TUserDialog } from '@/models/ST/st005';
import { useConfirmDialogStore } from '@/stores/Shared/confirmDialog';
import { useReasonDialogStore } from '@/stores/Shared/reasonDialog';
import { useSaveOptionDialogStore } from '@/stores/Shared/saveOptionDialog';
import { useUserDialogStore } from '@/stores/Shared/userDialog';
import { useWinnerDialogStore } from '@/stores/Shared/winnerDialog';
import type { PP007GetWinnerResponse } from '@/views/PP/models/PP007/pp007Model';

export const showActivityDialog = (id: string, programName?: string, title?: string): void => {
  document.dispatchEvent(
    new CustomEvent<{ id: string; programName?: string; title?: string }>('onShowActivityDialog', {
      detail: { id, programName, title },
    })
  );
};

export const showConfirmDialogAsync = (
  dialogType?: ConfirmDialogType,
  title?: string,
  cancelText?: string,
  confirmText?: string,
  description?: string,
  hideCancel?: boolean,
  confirmSeverity?: TConfirmSeverity
): Promise<boolean | undefined> => {
  const store = useConfirmDialogStore();
  return store.onOpenDialog(dialogType, title, cancelText, confirmText, description, hideCancel, confirmSeverity);
};

/**
 * แสดง modal แจ้งเตือนแบบมีปุ่มเดียว (ตกลง)
 * ใช้กรณีแสดงข้อความแจ้งเตือนล้วน ๆ ไม่ต้องการ confirm/cancel
 */
export const showAlertDialogAsync = (
  title: string,
  confirmText: string = 'ตกลง',
  description?: string
): Promise<boolean | undefined> => {
  const store = useConfirmDialogStore();
  return store.onOpenDialog(undefined, title, undefined, confirmText, description, true);
};

export const showReasonDialogAsync = (
  dialogType: ReasonDialogType,
  isRequired?: boolean,
  title?: string,
  cancelText?: string,
  confirmText?: string,
  oldRemark?: string,
): Promise<TDialogResult> => {
  const store = useReasonDialogStore();
  return store.onOpenDialog(dialogType, isRequired, title, cancelText, confirmText, oldRemark);
};

export const showUserDialogAsync = (departmentCode?: string): Promise<TUserDialog | undefined> => {
  const store = useUserDialogStore();
  return store.onOpenDialog(departmentCode);
};

export const showPartnerDialogAsync = (searchText?: string): Promise<TSt003List> => {
  return new Promise((resolve): void => {
    const handler = (event: Event): void => {
      const customEvent = event as CustomEvent<TSt003List>;
      resolve(customEvent.detail);

      document.removeEventListener('onClosePartnerDialog', handler);
    };

    document.addEventListener('onClosePartnerDialog', handler);

    document.dispatchEvent(new CustomEvent<TPartnerDialog>('onShowPartnerDialog', {
      detail: {
        searchText: searchText,
      }
    }));
  });
};

export const showWinnerDialogAsync = (procurementId: string, jp006Id: string, keyword?: string, type?: string, isRental: boolean = false): Promise<PP007GetWinnerResponse | undefined> => {
  const store = useWinnerDialogStore();
  return store.onOpenDialog(procurementId, jp006Id, keyword, type, isRental);
};

export const showSaveOptionDialogAsync = (): Promise<boolean | undefined> => {
  const store = useSaveOptionDialogStore();
  return store.onOpenDialog();
};
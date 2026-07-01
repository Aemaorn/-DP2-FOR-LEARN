import type { ConfirmDialogType } from "@/enums/dialog";
import type { TConfirmDialog, TConfirmSeverity } from "@/models/shared/dialog";
import { defineStore } from "pinia";
import { ref } from "vue";

export const useConfirmDialogStore = defineStore('confirm-dialog-store', () => {
  const isShow = ref(false);
  let resolvePromise: ((value: boolean | undefined) => void) | undefined;

  const onOpenDialog = async (
    dialogType?: ConfirmDialogType,
    title?: string,
    cancelText?: string,
    confirmText?: string,
    description?: string,
    hideCancel?: boolean,
    confirmSeverity?: TConfirmSeverity
  ) => {

    document.dispatchEvent(
      new CustomEvent<TConfirmDialog>('onShowConfirmDialog', {
        detail: {
          dialogType,
          title,
          description,
          cancelText,
          confirmText,
          hideCancel,
          confirmSeverity,
        },
      })
    );

    isShow.value = true;

    return new Promise<boolean | undefined>((resolve) => {
      resolvePromise = resolve;
    });
  };

  // isConfirmed: true = ยืนยัน, false = ปุ่มยกเลิก, undefined = ปิด dialog (กากบาท)
  const closeDialog = (isConfirmed?: boolean) => {
    isShow.value = false;
    if (resolvePromise) {
      resolvePromise(isConfirmed);
      resolvePromise = undefined;
    }
  };

  return {
    isShow,

    // Functions,
    onOpenDialog,
    closeDialog,
  };
});
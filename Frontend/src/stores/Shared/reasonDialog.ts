import type { ReasonDialogType } from "@/enums/dialog";
import type { TDialogResult, TReasonDialog } from "@/models/shared/dialog";
import { defineStore } from "pinia";

export const useReasonDialogStore = defineStore('reason-dialog-store', () => {
  let resolvePromise: ((value: TDialogResult) => void) | undefined;

  const onOpenDialog = async (
    dialogType: ReasonDialogType,
    isRequired?: boolean,
    title?: string,
    cancelText?: string,
    confirmText?: string,
    oldRemark?: string,
  ) => {

    document.dispatchEvent(
      new CustomEvent<TReasonDialog>('onShowReasonDialog', {
        detail: {
          dialogType,
          title,
          cancelText,
          confirmText,
          isRequired,
          oldRemark,
        },
      })
    );

    return new Promise<TDialogResult>((resolve) => {
      resolvePromise = resolve;
    });
  };

  const closeDialog = (result: TDialogResult) => {
    if (resolvePromise) {
      resolvePromise(result);
      resolvePromise = undefined;
    }
  };

  return {
    // Functions,
    onOpenDialog,
    closeDialog,
  };
});
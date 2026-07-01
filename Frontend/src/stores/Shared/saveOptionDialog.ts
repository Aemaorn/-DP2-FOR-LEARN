import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useSaveOptionDialogStore = defineStore('save-option-dialog-store', () => {
  const isShow = ref(false);
  let resolvePromise: ((value: boolean | undefined) => void) | undefined;

  const onOpenDialog = () => {
    isShow.value = true;

    return new Promise<boolean | undefined>((resolve) => {
      resolvePromise = resolve;
    });
  };

  const closeDialog = (result: boolean | undefined) => {
    isShow.value = false;
    if (resolvePromise) {
      resolvePromise(result);
      resolvePromise = undefined;
    }
  };

  return {
    isShow,

    // Functions
    onOpenDialog,
    closeDialog,
  };
});
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { HttpStatusCode } from 'axios';
import type { TAttachmentFileAllBody, TAttachmentFileAllCriteria } from '@/models/attachmentFileAll';
import AttachmentFileAllService from '@/services/attachmentFileAll';

export const useAttachmentFileAllStore = defineStore('attachment-file-all-store', () => {
  const searchCriteria = ref<TAttachmentFileAllCriteria>({ procurementId: '' });
  const body = ref<TAttachmentFileAllBody | null>(null);

  const onSearchAsync = async (): Promise<void> => {
    const { data, status } = await AttachmentFileAllService.onGetAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      body.value = data;
    }
  };

  return {
    searchCriteria,
    body,
    onSearchAsync,
  };
});

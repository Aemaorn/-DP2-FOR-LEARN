import type { MappingDocumentResponse } from '@/models/shared/mappingDocument';
import MappingDocumentService from '@/services/Shared/mappingDocument';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useMappingDocumentDialogStore = defineStore('mapping-document-dialog-store', () => {
  const criteria = ref<MappingDocumentResponse>({
    single: [],
    multiple: [],
  } as MappingDocumentResponse);

  const onGetMappingDocumentAsync = async (pathToGet: string): Promise<void> => {
    const { data, status } =
      await MappingDocumentService.onGetMappingDocumentServiceAsync(pathToGet);

    if (status === HttpStatusCode.Ok) {
      criteria.value = data;
    }
  };

  return {
    criteria,
    onGetMappingDocumentAsync,
  };
});

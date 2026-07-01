<script setup lang="ts">
import { ref } from 'vue';
import ChEditor from '@/components/Document/ChEditor.vue';
import { useMenuStore } from '@/stores/menu';
import { useRP002DetailStore } from '@/stores/RP/RP002/detail';

type Props = {
  readonly?: boolean;
}

const menuStore = useMenuStore();
const store = useRP002DetailStore();
const docId = defineModel<string | undefined>({
  required: true,
});

const { readonly } = defineProps<Props>();

const requestApproveDocRef = ref<InstanceType<typeof ChEditor> | null>(null);

const setPlaceholderInDocument = (text: string, hint?: string): void => {
  if (requestApproveDocRef.value) {
    // Assuming the method setPlaceholderInDocument is defined in ChEditor
    // and it takes a document ID as an argument.
    requestApproveDocRef.value.setPlaceholderInDocument(text, hint);
  }
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not read-only)
    if (requestApproveDocRef.value?.saveAndWait && menuStore.hasManage && !readonly) {
      requestApproveDocRef.value.saveAndWait(() => {
        resolve();
      });
    } else {
      resolve();
    }
  });
};

// Exposing methods for parent components
defineExpose({
  setPlaceholderInDocument,
  saveDocumentFirst,
});
</script>

<template>
  <ChEditor v-if="docId" :docId :docName="`approve-doc-${new Date().toDateString()}`"
    :readonly="!menuStore.hasManage || readonly" ref="requestApproveDocRef" :key="`${docId}-${store.body.status}`"
    :versions="store.body.documentVersions ?? []" :canRestoreVersion="store.status.canRestoreVersion"
    @restore-version="store.resetDocumentAsync" />
</template>

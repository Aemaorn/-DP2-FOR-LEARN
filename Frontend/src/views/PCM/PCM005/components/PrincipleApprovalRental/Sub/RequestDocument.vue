<script setup lang="ts">
import { ref } from 'vue';
import ChEditor from '@/components/Document/ChEditor.vue';
import type { DocumentVersion } from '@/models/shared/document';

const props = defineProps<{
  docId: string;
  isDisable: boolean;
  versions?: DocumentVersion[];
  canRestoreVersion?: boolean;
}>();

const emit = defineEmits<{
  (e: 'restore-version'): void;
}>();

const docRef = ref<InstanceType<typeof ChEditor> | null>(null);

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (docRef.value) {
    docRef.value.setPlaceholderInDocument(text, hint);
  }
};

const handleVersionRestored = () => {
  emit('restore-version');
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not disabled)
    if (docRef.value?.saveAndWait && !props.isDisable) {
      docRef.value.saveAndWait(() => {
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
  <ChEditor
    :docId="props.docId"
    :docName="`principle-approval-rental-approval-${new Date().toISOString()}-${props.docId}`"
    :readonly="isDisable"
    v-if="props.docId"
    :key="props.docId"
    :versions="props.versions ?? []"
    :canRestoreVersion="props.canRestoreVersion"
    @restore-version="handleVersionRestored" />
</template>

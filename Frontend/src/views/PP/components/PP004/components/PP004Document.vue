<script setup lang="ts">
import ChEditor from '@/components/Document/ChEditor.vue';
import { ref } from 'vue';
import type { DocumentVersion } from '@/models/shared/document';

type Props = {
  readonly?: boolean;
  versions?: DocumentVersion[];
  canRestoreVersion?: boolean;
  save?: () => void;
}

const props = withDefaults(defineProps<Props>(), {
  readonly: false,
  canRestoreVersion: false,
});

const emit = defineEmits<{
  restoreVersion: [];
}>();

const docId = defineModel<string>({
  required: true,
})

const documentRef1 = ref<InstanceType<typeof ChEditor> | null>(null);

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (documentRef1.value) {
    // Assuming the method setPlaceholderInDocument is defined in ChEditor
    // and it takes a document ID as an argument.
    documentRef1.value.setPlaceholderInDocument(text, hint);
  }
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not read-only)
    if (documentRef1.value?.saveAndWait && !props.readonly) {
      documentRef1.value.saveAndWait(() => {
        resolve();
      });
    } else {
      resolve();
    }
  });
};

const handleRestoreVersion = () => {
  emit('restoreVersion');
};

// Exposing methods for parent components
defineExpose({
  setPlaceholderInDocument,
  saveDocumentFirst,
});
</script>

<template>
  <ChEditor :docId="docId" :docName="new Date().toISOString()" :readonly="props.readonly"
    ref="documentRef1" :key="docId" :save="props.save"
    :versions="props.versions"
    :canRestoreVersion="props.canRestoreVersion"
    @restore-version="handleRestoreVersion" />
</template>
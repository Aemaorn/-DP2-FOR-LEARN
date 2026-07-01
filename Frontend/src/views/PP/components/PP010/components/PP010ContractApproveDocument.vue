<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { ref } from 'vue';
import ChEditor from '@/components/Document/ChEditor.vue';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
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

const store = useContractDraftStore();

const documentRef = ref<InstanceType<typeof ChEditor> | null>(null);

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (documentRef.value) {
    // Assuming the method setPlaceholderInDocument is defined in ChEditor
    // and it takes a document ID as an argument.
    documentRef.value.setPlaceholderInDocument(text, hint);
  }
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not read-only)
    if (documentRef.value?.saveAndWait && !props.readonly) {
      documentRef.value.saveAndWait(() => {
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
  <Card>
    <template #content>
      <TitleHeader label="เอกสารลงนามสัญญา" />
      <ChEditor v-if="store.body.approvalContractDraftDocumentId" :docId="store.body.approvalContractDraftDocumentId"
        :docName="new Date().toISOString()" :readonly="props.readonly"
        ref="documentRef" :key="store.body.approvalContractDraftDocumentId"
        :save="props.save"
        :versions="props.versions"
        :canRestoreVersion="props.canRestoreVersion"
        @restore-version="handleRestoreVersion" />
    </template>
  </Card>
</template>
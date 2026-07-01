<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { ref } from 'vue';
import ChEditor from '@/components/Document/ChEditor.vue';
import { usePcmContractDraftStore } from '@/stores/PCM/PCM005/pcmContractDraft';

type Props = {
  readonly?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  readonly: false,
});

const store = usePcmContractDraftStore();

const documentRef = ref<InstanceType<typeof ChEditor> | null>(null);

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (documentRef.value) {
    documentRef.value.setPlaceholderInDocument(text, hint);
  }
};

const handleVersionRestored = async () => {
  await store.api.resetDocumentAsync('ContractDraft');
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

// Exposing methods for parent components
defineExpose({
  setPlaceholderInDocument,
  saveDocumentFirst,
});
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="เอกสารร่างสัญญาหรือข้อตกลง" />
      <ChEditor
        v-if="store.body.contractDraftDocumentId"
        :docId="store.body.contractDraftDocumentId"
        :docName="new Date().toISOString()"
        :readonly="props.readonly"
        ref="documentRef"
        :key="store.body.contractDraftDocumentId"
        :versions="store.body.contractDraftDocumentVersions ?? []"
        :canRestoreVersion="store.states.canRestoreContractDraftDocument"
        @restore-version="handleVersionRestored" />
    </template>
  </Card>
</template>
<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { ref } from 'vue';
import ChEditor from '@/components/Document/ChEditor.vue';
import { usePurchaseOrder } from '@/views/PP/stores/PP007/PP007Store';
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

const store = usePurchaseOrder();

const documentRef = ref<InstanceType<typeof ChEditor> | null>(null);

const setPlaceholderInDocument = (text: string, hint?: string): void => {
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
      <TitleHeader label="เอกสาร จพ.006" />
      <ChEditor v-if="store.body.jp006DocumentId" :docId="store.body.jp006DocumentId" docName="bankPP005"
        :readonly="props.readonly" ref="documentRef" :key="`${store.body.jp006DocumentId}-${store.body.status}`"
        :save="props.save" :versions="props.versions" :canRestoreVersion="props.canRestoreVersion"
        @restore-version="handleRestoreVersion" />
    </template>
  </Card>
</template>
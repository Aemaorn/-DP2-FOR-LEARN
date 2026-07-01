<script setup lang="ts">
import { ref } from 'vue';
import ChEditor from '@/components/Document/ChEditor.vue';
import { useMenuStore } from '@/stores/menu';
import { usePcm003DetailStore } from '@/stores/PCM/pcm003';
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

const menuStore = useMenuStore();
const pcm003Store = usePcm003DetailStore();

const requestWinnerDocRef = ref<InstanceType<typeof ChEditor> | null>(null);
const docId = defineModel<string | undefined>({
  required: true,
});

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (requestWinnerDocRef.value) {
    // Assuming the method setPlaceholderInDocument is defined in ChEditor
    // and it takes a document ID as an argument.
    requestWinnerDocRef.value.setPlaceholderInDocument(text, hint);
  }
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not read-only)
    if (requestWinnerDocRef.value?.saveAndWait && !props.readonly) {
      requestWinnerDocRef.value.saveAndWait(() => {
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
  <ChEditor v-if="docId" :docId="docId" :docName="`winner-doc-${new Date().toDateString()}`"
    :readonly="!menuStore.hasManage || props.readonly" ref="requestWinnerDocRef" :key="`${docId}-${pcm003Store.detail.status}`"
    :save="props.save"
    :versions="props.versions"
    :canRestoreVersion="props.canRestoreVersion"
    @restore-version="handleRestoreVersion" />
</template>
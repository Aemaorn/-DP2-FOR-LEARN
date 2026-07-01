<script setup lang="ts">
import { usePcm005PrincipleStore } from '@/stores/PCM/PCM005/principle';
import { ref, computed } from 'vue';
import { useMenuStore } from '@/stores/menu';
import ChEditor from '@/components/Document/ChEditor.vue';

const props = defineProps<{
  save?: () => void;
  readonly?: boolean;
}>();

const menuStore = useMenuStore();
const store = usePcm005PrincipleStore();

const approveDocRef = ref<InstanceType<typeof ChEditor> | null>(null);

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (approveDocRef.value) {
    approveDocRef.value.setPlaceholderInDocument(text, hint);
  }
};

const documentVersions = computed(() => store.body.documentVersions ?? []);

const handleVersionRestored = async () => {
  await store.resetDocumentAsync();
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (user has manage permission)
    if (approveDocRef.value?.saveAndWait && menuStore.hasManage) {
      approveDocRef.value.saveAndWait(() => {
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
    :docId="store.body.documentTemplateId"
    :docName="`invite-${new Date().toISOString()}`"
    :readonly="!menuStore.hasManage || props.readonly"
    ref="approveDocRef"
    v-if="store.body.documentTemplateId"
    :key="store.body.documentTemplateId"
    :save="props.save"
    :versions="documentVersions"
    :canRestoreVersion="store.status.canRestoreVersion"
    @restore-version="handleVersionRestored" />
</template>

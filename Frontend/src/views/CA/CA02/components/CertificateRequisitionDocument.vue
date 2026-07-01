<script setup lang="ts">
import ChEditor from '@/components/Document/ChEditor.vue';
import { CA02Status } from '@/enums/CA/ca02';
import { useCA02DetailStore } from '@/stores/CA/ca02';
import { useMenuStore } from '@/stores/menu';
import { computed, ref } from 'vue';

type Props = {
  save?: () => void;
}

const { save } = defineProps<Props>();

const docRef = ref<InstanceType<typeof ChEditor> | null>(null);
const menuStore = useMenuStore();
const store = useCA02DetailStore();

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (docRef.value) {
    // Assuming the method setPlaceholderInDocument is defined in ChEditor
    // and it takes a document ID as an argument.
    docRef.value.setPlaceholderInDocument(text, hint);
  }
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not read-only)
    if (docRef.value?.saveAndWait && canEditDocument.value && menuStore.hasManage) {
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

const canEditDocument = computed(() => {
  return [CA02Status.Draft, CA02Status.Edit, CA02Status.Rejected].includes(store.body.status);

});
</script>

<template>
  <Card>
    <template #content>
      <ChEditor v-if="store.body.documentId" :docId="store.body.documentId"
        :docName="`certificate-${new Date().toISOString()}`" :readonly="!menuStore.hasManage || !canEditDocument"
        :key="`${store.body.documentId}-${store.body.status}`" ref="docRef" :save="save"
        :versions="store.body.documentVersions ?? []"
        :canRestoreVersion="store.states.canRestoreVersion"
        @restore-version="store.fn.resetDocumentAsync" />
    </template>
  </Card>
</template>

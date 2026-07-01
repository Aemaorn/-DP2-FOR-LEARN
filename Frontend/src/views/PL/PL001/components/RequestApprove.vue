<script setup lang="ts">
import { computed, ref } from 'vue';
import { usePL001DetailStore } from '@/stores/PL/pl001';
import { useMenuStore } from '@/stores/menu';
import ChEditor from '@/components/Document/ChEditor.vue';
import { PlanStatus } from '@/enums/plan';
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

const store = usePL001DetailStore();
const menuStore = useMenuStore();
const annoucementDocRef = ref<InstanceType<typeof ChEditor> | null>(null);

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (annoucementDocRef.value) {
    // Assuming the method setPlaceholderInDocument is defined in ChEditor
    // and it takes a document ID as an argument.
    annoucementDocRef.value.setPlaceholderInDocument(text, hint);
  }
};

const clickSave = () => {
  if (annoucementDocRef.value) {
    annoucementDocRef.value.clickSave();
  }
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not read-only)
    if (annoucementDocRef.value?.saveAndWait && store.canEditDocument && menuStore.hasManage && canEditDocument.value) {
      annoucementDocRef.value.saveAndWait(() => {
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
  clickSave,
  saveDocumentFirst,
});

const canEditDocument = computed(() => {
  return store.body.status === PlanStatus.WaitingAssign
    || store.body.status === PlanStatus.Assigned
    || store.body.status === PlanStatus.DraftRecordDocument
    || store.body.status === PlanStatus.RejectToAssignee
});
</script>

<template>
  <ChEditor :docId="store.body.planDocumentId"
    :docName="`plan-${new Date().toISOString()}-${store.body.planDocumentId}`"
    :readonly="!store.canEditDocument || !menuStore.hasManage || !canEditDocument" ref="annoucementDocRef"
    v-if="store.body.planDocumentId" :key="`${store.body.planDocumentId}-${store.body.status}`" :save="props.save"
    :versions="props.versions" :canRestoreVersion="props.canRestoreVersion" @restore-version="handleRestoreVersion" />
</template>

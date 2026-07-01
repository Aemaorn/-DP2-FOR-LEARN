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
const requestApproveDocRef = ref<InstanceType<typeof ChEditor> | null>(null);

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (requestApproveDocRef.value) {
    // Assuming the method setPlaceholderInDocument is defined in ChEditor
    // and it takes a document ID as an argument.
    requestApproveDocRef.value.setPlaceholderInDocument(text, hint);
  }
};

const clickSave = () => {
  if (requestApproveDocRef.value) {
    requestApproveDocRef.value.clickSave();
  }
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not read-only)
    if (requestApproveDocRef.value?.saveAndWait && store.canEditDocument && menuStore.hasManage && canEditDocument.value) {
      requestApproveDocRef.value.saveAndWait(() => {
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
  <ChEditor :docId="store.body.planAnnouncementDocumentId" :docName="`announcement-${new Date().toISOString()}`"
    :readonly="!store.canEditDocument || !menuStore.hasManage || !canEditDocument" ref="requestApproveDocRef"
    :key="`${store.body.planAnnouncementDocumentId}-${store.body.status}`" v-if="store.body.planAnnouncementDocumentId"
    :save="props.save" :versions="props.versions" :canRestoreVersion="props.canRestoreVersion"
    @restore-version="handleRestoreVersion" />
</template>

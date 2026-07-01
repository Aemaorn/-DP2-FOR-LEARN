<script setup lang="ts">
import ChEditor from '@/components/Document/ChEditor.vue';
import { PlanAnnouncementStatus } from '@/enums/planAnnouncement';
import { useMenuStore } from '@/stores/menu';
import { usePl002DetailStore } from '@/stores/PL/pl002';
import { computed, ref } from 'vue';
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

const documentRef = ref<InstanceType<typeof ChEditor> | null>(null);
const store = usePl002DetailStore();
const menuStore = useMenuStore();

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
    if (documentRef.value?.saveAndWait && menuStore.hasManage && canEditDocument.value) {
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

const canEditDocument = computed(() => {
  return store.body.status === PlanAnnouncementStatus.Draft
  || store.body.status === PlanAnnouncementStatus.Rejected
  || store.body.status === PlanAnnouncementStatus.WaitingAssign
});
</script>

<template>
  <Card>
    <template #content>
      <ChEditor
        v-if="store.body.approveDocumentId"
        :docId="store.body.approveDocumentId"
        :docName="`approve-${new Date().toISOString()}`"
        :readonly="!menuStore.hasManage || !canEditDocument"
        :key="store.body.approveDocumentId"
        ref="documentRef"
        :save="props.save"
        :versions="props.versions"
        :canRestoreVersion="props.canRestoreVersion"
        @restore-version="handleRestoreVersion"
      />
    </template>
  </Card>
</template>

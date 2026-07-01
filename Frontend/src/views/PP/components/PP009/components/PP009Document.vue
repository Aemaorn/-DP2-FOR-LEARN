<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { ref } from 'vue';
import ChEditor from '@/components/Document/ChEditor.vue';
import PP009Service from '@/views/PP/services/PP009/PP009Service';
import { HttpStatusCode } from 'axios';
import ToastHelper from '@/helpers/toast';
import type { DocumentVersion } from '@/models/shared/document';

type Props = {
  readonly?: boolean;
  docId?: string;
  versions?: DocumentVersion[];
  procurementId?: string;
  contractInvitationId?: string;
  vendorId?: string;
  canRestoreVersion?: boolean;
};

const props = withDefaults(defineProps<Props>(), {
  readonly: false,
  canRestoreVersion: false,
});

const emit = defineEmits<{
  versionRestored: [];
}>();

const documentRef = ref<InstanceType<typeof ChEditor> | null>(null);

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (documentRef.value) {
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
      // No editor, saveAndWait not available, or read-only, resolve immediately
      resolve();
    }
  });
};

const handleRestoreVersion = async () => {
  if (!props.procurementId || !props.contractInvitationId || !props.vendorId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถดึง version ได้ ข้อมูลไม่ครบถ้วน');
    return;
  }

  const { status } = await PP009Service.resetDocumentAsync(
    props.procurementId,
    props.contractInvitationId,
    props.vendorId
  );

  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    emit('versionRestored');
  }
};

defineExpose({
  setPlaceholderInDocument,
  saveDocumentFirst,
});
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="เอกสารเชิญชวนทำสัญญา" />
      <ChEditor
        v-if="props.docId"
        :docId="props.docId"
        :docName="`ca-invite-${new Date().toISOString()}-${props.docId}`"
        :readonly="props.readonly"
        :versions="props.versions"
        :canRestoreVersion="props.canRestoreVersion"
        ref="documentRef"
        :key="props.docId"
        @restore-version="handleRestoreVersion"
      />
      <div v-else class="no-document-message">
        <span class="material-symbols-outlined">description</span>
        <p>ยังไม่มีเอกสาร กรุณาบันทึกข้อมูลเพื่อสร้างเอกสาร</p>
      </div>
    </template>
  </Card>
</template>

<style scoped>
.no-document-message {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px;
  color: #64748b;
  background: #f8fafc;
  border: 1px dashed #cbd5e1;
  border-radius: 8px;
}

.no-document-message .material-symbols-outlined {
  font-size: 48px;
  margin-bottom: 12px;
  color: #94a3b8;
}

.no-document-message p {
  margin: 0;
  font-size: 14px;
}
</style>

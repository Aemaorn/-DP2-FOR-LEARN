<script setup lang="ts">
import { ref } from 'vue';
import ChEditor from '@/components/Document/ChEditor.vue';
import PP006Service from '@/views/PP/services/PP006/PP006Service';
import { HttpStatusCode } from 'axios';
import ToastHelper from '@/helpers/toast';
import type { DocumentVersion } from '@/models/shared/document';
import { useAuthenticationStore } from '@/stores/authentication';

type Props = {
  readonly?: boolean;
  versions?: DocumentVersion[];
  procurementId?: string;
  inviteId?: string;
  entrepreneurId?: string;
  entrepreneurName?: string;
  canRestoreVersion?: boolean;
  save?: () => Promise<void> | void;
}

const authStore = useAuthenticationStore();
const requestApproveDocRef = ref<InstanceType<typeof ChEditor> | null>(null);
const props = withDefaults(defineProps<Props>(), {
  canRestoreVersion: false,
});

const emit = defineEmits<{
  versionRestored: [];
}>();

const value = defineModel<string>({
  required: true,
});

// Handle save: call parent's save function (same as form save button)
const handleSave = async () => {
  if (props.save) {
    await props.save();
  }
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not read-only)
    if (requestApproveDocRef.value?.saveAndWait && !props.readonly) {
      requestApproveDocRef.value.saveAndWait(() => {
        resolve();
      });
    } else {
      // No editor, saveAndWait not available, or read-only, resolve immediately
      resolve();
    }
  });
};

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (requestApproveDocRef.value) {
    requestApproveDocRef.value.setPlaceholderInDocument(text, hint);
  }
};

const handleRestoreVersion = async () => {
  if (!props.procurementId || !props.inviteId || !props.entrepreneurId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถดึง version ได้ ข้อมูลไม่ครบถ้วน');
    return;
  }

  const { status } = await PP006Service.resetDocumentByEntrepreneurAsync(
    props.procurementId,
    props.inviteId,
    props.entrepreneurId,
    authStore.profile.id
  );

  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    emit('versionRestored');
  }
};

// Exposing methods for parent components
defineExpose({
  setPlaceholderInDocument,
  saveDocumentFirst,
});
</script>

<template>
  <div class="entrepreneur-document">
    <!-- Entrepreneur name header -->
    <div v-if="props.entrepreneurName" class="entrepreneur-header">
      <span class="material-symbols-outlined">person</span>
      <span class="entrepreneur-name">{{ props.entrepreneurName }}</span>
    </div>

    <!-- Editor -->
    <ChEditor
      v-if="value"
      :docId="value"
      :docName="`invite-${props.entrepreneurId}-${new Date().toISOString()}`"
      :readonly="props.readonly"
      :versions="props.versions"
      :canRestoreVersion="props.canRestoreVersion"
      :save="handleSave"
      ref="requestApproveDocRef"
      :key="value"
      @restore-version="handleRestoreVersion"
    />

    <!-- No document message -->
    <div v-else class="no-document-message">
      <span class="material-symbols-outlined">description</span>
      <p>ยังไม่มีเอกสาร กรุณาบันทึกข้อมูลเพื่อสร้างเอกสาร</p>
    </div>
  </div>
</template>

<style scoped>
.entrepreneur-document {
  margin-bottom: 24px;
}

.entrepreneur-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  background: #f1f5f9;
  border-radius: 8px 8px 0 0;
  border: 1px solid #e2e8f0;
  border-bottom: none;
}

.entrepreneur-header .material-symbols-outlined {
  font-size: 20px;
  color: #475569;
}

.entrepreneur-name {
  font-weight: 600;
  color: #1e293b;
  font-size: 14px;
}

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

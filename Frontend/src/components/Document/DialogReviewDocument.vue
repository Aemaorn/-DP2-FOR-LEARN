<script setup lang="ts">
import { Dialog } from 'primevue';
import ChEditor from '@/components/Document/ChEditor.vue';
import ButtonSave from '../Button/ButtonSave.vue';
import { ref } from 'vue';
import ButtonConfirmDocument from '../Button/ButtonConfirmDocument.vue';

const editorRef = ref<InstanceType<typeof ChEditor> | null>(null);

const onClickSave = () => {
  editorRef.value?.clickSave();
};

const props = defineProps<{
  docId: string;
  docName: string;
}>();

const emit = defineEmits<{
  (e: 'onClickUseDocument', text: string): void;
}>();

const showReviewDocumentDialog = defineModel<boolean>({ default: false });

const onSelectDocument = (docId: string): void => {
  emit('onClickUseDocument', docId);
};
</script>

<template>
  <Dialog v-model:visible="showReviewDocumentDialog" modal :style="{ width: '100%' }">
    <template #header>
      <div class="w-full">
        <h6>ตัวอย่างเอกสาร</h6>
      </div>
      <div class="w-full flex justify-end mt-2">
        <div id="edit-section" class="flex items-center gap-2">
          <ButtonSave text="บันทึกเอกสาร" @click="onClickSave" />
          <ButtonConfirmDocument @click="onSelectDocument(props.docId)"  />
        </div>
      </div>
    </template>
    <div class="flex justify-end gap-4 mt-3">
      <ChEditor
        ref="editorRef"
        :docId="props.docId"
        :docName="`plan-${new Date().toISOString()}-${props.docId}`"
        :readonly="false"
        v-if="props.docId"
        :key="props.docId"
      />
    </div>
  </Dialog>
</template>

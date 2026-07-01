<script setup lang="ts">
import { Datepicker } from '@/components/forms';
import ButtonSave from '@/components/Button/ButtonSave.vue';
import ToastHelper from '@/helpers/toast';
import { ref } from 'vue';
import { Dialog } from 'primevue';
import Button from 'primevue/button';

type Props = {
  onSave: (date: Date) => Promise<void>;
};

const props = defineProps<Props>();
const visible = defineModel<boolean>('visible', { required: true });
const dateSignValue = ref<Date>();

const onSaveDateSign = async () => {
  if (!dateSignValue.value) {
    ToastHelper.error('ข้อมูลไม่ถูกต้อง', 'กรุณาระบุวันที่ลงนามในสัญญา');
    return;
  }

  await props.onSave(dateSignValue.value);
  visible.value = false;
};

const onCancel = () => {
  visible.value = false;
};
</script>

<template>
  <Dialog v-model:visible="visible" modal header="บันทึกวันที่ลงนามในสัญญา" :style="{ width: '30rem' }"
    :draggable="false" :closable="false">
    <div class="flex flex-col gap-4">
      <p>กรุณาระบุวันที่ลงนามในสัญญา</p>
      <Datepicker label="วันที่ลงนามในสัญญา" v-model="dateSignValue" rules="required" class="mt-2" />
      <div class="flex justify-end gap-2 mt-2">
        <Button label="ยกเลิก" severity="secondary" variant="outlined" @click="onCancel" />
        <ButtonSave @click="onSaveDateSign" />
      </div>
    </div>
  </Dialog>
</template>

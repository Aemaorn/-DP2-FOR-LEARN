<script setup lang="ts">
import { Button, Dialog } from 'primevue';

const visible = defineModel<boolean>('visible', { default: false });

const emit = defineEmits<{
  (e: 'select-reference'): void;
  (e: 'select-manual'): void;
  (e: 'cancel'): void;
}>();

const onSelectReference = () => emit('select-reference');
const onSelectManual = () => emit('select-manual');
const onCancel = () => emit('cancel');
</script>

<template>
  <Dialog v-model:visible="visible" modal :draggable="false" :closable="false" :dismissable-mask="false"
    :style="{ width: '640px' }" :breakpoints="{ '1199px': '75vw', '575px': '95vw' }">
    <template #container>
      <div class="flex flex-col bg-white rounded-2xl">
        <div class="p-6 text-center">
          <h2 class="text-lg font-bold text-gray-800">กรุณาเลือกวิธีการสร้างเอกสาร</h2>
          <p class="text-sm text-gray-500 mt-1">เลือกว่าจะดึงข้อมูลจากเอกสารอ้างอิงในระบบ หรือสร้างใหม่โดยกรอกข้อมูลเอง</p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 px-6">
          <button type="button"
            class="flex flex-col items-center justify-between rounded-xl border-2 border-gray-200 hover:border-blue-500 hover:shadow-md transition p-6 cursor-pointer bg-white"
            @click="onSelectReference">
            <span class="material-symbols-outlined text-blue-600" style="font-size: 48px;">link</span>
            <div class="mt-3 text-center">
              <p class="text-base font-semibold text-gray-800">ค้นหาเอกสารอ้างอิง</p>
              <p class="text-xs text-gray-500 mt-1">ดึงข้อมูลจากสัญญาที่มีในระบบ</p>
            </div>
          </button>

          <button type="button"
            class="flex flex-col items-center justify-between rounded-xl border-2 border-gray-200 hover:border-blue-500 hover:shadow-md transition p-6 cursor-pointer bg-white"
            @click="onSelectManual">
            <span class="material-symbols-outlined text-blue-600" style="font-size: 48px;">edit_note</span>
            <div class="mt-3 text-center">
              <p class="text-base font-semibold text-gray-800">สร้างใหม่ (ไม่อ้างอิงเอกสาร)</p>
              <p class="text-xs text-gray-500 mt-1">กรอกข้อมูลด้วยตัวเอง ไม่ผูกกับเอกสารต้นทาง</p>
            </div>
          </button>
        </div>

        <div class="flex justify-center p-6">
          <Button label="ยกเลิก" severity="secondary" outlined class="w-40" @click="onCancel" />
        </div>
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { Dialog, Button } from 'primevue';
import { watch } from 'vue';
import { useRoute } from 'vue-router';
import { useSaveOptionDialogStore } from '@/stores/Shared/saveOptionDialog';
import { storeToRefs } from 'pinia';

const route = useRoute();
const store = useSaveOptionDialogStore();
const { isShow } = storeToRefs(store);
const { closeDialog } = store;

watch(() => route.path, () => {
  isShow.value = false;
});
</script>

<template>
  <Dialog v-model:visible="isShow" modal :draggable="false" :style="{ width: '70vw' }"
    :breakpoints="{ '1199px': '85vw', '575px': '95vw' }">
    <template #container>
      <div class="p-6">
        <!-- Header -->
        <div class="flex justify-between items-center mb-6">
          <h6 class="font-bold text-lg">ยืนยันการบันทึกข้อมูล</h6>
          <span class="material-symbols-outlined cursor-pointer" @click="closeDialog(undefined)">
            close
          </span>
        </div>

        <!-- Option Cards -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
          <!-- Option 1: Save Form with Reset Document (destructive) -->
          <div class="border border-gray-200 rounded-lg p-6 flex flex-col items-center">
            <h6 class="font-semibold mb-4">บันทึกข้อมูลบนหน้าจอพร้อมรีเซตเอกสาร (Ch-Editor)</h6>
            <div class="text-xl text-gray-600 text-left w-full space-y-2 mb-6 flex-1">
              <div class="flex items-start gap-2">
                <span class="material-symbols-outlined text-green-500 text-xl shrink-0 mt-0.5">check_circle</span>
                <span>บันทึกข้อมูลหน้าจอ</span>
              </div>
              <div class="flex items-start gap-2">
                <span class="material-symbols-outlined text-green-500 text-xl shrink-0 mt-0.5">check_circle</span>
                <span>เอกสารฉบับเดิมที่เคยแก้ไข จะถูกแทนที่ทั้งหมด</span>
              </div>
            </div>
            <Button label="บันทึกข้อมูลบนหน้าจอ + รีเซตเอกสาร (Ch-Editor)" class="px-8" severity="success" @click="closeDialog(true)" />
          </div>

          <!-- Option 2: Save Form Only (safe, recommended) -->
          <div class="border border-gray-200 rounded-lg p-6 flex flex-col items-center">
            <h6 class="font-semibold mb-4">บันทึกข้อมูลบนหน้าจออย่างเดียว</h6>
            <div class="text-xl text-gray-600 text-left w-full space-y-2 mb-6 flex-1">
              <div class="flex items-start gap-2">
                <span class="material-symbols-outlined text-green-500 text-xl shrink-0 mt-0.5">check_circle</span>
                <span>บันทึกข้อมูลหน้าจอเท่านั้น เอกสารจะไม่เปลี่ยนแปลง</span>
              </div>
            </div>
            <Button label="บันทึกข้อมูลบนหน้าจอเท่านั้น" class="px-8" severity="success" @click="closeDialog(false)" />
          </div>
        </div>

        <!-- Cancel Button -->
        <div class="flex justify-center">
          <Button label="ยกเลิก" class="px-8" severity="secondary" variant="outlined"
            @click="closeDialog(undefined)" />
        </div>
      </div>
    </template>
  </Dialog>
</template>
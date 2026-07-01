<script setup lang="ts">
import { Dialog, Button } from 'primevue';
import { useEventListener } from '@vueuse/core';
import { onMounted, ref, watch } from 'vue';
import type { TConfirmDialog } from '@/models/shared/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { useRoute } from 'vue-router';
import { useConfirmDialogStore } from '@/stores/Shared/confirmDialog';
import { storeToRefs } from 'pinia';
import { Form as VeeForm } from 'vee-validate';

const config = ref({} as TConfirmDialog);
const route = useRoute();
const store = useConfirmDialogStore();
const { isShow } = storeToRefs(store);
const { closeDialog } = store;

onMounted((): void => {
  useEventListener(document, 'onShowConfirmDialog', (data: CustomEvent<TConfirmDialog>): void => {

    config.value = data.detail;
  });

  useEventListener(document, 'keydown', (e: KeyboardEvent) => {
    if (!isShow.value || e.key !== 'Enter') return;

    e.preventDefault();
    closeDialog(true);
  });
});

const dialogMessage = (): { title: string, cancelText: string, confirmText: string } => {
  switch (config.value.dialogType) {
    case ConfirmDialogType.Delete:
      return { title: "ต้องการลบข้อมูลออกจากระบบหรือไม่?", cancelText: "ยกเลิก", confirmText: "ลบข้อมูล" };
    case ConfirmDialogType.Logout:
      return { title: "ต้องการออกจากระบบหรือไม่?", cancelText: "ยกเลิก", confirmText: "ออกจากระบบ" };
    case ConfirmDialogType.Edit:
      return { title: "ต้องการเรียกคืนแก้ไขหรือไม่?", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    case ConfirmDialogType.SendApprove:
      return { title: "ต้องการส่งเห็นชอบหรือไม่?", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    case ConfirmDialogType.SendConfirm:
      return { title: "ต้องการส่งอนุมัติหรือไม่?", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    case ConfirmDialogType.SendApproveConfirm:
      return { title: "ต้องการส่งเห็นชอบ/อนุมัติหรือไม่?", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    case ConfirmDialogType.Assigned:
      return { title: "ต้องการมอบหมายงานหรือไม่?", cancelText: "ยกเลิก", confirmText: "มอบหมาย" };
    case ConfirmDialogType.ConfirmTemplate:
      return { title: "หากเปลี่ยนรูปแบบเอกสาร จะเป็นการล้างข้อมูลทั้งหมด", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    case ConfirmDialogType.ConfirmChange:
      return { title: "หากเปลี่ยนแปลงข้อมูล จะเป็นการล้างข้อมูลทั้งหมด", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    case ConfirmDialogType.Changed:
      return { title: "ต้องการขอเปลี่ยนแปลงใช่หรือไม่?", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    case ConfirmDialogType.Canceled:
      return { title: "ต้องการขอยกเลิกใช่หรือไม่?", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    case ConfirmDialogType.ConfirmData:
      return { title: "ต้องการยืนยันข้อมูลหรือไม่?", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    case ConfirmDialogType.SendEdit:
      return { title: "ต้องการส่งกลับแก้ไขหรือไม่?", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    case ConfirmDialogType.AnnouncementPlan:
      return { title: "ต้องการเผยแพร่แผนใช่หรือไม่?", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    case ConfirmDialogType.ConfirmReplaceDocument:
      return { title: "คุณต้องการยืนยันการเปลี่ยนแปลงข้อมูลในเอกสารหรือไม่?", cancelText: "ไม่ยืนยัน", confirmText: "ยืนยันการเปลี่ยนแปลง" };
    case ConfirmDialogType.CancelClosePlan:
      return { title: "ต้องการยกเลิกปิดงานหรือไม่", cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
    default: return { title: config.value.title ?? '', cancelText: config.value.cancelText ?? "ยกเลิก", confirmText: config.value.confirmText ?? "ยืนยัน" };
  }
};

const dialogConfirmBtnColor = (): "secondary" | "success" | "info" | "warn" | "help" | "danger" | "contrast" | undefined => {
  switch (config.value.dialogType) {
    case ConfirmDialogType.Delete:
    case ConfirmDialogType.Logout:
      return undefined;
    case ConfirmDialogType.SendApprove:
    case ConfirmDialogType.SendConfirm:
    case ConfirmDialogType.SendApproveConfirm:
    case ConfirmDialogType.Edit:
    case ConfirmDialogType.Assigned:
    case ConfirmDialogType.Changed:
    case ConfirmDialogType.AnnouncementPlan:
    case ConfirmDialogType.ConfirmTemplate:
    case ConfirmDialogType.Canceled:
    case ConfirmDialogType.ConfirmData:
    case ConfirmDialogType.SendEdit:
      return 'success';

    case ConfirmDialogType.ConfirmReplaceDocument:
      return 'warn';

    case ConfirmDialogType.CancelClosePlan:
      return 'danger';

    default: return config.value.confirmSeverity;
  }
};

watch(() => route.path, () => {
  isShow.value = false;
});
</script>

<template>

  <Dialog v-model:visible="isShow" modal :draggable="false" :style="{ width: '60vw' }"
    :breakpoints="{ '1199px': '75vw', '575px': '90vw' }">
    <template #container>
      <VeeForm @submit="() => closeDialog(true)">
        <div class="h-full overflow-y-auto hide-scrollbar">
          <div class="sticky top-0 bg-white z-10 rounded-lg">
            <div class="p-4">
              <div class="flex gap-2 justify-between items-center p-2">
                <div class="flex gap-2 md:gap-4 items-center w-full">
                  <h6 class="font-bold">{{ dialogMessage().title }}</h6>
                </div>
                <span class="material-symbols-outlined cursor-pointer" @click="() => closeDialog(undefined)">
                  close
                </span>
              </div>
              <div v-if="config.description" class="bg-gray-100 rounded-lg p-3 mx-2 mt-2">
                <p class="text-sm text-gray-700"><span class="font-semibold">หมายเหตุ</span>&nbsp; {{ config.description
                }}</p>
              </div>
              <div class="flex items-center lg:justify-end gap-4 w-full mt-14">
                <Button v-if="!config.hideCancel" class="lg:px-6 lg:w-fit w-full" severity="secondary" variant="outlined"
                  :label="dialogMessage().cancelText" @click="() => closeDialog(false)" type="button" />
                <Button type="submit" class="lg:px-6 lg:w-fit w-full" :severity="dialogConfirmBtnColor()"
                  :label="dialogMessage().confirmText" />
              </div>
            </div>
          </div>
        </div>
      </VeeForm>
    </template>
  </Dialog>
</template>
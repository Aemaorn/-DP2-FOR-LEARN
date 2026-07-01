<script setup lang="ts">
import { Button } from 'primevue';
import InputArea from '../forms/InputArea.vue';
import { UploadFileGroup } from '../forms';
import { ReasonDialogType } from '@/enums/dialog';
import type { TReasonDialog } from '@/models/shared/dialog';
import type { Attachments } from '@/models/shared/uploadFile';
import { useEventListener } from '@vueuse/core';
import { computed, onMounted, ref, watch } from 'vue';
import { Form as VeeForm } from 'vee-validate';
import { useRoute } from 'vue-router';
import { useReasonDialogStore } from '@/stores/Shared/reasonDialog';

// ประเภทเอกสารแนบเริ่มต้นของ modal ปิดงาน
const CLOSE_PLAN_ATTACHMENT_TYPE = 'AttachmentType002';

const config = ref({} as TReasonDialog);
const route = useRoute();
const store = useReasonDialogStore();
const { closeDialog } = store;
const isShow = ref(false);
const reason = ref<string>('');
const attachments = ref<Attachments[]>([]);

const isClosePlan = computed(() => config.value.dialogType === ReasonDialogType.ClosePlan);

onMounted((): void => {
  useEventListener(document, 'onShowReasonDialog', (data: CustomEvent<TReasonDialog>): void => {
    reason.value = '';
    config.value = data.detail;

    attachments.value = config.value.dialogType === ReasonDialogType.ClosePlan
      ? [{ sequence: 1, documentTypeCode: CLOSE_PLAN_ATTACHMENT_TYPE, fileAttachments: [] }]
      : [];

    if (config.value.oldRemark) {
      reason.value = config.value.oldRemark;
    }

    if ([
      ReasonDialogType.Reject,
      ReasonDialogType.NotAgree,
      ReasonDialogType.RemarkOfficer,
      ReasonDialogType.RequestCancel,
      ReasonDialogType.RequestChange,
      ReasonDialogType.UnableToPerformDuties,
      ReasonDialogType.ClosePlan].includes(config.value.dialogType)) {
      config.value.isRequired = true;
    }

    isShow.value = true;
  });
});

const dialogMessage = (): { title: string, cancelText: string, confirmText: string, icon?: string } => {
  switch (config.value.dialogType) {
    case ReasonDialogType.Approve:
      return { title: "หมายเหตุอนุมัติ", cancelText: "ยกเลิก", confirmText: "ยืนยันอนุมัติ", icon: 'pi pi-user-plus' };
    case ReasonDialogType.Accepted:
      return { title: "หมายเหตุเห็นชอบ", cancelText: "ยกเลิก", confirmText: "ยืนยันเห็นชอบ", icon: 'pi pi-user-plus' };
    case ReasonDialogType.RemarkOfficer:
      return { title: "ความเห็นเจ้าหน้าที่พัสดุ", cancelText: "ยกเลิก", confirmText: "ยืนยันให้ความเห็น", icon: 'pi pi-user-plus' };
    case ReasonDialogType.Reject:
      return { title: "ต้องการส่งกลับแก้ไขหรือไม่", cancelText: "ยกเลิก", confirmText: "ส่งกลับแก้ไข", icon: 'pi pi-user-minus' };
    case ReasonDialogType.NotAgree:
      return { title: "ต้องการไม่เห็นชอบหรือไม่", cancelText: "ยกเลิก", confirmText: "ไม่เห็นชอบ", icon: 'pi pi-user-minus' };
    case ReasonDialogType.RequestChange:
      return { title: "ต้องการขอเปลี่ยนแปลงหรือไม่", cancelText: "ยกเลิก", confirmText: "ขอเปลี่ยนแปลง", icon: 'pi pi-user-edit' };
    case ReasonDialogType.RequestCancel:
      return { title: "ต้องการขอยกเลิกหรือไม่", cancelText: "ยกเลิก", confirmText: "ขอยกเลิก", icon: 'pi pi-user-minus' };
    case ReasonDialogType.UnableToPerformDuties:
      return { title: "หมายเหตุไม่สามารถปฎิบัติงานได้", cancelText: "ยกเลิก", confirmText: "ยืนยัน", icon: 'pi pi-user-minus' };
    case ReasonDialogType.Confirm:
      return { title: config.value.title ?? "หมายเหตุ", cancelText: "ยกเลิก", confirmText: "ยืนยัน", icon: 'pi pi-user-plus' };
    case ReasonDialogType.ClosePlan:
      return { title: "ปิดงาน", cancelText: "ยกเลิก", confirmText: "ยืนยันปิดงาน", icon: 'pi pi-lock' };
    default: return { title: config.value.title ?? '', cancelText: "ยกเลิก", confirmText: "ยืนยัน" };
  }
};

const severityColorByType = (): 'secondary' | 'success' | 'info' | 'warn' | 'help' | 'danger' | 'contrast' | undefined => {
  switch (config.value.dialogType) {
    case ReasonDialogType.Approve:
    case ReasonDialogType.Accepted:
    case ReasonDialogType.RemarkOfficer:
    case ReasonDialogType.RequestChange:
    case ReasonDialogType.RequestCancel:
    case ReasonDialogType.UnableToPerformDuties:
    case ReasonDialogType.Confirm:
      return 'success';

    case ReasonDialogType.Reject:
    case ReasonDialogType.NotAgree:
      return 'danger';

    default: return undefined;
  }
};

const conditionLabel = (): string => {
  if (config.value.dialogType === ReasonDialogType.RemarkOfficer) {
    return 'ความเห็น';
  }

  return 'เหตุผล';
};

const onConfirm = () => {
  closeDialog({
    isConfirm: true,
    reason: reason.value,
    attachments: isClosePlan.value ? attachments.value : undefined,
  });

  isShow.value = false;
};

const onCancel = () => {
  closeDialog({ isConfirm: false });

  isShow.value = false;
};

watch(isShow, (val: boolean) => {
  if (!val) {
    config.value.oldRemark = undefined;
  }
});

watch(() => route.path, () => {
  isShow.value = false;
});
</script>

<template>
  <Dialog v-model:visible="isShow" modal :draggable="false" :style="{ width: isClosePlan ? '80vw' : '50vw' }"
    :breakpoints="{ '1199px': '90vw', '575px': '95vw' }" maximizable>
    <template #container>
      <div class="h-full overflow-y-auto hide-scrollbar">
        <div class="sticky top-0 bg-white z-10 rounded-lg">
          <div class="p-4">
            <div class="flex gap-2 justify-between items-center p-2">
              <div class="flex gap-2 md:gap-4 items-center w-full">
                <h6 class="font-bold">{{ dialogMessage().title }}</h6>
              </div>
              <span class="material-symbols-outlined cursor-pointer" @click="() => onCancel()">
                close
              </span>
            </div>
            <VeeForm @submit="() => onConfirm()">
              <template #default>
                <div class="px-4 pt-2 mt-6">
                  <InputArea v-model="reason" :label="conditionLabel()"
                    :rules="`${config.isRequired ? 'required' : ''}`" />
                </div>
                <div class="px-4 pt-2" v-if="isClosePlan">
                  <UploadFileGroup v-model="attachments" />
                </div>
                <div class="flex items-center justify-end gap-4 px-4 mt-4">
                  <Button severity="secondary" variant="outlined" :label="dialogMessage().cancelText"
                    @click="() => onCancel()" />
                  <Button :icon="dialogMessage().icon" :label="dialogMessage().confirmText" type="submit"
                    :severity="severityColorByType()" />
                </div>
              </template>
            </VeeForm>
          </div>
        </div>
      </div>
    </template>
  </Dialog>
</template>

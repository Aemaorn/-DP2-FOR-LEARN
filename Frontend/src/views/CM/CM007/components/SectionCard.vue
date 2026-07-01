<script setup lang="ts">
import { ref } from 'vue';
import { Tag } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Form as VeeForm } from 'vee-validate';

type Props = {
  componentCode: string;
  componentName: string;
  isEdited?: boolean;
  disabled?: boolean;
};

defineProps<Props>();

const showOldData = ref(false);
</script>

<template>
  <div class="my-3 flex flex-col gap-4">
    <TitleHeader :label="componentName">
      <template #action>
        <Tag v-if="isEdited" value="มีการแก้ไข" severity="warn" class="text-xs" />
      </template>
    </TitleHeader>

    <div class="bg-white border border-gray-100 overflow-hidden border-l-4 border-l-gray-400">
      <button type="button"
        class="w-full flex items-center justify-between gap-2 px-4 py-3 bg-gray-50 hover:bg-gray-100 transition-colors text-gray-700"
        @click="showOldData = !showOldData">
        <div class="flex items-center gap-2">
          <span class="text-lg md:text-xl font-semibold">จากเดิม</span>
        </div>
        <i :class="showOldData ? 'pi pi-chevron-up' : 'pi pi-chevron-down'" class="text-sm font-bold" />
      </button>
      <div v-if="showOldData" class="old-data-wrapper pointer-events-none pb-2">
        <VeeForm as="div">
          <slot name="old" />
        </VeeForm>
      </div>
    </div>

    <div class="bg-white shadow-md border border-gray-100 overflow-hidden border-l-4 border-l-emerald-400">
      <div class="flex items-center gap-2 font-semibold tracking-wide px-4 py-3 bg-gray-50 text-gray-700">
        <span class="text-lg md:text-xl font-semibold">แก้ไขเป็น</span>
      </div>
      <div class="px-4 py-4">
        <div class="new-data-wrapper" :class="{ 'pointer-events-none opacity-75': disabled }">
          <slot name="new" />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Remove all shadows inside old-data section */
.old-data-wrapper :deep(*) {
  box-shadow: none !important;
}

/* Make all inputs in old-data section look like plain text labels */
.old-data-wrapper :deep(input),
.old-data-wrapper :deep(textarea),
.old-data-wrapper :deep(.p-inputtext),
.old-data-wrapper :deep(.p-textarea),
.old-data-wrapper :deep(.p-select .p-select-label) {
  border: none !important;
  border-bottom: 1px dashed #d1d5db !important;
  background: transparent !important;
  opacity: 1 !important;
  color: #374151 !important;
  font-weight: 500;
  cursor: default;
  box-shadow: none !important;
}

/* Datepicker: override large left padding from calendar icon */
.old-data-wrapper :deep(.dp__input),
.old-data-wrapper :deep(.dp__input_readonly) {
  border: none !important;
  border-bottom: 1px dashed #d1d5db !important;
  background: transparent !important;
  opacity: 1 !important;
  padding: 0.75rem 0.75rem 0.8rem 0.75rem !important;
  color: #374151 !important;
  font-weight: 500;
  cursor: default;
  box-shadow: none !important;
}

.old-data-wrapper :deep(.p-select),
.old-data-wrapper :deep(.p-inputnumber),
.old-data-wrapper :deep(.p-inputgroup),
.old-data-wrapper :deep(.p-inputgroup-addon),
.old-data-wrapper :deep(.p-card) {
  background: #ffffff !important;
  border: none !important;
  box-shadow: none !important;
  opacity: 1 !important;
}

.old-data-wrapper :deep(.p-card.p-component) {
  box-shadow: none !important;
}

/* Hide TitleHeader inside old data */
.old-data-wrapper :deep(.p-card-content > div.w-full.my-4) {
  display: none !important;
}

/* Hide TitleHeader label/line in new-data but keep action slot (buttons) visible */
.new-data-wrapper :deep(.p-card-content > div.w-full.my-4 > div > p),
.new-data-wrapper :deep(.p-card-content > div.w-full.my-4 > div > div.h-px),
.new-data-wrapper :deep(.p-card-content > div.w-full.my-4 > div > div.bg-primary) {
  display: none !important;
}

.new-data-wrapper :deep(.p-card-content > div.w-full.my-4 > div) {
  justify-content: flex-end;
}


.old-data-wrapper :deep(.p-card-body),
.old-data-wrapper :deep(.p-card-content) {
  background: transparent !important;
}

/* Hide action buttons, icons, drag handles, upload in old-data section */
.old-data-wrapper :deep(button),
.old-data-wrapper :deep(.p-button),
.old-data-wrapper :deep(.pi-trash),
.old-data-wrapper :deep(.pi-plus),
.old-data-wrapper :deep(.drag-handle),
.old-data-wrapper :deep(.p-select-dropdown),
.old-data-wrapper :deep(.p-datepicker-trigger),
.old-data-wrapper :deep(.p-inputicon),
.old-data-wrapper :deep(.p-datepicker-input-icon-container),
.old-data-wrapper :deep(.p-input-icon-right > i),
.old-data-wrapper :deep(.p-input-icon-left > i),
.old-data-wrapper :deep(.dp__input_icon),
.old-data-wrapper :deep(.dp__clear_icon) {
  display: none !important;
}
</style>

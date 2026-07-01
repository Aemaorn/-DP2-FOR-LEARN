<script setup lang="ts">
import type { Cm007Component } from '@/models/CM/cm007';
import Cm007Constants from '@/constants/CM/cm007';
import { Button, Checkbox, Dialog } from 'primevue';
import { computed, ref, watch } from 'vue';

type Props = {
  templateCode: string;
  components: Cm007Component[];
  disableForm?: boolean;
};

const props = defineProps<Props>();

const value = defineModel<boolean>({
  default: false,
});

const emit = defineEmits<{
  confirm: [selectedSections: { componentCode: string; componentName: string }[]];
}>();

const selectedCodes = ref<string[]>([]);

const availableSections = computed(() => {
  if (!props.templateCode) return [];

  const templateKey = Object.keys(Cm007Constants.TEMPLATE_SECTION_MAP).find(
    key => key === props.templateCode
  );

  if (templateKey) {
    return Cm007Constants.TEMPLATE_SECTION_MAP[templateKey];
  }

  return [];
});

watch(value, (val) => {
  if (val) {
    selectedCodes.value = props.components
      .filter(c => c.isEdited)
      .map(c => c.componentCode);
  }
});

const onConfirm = () => {
  const selected = availableSections.value
    .filter(s => selectedCodes.value.includes(s.componentCode))
    .map(s => ({ componentCode: s.componentCode, componentName: s.label }));

  emit('confirm', selected);
};

const isAllSelected = computed(() =>
  availableSections.value.length > 0 && selectedCodes.value.length === availableSections.value.length
);

const toggleSelectAll = () => {
  if (isAllSelected.value) {
    selectedCodes.value = [];
  } else {
    selectedCodes.value = availableSections.value.map(s => s.componentCode);
  }
};

const onCancel = () => {
  value.value = false;
};
</script>

<template>
  <Dialog v-model:visible="value" modal :draggable="false" :style="{ width: '600px' }"
    :breakpoints="{ '575px': '90vw' }" @hide="() => (value = false)">
    <template #header>
      <span class="font-bold text-lg">เลือกข้อที่จะแก้ไข</span>
    </template>
    <template #default>
      <div v-if="availableSections.length === 0" class="text-center text-gray-500 py-4">
        ไม่พบข้อมูล
      </div>
      <div v-else class="flex flex-col gap-3 py-2">
        <div class="flex items-center gap-2 p-2 rounded hover:bg-gray-50 border-b pb-3 mb-1">
          <Checkbox :modelValue="isAllSelected" @update:modelValue="toggleSelectAll" binary inputId="selectAll"
            :disabled="props.disableForm" />
          <label for="selectAll" class="cursor-pointer font-semibold">เลือกทั้งหมด</label>
        </div>
        <div v-for="section in availableSections" :key="section.componentCode"
          class="flex items-center gap-2 p-2 rounded hover:bg-gray-50">
          <Checkbox v-model="selectedCodes" :value="section.componentCode" :inputId="section.componentCode"
            :disabled="props.disableForm" />
          <label :for="section.componentCode" class="cursor-pointer">{{ section.label }}</label>
        </div>
      </div>
    </template>
    <template #footer>
      <div class="flex justify-end gap-2">
        <Button label="ยกเลิก" variant="outlined" @click="onCancel" />
        <Button label="ยืนยัน" :disabled="selectedCodes.length === 0 || props.disableForm" @click="onConfirm" />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { Dialog, Button, InputText } from 'primevue';
import { usePP005DetailStore } from '@/views/PP/stores/PP005/PP005Store';

const props = defineProps<{
  modelValue: boolean;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
}>();

const detailStore = usePP005DetailStore();
const jorPorNumber = ref('');

const onClose = (): void => {
  jorPorNumber.value = '';
  emit('update:modelValue', false);
};

const onSubmitAsync = async (): Promise<void> => {
  detailStore.body.jorPorNumber = jorPorNumber.value;
  await detailStore.fn.onSubmitAsync();
  onClose();
};

watch(() => props.modelValue, (newValue) => {
  if (newValue) {
    jorPorNumber.value = detailStore.body.jorPorNumber ?? '';
  } else {
    jorPorNumber.value = '';
  }
});
</script>

<template>
  <Dialog :visible="modelValue" modal header="บันทึกเลขที่คำสั่ง จพ." :style="{ width: '30rem' }"
    @update:visible="(val) => emit('update:modelValue', val)">
    <div class="space-y-4">
      <div>
        <label for="jp-number-input" class="block mb-2">
          เลขที่ จพ.<span class="text-red-500">*</span>
        </label>
        <InputText id="jp-number-input" v-model="jorPorNumber" class="w-full" />
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2">
        <Button label="ยกเลิก" severity="secondary" variant="outlined" @click="onClose" />
        <Button label="บันทึก" icon="pi pi-save" severity="success" @click="onSubmitAsync" />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { YearOptions } from '@/constants/date';
import Select from './Select.vue';
import { onMounted } from 'vue';

type Props = {
  name?: string;
  id?: string;
  rules?: string;
  disabled?: boolean;
  helperText?: string;
  hideDetails?: boolean;
  label?: string
  notSetDefault?: boolean;
}

const value = defineModel<string | number | undefined>({
  required: true,
});

const props = defineProps<Props>();

onMounted((): void => {
  if (!value.value && !props.notSetDefault) {
    value.value = new Date().getFullYear() + 543;
  }
});
</script>

<template>
  <Select :label="props.label ?? 'ปีงบประมาณ'" v-model="value" :options="YearOptions" :rules="rules"
    :disabled="disabled" :helper-text="helperText" :hide-details="hideDetails" />
</template>
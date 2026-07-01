<script setup lang="ts">
import VueDatePicker from '@vuepic/vue-datepicker';
import '@vuepic/vue-datepicker/dist/main.css'
import { FloatLabel } from 'primevue';
import { computed, ref } from 'vue';
import { Field } from 'vee-validate';
import { v4 as uuidv4 } from 'uuid';
import { th } from 'date-fns/locale';
import { getThaiMonthName, ToTHDateFullMonthOnly } from '@/helpers/dateTime';

type Props = {
  name?: string;
  id?: string;
  rules?: string;
  label?: string;
  placeholder?: string;
  disabled?: boolean;
  readonly?: boolean;
  helperText?: string;
  minDate?: Date;
  maxDate?: Date;
  disabledDates?: Date[];
  hideDetails?: boolean;
};

const props = defineProps<Props>();
const value = defineModel<Date | undefined>({
  required: true,
});
const emit = defineEmits<{
  onSelected: [date?: Date]
}>();
const key = uuidv4();
const id = ref(props.id ?? key);
const name = ref(props.name ?? key);
const isFocused = ref(false);

const formatBE = (date: Date): string => {
  if (!date) return '';

  return ToTHDateFullMonthOnly(date);
};

const computedMinDate = computed(() => {
  if (props.minDate) {
    const minDateProps = new Date(props.minDate);
    minDateProps.setHours(0, 0, 0, 0);

    return minDateProps;
  }

  return undefined;
});

const computeMaxDate = computed(() => {
  if (props.maxDate) {
    const minDateProps = new Date(props.maxDate);
    minDateProps.setHours(23, 59, 0, 0);

    return minDateProps;
  }

  return undefined;
});
</script>

<template>
  <Field v-model="value" :name="name" :rules="rules" v-slot="{ errorMessage }" as="div">
    <FloatLabel variant="on">
      <VueDatePicker :id="id" v-model="value" :format-locale="th" :format="formatBE" @focus="() => isFocused = true"
        @blur="() => isFocused = false" :enable-time-picker="false" cancelText="ยกเลิก" selectText="เลือก" auto-apply
        :class="[{ 'error-border border-red-500 border-1 rounded-md': errorMessage, 'p-inputwrapper-filled': value, 'p-inputwrapper-focus': isFocused }]"
        :disabled="props.disabled" :readonly="props.readonly" :disabled-dates="props.disabledDates"
        :min-date="computedMinDate" :max-date="computeMaxDate" @update:model-value="(e?: Date) => emit('onSelected', e)"
        :teleport="true">
        <template #year="{ value }">
          {{ (value + 543) }}
        </template>
        <template #year-overlay-value="{ value }">
          {{ (value + 543) }}
        </template>
        <template #month="{ value }">
          {{ getThaiMonthName(value) }}
        </template>
      </VueDatePicker>
      <label :for="id" class="p-float-label-always" v-if="props.label">
        {{ props.label }}
        <span v-if="props.rules?.includes('required')" class="text-red-500">*</span>
      </label>
    </FloatLabel>
    <small class="pl-2 text-gray-500!" v-if="props.helperText">
      {{ props.helperText }}
    </small>
    <small class="pl-2 text-red-500!" v-if="(!props.hideDetails)">
      {{ errorMessage }}
    </small>
  </Field>
</template>

<style lang="scss" scoped>
.p-float-label-always {
  font-size: 1.25rem !important;
  z-index: 2;
}

/* Default label position - push past calendar icon */
:deep(.p-floatlabel) label {
  top: 1.25rem !important;
  left: 2rem !important;
}

/* Floated label position - sit on the border like Select */
:deep(.p-floatlabel:has(.p-inputwrapper-filled)) label,
:deep(.p-floatlabel:has(.p-inputwrapper-focus)) label {
  left: 0.75rem !important;
}

:deep(.dp__input_readonly) {
  font-size: 1.15rem;
}

:deep(.dp__input),
:deep(.dp__input_readonly) {
  height: 2.8rem !important;
  padding: 0.75rem 0.75rem 0.8rem 2.5rem !important;
}

.error-border :deep(.dp__input_wrap),
.error-border :deep(.dp__input_readonly) {
  border: none !important;
  box-shadow: none !important;
}

:deep(.dp__disabled) {
  background-color: #E2E8F0 !important;
  color: #64748B !important
}
</style>
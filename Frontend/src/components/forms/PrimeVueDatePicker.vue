<script setup lang="ts">
import { DatePicker, FloatLabel } from 'primevue';
import { Field } from 'vee-validate';
import { v4 as uuidv4 } from 'uuid';
import { computed, ref } from 'vue';
import { vOverflowTooltip } from '@/directives/overflowTooltip';

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
  view?: 'date' | 'month' | 'year' | 'smallmonth' | undefined;
}

const props = defineProps<Props>();

const value = defineModel<Date | undefined>(
  {
    get(value) {
      if (value) {
        convertData(value);

        return new Date(value);
      }

      dateSelected.value = undefined;
      return value;
    },
  }
);

const emit = defineEmits(['updateValue']);

const key = uuidv4();
const minLength = 2300;
const maxLength = 3100;

const id = ref(props.id ?? key);
const name = ref(props.name ?? key);
const dateSelected = ref<Date>();

const convertData = (data: Date): void => {
  const date = new Date(data);
  const year = date.getFullYear();

  if (year >= minLength && year <= maxLength) {
    date.setFullYear(year - 543);

    value.value = date;
  } else {
    date.setFullYear(year + 543);

    value.value = data;
    dateSelected.value = date;
  }
};

const handleDateChange = (data?: Date) => {
  if (data) {
    convertData(data);

    return;
  }

  dateSelected.value = undefined;
  value.value = undefined;
};

const dateFormat = () => {
  if (props.view === 'year') {
    return 'yy';
  }

  if (props.view === 'month') {
    return 'MM yy';
  }

  if (props.view === 'smallmonth') {
    return 'mm/yy';
  }

  return 'dd MM yy';
};

const onClear = () => {
  dateSelected.value = undefined;
  value.value = undefined;
  emit('updateValue', undefined);
};

const computedView = computed(() => {
  if (props.view === undefined) {
    return 'date';
  }

  if (props.view === 'smallmonth') {
    return 'month';
  };

  return props.view;
});

const computedMinDate = computed(() => {
  if (props.minDate) {
    const minDateProps = new Date(props.minDate);
    minDateProps.setFullYear(minDateProps.getFullYear() + 543);

    return minDateProps;
  }

  return undefined;
});

const computeMaxDate = computed(() => {
  if (props.maxDate) {
    const maxDateProps = new Date(props.maxDate);
    maxDateProps.setFullYear(maxDateProps.getFullYear() + 543);

    return maxDateProps;
  }

  return undefined;
});

const computedDisabledDates = computed(() => {
  if (props.disabledDates) {
    const disabledDatesProps = props.disabledDates.map(s => {
      const date = new Date(s);
      date.setFullYear(date.getFullYear() + 543);

      return date;
    });

    return disabledDatesProps;
  }

  return [];
});
</script>

<template>
  <Field v-model="value" :name="name" :rules="rules" v-slot="{ errorMessage }" as="div">
    <FloatLabel variant="on">
      <DatePicker :id="id" :input-id="id" :name="name" :placeholder="props.placeholder" v-model="dateSelected"
        :dateFormat="dateFormat()" :view="computedView" :invalid="!!(rules && errorMessage)" :manual-input="false"
        :disabled="props.disabled" :readonly="props.readonly" showIcon iconDisplay="input" :minDate="computedMinDate"
        :maxDate="computeMaxDate" :disabledDates="computedDisabledDates" showButtonBar fluid
        @clear-click="() => onClear()" :input-class="`${errorMessage ? 'p-invalid' : ''}`" :showOtherMonths="false"
        updateModelType="replace" @date-select="(e) => handleDateChange(e)" :pt="{
          root(options) {
            if (options.state.currentYear === new Date().getFullYear()) {
              options.state.currentYear = (options.state.currentYear + 543)
            }
          },
        }" @update:model-value="(e) => handleDateChange(e as Date)" />
      <label for="on_label" class="p-float-label-always" v-if="props.label" v-overflow-tooltip="props.label">{{ props.label }}
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
  z-index: 2 !important;
  max-width: 80% !important;
}
</style>

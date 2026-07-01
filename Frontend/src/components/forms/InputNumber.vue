<script setup lang="ts">
import { FloatLabel, InputNumber } from 'primevue';
import { Field } from 'vee-validate';
import { v4 as uuidv4 } from 'uuid';
import { computed, ref } from 'vue';
import { vOverflowTooltip } from '@/directives/overflowTooltip';

type Props = {
  name?: string;
  id?: string;
  rules?: string;
  type?: string;
  label?: string;
  placeholder?: string;
  disabled?: boolean;
  readonly?: boolean;
  appendIconClass?: string;
  prependIconClass?: string;
  helperText?: string;
  hideDetails?: boolean;
  grouping?: boolean;
  minFractionDigits?: number;
  maxFractionDigits?: number;
  isCenter?: boolean
  inputClass?: string;
  maxNumber?: number;
  minNumber?: number;
  defaultZeroWhenEmpty?: boolean;
};

const value = defineModel<number | undefined>({
  required: true,
});

const props = defineProps<Props>();

const emits = defineEmits<{
  (event: 'onClickAppendIcon'): void;
  (event: 'onClickPrependIcon'): void;
  (event: 'onChange', value: number): void;
}>();
const key = uuidv4();
const id = ref(props.id ?? key);
const name = ref(props.name ?? key);

const onClickAppendIcon = () => {
  emits('onClickAppendIcon');
};

const onClickPrependIcon = () => {
  emits('onClickPrependIcon');
};

const computedInputClass = () => computed(() => {
  if (props.inputClass) {
    return props.inputClass;
  }

  if (props.grouping) {
    return 'text-end';
  }

  if (props.isCenter) {
    return 'text-center';
  }

  return undefined;
});

const internalValue = computed<number | undefined>({
  get() {
    return value.value;
  },
  set(val) {
    if (
      props.defaultZeroWhenEmpty &&
      (val === null || val === undefined)
    ) {
      value.value = 0;
    } else {
      value.value = val;
    }
  }
});
</script>

<template>
  <Field v-model="internalValue" :name="name" :rules="rules" v-slot="{ errorMessage }" as="div">
    <FloatLabel variant="on">
      <InputGroup>
        <slot name="prependAction"> </slot>
        <IconField>
          <InputIcon v-if="props.appendIconClass" :class="props.appendIconClass" @click="onClickAppendIcon" />
          <InputNumber :type="type" :id="id" :name="name" v-model="internalValue" :placeholder="placeholder"
            :disabled="disabled" :invalid="!!(rules && errorMessage)" :readonly="props.readonly" fluid
            @value-change="(e) => emits('onChange', e)" :pt="{ root: { name: name } }" :useGrouping="props.grouping"
            :minFractionDigits="props.minFractionDigits"
            :maxFractionDigits="props.maxFractionDigits ?? props.minFractionDigits"
            :inputClass="computedInputClass().value" :max="maxNumber" :min="minNumber" />
          <InputIcon v-if="props.prependIconClass" :class="props.prependIconClass" @click="onClickPrependIcon" />
        </IconField>
        <slot name="appendAction"> </slot>
      </InputGroup>
      <label for="on_label" class="p-float-label-always" v-if="props.label" v-overflow-tooltip="label">{{ label }}
        <span v-if="rules?.includes('required')" class="text-red-500">*</span>
      </label>
    </FloatLabel>
    <small class="pl-2 text-gray-500! mt-1.5" v-if="props.helperText">
      {{ props.helperText }}
    </small>
    <small class="pl-2 text-red-500!" v-if="!props.hideDetails">
      {{ errorMessage }}
    </small>
  </Field>
</template>

<style lang="scss" scoped>
.p-float-label-always {
  font-size: 1.25rem !important;
  z-index: 2 !important;
}
</style>

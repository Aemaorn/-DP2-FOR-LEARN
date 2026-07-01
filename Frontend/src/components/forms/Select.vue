<script setup lang="ts">
import { Select, FloatLabel } from 'primevue';
import { Field } from 'vee-validate';
import type { Option } from '@/models/shared/option';
import { v4 as uuidv4 } from 'uuid';
import { ref, watchEffect } from 'vue';
import { vOverflowTooltip } from '@/directives/overflowTooltip';

type Props = {
  name?: string;
  id?: string;
  rules?: string;
  label?: string;
  placeholder?: string;
  options: Option[];
  disabled?: boolean;
  helperText?: string;
  searchable?: boolean;
  hideDetails?: boolean;
  defaultValue?: string | number;
};
const props = withDefaults(defineProps<Props>(), {
  searchable: true,
});
const value = defineModel({
  required: true,
});

const emit = defineEmits(['onSelect', 'enterClose']);
const key = uuidv4();
const id = ref(props.id ?? key);
const name = ref(props.name ?? key);

const onEnterKey = (e: KeyboardEvent) => {
  const ele = e.currentTarget as HTMLElement;
  const spanSelected = ele.querySelector(".p-select-label");

  setTimeout(() => {
    const isOpen = spanSelected?.getAttribute("aria-expanded");
    if (isOpen == 'false') {
      emit('enterClose');
    }
  }, 0);
}

watchEffect(() => {
  if (props.rules?.includes('required') && props.options?.length === 1 && (value.value === null || value.value === undefined || value.value === '')) {
    value.value = props.defaultValue ?? props.options[0].value;
  }
});
</script>

<template>
  <Field v-model="value" :name="name" :rules="rules" v-slot="{ errorMessage }" as="div">
    <FloatLabel variant="on">
      <InputGroup>
        <slot name="prependAction"> </slot>
        <Select :id="id" :name="name" v-model="value" :placeholder="placeholder" :options="props.options"
          @keydown.enter="(e: any) => onEnterKey(e)" optionLabel="label" optionValue="value" :invalid="!!errorMessage"
          :disabled="props.disabled" :filter="searchable" :showClear="!props.disabled" fluid optionDisabled="disabled"
          :pt="{ root: { name: name, class: 'min-h-[2.85rem]' }, overlay: { class: '!z-[99999]' } }"
          :defaultValue="props.defaultValue" @change="(e) => emit('onSelect', e.value)" :resetFilterOnHide="true" />
        <slot name="appendAction"> </slot>
      </InputGroup>
      <label for="on_label" class="p-float-label-always" v-if="props.label" v-overflow-tooltip="label">{{ label }}
        <span v-if="rules?.includes('required')" class="text-red-500">*</span>
      </label>
    </FloatLabel>
    <small class="pl-2 text-gray-500!" v-if="props.helperText">
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
  z-index: 2;
}
</style>

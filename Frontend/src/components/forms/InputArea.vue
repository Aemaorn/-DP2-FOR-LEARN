<script setup lang="ts">
import { Textarea } from 'primevue';
import { Field } from 'vee-validate';
import { v4 as uuidv4 } from 'uuid';
import { ref } from 'vue';
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
  row?: number;
  hideDetails?: boolean;
  autoHeight?: boolean;
  autoResize?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  autoResize: true,
});
const value = defineModel<string | undefined>({
  required: true,
});

const key = uuidv4();
const id = ref(props.id ?? key);
const name = ref(props.name ?? key);

</script>

<template>
  <Field v-model="value" :name="name" :rules="rules" v-slot="{ errorMessage }" as="div">
    <FloatLabel variant="on">
      <Textarea :id="id" :name="name" v-model="value" :placeholder="placeholder"
        v-bind="props.autoHeight ? {} : { rows: props.row ?? 3 }"
        :autoResize="props.autoResize"
        :class="[{ 'auto-height-textarea': props.autoHeight }]" :invalid="!!(errorMessage)" :readonly="props.readonly"
        :disabled="props.disabled" fluid :pt="{ root: { name: name } }" />
      <label for="on_label" class="p-float-label-always" v-if="props.label" v-overflow-tooltip="label">{{ label }}
        <span v-if="rules?.includes('required')" class="text-red-500">*</span>
        <span v-if="props.helperText" class="helper-text-inline">{{ props.helperText }}</span>
      </label>
    </FloatLabel>
    <small class="pl-2 text-red-500!" v-if="(!props.hideDetails)">
      {{ errorMessage }}
    </small>
  </Field>
</template>

<style lang="scss" scoped>
.p-float-label-always {
  font-size: 1.25rem !important;
  z-index: 2 !important;
}

textarea:focus+.p-float-label-always {
  top: 0 !important;
  font-size: 1.25rem !important;
}

.auto-height-textarea {
  width: 100%;
  min-height: 2.5rem;
  resize: none;
  overflow: hidden;
}

.helper-text-inline {
  font-size: 0.875rem;
  color: #9ca3af;
  white-space: nowrap;
  pointer-events: none;
  margin-left: 4px;
}
</style>

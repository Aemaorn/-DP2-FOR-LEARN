<script setup lang="ts">
import { InputText, FloatLabel } from 'primevue';
import { Field } from 'vee-validate';
import { v4 as uuidv4 } from 'uuid';
import { ref } from 'vue';
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
  showRequired?: boolean;
  eager?: boolean;
}

const [value, modifiers] = defineModel<string | undefined>({
  required: true,
  set(val) {
    if (modifiers.trim) {
      return val?.trim();
    }

    return val;
  },
});

const props = defineProps<Props>();
const emits = defineEmits<{
  (event: "onClickAppendIcon"): void,
  (event: "onClickPrependIcon"): void,
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
</script>

<template>
  <Field v-model="value" :name="name" :rules="rules" v-slot="{ errorMessage }" as="div" :validateOnModelUpdate="props.eager">
    <div class="relative">
      <FloatLabel variant="on">
        <InputGroup>
          <slot name="prependAction">
          </slot>
          <InputIcon v-if="props.appendIconClass" :class="props.appendIconClass" @click="onClickAppendIcon" />
          <InputText :type="type" :id="id" :name="name" v-model="value" :placeholder="placeholder" :disabled="disabled"
            :invalid="!!(rules && errorMessage)" :readonly="props.readonly" fluid :pt="{ root: { name: name } }" />
          <InputIcon v-if="props.prependIconClass" :class="props.prependIconClass" @click="onClickPrependIcon" />
          <slot name="appendAction">
          </slot>
        </InputGroup>
        <label for="on_label" class="p-float-label-always" v-if="props.label" v-overflow-tooltip="label">{{ label }}
          <span v-if="rules?.includes('required') || props.showRequired" class="text-red-500">*</span>
          <span v-if="props.helperText" class="helper-text-inline">{{ props.helperText }}</span>
        </label>
      </FloatLabel>
    </div>
    <div v-if="!props.hideDetails">
      <small class="pl-2 text-red-500!">
        {{ errorMessage }}
      </small>
    </div>
  </Field>
</template>

<style lang="scss" scoped>
.p-float-label-always {
  font-size: 1.25rem !important;
  z-index: 2 !important;
}

.helper-text-inline {
  font-size: 1rem;
  color: #9ca3af;
  white-space: nowrap;
  pointer-events: none;
  margin-left: 4px;
}

:deep(input::placeholder) {
  opacity: 1 !important;
}
</style>

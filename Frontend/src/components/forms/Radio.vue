<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { RadioButton } from 'primevue';
import { Field } from 'vee-validate';
import { v4 as uuidv4 } from 'uuid';
import { ref } from 'vue';

type Props = {
  name?: string;
  id?: string;
  rules?: string;
  label?: string;
  disabled?: boolean;
  class?: string;
  options: Option[];
  vertical?: boolean;
  baseline?: boolean;
  hideDetails?: boolean;
};

const props = defineProps<Props>();
const value = defineModel<string | boolean | number>();
const emit = defineEmits(['update', 'change']);

const key = uuidv4();

const name = ref(props.name ?? key);

const onChange = (e: Event) => {
  const target = e.target as HTMLInputElement | null;
  emit('change', target?.value as string | number | boolean)

}
</script>

<template>
  <Field v-model="value" :name="name" :rules="rules" v-slot="{ errorMessage }" as="div" :class="props.class">
    <p v-if="label" class="text-xl mb-2">
      {{ label }}
      <span v-if="rules?.includes('required')" class="text-red-500"> * </span>
    </p>
    <div class="flex flex-wrap gap-4" :class="`${props.vertical ? 'flex-col' : ''}`">
      <div v-for="(item, index) in options" :key="item.value.toString()" class="flex gap-2"
        :class="`${props.baseline ? 'items-baseline' : 'items-center'}`">
        <RadioButton v-model="value" :inputId="`${name}-${item.value}-${index}`" :value="item.value"
          @update:modelValue="(e) => emit('update', e)" @change="onChange" :disabled="props.disabled" />
        <label :for="`${name}-${item.value}-${index}`"
          :class="['text-lg md:text-xl!', { 'cursor-pointer': !props.disabled }]">{{ item.label }}</label>
      </div>
    </div>
    <small class="pl-2 text-red-500!" v-if="!props.hideDetails">
      {{ errorMessage }}
    </small>
  </Field>
</template>

<style lang="scss">
.p-radiobutton-box[data-p="checked disabled"] {
  background: var(--color-gray-200) !important;
}

.p-radiobutton-icon[data-p="checked disabled"] {
  background: var(--color-gray-400) !important;
}

.p-radiobutton-checked .p-radiobutton-box {
  background: white;
}

.p-radiobutton-checked .p-radiobutton-box .p-radiobutton-icon {
  background: var(--color-primary);
}
</style>
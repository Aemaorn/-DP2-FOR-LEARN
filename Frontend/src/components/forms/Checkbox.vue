<script setup lang="ts">
import { Checkbox } from 'primevue';
import { Field } from 'vee-validate';
import { v4 as uuidv4 } from 'uuid';
import { ref } from 'vue';

const value = defineModel<boolean | undefined>({
  required: true,
});

const props = defineProps({
  label: { type: String },
  disabled: { type: Boolean, default: false, },
  name: { type: String },
  id: { type: String },
  rules: { type: String },
  helperText: { type: String },
  class: { type: String, default: '' },
  hideDetails: { type: Boolean },
  trueValue: { type: Boolean, default: true },
  falseValue: { type: Boolean, default: false },
});

const key = uuidv4();
const id = ref(props.id ?? key);
const name = ref(props.name ?? key);
const emit = defineEmits(['onChange']);
</script>

<template>
  <Field v-model="value" :name="name" :id="id" :rules="rules" v-slot="{ errorMessage }">
    <div class="flex flex-col">
      <div :class="`flex items-center gap-2 ${props.class ? props.class : ''}`">
        <Checkbox :name="name" :id="id" v-model="value" binary :disabled="props.disabled" :true-value="trueValue"
          :false-value="falseValue" @value-change="(e: boolean) => emit('onChange', e)" />
        <label v-if="props.label" :for="id" class="cursor-pointer select-none"
          @click="() => { if (!props.disabled) value = value ? falseValue : trueValue; }">{{ props.label }}</label>
      </div>
      <small class="pl-2 text-gray-500!" v-if="props.helperText">
        {{ props.helperText }}
      </small>
      <small class="pl-2 text-red-500!" v-if="(!props.hideDetails)">
        {{ errorMessage }}
      </small>
    </div>
  </Field>
</template>

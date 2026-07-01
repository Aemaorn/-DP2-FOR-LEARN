<script lang="ts" setup>
import { ToggleSwitch } from 'primevue';
import { Field } from 'vee-validate';
import { v4 as uuidv4 } from 'uuid';
import { ref } from 'vue';

const value = defineModel({
  type: Boolean,
  required: true,
});

const props = defineProps({
  label: { type: String, },
  disabled: { type: Boolean, default: false, },
  activeColor: { type: String, default: 'bg-primary' },
  name: { type: String },
  id: { type: String },
  rules: { type: String },
  helperText: { type: String },
  hideDetails: { type: Boolean },
});


const ConditionClass = (): string => {
  return (value.value) ? `${props.activeColor}!` : '';
};

const key = uuidv4();
const id = ref(props.id ?? key);
const name = ref(props.name ?? key);
</script>

<template>
  <div>
    <Field v-model="value" :name="name" :id="id" :rules="rules" v-slot="{ errorMessage }">
      <div class="h-full mt-2">
        <div class="flex items-center gap-2">
          <ToggleSwitch :pt="{
            slider: {
              class: `${ConditionClass()}`,
            },
          }" v-model="value" :disabled="props.disabled" />
          <p v-if="props.label">{{ props.label }}</p>
        </div>
        <small class="pl-2 text-gray-500!" v-if="props.helperText">
          {{ props.helperText }}
        </small>
        <small class="pl-2 text-red-500!" v-if="(!props.hideDetails)">
          {{ errorMessage }}
        </small>
      </div>
    </Field>
  </div>
</template>

<script setup lang="ts">
import { Password, FloatLabel } from 'primevue';
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
  disabled?: boolean;
  readonly?: boolean;
  helperText?: string;
  hideDetails?: boolean;
};

const value = defineModel<string | undefined>({
  required: true,
});

const props = defineProps<Props>();
const key = uuidv4();
const id = ref(props.id ?? key);
const name = ref(props.name ?? key);

</script>

<template>
  <Field v-model="value" :name="name" :rules="rules" v-slot="{ errorMessage }" as="div">
    <FloatLabel variant="on">
      <Password v-model="value" inputId="on_label" :id="id" :name="name" toggle-mask :disabled="props.disabled"
        :invalid="!!(rules && errorMessage)" :readonly="props.readonly" fluid :pt="{ root: { name: name } }"
        :feedback="false" />
      <label for="on_label" class="p-float-label-always" v-if="props.label" v-overflow-tooltip="label">{{ label }}
        <span v-if="rules?.includes('required')" class="text-red-500">*</span>
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
}
</style>
<script setup lang="ts">
import { DatePicker, FloatLabel } from 'primevue';
import { Field } from 'vee-validate';
import { v4 as uuidv4 } from 'uuid';
import { ref, watch } from 'vue';
import { vOverflowTooltip } from '@/directives/overflowTooltip';

type Props = {
  name?: string;
  id?: string;
  rules?: string;
  label?: string;
  disabled?: boolean;
  readonly?: boolean;
  helperText?: string;
  hideDetails?: boolean;
};

const props = defineProps<Props>();

const value = defineModel<string | undefined>({
  required: true,
});
const key = uuidv4();
const id = ref(props.id ?? key);
const name = ref(props.name ?? key);

const valueRef = ref<Date>();
const tempTimeWhenShow = ref<Date>();
const tempDataSelected = ref<Date>();
const isConfirmWhenClick = ref(false);

const isMidnight = (date: Date): boolean => {
  return (
    date.getHours() === 0 &&
    date.getMinutes() === 0 &&
    date.getSeconds() === 0 &&
    date.getMilliseconds() === 0
  );
};

const onChangeTime = (val?: Date) => {
  if (val) {
    const timeStr = `${val.getHours().toString().padStart(2, "0")}:${val.getMinutes().toString().padStart(2, "0")}`;

    value.value = timeStr;

    return;
  };

  value.value = undefined;
  valueRef.value = undefined;
};

const valueChange = (val?: Date) => {
  if (val && !isMidnight(val)) {
    tempDataSelected.value = val;

    return;
  }

  if (!isConfirmWhenClick.value && val && isMidnight(val)) {
    tempDataSelected.value = undefined;
  }
};

const onConfirmTime = () => {
  if (!value.value) {
    return onChangeTime(tempTimeWhenShow.value);
  }

  if (value.value && tempDataSelected.value && !isMidnight(tempDataSelected.value)) {
    onChangeTime(tempDataSelected.value);
    valueRef.value = tempDataSelected.value;
  }

  isConfirmWhenClick.value = false;
};

const onShow = () => {
  tempTimeWhenShow.value = new Date();
};

watch(() => value.value, (val) => {
  if (val) {
    const tempDate = new Date();
    const valSplit = val.split(":");
    tempDate.setHours(Number(valSplit[0]));
    tempDate.setMinutes(Number(valSplit[1]));

    valueRef.value = tempDate;
  }
}, {
  immediate: true
});
</script>

<template>
  <Field v-model="value" :name="name" :rules="rules" v-slot="{ errorMessage }" as="div">
    <FloatLabel variant="on">
      <DatePicker :id="id" :input-id="id" :name="name" v-model="valueRef" :invalid="!!(rules && errorMessage)"
        @show="onShow" @value-change="(e) => onChangeTime(e as Date | undefined)" :manual-input="false" showIcon
        show-button-bar @clear-click="() => onConfirmTime()" @today-click="() => onChangeTime()" iconDisplay="input"
        :disabled="props.disabled" :readonly="props.readonly" fluid hourFormat="24" :pt="{ root: { name: name } }"
        :input-class="`${errorMessage ? 'p-invalid' : ''}`" timeOnly
        @update:model-value="(e) => valueChange(e as Date | undefined)">
        <template #inputicon="slotProps">
          <i class="pi pi-clock" @click="slotProps.clickCallback" />
        </template>
        <template #todaybutton="{ actionCallback }">
          <Button @click="(e) => { isConfirmWhenClick = true; actionCallback(e as Event) }"
            class="p-button p-component p-button-secondary p-button-text p-button-sm p-datepicker-clear-button rounded-lg py-1.5 font-semibold text-xl">
            ล้าง
          </Button>
        </template>
        <template #clearbutton="{ actionCallback }">
          <Button @click="(e) => { isConfirmWhenClick = true; actionCallback(e as Event) }"
            class="p-button p-component p-button-secondary p-button-text p-button-sm p-datepicker-clear-button rounded-lg py-1.5 font-semibold text-xl">
            ตกลง
          </Button>
        </template>
      </DatePicker>
      <label for="on_label" class="p-float-label-always" v-overflow-tooltip="props.label">{{ props.label }}
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
}
</style>
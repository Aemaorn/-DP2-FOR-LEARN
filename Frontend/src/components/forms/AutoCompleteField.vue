<script setup lang="ts">
import { InputText, FloatLabel, Popover } from 'primevue';
import { Field } from 'vee-validate';
import type { Option } from '@/models/shared/option';
import { computed, ref, watch } from 'vue';
import { v4 as uuidv4 } from 'uuid';
import { vOverflowTooltip } from '@/directives/overflowTooltip';

type Props = {
  name?: string;
  id?: string;
  rules?: string;
  label?: string;
  options?: Option[];
  disabled?: boolean;
  hideDetails?: boolean;
};

const props = withDefaults(defineProps<Props>(), { options: () => [] });
const modelValue = defineModel<string | undefined>();

const key = uuidv4();
const inputId = ref(props.id ?? key);
const fieldName = ref(props.name ?? key);
const popoverRef = ref<InstanceType<typeof Popover>>();
const wrapperRef = ref<HTMLElement | null>(null);
const popoverWidth = ref(0);
const isFocused = ref(false);

const getLabel = (code: string | undefined): string => {
  if (!code) return '';
  return props.options.find((o): boolean => String(o.value) === code)?.label ?? code;
};

const query = ref<string>(getLabel(modelValue.value));
const filterQuery = ref<string>('');

// external model change → sync display (only when not editing)
watch(
  () => modelValue.value,
  (code: string | undefined): void => {
    if (isFocused.value) return;
    const label = getLabel(code);
    if (label !== query.value) query.value = label;
  }
);

// options loaded → re-resolve display text
watch(
  () => props.options,
  (): void => {
    if (isFocused.value) return;
    const label = getLabel(modelValue.value);
    if (label !== query.value) query.value = label;
  }
);

const filteredOptions = computed((): Option[] => {
  const q = filterQuery.value.toLowerCase();
  if (!q) return props.options;
  return props.options.filter(
    (o): boolean => o.label.toLowerCase().includes(q) || String(o.value).toLowerCase().includes(q)
  );
});

const onUpdate = (val: string | undefined): void => {
  query.value = val ?? '';
  filterQuery.value = val ?? '';
  const match = props.options.find((o): boolean => o.label === query.value || String(o.value) === query.value);
  modelValue.value = match ? String(match.value) : (query.value || undefined);
};

const onFocus = (): void => { isFocused.value = true; };

const onBlur = (): void => {
  isFocused.value = false;
  const match = props.options.find((o): boolean => o.label === query.value);
  if (match) modelValue.value = String(match.value);
};

const selectOption = (opt: Option): void => {
  modelValue.value = String(opt.value);
  query.value = opt.label;
  popoverRef.value?.hide();
};

const togglePopover = (e: MouseEvent): void => {
  filterQuery.value = '';
  popoverWidth.value = wrapperRef.value?.offsetWidth ?? 0;
  popoverRef.value?.toggle(e, wrapperRef.value ?? undefined);
};

const onClear = (): void => {
  query.value = '';
  modelValue.value = undefined;
};
</script>

<template>
  <Field v-model="modelValue" :name="fieldName" :rules="rules" v-slot="{ errorMessage }" as="div">
    <FloatLabel variant="on">
      <div ref="wrapperRef" class="input-wrapper" :class="{ 'p-invalid': !!errorMessage, 'p-disabled': disabled }">
        <InputText
          :id="inputId"
          :model-value="query"
          @update:model-value="onUpdate"
          @focus="onFocus"
          @blur="onBlur"
          :invalid="!!errorMessage"
          :disabled="disabled"
          fluid
          :pt="{ root: { name: fieldName, class: 'min-h-[2.85rem] border-0! shadow-none! pr-16' } }"
        />
        <div class="input-actions">
          <button
            v-if="query && !disabled"
            type="button"
            tabindex="-1"
            class="action-btn"
            @mousedown.prevent="onClear"
          >
            <i class="pi pi-times" />
          </button>
          <button
            type="button"
            tabindex="-1"
            class="action-btn"
            :disabled="disabled"
            @mousedown.prevent="togglePopover"
          >
            <i class="pi pi-chevron-down" />
          </button>
        </div>
      </div>
      <label for="on_label" class="p-float-label-always" v-if="label" v-overflow-tooltip="label">
        {{ label }}
        <span v-if="rules?.includes('required')" class="text-red-500">*</span>
      </label>
    </FloatLabel>
    <small class="pl-2 text-red-500!" v-if="!hideDetails">
      {{ errorMessage }}
    </small>
  </Field>

  <Popover ref="popoverRef" :pt="{ root: { class: '!z-[99999]', style: `width: ${popoverWidth}px` } }">
    <div class="max-h-60 overflow-y-auto min-w-48">
      <div
        v-for="opt in filteredOptions"
        :key="String(opt.value)"
        class="px-3 py-2 cursor-pointer hover:bg-gray-100 text-sm"
        @mousedown.prevent="selectOption(opt)"
      >
        {{ opt.label }}
      </div>
      <div v-if="filteredOptions.length === 0" class="px-3 py-2 text-sm text-gray-400">
        ไม่พบข้อมูล
      </div>
    </div>
  </Popover>
</template>

<style lang="scss" scoped>
.p-float-label-always {
  font-size: 1.25rem !important;
  z-index: 2;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
  border: 1px solid var(--p-inputtext-border-color, #d1d5db);
  border-radius: var(--p-inputtext-border-radius, 6px);
  transition: border-color 0.2s, box-shadow 0.2s;

  &:focus-within {
    border-color: var(--p-inputtext-focus-border-color, #6366f1);
    box-shadow: var(--p-focus-ring-shadow);
  }

  &.p-invalid {
    border-color: var(--p-inputtext-invalid-border-color, #ef4444);

    &:focus-within {
      box-shadow: none;
    }
  }
}

.input-actions {
  position: absolute;
  right: 0.5rem;
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  background: none;
  border: none;
  cursor: pointer;
  padding: 0.25rem;
  color: var(--p-inputtext-placeholder-color, #9ca3af);
  border-radius: 4px;

  &:hover {
    color: var(--p-text-color, #374151);
  }

  &:disabled {
    cursor: not-allowed;
    opacity: 0.5;
  }

  .pi {
    font-size: 0.75rem;
  }
}
</style>

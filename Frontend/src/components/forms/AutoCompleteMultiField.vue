<script lang="ts">
export default { inheritAttrs: false };
</script>

<script setup lang="ts">
import { InputText, Popover } from 'primevue';
import { Field } from 'vee-validate';
import type { Option } from '@/models/shared/option';
import { computed, ref, useAttrs, watch } from 'vue';
import { v4 as uuidv4 } from 'uuid';
import { vOverflowTooltip } from '@/directives/overflowTooltip';

const attrs = useAttrs();

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
const modelValue = defineModel<string[] | undefined>();

const key = uuidv4();
const inputId = ref(props.id ?? key);
const fieldName = ref(props.name ?? key);
const popoverRef = ref<InstanceType<typeof Popover>>();
const wrapperRef = ref<HTMLElement | null>(null);
const popoverWidth = ref(0);

const filterQuery = ref<string>('');
const isFocused = ref(false);

const isLabelActive = computed((): boolean =>
  isFocused.value || (modelValue.value?.length ?? 0) > 0 || !!filterQuery.value
);

const selectedValues = computed((): string[] => Array.isArray(modelValue.value) ? modelValue.value : []);

const selectedOptions = computed((): Option[] => {
  const optionMap = new Map(props.options.map((o): [string, string] => [String(o.value), o.label]));
  return selectedValues.value
    .filter((v): boolean => optionMap.has(v))
    .map((v): Option => ({ value: v, label: optionMap.get(v)! }));
});

const filteredOptions = computed((): Option[] => {
  const q = filterQuery.value.toLowerCase();
  const selected = new Set(selectedValues.value);
  return props.options.filter((o): boolean => {
    if (selected.has(String(o.value))) return false;
    if (!q) return true;
    return o.label.toLowerCase().includes(q) || String(o.value).toLowerCase().includes(q);
  });
});

const addValue = (val: string): void => {
  const current = selectedValues.value;
  if (current.includes(val)) return;
  modelValue.value = [...current, val];
};

const removeValue = (val: string): void => {
  modelValue.value = selectedValues.value.filter((v): boolean => v !== val);
};

const selectOption = (opt: Option): void => {
  addValue(String(opt.value));
  filterQuery.value = '';
};

const onClear = (): void => {
  modelValue.value = [];
  filterQuery.value = '';
};

const togglePopover = (e: MouseEvent): void => {
  popoverWidth.value = wrapperRef.value?.offsetWidth ?? 0;
  popoverRef.value?.toggle(e, wrapperRef.value ?? undefined);
};

const openPopover = (e: FocusEvent | MouseEvent): void => {
  popoverWidth.value = wrapperRef.value?.offsetWidth ?? 0;
  popoverRef.value?.show(e, wrapperRef.value ?? undefined);
};

const onFocus = (): void => { isFocused.value = true; };
const onBlur = (): void => {
  isFocused.value = false;
  filterQuery.value = '';
  popoverRef.value?.hide();
};

const onInput = (val: string | undefined): void => {
  filterQuery.value = val ?? '';
};

const onKeydown = (e: KeyboardEvent): void => {
  if (e.key === 'Backspace' && !filterQuery.value && selectedValues.value.length) {
    removeValue(selectedValues.value[selectedValues.value.length - 1]);
  } else if (e.key === 'Enter') {
    e.preventDefault();
    const first = filteredOptions.value[0];
    if (first) selectOption(first);
  }
};

watch(
  () => props.options,
  (): void => {
    // re-resolve label cache by touching computed
    filterQuery.value = filterQuery.value;
  }
);
</script>

<template>
  <Field v-bind="attrs" :model-value="modelValue" :name="fieldName" :rules="rules" v-slot="{ errorMessage }" as="div">
    <div class="floatlabel-root">
      <div
        ref="wrapperRef"
        class="input-wrapper"
        :class="{ 'p-invalid': !!errorMessage, 'p-disabled': disabled }"
        @click="openPopover"
      >
        <div class="chips-area">
          <span v-for="opt in selectedOptions" :key="String(opt.value)" class="chip">
            <span class="chip-label">{{ opt.label }}</span>
            <button
              v-if="!disabled"
              type="button"
              tabindex="-1"
              class="chip-remove"
              @mousedown.prevent.stop="removeValue(String(opt.value))"
            >
              <i class="pi pi-times" />
            </button>
          </span>
          <InputText
            :id="inputId"
            :model-value="filterQuery"
            @update:model-value="onInput"
            @focus="(e: FocusEvent) => { onFocus(); openPopover(e); }"
            @blur="onBlur"
            @keydown="onKeydown"
            :invalid="!!errorMessage"
            :disabled="disabled"
            :pt="{ root: { class: 'chip-input border-0! shadow-none! outline-none! bg-transparent!' } }"
          />
        </div>
        <div class="input-actions">
          <button
            v-if="selectedValues.length && !disabled"
            type="button"
            tabindex="-1"
            class="action-btn"
            @mousedown.prevent.stop="onClear"
          >
            <i class="pi pi-times" />
          </button>
          <button
            type="button"
            tabindex="-1"
            class="action-btn"
            :disabled="disabled"
            @mousedown.prevent.stop="togglePopover"
          >
            <i class="pi pi-chevron-down" />
          </button>
        </div>
      </div>
      <label for="on_label" class="multi-label" :class="{ 'multi-label--active': isLabelActive }" v-if="label"
        v-overflow-tooltip="label">
        {{ label }}
        <span v-if="rules?.includes('required')" class="text-red-500">*</span>
      </label>
    </div>
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
        @mousedown.prevent
        @click.stop="selectOption(opt)"
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
.floatlabel-root {
  display: block;
  width: 100%;
  position: relative;
}

.multi-label {
  position: absolute !important;
  left: 0.75rem !important;
  top: 50%;
  transform: translateY(-50%);
  padding: 0 0.25rem;
  background: transparent;
  color: var(--p-inputtext-placeholder-color, #9ca3af);
  font-size: 1.25rem !important;
  line-height: 1;
  pointer-events: none;
  transition: top 0.15s ease, color 0.15s ease, background 0.15s ease;
  z-index: 2;
  max-width: calc(100% - 4.5rem);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.multi-label--active {
  top: 0 !important;
  transform: translateY(-50%) !important;
  padding: 0.125rem 0.25rem !important;
  background: #ffffff;
  color: inherit;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
  min-height: 2.85rem;
  padding-right: 4rem;
  border: 1px solid var(--p-inputtext-border-color, #d1d5db);
  border-radius: var(--p-inputtext-border-radius, 6px);
  transition: border-color 0.2s, box-shadow 0.2s;
  cursor: text;

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

  &.p-disabled {
    background: #f3f4f6;
    cursor: not-allowed;
  }
}

.chips-area {
  display: flex;
  flex-wrap: wrap;
  gap: 0.25rem;
  align-items: center;
  flex: 1 1 auto;
  padding: 0.25rem 0.5rem;
  min-width: 0;
}

.chip {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  background: #e0e7ff;
  color: #1e3a8a;
  padding: 0.125rem 0.5rem;
  border-radius: 9999px;
  font-size: 0.8125rem;
  line-height: 1.25rem;
  max-width: 100%;
}

.chip-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 12rem;
}

.chip-remove {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: none;
  border: none;
  cursor: pointer;
  color: #1e3a8a;
  padding: 0;

  .pi {
    font-size: 0.625rem;
  }

  &:hover {
    color: #ef4444;
  }
}

:deep(.chip-input) {
  flex: 1 1 6rem;
  min-width: 6rem;
  width: 0;
  background: transparent !important;
  border: 0 !important;
  box-shadow: none !important;
  padding: 0.25rem 0 !important;
}

.input-actions {
  position: absolute;
  right: 0.5rem;
  top: 50%;
  transform: translateY(-50%);
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

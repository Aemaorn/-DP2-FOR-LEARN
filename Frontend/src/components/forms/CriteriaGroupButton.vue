<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { SelectButton } from 'primevue';
import { ref } from 'vue';

type Prop = {
  options: Option[];
};

const value = defineModel<string | number | undefined>();
const props = defineProps<Prop>();

const scrollContainer = ref<HTMLElement>();

const scroll = (direction: 'left' | 'right') => {
  if (!scrollContainer.value) return;
  const amount = 150;
  scrollContainer.value.scrollBy({
    left: direction === 'left' ? -amount : amount,
    behavior: 'smooth',
  });
};
</script>

<template>
  <div class="mb-4">
    <div class="flex items-center gap-1">
      <button type="button"
        class="shrink-0 w-7 h-7 flex items-center justify-center rounded-full border border-gray-300 bg-white shadow-sm hover:bg-gray-100 active:scale-95 transition-all lg:hidden"
        @click="scroll('left')">
        <i class="pi pi-chevron-left text-xs text-gray-600" />
      </button>
      <div ref="scrollContainer" class="overflow-x-auto scrollbar-hide">
        <SelectButton v-model="value" :allowEmpty="false" :options="props.options" option-label="label"
          option-value="value" unstyled class="flex flex-nowrap">
          <template #option="slotProps">
            <div class="flex items-center py-1">
              <p class="cursor-pointer duration-100 whitespace-nowrap px-2 pb-0.5 border-b-4"
                :class="slotProps.option.value == value
                  ? 'border-primary font-bold'
                  : 'border-transparent'">
                {{ slotProps.option.label }}
              </p>
              <div class="mx-2" />
            </div>
          </template>
        </SelectButton>
      </div>
      <button type="button"
        class="shrink-0 w-7 h-7 flex items-center justify-center rounded-full border border-gray-300 bg-white shadow-sm hover:bg-gray-100 active:scale-95 transition-all lg:hidden"
        @click="scroll('right')">
        <i class="pi pi-chevron-right text-xs text-gray-600" />
      </button>
    </div>
  </div>
</template>

<style scoped>
.scrollbar-hide {
  -ms-overflow-style: none;
  scrollbar-width: none;
}

.scrollbar-hide::-webkit-scrollbar {
  display: none;
}
</style>

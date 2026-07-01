<script setup lang="ts">
import type { OptionBadge } from '@/models/shared/option';
import { SelectButton, Badge } from 'primevue';
import { ref } from 'vue';

type Prop = {
  optionBadges: OptionBadge[];
  hideCount?: boolean;
}

const value = defineModel<string | number | undefined>();
const props = defineProps<Prop>();

const scrollContainer = ref<HTMLElement>();

const scroll = (direction: 'left' | 'right') => {
  if (!scrollContainer.value) return;
  const amount = 200;
  scrollContainer.value.scrollBy({
    left: direction === 'left' ? -amount : amount,
    behavior: 'smooth',
  });
};
</script>

<template>
  <div v-if="props.optionBadges.length > 0"
    class="relative flex items-center bg-primary-50 mb-4 shadow-md rounded-sm">
    <button type="button" @click="scroll('left')"
      class="hidden max-[1400px]:flex shrink-0 z-10 items-center justify-center w-8 h-full cursor-pointer bg-primary-100/70 hover:bg-primary-200/60 rounded-l-sm">
      <i class="pi pi-chevron-left text-sm text-primary" />
    </button>

    <div ref="scrollContainer" class="overflow-x-auto scrollbar-hide flex-1">
      <SelectButton v-model="value" :allowEmpty="false" :options="props.optionBadges" option-label="label"
        class="px-4 py-3 flex-nowrap! whitespace-nowrap" option-value="value" unstyled>
        <template #option="slotProps">
          <div class="cursor-pointer duration-100 flex! flex-nowrap! items-center gap-1.5 mr-6 py-1"
            :class="`${slotProps.option.value == value ? 'border-b-4 px-2 border-primary/90 font-bold' : 'border-b-4 border-transparent'}`">
            <Badge :class="`${slotProps.option.bgColorClass} ${slotProps.option.textColorClass}`"
              class="rounded-full! aspect-square min-w-[1.3rem]! h-[1.3rem]! p-0! text-xs! flex! items-center justify-center"
              v-if="!props.hideCount">{{
                (slotProps.option.count ?? 0).toLocaleString()
              }}
            </Badge>
            <small :class="`${slotProps.option.value == value ? 'text-primary' : 'text-gray-600'}`"
              class="whitespace-nowrap">
              {{ slotProps.option.label }}
            </small>
          </div>
        </template>
      </SelectButton>
    </div>

    <button type="button" @click="scroll('right')"
      class="hidden max-[1400px]:flex shrink-0 z-10 items-center justify-center w-8 h-full cursor-pointer bg-primary-100/70 hover:bg-primary-200/60 rounded-r-sm">
      <i class="pi pi-chevron-right text-sm text-primary" />
    </button>
  </div>
</template>

<style scoped>
.scrollbar-hide::-webkit-scrollbar {
  display: none;
}

.scrollbar-hide {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>

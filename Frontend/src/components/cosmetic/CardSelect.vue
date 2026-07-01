<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick, watch } from 'vue';
import Button from 'primevue/button';
import type { CardSelectItems } from '@/models/shared/option';

type Props = {
  items: Array<CardSelectItems>;
  value?: string | number;
};

const props = defineProps<Props>();
const emit = defineEmits<{ select: [value: string | number] }>();

const scroller = ref<HTMLElement | null>(null);
const canScrollLeft = ref(false);
const canScrollRight = ref(false);
let resizeObserver: ResizeObserver | null = null;

const updateScroll = () => {
  const el = scroller.value as HTMLElement | null;
  if (!el) return;
  canScrollLeft.value = el.scrollLeft > 0;
  canScrollRight.value = el.scrollLeft + el.clientWidth < el.scrollWidth - 1;
};

onMounted(() => {
  nextTick(updateScroll);
  const el = scroller.value;
  if (el) el.addEventListener('scroll', updateScroll, { passive: true });
  window.addEventListener('resize', updateScroll);
  if (typeof globalThis !== 'undefined' && 'ResizeObserver' in globalThis && el) {
    // observe size changes to update button visibility when content/layout changes
    resizeObserver = new (globalThis as any).ResizeObserver(() => updateScroll());
    resizeObserver?.observe(el);
  }
});

onUnmounted(() => {
  const el = scroller.value;
  if (el) el.removeEventListener('scroll', updateScroll);
  window.removeEventListener('resize', updateScroll);
  if (resizeObserver) {
    resizeObserver.disconnect();
    resizeObserver = null;
  }
});

// when items change, re-evaluate scrollable state
watch(() => props.items && props.items.length, () => {
  nextTick(updateScroll);
});

const scrollBy = (delta: number) => {
  const el = scroller.value;
  if (!el) return;
  el.scrollBy({ left: delta, behavior: 'smooth' });
};

const isSelected = (itemValue: string | number | boolean, current?: string | number) => {
  return String(itemValue) === String(current);
};

const onSelected = (e: string | number) => {
  if (e === props.value) return;

  emit('select', e);
};
</script>

<template>
  <div class="relative">
    <Button v-show="canScrollLeft" aria-label="scroll left"
      class="absolute left-0 top-1/2 -translate-y-1/2 z-60 hover:shadow-lg" icon="pi pi-chevron-left"
      @click="() => scrollBy(-((scroller?.clientWidth ?? 320) * 0.8))" />
    <div ref="scroller" class="flex gap-4 overflow-x-auto card-select-scroll py-0.5 overflow-y-hidden px-2">
      <div v-for="(item, index) in items" :key="String(item.value)"
        class="relative z-50 cursor-pointer rounded-xl bg-white min-w-[325px] max-w-[400px] flex-none transition-all duration-200 select-none overflow-visible"
        :class="[
          isSelected(item.value, value)
            ? item.isCompleted !== undefined
              ? item.isCompleted ? 'border-3 border-green-500 shadow-lg' : 'border-3 border-orange-400 shadow-lg'
              : 'border-3 border-primary-600 shadow-lg'
            : 'border-2 border-gray-200 hover:border-gray-300 hover:shadow-sm hover:scale-105',
          item.isCompleted !== undefined && !isSelected(item.value, value)
            ? item.isCompleted ? 'border-l-green-500! border-l-4!' : 'border-l-orange-400! border-l-4!'
            : ''
        ]" @click="() => onSelected(item.value as string | number)">
        <!-- Status indicator -->
        <div v-if="item.isCompleted !== undefined" class="flex items-center gap-1.5 px-5 pt-3 pb-0">
          <span v-if="item.isCompleted" class="flex items-center gap-1.5 text-xs font-semibold text-green-600">
            <i class="pi pi-check-circle text-sm" />
            ดำเนินการเสร็จ
          </span>
          <span v-else class="flex items-center gap-1.5 text-xs font-semibold text-orange-500">
            <i class="pi pi-clock text-sm" />
            อยู่ระหว่างดำเนินการ
          </span>
        </div>

        <!-- Card body -->
        <div class="px-5 py-4" :class="{ 'pt-2!': item.isCompleted !== undefined }">
          <!-- Title row -->
          <div class="flex items-center justify-between gap-2">
            <div class="flex items-center gap-2.5 relative label-container" :class="[{ 'max-w-[50%]': item.status }]">
              <p class="text-base font-bold leading-snug truncate label-wrapper" :class="'text-gray-800'">
                <slot name="title" :item="item" :index="index" :selected="isSelected(item.value, value)">
                  {{ item.title }}
                </slot>
              </p>
              <div class="label-tooltip border border-primary-600 px-3 py-1" role="tooltip">{{ item.title }}</div>
            </div>
            <slot name="badge" :item="item" :index="index" :selected="isSelected(item.value, value)" />
          </div>

          <!-- Subtitle / Description slot -->
          <div class="label-container mt-1.5 ml-0.5">
            <p class="text-sm text-gray-400 label-wrapper truncate">
              <slot name="description" :item="item" :index="index" :selected="isSelected(item.value, value)">
                {{ item.description }}
              </slot>
            </p>
            <div class="label-tooltip border border-primary-600 px-3 py-1" role="tooltip">{{ item.description }}</div>
          </div>
        </div>
      </div>
    </div>

    <Button v-show="canScrollRight" aria-label="scroll right"
      class="absolute right-0 top-1/2 -translate-y-1/2 z-60 p-3 rounded-full shadow-md hover:shadow-lg"
      icon="pi pi-chevron-right" @click="() => scrollBy((scroller?.clientWidth ?? 320) * 0.8)" />
  </div>
</template>

<style scoped>
.card-select-scroll {
  -webkit-overflow-scrolling: touch;
}

.card-select-scroll::-webkit-scrollbar {
  display: none;
}

.absolute>*[aria-label="scroll left"],
.absolute>*[aria-label="scroll right"] {
  width: 20px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.label-wrapper {
  display: inline-block;
  max-width: 100%;
}

.label-wrapper.truncate {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* keep label truncated; tooltip shows full text on hover */

.label-container {
  position: relative;
}

.label-tooltip {
  display: none;
  position: absolute;
  left: 0;
  top: calc(100% + 4px);
  white-space: nowrap;
  background: #ffffff;
  color: #0f172a;
  border-radius: 6px;
  box-shadow: 0 6px 18px rgba(15, 23, 42, 0.12);
  font-size: 13px;
  z-index: 70;
}

.label-container:hover .label-tooltip {
  display: block;
}
</style>

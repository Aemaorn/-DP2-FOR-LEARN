<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import type { SectionItem } from '../sectionConfig';
import { tailwindBreakpoint } from '@/helpers/breakpoint';

const props = defineProps<{
  sections: SectionItem[];
  activeSectionId: string;
}>();

const emit = defineEmits<{
  navigate: [id: string];
}>();

const { lgAndUp } = tailwindBreakpoint();

const isExpanded = ref(false);
const navigatorRef = ref<HTMLElement | null>(null);

const onPointerDownOutside = (event: PointerEvent) => {
  if (!isExpanded.value) return;
  const el = navigatorRef.value;
  if (!el) return;
  const path = event.composedPath();
  if (!path.includes(el)) {
    isExpanded.value = false;
  }
};

onMounted(() => {
  document.addEventListener('pointerdown', onPointerDownOutside, true);
});
onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onPointerDownOutside, true);
});

const activeIndex = computed(() => {
  const idx = props.sections.findIndex((s) => s.id === props.activeSectionId);
  return Math.max(idx, 0);
});

const onNavigate = (id: string) => {
  emit('navigate', id);
  if (!lgAndUp.value) {
    isExpanded.value = false;
  }
};
</script>

<template>
  <!-- Desktop: floating left panel -->
  <div v-if="lgAndUp" ref="navigatorRef" class="fixed left-4 top-[120px] z-[9999]" style="width: 256px">
    <!-- Collapsed button -->
    <button v-if="!isExpanded"
      class="flex items-center gap-2 px-3 py-2 bg-white shadow-lg rounded-xl border border-gray-200 hover:bg-gray-50 transition-colors cursor-pointer"
      @click="isExpanded = true">
      <i class="pi pi-list text-primary" />
      <span class="text-sm font-medium text-gray-700">
        {{ activeIndex + 1 }} / {{ sections.length }}
      </span>
    </button>

    <!-- Expanded panel -->
    <Transition name="slide-left">
      <div v-if="isExpanded" class="bg-white shadow-xl rounded-xl border border-gray-200 flex flex-col"
        style="max-height: calc(100vh - 160px)">
        <!-- Header -->
        <div class="flex items-center justify-between px-4 py-3 border-b border-gray-100 shrink-0">
          <span class="text-sm font-semibold text-gray-800">สารบัญ</span>
          <button class="p-1 hover:bg-gray-100 rounded-md transition-colors cursor-pointer" @click="isExpanded = false">
            <i class="pi pi-times text-gray-400 text-xs" />
          </button>
        </div>

        <!-- Section list -->
        <div class="overflow-y-auto flex-1 py-2">
          <button v-for="(section, index) in sections" :key="section.id"
            class="w-full text-left px-4 py-2 flex items-start gap-3 transition-colors cursor-pointer" :class="[
              section.id === activeSectionId
                ? 'bg-primary/5 border-l-3 border-primary font-semibold'
                : 'border-l-3 border-transparent hover:bg-gray-50',
            ]" @click="onNavigate(section.id)">
            <span class="shrink-0 w-6 h-6 rounded-full flex items-center justify-center text-xs" :class="[
              section.id === activeSectionId
                ? 'bg-primary text-white'
                : 'bg-gray-100 text-gray-500',
            ]">
              {{ index + 1 }}
            </span>
            <span class="text-sm line-clamp-2 leading-tight" :class="[
              section.id === activeSectionId ? 'text-primary' : 'text-gray-600',
            ]">
              {{ section.label }}
            </span>
          </button>
        </div>

        <!-- Footer -->
        <div class="px-4 py-2 border-t border-gray-100 shrink-0">
          <span class="text-xs text-gray-400">{{ activeIndex + 1 }} / {{ sections.length }}</span>
        </div>
      </div>
    </Transition>
  </div>

  <!-- Mobile: bottom drawer -->
  <div v-else>
    <!-- Collapsed button -->
    <button v-if="!isExpanded"
      class="fixed bottom-4 left-4 z-[999] flex items-center gap-2 px-3 py-2 bg-white shadow-lg rounded-full border border-gray-200 hover:bg-gray-50 transition-colors cursor-pointer"
      @click="isExpanded = true">
      <i class="pi pi-list text-primary" />
      <span class="text-sm font-medium text-gray-700">
        {{ activeIndex + 1 }} / {{ sections.length }}
      </span>
    </button>

    <!-- Backdrop -->
    <Transition name="fade">
      <div v-if="isExpanded" class="fixed inset-0 bg-black/30 z-[999]" @click="isExpanded = false" />
    </Transition>

    <!-- Drawer -->
    <Transition name="slide-up">
      <div v-if="isExpanded" class="fixed bottom-0 left-0 right-0 z-[1000] bg-white rounded-t-2xl shadow-2xl"
        style="max-height: 60vh">
        <!-- Handle -->
        <div class="flex justify-center pt-2 pb-1">
          <div class="w-10 h-1 bg-gray-300 rounded-full" />
        </div>

        <!-- Header -->
        <div class="flex items-center justify-between px-4 py-2 border-b border-gray-100">
          <span class="text-sm font-semibold text-gray-800">สารบัญ</span>
          <button class="p-1 hover:bg-gray-100 rounded-md transition-colors cursor-pointer" @click="isExpanded = false">
            <i class="pi pi-times text-gray-400 text-xs" />
          </button>
        </div>

        <!-- Section list -->
        <div class="overflow-y-auto" style="max-height: calc(60vh - 80px)">
          <button v-for="(section, index) in sections" :key="section.id"
            class="w-full text-left px-4 py-3 flex items-start gap-3 transition-colors cursor-pointer" :class="[
              section.id === activeSectionId
                ? 'bg-primary/5 border-l-3 border-primary font-semibold'
                : 'border-l-3 border-transparent hover:bg-gray-50',
            ]" @click="onNavigate(section.id)">
            <span class="shrink-0 w-6 h-6 rounded-full flex items-center justify-center text-xs" :class="[
              section.id === activeSectionId
                ? 'bg-primary text-white'
                : 'bg-gray-100 text-gray-500',
            ]">
              {{ index + 1 }}
            </span>
            <span class="text-sm line-clamp-2 leading-tight" :class="[
              section.id === activeSectionId ? 'text-primary' : 'text-gray-600',
            ]">
              {{ section.label }}
            </span>
          </button>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.slide-left-enter-active,
.slide-left-leave-active {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.slide-left-enter-from,
.slide-left-leave-to {
  transform: translateX(-16px);
  opacity: 0;
}

.slide-up-enter-active,
.slide-up-leave-active {
  transition: transform 0.3s ease;
}

.slide-up-enter-from,
.slide-up-leave-to {
  transform: translateY(100%);
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>

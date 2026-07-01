<script setup lang="ts">
import { useLoadingStore } from '@/stores/loading';

const loadingStore = useLoadingStore();
</script>

<template>
  <Transition name="fade">
    <div v-if="loadingStore.isLoading"
      class="backdrop w-screen h-dvh grid place-items-center fixed top-0 left-0 cursor-wait z-[9999]">
      <ProgressSpinner strokeWidth="8" class="w-[3rem] h-[3rem]" animationDuration=".5s" />
    </div>
  </Transition>
</template>

<style scoped lang="scss">
.backdrop {
  background-color: rgba(0, 0, 0, 0.4);
}

.fade-enter-active,
.fade-leave-active {
  transition: all 0.3s ease-in-out;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.fade-enter-to,
.fade-leave-from {
  opacity: 1;
}

:deep(.p-progressspinner-circle) {
  stroke-dasharray: 89, 200;
  stroke-dashoffset: 0;
  stroke: var(--color-primary-600);
  animation: 1.5s ease-in-out infinite, 6s ease-in-out infinite;
  stroke-linecap: round;
}

.loader {
  fill: var(--color-primary-500);
  animation: ease pulse 1.5s infinite;
}

@keyframes pulse {
  0% {
    fill: var(--color-primary-500);
    opacity: 1;
  }

  50% {
    fill: var(--color-primary-500);
    opacity: .5;
  }

  100% {
    fill: var(--color-primary-500);
    opacity: 1;
  }
}
</style>
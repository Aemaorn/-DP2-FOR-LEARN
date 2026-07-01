<script setup lang="ts">
export interface StepItem {
  label: string;
  value?: string;
  active: boolean;
  onClick?: () => void;
}

defineProps<{ steps: StepItem[] }>();
</script>

<template>
  <div class="flex items-start w-full py-3 px-4">
    <template v-for="(step, index) in steps" :key="index">
      <div class="flex flex-col items-center min-w-[140px]">
        <div class="w-3 h-3 rounded-full" :class="step.active ? 'bg-primary' : 'bg-gray-300'" />
        <span class="text-xs mt-1 text-gray-500">{{ step.label }}</span>
        <span v-if="step.value" class="text-sm font-semibold cursor-pointer text-blue-600 underline"
          @click="step.onClick?.()">
          {{ step.value }}
        </span>
        <span v-else class="text-sm text-gray-400">-</span>
      </div>
      <div v-if="index < steps.length - 1" class="h-0.5 flex-1 mt-1.5"
        :class="step.active ? 'bg-primary' : 'bg-gray-300'" />
    </template>
  </div>
</template>

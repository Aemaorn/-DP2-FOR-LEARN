<script setup lang="ts">
defineProps<{
  label: string,
  labelSpan?: number,
  contentSpan?: number,
}>()
</script>

<template>
  <div class="grid lg:grid-cols-12">
    <p :class="`lg:col-span-${labelSpan ? `${labelSpan}` : '4'} font-medium`">{{ label }} :</p>
    <div :class="`lg:col-span-${contentSpan ? `${contentSpan}` : '8'} min-w-0 break-words`">
      <slot />
    </div>
  </div>
</template>

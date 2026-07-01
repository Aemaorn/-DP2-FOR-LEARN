<script setup lang="ts">

type Props = {
  label?: string;
}

const props = defineProps<Props>();
</script>

<template>
  <div class="flex  items-center justify-between bg-secondary-500 py-2 px-4">
    <p class="text-primary text-lg md:text-xl">{{ props.label }}</p>
    <AccordionHeader class="flex items-center text-primary font-bold" />
  </div>
</template>
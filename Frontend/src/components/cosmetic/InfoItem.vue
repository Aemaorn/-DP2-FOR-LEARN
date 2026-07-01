<script setup lang="ts">

type Prop = {
  title?: string;
  content?: string | number;
};


const { title, content } = defineProps<Prop>();
</script>

<template>
  <div class="flex flex-col">
    <small class="text-gray-400">
      {{ title || '\u00A0' }}
    </small>
    <slot name="content" :item="content">
      <p>{{ content ?? '-' }}</p>
    </slot>
  </div>
</template>
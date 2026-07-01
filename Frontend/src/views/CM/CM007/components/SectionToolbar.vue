<script setup lang="ts">
import type { Cm007Component } from '@/models/CM/cm007';
import { Button, Tag } from 'primevue';

type Props = {
  components: Cm007Component[];
  disabled?: boolean;
};

defineProps<Props>();

const emit = defineEmits<{
  openModal: [];
}>();
</script>

<template>
  <div class="flex items-center gap-2 flex-wrap p-3 bg-white rounded border border-gray-200">
    <Tag v-for="comp in components" :key="comp.componentCode" :value="comp.componentName"
      :severity="comp.isEdited ? 'warn' : 'secondary'" class="text-sm" />

    <Button v-if="!disabled" label="เลือกข้อที่จะแก้ไข" icon="pi pi-pencil" severity="secondary" variant="outlined"
      size="small" @click="emit('openModal')" />
  </div>
</template>

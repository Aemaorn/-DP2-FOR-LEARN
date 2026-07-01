<script setup lang="ts">
import { Card } from 'primevue';
import { InputArea } from '@/components/forms';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';

const value = defineModel<string>({
  default: '',
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();

if (!value.value) {
  value.value = '';
}
</script>

<template>
  <Card class="mb-4" data-section-id="document-description" data-section-label="เอกสารประกอบการเสนอราคา (ถ้ามี)">
    <template #content>
      <TitleHeader label="เอกสารประกอบการเสนอราคา (ถ้ามี)" />
      <div class="md:grid grid-cols-1 gap-2">
        <InputArea v-model="value"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>
<script setup lang="ts">
import { Card } from 'primevue';
import { InputArea } from '@/components/forms';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';

const value = defineModel<string>({
  default: '',
  required: true,
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();

if (!value.value) {
  value.value = '';
}
</script>

<template>
  <Card class="mb-4" data-section-id="source-and-reasons" data-section-label="ความเป็นมา/เหตุผลและความจำเป็น">
    <template #content>
      <TitleHeader label="ความเป็นมา/เหตุผลและความจำเป็น" />
      <div class="md:grid grid-cols-1 gap-2 mt-9">
        <InputArea label="รายละเอียด" v-model.trim="value" rules="required"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>
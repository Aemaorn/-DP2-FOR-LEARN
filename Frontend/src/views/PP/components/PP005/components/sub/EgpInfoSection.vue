<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputField } from '@/components/forms';
import { useMenuStore } from '@/stores/menu';

const props = defineProps({
  isDisabled: {
    type: Boolean,
    default: false,
  }
});
const value = defineModel<string | undefined>({
  required: true,
});

const menuStore = useMenuStore();
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูล e-GP" />
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mt-10">
        <InputField label="เลขที่โครงการ e-GP" v-model="value" :disabled="props.isDisabled || !menuStore.hasManage"
          rules="required" />
      </div>
    </template>
  </Card>
</template>
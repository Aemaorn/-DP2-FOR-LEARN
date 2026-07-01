<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputNumber, Select } from '@/components/forms';
import { onMounted } from 'vue';
import { usePP004Store } from '@/views/PP/stores/PP004/pp004Store';
import { useMenuStore } from '@/stores/menu';

const pp004Store = usePP004Store();
const menuStore = useMenuStore();

onMounted(() => {
  pp004Store.fetchDeliveryConditionOptions();
  pp004Store.fetchDateTypeOptions();
});

</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ระยะเวลาดำเนินการ/ส่งมอบงาน" />
      <div class="mt-8 grid grid-cols-1 lg:grid-cols-3 gap-2">
        <InputNumber label="ส่งมอบภายใน" v-model="pp004Store.body.requisition.deliveryPeriod"
          :disabled="!pp004Store.IsEdit || !menuStore.hasManage" rules="required" />
        <Select label="ระยะเวลา" v-model="pp004Store.body.requisition.deliveryPeriodTypeCode"
          :options="pp004Store.dateTypeOptions" :disabled="!pp004Store.IsEdit || !menuStore.hasManage"
          rules="required" />
        <Select label="เงื่อนไขส่งมอบงาน" v-model="pp004Store.body.requisition.deliveryConditionCode"
          :options="pp004Store.deliveryConditionOptions" :disabled="!pp004Store.IsEdit || !menuStore.hasManage"
          rules="required" />
      </div>
    </template>
  </Card>
</template>
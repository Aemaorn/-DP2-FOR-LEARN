<script setup lang="ts">
import { Radio, InputNumber, Select } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { Card } from 'primevue';
import SharedConstants from '@/constants/shared';
import type { JorPor04Warranty } from '@/views/PP/models/PP004/pp004Model';
import { usePP004Store } from '@/views/PP/stores/PP004/pp004Store';
import { onMounted } from 'vue';
import { useMenuStore } from '@/stores/menu';

const menuStore = useMenuStore();
const pp004Store = usePP004Store();

const warrantyChange = () => {
  if (pp004Store.body.requisition.hasWarranty) {
    pp004Store.body.warranties = [
      {
        period: 0,
        periodTypeCode: '',
        conditionOther: '',
        hasWarranty: true,
      } as JorPor04Warranty,
    ];
  } else {
    pp004Store.body.warranties = [];
  }
}


onMounted((): void => {
  pp004Store.fetchWarrantyConditionOptions();
});
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="การรับประกันความชำรุดบกพร่อง" />
      <Radio class="px-4" v-model="pp004Store.body.requisition.hasWarranty" :options="SharedConstants.HasOptions"
        @change="warrantyChange" :disabled="!pp004Store.IsEdit || !menuStore.hasManage" />
      <div class="px-4 grid grid-cols-1 lg:grid-cols-3 gap-2 mt-5" v-if="pp004Store.body.requisition.hasWarranty && pp004Store.body.warranties?.[0]">
        <InputNumber label="เป็นระยะเวลา" v-model="pp004Store.body.warranties[0].period"
          :disabled="!pp004Store.IsEdit || !menuStore.hasManage" rules="required" />
        <Select label="ระยะเวลา" v-model="pp004Store.body.warranties[0].periodTypeCode"
          :options="pp004Store.dateTypeOptions" :disabled="!pp004Store.IsEdit || !menuStore.hasManage"
          rules="required" />
        <Select label="เงื่อนไขรับประกัน" v-model="pp004Store.body.warranties[0].conditionOther"
          :options="pp004Store.warrantyConditionOptions" :disabled="!pp004Store.IsEdit || !menuStore.hasManage"
          rules="required" />
      </div>
    </template>
  </Card>
</template>
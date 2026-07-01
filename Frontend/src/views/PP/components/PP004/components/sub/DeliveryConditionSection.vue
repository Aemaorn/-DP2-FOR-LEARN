<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Datepicker, InputNumber, Select } from '@/components/forms';
import { usePP004Store } from '@/views/PP/stores/PP004/pp004Store';
import { useMenuStore } from '@/stores/menu';

const pp004Store = usePP004Store();
const menuStore = useMenuStore();

const clearData = (): void => {
  if(pp004Store.body.requisition.deliveryConditionCode != 'DelvCUnit005')
  {
    pp004Store.body.requisition.deliveryDate = undefined as unknown as Date;
  }else
  {
    pp004Store.body.requisition.deliveryPeriod = undefined as unknown as number;
    pp004Store.body.requisition.deliveryPeriodTypeCode = undefined as unknown as string;
  }
};

</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ระยะเวลาดำเนินการ/ส่งมอบ" />
      <div class="mt-4">
        <p class="my-2">กำหนดเวลาที่ต้องการใช้พัสดุนั้น หรือ ให้งานนั้นแล้วเสร็จ</p>
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 gap-y-8 mt-10">
          <Select label="เงื่อนไขระยะเวลาดำเนินการ/ส่งมอบ" v-model="pp004Store.body.requisition.deliveryConditionCode" rules="required"
            :options="pp004Store.deliveryConditionOptions" :disabled="!pp004Store.IsEdit || !menuStore.hasManage"
            @update:model-value="() => clearData()" />
        </div>
        <div v-if="pp004Store.body.requisition.deliveryConditionCode != 'DelvCUnit005'" class="md:grid grid-cols-6 gap-2 mt-8">
          <InputNumber class="col-start-1" label="ระยะเวลาดำเนินการ/ส่งมอบ ภายใน" v-model="pp004Store.body.requisition.deliveryPeriod"
            :disabled="!pp004Store.IsEdit || !menuStore.hasManage" rules="required" />
          <Select v-model="pp004Store.body.requisition.deliveryPeriodTypeCode"
            :options="pp004Store.dateTypeOptions" :disabled="!pp004Store.IsEdit || !menuStore.hasManage"
            rules="required" />
        </div>
        <div v-else class="md:grid grid-cols-3 gap-2 mt-8">
          <Datepicker :disabled="!pp004Store.IsEdit || !menuStore.hasManage" label="ระยะเวลาดำเนินการ/ส่งมอบ ภายในวันที่"
            v-model="pp004Store.body.requisition.deliveryDate"  />
        </div>
      </div>
    </template>
  </Card>
</template>
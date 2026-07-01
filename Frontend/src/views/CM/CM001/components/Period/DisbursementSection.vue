<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Datepicker, InputArea, InputNumber } from '@/components/forms';
import { useCM001PeriodStore } from '@/stores/CM/cm001Period';
import { Card } from 'primevue';

const store = useCM001PeriodStore();
</script>

<template>
  <Card class="mt-4">
    <template #content>
      <TitleHeader :label="'ข้อมูลเบิกจ่าย'" />
      <div class="grid grid-cols-1 lg:grid-cols-3 mt-8 gap-4 gap-y-8">
        <Datepicker label="วันที่เบิกจ่าย" v-model="store.body.disbursementDate"
          :disabled="!store.states.isDisbursementInputManageable" rules="required" />
        <InputNumber class="col-start-1" label="จำนวนเงินเบิกจ่าย" v-model="store.body.disbursementAmount"
          :disabled="!store.states.isDisbursementInputManageable" grouping :min-fraction-digits="2" rules="required" />
        <InputNumber label="ค่าปรับ" v-model="store.body.deductionAmount" grouping :min-fraction-digits="2" :disabled="true"
          input-class="text-red-500" />
        <InputArea class="lg:col-span-3 col-start-1" rules="required" label="หมายเหตุ" v-model="store.body.disbursementRemark"
          :disabled="!store.states.isDisbursementInputManageable" />
      </div>
    </template>
  </Card>
</template>

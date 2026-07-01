<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Datepicker, InputArea, InputNumber } from '@/components/forms';
import { useCm006DetailStore } from '@/stores/CM/CM006/cm006.detail';
import { Card } from 'primevue';

const store = useCm006DetailStore();
</script>

<template>
  <Card class="mt-4" id="disbursement-section">
    <template #content>
      <TitleHeader :label="'ข้อมูลเบิกจ่าย'" />
      <div class="grid grid-cols-1 lg:grid-cols-3 mt-8 gap-4 gap-y-8">
        <Datepicker label="วันที่เบิกจ่าย" v-model="store.body.guaranteeReturn.disbursementDate"
          :disabled="!store.isAccountingCanEdit" rules="required" />
        <InputNumber class="col-start-1" label="จำนวนเงินเบิกจ่าย" v-model="store.body.guaranteeReturn.disbursementAmount"
          :disabled="!store.isAccountingCanEdit" grouping :min-fraction-digits="2" rules="required" />
        <InputNumber label="ค่าปรับ" v-model="store.body.guaranteeReturn.deductedAmount" grouping :min-fraction-digits="2"
          :disabled="true" input-class="text-red-500" />
        <InputArea class="lg:col-span-3 col-start-1" label="หมายเหตุ" v-model="store.body.guaranteeReturn.disbursementRemark"
          :disabled="!store.isAccountingCanEdit" />
      </div>
    </template>
  </Card>
</template>

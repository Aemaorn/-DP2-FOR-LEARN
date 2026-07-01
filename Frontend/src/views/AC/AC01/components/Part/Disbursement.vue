<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Datepicker, InputArea, Checkbox, InputNumber } from '@/components/forms';
import { useAc01DetailStore } from '@/stores/AC/ac01';
import { useMenuStore } from '@/stores/menu';

const menuStore = useMenuStore();
const store = useAc01DetailStore();
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลการเบิกจ่าย" />
      <div class="px-4">
        <div class="grid grid-cols-1 lg:grid-cols-4 gap-4">
          <Checkbox rules="required" label="ระบุจำนวนเงินสุทธิ" class="mb-4"
            v-model="store.body.isInvoiceAmount" binary :disabled="!store.state.canConfirm || !menuStore.hasManage" />
          <InputNumber v-if="store.body.isInvoiceAmount" label="จำนวนเงินสุทธิ"
            v-model="store.body.invoiceAmount" grouping :min-fraction-digits="2"
            :disabled="!store.state.canConfirm || !menuStore.hasManage" />
          <Datepicker label="วันที่เบิกจ่าย" v-model="store.body.advancePaymentDate" rules="required"
            class="col-start-1" :disabled="!store.state.canConfirm || !menuStore.hasManage" />
          <InputArea label="หมายเหตุ" v-model="store.body.description" class="col-span-1 lg:col-span-4"
            :disabled="!store.state.canConfirm || !menuStore.hasManage" />
        </div>
      </div>
    </template>
  </Card>
</template>
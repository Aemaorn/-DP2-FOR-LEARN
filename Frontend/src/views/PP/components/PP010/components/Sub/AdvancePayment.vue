<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputNumber, Radio } from '@/components/forms';
import type { TAdvancePayment, TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';
import { watch } from 'vue';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });

const buyerRadioOption = [
  { value: true, label: 'จ่ายเงินล่วงหน้า' },
  { value: false, label: 'ไม่มีเงินจ่ายล่วงหน้า' },
];

watch(() => body.value.detail.advancePayment?.hasAdvancePayment, (newValue) => {
  if (!newValue) {
    body.value.detail.advancePayment = {
      hasAdvancePayment: false,
    } as TAdvancePayment;
  };
});
</script>

<template>
  <Card v-if="body.detail.advancePayment"
    :pt="{ root: { 'data-section-id': 'advance-payment', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <Radio :disabled="props.disable" label="การชำระเงินให้แก่ผู้ซื้อมีการจ่ายเงินล่วงหน้าหรือไม่"
        :options="buyerRadioOption" class="mt-5" v-model="body.detail.advancePayment.hasAdvancePayment" />
      <div v-if="body.detail.advancePayment.hasAdvancePayment" class="grid lg:grid-cols-3 gap-2 mt-4">
        <InputNumber :disabled="props.disable" rules="required" label="จ่ายเงินล่วงหน้าค่าจ้างล่วงหน้า"
          v-model="body.detail.advancePayment.amount" use-grouping :min-fraction-digits="2" grouping />
        <InputNumber :disabled="props.disable" rules="required" label="อัตราร้อยละ(ของราคาจ้าง)"
          v-model="body.detail.advancePayment.percentage" :min-fraction-digits="2" :max-fraction-digits="3" grouping />
      </div>
    </template>
  </Card>
</template>
<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputNumber, Radio } from '@/components/forms';
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });

const buyerRadioOption = [
  { value: true, label: 'ต้องการ' },
  { value: false, label: 'ไม่ต้องการ' },
];
</script>

<template>
  <Card v-if="body.detail.retentionPayment"
    :pt="{ root: { 'data-section-id': 'retention-payment', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <Radio :disabled="props.disable" label="ต้องการหักเงินประกันผลงานหรือไม่" :options="buyerRadioOption" class="mt-5"
        v-model="body.detail.retentionPayment.hasRetentionPayment" />
      <div v-if="body.detail.retentionPayment.hasRetentionPayment" class="grid lg:grid-cols-3 gap-2 mt-4">
        <InputNumber :disabled="props.disable" rules="required" label="จำนวนร้อยละ ของเงินที่ต้องจ่ายในงวดนั้น"
          v-model="body.detail.retentionPayment.percentage" :min-fraction-digits="2" :max-fraction-digits="3" grouping />
        <InputNumber :disabled="props.disable" rules="required" label="จำนวนเงิน(บาท)"
          v-model="body.detail.retentionPayment.amount" :min-fraction-digits="2" grouping use-grouping />
      </div>
    </template>
  </Card>
</template>
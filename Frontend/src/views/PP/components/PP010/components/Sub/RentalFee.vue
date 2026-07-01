<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });

</script>

<template>
  <Card v-if="body.detail.agreement" :pt="{ root: { 'data-section-id': 'rental-fee', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
        <div class="grid lg:grid-cols-3 gap-2 mt-10 gap-y-6">
           <InputNumber :disabled="props.disable" label="อัตราค่าเช่าดือนละไม่เกิน" grouping
              v-model="body.detail.agreement.agreementPrice" rules="required" :min-fraction-digits="2"/>
           <InputNumber :disabled="props.disable" label="รวมเป็นจำนวนเงิน" grouping
              v-model="body.detail.agreement.totalAmount" rules="required" :min-fraction-digits="2"/>
        </div>
    </template>
  </Card>
</template>
<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField, InputNumber } from '@/components/forms';
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });
</script>

<template>
  <Card :pt="{ root: { 'data-section-id': 'edit-po', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <div class="grid lg:grid-cols-3 gap-2 mt-10 gap-y-6">
        <InputField :disabled="props.disable" label="เลข PO (SAP)" v-model="body.poNumber" />
        <InputNumber :disabled="props.disable" label="วงเงินตามสัญญา" v-model="body.budget" grouping
          :max-fraction-digits="3" :min-fraction-digits="2" />
      </div>
    </template>
  </Card>
</template>

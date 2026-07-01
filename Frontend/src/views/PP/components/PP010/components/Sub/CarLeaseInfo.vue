<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputNumber } from '@/components/forms';
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const store = useContractDraftStore();

const body = defineModel<TContractDraftBody>("body", { required: true });
</script>

<template>
  <Card v-if="body.detail.carLease" :pt="{ root: { 'data-section-id': 'car-lease-info', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <div class="grid lg:grid-cols-3 gap-2 mt-10">
        <InputNumber :disabled="props.disable" label="ค่าเช่า/คัน" v-model="body.detail.carLease.rentPerVehicle"
          use-grouping :min-fraction-digits="2" grouping />
        <Select :disabled="props.disable" :options="store.dropdown.periodTypeOptions" label="ต่อ"
          v-model="body.detail.carLease.unitCode" />
      </div>
    </template>
  </Card>
</template>
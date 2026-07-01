<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  Select,
} from '@/components/forms';
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { ref, watch } from 'vue';

type Props = {
  label: string;
  disable?: boolean;
  showIsPenalty?: boolean;
}

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });

const store = useContractDraftStore();

const updatingFrom = ref<'rate' | 'amount' | null>(null);

const onIsPenaltyClick = (val: boolean) => {
  if (!val && body.value.detail?.penalty) {
    body.value.detail.penalty.typeCode = null as any;
    body.value.detail.penalty.rate = null as any;
    body.value.detail.penalty.amount = null as any;
    body.value.detail.penalty.rateTypeCode = null as any;
  }
};

watch(
  () => [body.value.budget, body.value.detail?.penalty?.rate],
  () => {
    if (updatingFrom.value === 'amount') return;

    const budget = body.value.budget;
    const item = body.value.detail?.penalty;

    if (!budget || !item) return;

    updatingFrom.value = 'rate';
    const percent = Number(item.rate || 0);
    item.amount = Math.round(((percent / 100) * budget) * 100) / 100;
    updatingFrom.value = null;
  },
  { deep: true }
);

watch(
  () => [body.value.budget, body.value.detail?.penalty?.amount],
  () => {
    if (updatingFrom.value === 'rate') return;

    const budget = body.value.budget;
    const item = body.value.detail?.penalty;

    if (!budget || !item) return;

    updatingFrom.value = 'amount';
    const amount = Number(item.amount || 0);
    item.rate = Math.round(((amount / budget) * 100) * 1000) / 1000;
    updatingFrom.value = null;
  },
  { deep: true }
);
</script>

<template>
  <Card v-if="body.detail.penalty" :pt="{ root: { 'data-section-id': 'mulct', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <div v-if="props.showIsPenalty" class="flex gap-6 mt-4">
        <div class="flex items-center gap-2">
          <RadioButton v-model="body.detail.penalty.isPenalty" :value="true" inputId="hasPenalty" :disabled="props.disable" @click="onIsPenaltyClick(true)" />
          <label for="hasPenalty">มีค่าปรับ</label>
        </div>
        <div class="flex items-center gap-2">
          <RadioButton v-model="body.detail.penalty.isPenalty" :value="false" inputId="noPenalty" :disabled="props.disable" @click="onIsPenaltyClick(false)" />
          <label for="noPenalty">งดค่าปรับ</label>
        </div>
      </div>
      <template v-if="!props.showIsPenalty || body.detail.penalty.isPenalty">
        <div class="grid lg:grid-cols-3 gap-2 mt-8">
          <Select :disabled="props.disable" :options="store.dropdown.fineTypeOptions" label="ประเภทค่าปรับ"
            v-model="body.detail.penalty.typeCode" rules="required" />
        </div>
        <div class="grid lg:grid-cols-3 gap-2 mt-8">
          <InputNumber :min-fraction-digits="2" :max-fraction-digits="3" :max-number="100" :disabled="props.disable" rules="required"
            label="ค่าปรับอัตราร้อยละ" v-model="body.detail.penalty.rate" />
          <InputNumber :disabled="props.disable" rules="required" label="จำนวนเงินค่าปรับ"
            v-model="body.detail.penalty.amount" grouping :min-fraction-digits="2" />
          <Select :disabled="props.disable" :options="store.dropdown.periodConditionTypeOptions"
            v-model="body.detail.penalty.rateTypeCode" rules="required" />
        </div>
      </template>
    </template>
  </Card>
</template>
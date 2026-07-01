<script setup lang="ts">
import { InputNumber, Select } from '@/components/forms';
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });
const store = useContractDraftStore();
</script>

<template>
  <Card v-if="body.detail.termination" :pt="{ root: { 'data-section-id': 'termination-info-duration', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <Select :disabled="props.disable"
          :options="store.dropdown.conditionTypeOptions" label="ระยะเวลาเริ่มต้นสัญญา" class="grid lg:grid-cols-3 gap-2 mt-8"
          v-model="body.periodConditionType" rules="required" />
      <div v-if="body.periodConditionType === 'CSDPCond003'" class="grid lg:grid-cols-3 gap-2 mt-8">
        <Datepicker :disabled="props.disable" label="ผู้รับจ้างตกลงให้บริการระยะเวลา ตั้งแต่วันที่"
          v-model="body.detail.termination.startDate" />
        <Datepicker :disabled="props.disable" label="จนถึง" v-model="body.detail.termination.endDate" />
      </div>
      <div v-else-if="body.periodConditionType" class="grid lg:grid-cols-3 gap-2  mt-8">
        <InputNumber :disabled="props.disable" label="ระยะเวลาการให้บริการ (ปี)"
          v-model="body.detail.termination.vendorProcessingTime.year" />
        <InputNumber :disabled="props.disable" label="(เดือน)"
          v-model="body.detail.termination.vendorProcessingTime.month" />
        <InputNumber :disabled="props.disable" label="(วัน)"
          v-model="body.detail.termination.vendorProcessingTime.day"  />
      </div>
    </template>
  </Card>
</template>
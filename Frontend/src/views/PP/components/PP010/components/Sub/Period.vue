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
  <Card v-if="body.detail.agreement" :pt="{ root: { 'data-section-id': 'period', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
        <div v-if="body.detail.agreement.duration"
          class="grid lg:grid-cols-3 gap-2 mt-10 gap-y-6">
          <InputNumber :disabled="props.disable" label="ระยะเวลาเช่า (ปี)"
            v-model="body.detail.agreement.duration.year" rules="required" />
          <InputNumber :disabled="props.disable" label="(เดือน)" v-model="body.detail.agreement.duration.month"
            rules="required" />
          <InputNumber :disabled="props.disable" label="(วัน)" v-model="body.detail.agreement.duration.day" rules="required" />
        </div>

        <div class="grid lg:grid-cols-3 gap-2 mt-6 gap-y-6">
          <Datepicker :disabled="props.disable" label="นับตั้งแต่วันที่" :max-date="body.detail.agreement.endDate"
            v-model="body.detail.agreement.startDate" rules="required" />
          <Datepicker :disabled="props.disable" label="จนถึง" :min-date="body.detail.agreement.startDate"
            v-model="body.detail.agreement.endDate" rules="required" />
        </div>
    </template>
  </Card>
</template>
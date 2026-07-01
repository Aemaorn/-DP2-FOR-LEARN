<script setup lang="ts">
import { Datepicker } from '@/components/forms';
import { TContractDraftStatus } from '@/views/PP/enums/pp010';
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });
</script>

<template>
  <Card v-if="body.detail.termination" :pt="{ root: { 'data-section-id': 'termination-info-date', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <div class="grid lg:grid-cols-3 gap-2 mt-8">
        <Datepicker :disabled="props.disable && body.contractStatus === TContractDraftStatus.Approved"
          label="ผู้รับจ้างต้องเริ่มทำงาน ภายในวันที่" v-model="body.detail.termination.startDate" />
        <Datepicker :disabled="props.disable && body.contractStatus === TContractDraftStatus.Approved"
          label="ผู้รับจ้างต้องทำงานให้เสร็จบริบูรณ์ภายในวันที่" v-model="body.detail.termination.endDate" />
      </div>
    </template>
  </Card>
</template>
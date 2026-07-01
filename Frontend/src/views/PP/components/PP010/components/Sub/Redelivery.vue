<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Select } from '@/components/forms';
import { ContractDraftTemplate } from '@/enums/contractDraftt';
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
  <Card v-if="body.detail.redelivery" :pt="{ root: { 'data-section-id': 'redelivery', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />

      <div v-if="body.template == ContractDraftTemplate.CFormat005" class="flex flex-col gap-4 mt-8">
        <div class="grid lg:grid-cols-3 gap-2 gap-y-8">
          <InputNumber :disabled="props.disable" label="หากชำรุดบกพร่อง ต้องซ่อมแชมหรือติดตั้งใหม่ ภายในกำหนด"
            v-model="body.detail.redelivery.redeliveryDeadline" rules="required" />
          <Select :disabled="props.disable" :options="store.dropdown.periodTypeOptions"
            v-model="body.detail.redelivery.redeliveryDeadlineTypeCode" rules="required" />
        </div>
      </div>

      <div v-if="body.template == ContractDraftTemplate.CFormat012" class="flex flex-col gap-4 mt-8">
        <InputArea
          label="ผู้ให้แลกเปลี่ยนได้รับตรวจรับฯ ผู้รับแลกเปลี่ยนจะออกหลีกฐานฯ เพื่อให้ผู้รับแลกเปลี่ยนมารับสิ่งของของผู้ให้แลกเปลี่ยนจาก"
          v-model="body.detail.redelivery.description" rules="required" />
        <div class="grid lg:grid-cols-3 gap-2 gap-y-8 mt-4" v-if="body.detail.redelivery.rentalDuration">
          <InputNumber :disabled="props.disable" label="กำหนดเวลาจะซื้อจะขาย(ปี)"
            v-model="body.detail.redelivery.rentalDuration.year" rules="required" />
          <InputNumber :disabled="props.disable" label="(เดือน)" v-model="body.detail.redelivery.rentalDuration.month"
            rules="required" />
          <InputNumber :disabled="props.disable" label="(วัน)" v-model="body.detail.redelivery.rentalDuration.day" rules="required" />
        </div>
      </div>

      <div v-if="body.template == ContractDraftTemplate.CFormat008" class="flex flex-col gap-8 mt-12">
        <div class="grid lg:grid-cols-3 gap-2 gap-y-8">
          <InputNumber :disabled="props.disable" label="ส่งมอบไม่ถูกต้อง ต้องนำรถยนต์คันอื่นมาส่งมอบให้ใหม่ภายใน"
            v-model="body.detail.redelivery.correctionDue" rules="required" />
          <Select label="ระยะเวลา" :disabled="props.disable" :options="store.dropdown.periodTypeOptions"
            v-model="body.detail.redelivery.correctionDueTypeCode" rules="required" />
        </div>
        <div class="grid lg:grid-cols-3 gap-2 gap-y-8">
          <InputNumber :disabled="props.disable" label="หรือต้องทำการแก้ไขให้ถูกต้องด้วยค่าใช่จ่ายผู้ให้เช่าเองภายใน"
            v-model="body.detail.redelivery.redeliveryDeadline" rules="required" />
          <Select label="ระยะเวลา" :disabled="props.disable" :options="store.dropdown.periodTypeOptions"
            v-model="body.detail.redelivery.redeliveryDeadlineTypeCode" rules="required" />
        </div>
      </div>
    </template>
  </Card>
</template>
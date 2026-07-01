<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputNumber } from '@/components/forms';
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });

</script>

<template>
  <Card v-if="body.detail.warranty"
    :pt="{ root: { 'data-section-id': 'warranty-ma', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <div class="flex flex-col gap-6 mt-12">
        <div>
          <span class="text-xl">เวลาคอมพิวเตอร์ขัดข้อง รวมตามเกณฑ์ นับไม่เกินเดือนละ</span>
          <div class="flex items-end gap-4 mt-4">
            <InputNumber class="w-40" :disabled="props.disable" label="ชั่วโมง" input-class="text-end"
              v-model="body.detail.warranty.warrantyMonthlyAllowedDowntimeHours" hide-details />
            <InputNumber class="w-48" :disabled="props.disable" label="หรือร้อยละ" input-class="text-end"
              v-model="body.detail.warranty.warrantyDowntimePercentPerMonth" :max-number="100" :min-fraction-digits="2"
              :max-fraction-digits="3" hide-details />
            <span class="text-xl whitespace-nowrap mb-2">ของเวลาใช้งานทั้งหมด</span>
          </div>
        </div>
        <div>
          <span class="text-xl">มิฉะนั้นผู้รับจ้าง ต้องยินยอมให้คิดค่าปรับ</span>
          <div class="flex items-end gap-4 mt-4">
            <InputNumber class="w-48" :disabled="props.disable" grouping :min-fraction-digits="2" label="ชั่วโมงละ"
              input-class="text-end" v-model="body.detail.warranty.warrantyPenaltyPerHour" hide-details />
            <span class="text-xl whitespace-nowrap mb-2">บาท</span>
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>
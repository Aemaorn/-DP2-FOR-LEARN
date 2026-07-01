<script setup lang="ts">
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
  <Card v-if="body.detail.copierLease" :pt="{ root: { 'data-section-id': 'copier-lease-info', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <div class="flex flex-col gap-6 mt-5">
        <div>
          <div class="flex gap-4 mt-4">
            <InputNumber class="w-100" :disabled="props.disable" label="ค่าเช่ารายเดือน เดือนละ" input-class="text-end"
              :min-fraction-digits="2" v-model="body.detail.copierLease.monthlyRentPerMachine" use-grouping rules="required" hide-details />
              <span class="text-xl whitespace-nowrap mb-2">บาท ต่อเครื่องถ่ายเอกสารหนึ่งเครื่อง</span>
          </div>
        </div>

        <div>
          <div class="flex gap-4 mt-4">
            <InputNumber class="w-100" :disabled="props.disable" label="ค่าเช่ารายเดือนรวม เดือนละ" input-class="text-end"
              :min-fraction-digits="2" v-model="body.detail.copierLease.totalMonthlyRent" use-grouping rules="required"  hide-details/>
              <span class="text-xl whitespace-nowrap mb-2">บาท</span>
          </div>
        </div>

        <div>
          <div class="flex items-end gap-4 mt-4">
            <InputNumber class="w-100" :disabled="props.disable" label="ประเมินจากจำนวนสำเนาเอกสาร เดือนละ" input-class="text-end"
              v-model="body.detail.copierLease.estimatedMonthlyCopies" use-grouping rules="required" hide-details />
            <span class="text-xl whitespace-nowrap mb-2">แผ่น</span>
          </div>
        </div>

        <div>
          <div class="flex items-end gap-4 mt-4">
            <InputNumber class="w-100" :disabled="props.disable" label="กรณีจำนวนถ่ายเอกสารไม่ถึง" input-class="text-end"
              v-model="body.detail.copierLease.belowEstimateCondition" rules="required" hide-details />
            <span class="text-xl whitespace-nowrap mb-2">แผ่น ต่อเดือน</span>
          </div>
        </div>

        <div>
          <div class="flex items-end gap-4 mt-4">
            <InputNumber class="w-100" :disabled="props.disable" label="เปลี่ยนเป็นคิดคำนวณอัตราสำเนาแผ่นละ" input-class="text-end"
              :min-fraction-digits="2" v-model="body.detail.copierLease.perCopyRateCondition" use-grouping rules="required" hide-details />
            <span class="text-xl whitespace-nowrap mb-2">บาท</span>
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>
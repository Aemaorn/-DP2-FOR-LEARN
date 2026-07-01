<script setup lang="ts">
import { InputNumber, Select, Datepicker } from '@/components/forms';
import { ContractDraftTemplate } from '@/enums/contractDraftt';
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
  <Card v-if="body.detail.computerLease" :pt="{ root: { 'data-section-id': 'computer-lease-info', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />

      <div v-if="body.template == ContractDraftTemplate.CFormat005" class="flex flex-col gap-4 mt-12">
        <p>3.3 กำหนดระยะเวลาอนุญาตให้ใช้สิทธิในโปรแกรมคอมพิวเตอร์เริ่มต้นตั้งแต่วันที่ได้รับมอบโปรแกรมคอมพิวเตอร์</p>
        <div class="grid lg:grid-cols-3 gap-2 mt-4">
          <Datepicker v-if="body.detail.delivery" label="วันที่สิ้นสุดระยะเวลาอนุญาตให้ใช้สิทธิในโปรแกรมคอมฯ"
            v-model="body.detail.delivery.date" rules="required" :disabled="props.disable" />
        </div>
        <div v-if="body.detail.computerLease.duration" class="grid lg:grid-cols-3 gap-2">
          <InputNumber :disabled="props.disable" label="กำหนดระยะเวลา (ปี)"
            v-model="body.detail.computerLease.duration.year" rules="required" />
          <InputNumber :disabled="props.disable" label="(เดือน)" v-model="body.detail.computerLease.duration.month"
            rules="required" />
          <InputNumber :disabled="props.disable" label="(วัน)" v-model="body.detail.computerLease.duration.day" rules="required" />
        </div>
      </div>

      <div v-if="body.template == ContractDraftTemplate.CFormat006" class="flex flex-col gap-4 mt-12">
        <div v-if="body.detail.computerLease.duration" class="grid lg:grid-cols-4 gap-2">
          <InputNumber :disabled="props.disable" label="ระยะเวลาการคำนวนค่าเช่าคอมพิวเตอร์ (ปี)"
            v-model="body.detail.computerLease.duration.year" rules="required" />
          <InputNumber :disabled="props.disable" label="(เดือน)" v-model="body.detail.computerLease.duration.month"
            rules="required" />
          <InputNumber :disabled="props.disable" label="(วัน)" v-model="body.detail.computerLease.duration.day" rules="required" />
          <Select :disabled="props.disable" t label="เงื่อนไขการนับ"
            :options="store.dropdown.conditionTypeOptions"
            v-model="body.detail.computerLease.rentalStartCondition" />
        </div>
      </div>
    </template>
  </Card>
</template>
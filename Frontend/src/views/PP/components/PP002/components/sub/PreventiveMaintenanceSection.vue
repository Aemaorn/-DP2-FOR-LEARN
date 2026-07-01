<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import type { TorPreventiveMaintenanceModel } from '@/views/PP/models/PP002/pp002Model';
import { InputArea, InputField, InputNumber, Radio, Select } from '@/components/forms';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { onMounted, ref, watch } from 'vue';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import SharedService from '@/services/Shared/dropdown';
import { useMenuStore } from '@/stores/menu';
import Card from 'primevue/card';
import type { Ref } from 'vue';
import { usePPDetailStore } from '@/stores/PP/ppStore';

const value = defineModel<TorPreventiveMaintenanceModel>({
  default: () => ({} as TorPreventiveMaintenanceModel),
});

const isPM = defineModel<boolean | undefined>('isPM', {
  default: undefined,
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();

const mockRadioData = [
  { label: 'มี', value: true },
  { label: 'ไม่มี', value: false },
];

const periodDropdown = ref<Array<Option>>([]);
const timeTypeDropdown = ref<Option[]>([]);
const pmFineTypeDropdown = ref<Array<Option>>([]);
const procurementStore = usePPDetailStore();

onMounted(async () => {
  await Promise.all([getDropdownAsync(periodDropdown, EGroupCode.PeriodType), getDropdownAsync(timeTypeDropdown, EGroupCode.PTimeType), getDropdownAsync(pmFineTypeDropdown, EGroupCode.PMFineType)]);
});

const getDropdownAsync = async (target: Ref<Array<Option>>, group: EGroupCode) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(group, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

watch(isPM, (val) => {
  if (!val) {
    value.value = {} as TorPreventiveMaintenanceModel;
  }
});

watch(() => value.value.disruptedFinePercent, (percent) => {
  const budget = procurementStore.procurementDetail?.budget;
  if (percent != null && budget != null) {
    value.value.disruptedFineAmount = (percent / 100) * budget;
  } else {
    value.value.disruptedFineAmount = undefined;
  }
});
</script>

<template>
  <Card class="mb-4" data-section-id="preventive-maintenance"
    data-section-label="การบำรุงรักษา (Preventive Maintenance) (IT)">
    <template #content>
      <TitleHeader label="การบำรุงรักษา (Preventive Maintenance) (IT)" />
      <div class="md:grid grid-cols-3 gap-2">
        <Radio label="การบำรุงรักษา" v-model="isPM" :options="mockRadioData"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" rules="required" />
      </div>
      <template v-if="isPM">
        <div class="md:grid grid-cols-6 gap-2 mt-10">
          <InputField class="col-span-4" label="ชื่อพัสดุ" v-model="value.pmProductName"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>
        <div class="md:grid grid-cols-6 gap-2 mt-8">
          <InputNumber class="col-span-2" label="มิฉะนั้นจะยอมให้คิดค่าปรับในอัตราร้อยละ" v-model="value.pmFinePct"
            :max-number="100" grouping :min-fraction-digits="2" :max-fraction-digits="3"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <Select class="col-span-2" v-model="value.pmFinePctUnit" :options="pmFineTypeDropdown"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <InputNumber class="col-span-2" label="มูลค่ารวมของการปรับแต่ละครั้งต่ำสุด" v-model="value.pmFineAmount"
            grouping :min-fraction-digits="2" :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>
        <div class="md:grid grid-cols-6 gap-2 mt-8">
          <div class="flex gap-x-2 col-span-2">
            <InputNumber class="flex-1" label="ตรวจสอบบำรุงรักษาอย่างน้อย" v-model="value.pmCount" grouping
              :disabled="!store.status.canEditTor || !menuStore.hasManage" />
            <p class="mt-2">ครั้งต่อ</p>
          </div>
          <Select class="col-span-2" v-model="value.pmUnit" :options="periodDropdown"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>
        <div class="md:grid grid-cols-6 gap-2 mt-8">
          <InputNumber class="col-span-2" label="ขัดข้องรวมตามเกณฑ์ไม่เกินเดือนละ" v-model="value.disruptedCount"
            grouping :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <Select class="col-span-2" v-model="value.disruptedCountUnit" :options="timeTypeDropdown"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <InputNumber class="col-span-2" label="หรือไม่เกินร้อยละ (%) ของเวลาใช้งานทั้งหมดของเดือนนั้น"
            v-model="value.disruptedPercent" :max-number="100" grouping :min-fraction-digits="2"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>
        <div class="md:grid grid-cols-6 gap-2 mt-8">
          <InputNumber class="col-span-2" label="มิฉะนั้นจะถูกปรับอัตราชั่วโมงละ (%)"
            v-model="value.disruptedFinePercent" :max-number="100" grouping :min-fraction-digits="2"
            :max-fraction-digits="3" helper-text="กำหนดในอัตราระหว่างร้อยละ 0.025 - 0.035 ของราคาตามสัญญาต่อชั่วโมง"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />

        </div>
        <div class="md:grid grid-cols-6 gap-2 mt-8">
          <InputArea class="col-span-6" label="เงื่อนไขการบำรุงรักษา" v-model="value.condition"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>
      </template>
    </template>
  </Card>
</template>
<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { InputNumber, Select, TimePicker, Radio } from '@/components/forms';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { onMounted, ref, watch, type Ref } from 'vue';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import SharedService from '@/services/Shared/dropdown';
import { useMenuStore } from '@/stores/menu';
import type { TorCorrectiveMaintenanceModel } from '@/views/PP/models/PP002/pp002Model';

const value = defineModel<TorCorrectiveMaintenanceModel>({
  default: () => ({} as TorCorrectiveMaintenanceModel),
});

const isCM = defineModel<boolean | undefined>('isCM', {
  default: undefined,
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();

const mockRadioData = [
  { label: 'มี', value: true },
  { label: 'ไม่มี', value: false },
];

const periodDropdown = ref<Option[]>([]);
const dowDropdown = ref<Option[]>([]);
const cmFineType = ref<Option[]>([]);

onMounted(async () => {
  await Promise.all([getDropdownAsync(periodDropdown, EGroupCode.PeriodType), getDropdownAsync(dowDropdown, EGroupCode.DOW), getDropdownAsync(cmFineType, EGroupCode.CMFineType)]);
});

const getDropdownAsync = async (target: Ref<Array<Option>>, group: EGroupCode) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(group, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

watch(isCM, (val) => {
  if (!val) {
    value.value = {} as TorCorrectiveMaintenanceModel;
  }
});
</script>

<template>
  <Card class="mb-4" data-section-id="corrective-maintenance"
    data-section-label="การซ่อมแซมแก้ไข (Corrective Maintenance)">
    <template #content>
      <TitleHeader label="การซ่อมแซมแก้ไข (Corrective Maintenance)" />
      <div class="md:grid grid-cols-3 gap-2">
        <Radio label="การซ่อมแซมแก้ไข" v-model="isCM" :options="mockRadioData"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" rules="required" />
      </div>
      <template v-if="isCM">
        <div class="md:grid grid-cols-6 gap-2 mt-10">
          <Select label="ต้องให้บริการซ่อมแซม ในวัน" class="col-span-2" v-model="value.dayStart" :options="dowDropdown"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <Select label="ถึงวัน" class="col-span-2" v-model="value.dayEnd" :options="dowDropdown"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>
        <div class="md:grid grid-cols-6 gap-2 mt-8">
          <TimePicker label="เวลา" class="col-span-2" v-model="value.startTime"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <TimePicker label="ถึงเวลา" class="col-span-2" v-model="value.endTime"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>
        <div class="md:grid grid-cols-6 gap-2 mt-8">
          <InputNumber class="col-span-2" label="เริ่มจัดการซ่อมแซมภายใน" v-model="value.cmCount" grouping
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <Select class="col-span-2" v-model="value.cmUnit" :options="periodDropdown"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>
        <div class="md:grid grid-cols-6 gap-2 mt-8">
          <InputNumber class="col-span-2" label="ให้แล้วเสร็จภายใน" v-model="value.cmCompleteCount" grouping
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <Select class="col-span-2" v-model="value.cmCompleteUnit" :options="periodDropdown"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>
        <div class="md:grid grid-cols-3 gap-2 mt-8">
          <span class="col-span-2">หากไม่เข้ามาซ่อมแซมภายในเวลาที่กำหนด</span>
        </div>
        <div class="md:grid grid-cols-6 gap-2 mt-8">
          <InputNumber class="col-span-2" label="ยินยอมให้คิดค่าปรับ (%)" :max-number="100" :min-fraction-digits="2"
            v-model="value.cmFinePercent" grouping :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <!-- <InputNumber class="col-span-2" label="ค่าปรับกรณีเกิดความเสียหาย (%)" v-model="value.cmDisruptedFinePercent"
            :max-number="100" grouping :min-fraction-digits="2"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" /> -->
          <Select class="col-span-2" v-model="value.cmFinePercentUnit" :options="cmFineType"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>
      </template>
    </template>
  </Card>
</template>

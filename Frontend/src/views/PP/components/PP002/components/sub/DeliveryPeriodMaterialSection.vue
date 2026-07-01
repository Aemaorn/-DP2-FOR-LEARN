<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import type { TechnicalPeriods } from '@/views/PP/models/PP002/pp002Model';
import { Datepicker, InputNumber, Select } from '@/components/forms';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { onMounted, ref, type Ref } from 'vue';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import defaultProps from '@/helpers/defaultProps';
import SharedService from '@/services/Shared/dropdown';

const props = defineProps({
  titleName: defaultProps(''),
  label: defaultProps(''),
});

const value = defineModel<TechnicalPeriods>({
  default: () => ({} as TechnicalPeriods),
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();

const periodDropdown = ref<Option[]>([]);
const periodConDropdown = ref<Array<Option>>([]);
const deliveryConditionDropdown = ref<Array<Option>>([]);

onMounted(async () => {
  await Promise.all([
    getDropdownAsync(periodDropdown, EGroupCode.PeriodType),
    getDropdownAsync(periodConDropdown, EGroupCode.DelvCUnit),
    getDropdownAsync(deliveryConditionDropdown, EGroupCode.DelvCUnit)
  ]);
});

const getDropdownAsync = async (target: Ref<Array<Option>>, groupCode: EGroupCode) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(groupCode, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const clearData = (): void => {
  value.value = {
    ...value.value,
    period: undefined,
    periodTypeCode: undefined,
    periodConditionCode: value.value.deliveryConditionCode,
    deliveryDate: undefined,
  } as TechnicalPeriods;
};
</script>

<template>
  <Card class="mb-4" data-section-id="delivery-period-material" :data-section-label="props.titleName">
    <template #content>
      <TitleHeader :label="props.titleName" />
      <div class="grid grid-cols-3 mt-12">
        <Select label="เงื่อนไขระยะเวลาดำเนินการ/ส่งมอบ" v-model="value.deliveryConditionCode"
          :options="deliveryConditionDropdown" :disabled="!store.status.canEditTor || !menuStore.hasManage"
          rules="required" @update:model-value="() => clearData()" />
      </div>
      <div v-if="value.deliveryConditionCode != 'DelvCUnit005'" class="md:grid grid-cols-6 gap-2 mt-8">
        <InputNumber class="col-span-2" :label="props.label" v-model="value.period"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
        <Select v-model="value.periodTypeCode" :options="periodDropdown"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" />
      </div>
      <div v-else class="md:grid grid-cols-3 gap-2 mt-8">
        <Datepicker :disabled="!store.status.canEditTor" label="ส่งมอบภายในวันที่"
          v-model="value.deliveryDate" />
      </div>
    </template>
  </Card>
</template>

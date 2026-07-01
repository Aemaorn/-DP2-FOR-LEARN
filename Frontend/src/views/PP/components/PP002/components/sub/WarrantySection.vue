<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import type { Warranties } from '@/views/PP/models/PP002/pp002Model';
import { InputNumber, Select, Radio } from '@/components/forms';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { onMounted, ref, watch, type Ref } from 'vue';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import defaultProps from '@/helpers/defaultProps';
import SharedService from '@/services/Shared/dropdown';

const props = defineProps({
  titleName: defaultProps(''),
});

const value = defineModel<Warranties>({
  default: () => ({
    hasWarranty: false,
  } as Warranties),
  required: true,
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();
const mockRadioStockData = [
  {
    label: 'มี',
    value: true,
  },
  {
    label: 'ไม่มี',
    value: false,
  },
];

const periodTypeDropdown = ref<Array<Option>>([]);
const periodConditionDropdown = ref<Array<Option>>([]);

onMounted(async () => {
  await Promise.all([getDropdownAsync(periodTypeDropdown, EGroupCode.PeriodType), getDropdownAsync(periodConditionDropdown, EGroupCode.DWCUnit)]);
});

const getDropdownAsync = async (target: Ref<Array<Option>>, group: EGroupCode) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(group, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

watch(() => value.value.hasWarranty, (val) => {
  if (!val) {
    value.value.period = undefined;
    value.value.periodTypeCode = undefined;
    value.value.conditionOther = undefined;
  }
});
</script>

<template>
  <Card class="mb-4" data-section-id="warranty" :data-section-label="props.titleName">
    <template #content>
      <TitleHeader :label="props.titleName" />
      <div class="md:grid grid-cols-3 gap-2">
        <Radio label="การรับประกันความชำรุดบกพร่อง" v-model="value.hasWarranty" :options="mockRadioStockData"
          rules="required" :disabled="!store.status.canEditTor || !menuStore.hasManage" />
      </div>
      <div class="md:grid grid-cols-6 gap-2 mt-4" v-if="value.hasWarranty">
        <InputNumber class="col-span-2" label="ระยะเวลา" v-model="value.period" rules="required|min_value:1"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
        <Select v-model="value.periodTypeCode" :options="periodTypeDropdown" rules="required"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        <Select class="col-span-2" v-model="value.conditionOther" :options="periodConditionDropdown" rules="required"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>

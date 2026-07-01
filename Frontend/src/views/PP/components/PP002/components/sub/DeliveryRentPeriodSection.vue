<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import type { TechnicalPeriods } from '@/views/PP/models/PP002/pp002Model';
import { InputNumber, Datepicker } from '@/components/forms';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { onMounted, ref } from 'vue';
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

const dropdown = ref<Option[]>([]);

onMounted(() => {
  getDropdownAsync();
});

const getDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PeriodType, undefined, true);

  if (status === HttpStatusCode.Ok) {
    dropdown.value = data;
  }
};
</script>

<template>
  <Card class="mb-4" data-section-id="delivery-rent-period" :data-section-label="props.titleName">
    <template #content>
      <TitleHeader :label="props.titleName" />
      <div class="md:grid grid-cols-6 gap-2 mt-10">
        <InputNumber class="col-span-2" label="ผู้ได้รับการคัดเลือกจะต้องส่งมอบรถยนต์เช่า จำนวน" v-model="value.period"
          rules="required" :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
        <!-- <Select v-model="value.periodTypeCode" :options="dropdown" rules="required"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" /> -->
        <Datepicker class="col-span-2" label="ติดตั้งอุปกรณ์ประจำรถยนต์ และป้ายทะเบียน ภายใน" v-model="value.endDate"
          rules="required" :disabled="!store.status.canEditTor || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>

<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import type { PaymentTerms } from '@/views/PP/models/PP002/pp002Model';
import { InputNumber, Select } from '@/components/forms';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { onMounted, ref } from 'vue';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import defaultProps from '@/helpers/defaultProps';
import SharedService from '@/services/Shared/dropdown';
import { useMenuStore } from '@/stores/menu';

const props = defineProps({
  titleName: defaultProps(''),
  label: defaultProps(''),
});

const value = defineModel<PaymentTerms>({
  default: () => ({} as PaymentTerms),
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();

const dropdown = ref<Option[]>([]);
const periodDropdown = ref<Array<Option>>([]);

onMounted(() => {
  getDropdownAsync();
});

const getDropdownAsync = async () => {
  const [split, period] = await Promise.all([SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SplitPayment, undefined, true), SharedService.onGetParameterByGroupCodeAsync(EGroupCode.MPeriodType, undefined, true)]);

  if (split.status === HttpStatusCode.Ok) {
    dropdown.value = split.data;
  }

  if (period.status === HttpStatusCode.Ok) {
    periodDropdown.value = period.data;
  }
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader :label="props.titleName" />
      <div class="md:grid grid-cols-6 gap-2 mt-10">
        <Select class="col-span-2" label="การแบ่งจ่าย" v-model="value.proRateTypeCode" :options="dropdown"
          rules="required" :disabled="!store.status.canEditTor || !menuStore.hasManage" />
      </div>
      <div class="md:grid grid-cols-6 gap-2 mt-10">
        <InputNumber class="col-span-2" label="ชำระเงินในอัตราร้อยละ" v-model="value.paymentPercent" rules="required"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping :min-fraction-digits="2" />
        <InputNumber class="col-span-2" label="จำนวน งวด/เดือน/อื่น ๆ" v-model="value.totalPeriod" rules="required"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
        <Select v-model="value.totalPeriodTypeCode" :options="periodDropdown" rules="required"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>

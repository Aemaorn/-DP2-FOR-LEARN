<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputNumber, Select } from '@/components/forms';
import type { PP005Response } from '@/views/PP/models/PP005/pp005Model';
import { onMounted, ref, type Ref } from 'vue';
import { EGroupCode } from '@/enums/shared';
import type { Option } from '@/models/shared/option';
import SharedService from '@/services/Shared/dropdown';
import { HttpStatusCode } from 'axios';
import { useMenuStore } from '@/stores/menu';

type Props = {
  isDisabled?: boolean;
}

const props = defineProps<Props>();
const menuStore = useMenuStore();
const evaluationPeriodTypeCodeDropdown = ref<Array<Option>>([]);
const evaluationPeriodConditionCodeDropdown = ref<Array<Option>>([]);

onMounted(async () => {
  await Promise.all([
    onGetDropdownByTypeAsync(evaluationPeriodTypeCodeDropdown, EGroupCode.PeriodType),
    onGetDropdownByTypeAsync(evaluationPeriodConditionCodeDropdown, EGroupCode.BRCPType),
  ]);
});

const onGetDropdownByTypeAsync = async (target: Ref<Array<Option>>, type: EGroupCode) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(type, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const value = defineModel<PP005Response>({
  required: true,
});
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="การกำหนดระยะเวลาในการพิจารณาผลการเสนอราคา" />
      <div class="mt-10 grid grid-cols-1 lg:grid-cols-4 gap-2">
        <InputNumber label="ระยะเวลาในการพิจารณาผลการเสนอราคา" class="col-span-2" v-model="value.evaluationDueDate"
          rules="required" :disabled="props.isDisabled || !menuStore.hasManage" :min-number="1" />
        <Select :options="evaluationPeriodTypeCodeDropdown" v-model="value.evaluationPeriodTypeCode" rules="required"
          :disabled="props.isDisabled || !menuStore.hasManage" />
        <Select :options="evaluationPeriodConditionCodeDropdown" v-model="value.evaluationPeriodConditionCode"
          rules="required" :disabled="props.isDisabled || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>
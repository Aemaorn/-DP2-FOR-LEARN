<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Radio } from '@/components/forms';
import type { PP008Detail } from '@/views/PP/models/PP008/pp008model';
import { UseContractType } from '@/enums/PCM005/principleApprovalRental';
import type { Option } from '@/models/shared/option';
import { onMounted, ref, watch } from 'vue';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';

type Props = {
  disabled?: boolean;
}

const radioOptions = [
  { label: 'ใช้สัญญาส่วนกลาง', value: true },
  { label: 'ใช้สัญญาคู่ค้า', value: false },
];

const { disabled } = defineProps<Props>();

const value = defineModel<PP008Detail>({
  required: true,
});

const showSelectType = ref<boolean>(value.value.contractType !== UseContractType.Vendor);

const dropdown = ref<Array<Option>>([]);

const onGetContractDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CType);

  if (status === HttpStatusCode.Ok) {
    dropdown.value = data;
  }
};

onMounted(async (): Promise<void> => {
  await Promise.all([onGetContractDropdownAsync()]);
});

const onChangeContractDropdownAsync = async () => {
  value.value.assignees = [];
};

const onChangeRadio = (val: boolean) => {
  if (val) {
    showSelectType.value = true;
    value.value.contractType = "CType001";
  } else {
    showSelectType.value = false;
    value.value.assignees = [];
    value.value.contractType = UseContractType.Vendor;
  }
};

watch(() => value.value.contractType, (newVal) => {
  if (newVal === UseContractType.Vendor) {
    showSelectType.value = false;
  } else {
    showSelectType.value = true;
  }
});
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="เงื่อนไข การใช้สัญญา" />
      <div class="grid lg:grid-cols-2 gap-4">
        <Radio :options="radioOptions" hide-details v-model="showSelectType"
          @update:model-value="() => onChangeRadio(showSelectType)" :disabled />
        <Select v-if="showSelectType" class="col-start-1 mt-8" label="ประเภทอนุมัติใบสั่งซื้อ/จ้าง/เช่า"
          :options="dropdown" v-model="value.contractType" rules="required" :disabled
          @on-select="onChangeContractDropdownAsync" />
      </div>
    </template>
  </Card>
</template>
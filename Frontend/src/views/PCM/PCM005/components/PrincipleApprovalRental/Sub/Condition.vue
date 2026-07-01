<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Radio } from '@/components/forms';
import { UseContractType } from '@/enums/PCM005/principleApprovalRental';
import { useMenuStore } from '@/stores/menu';
import { usePcm005PrinApproveRentStore } from '@/stores/PCM/PCM005/principleApprovalRental';

const menuStore = useMenuStore();
const store = usePcm005PrinApproveRentStore();
const radioOptions = [
  { label: 'ใช้สัญญาส่วนกลาง', value: UseContractType.CentralContract },
  { label: 'ใช้สัญญาคู่ค้า', value: UseContractType.Vendor },
];
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="เงื่อนไข การใช้สัญญา" />
      <Radio :options="radioOptions" hide-details v-model="store.body.useContract"
        :disabled="!store.status.canEdit || !menuStore.hasManage" />
    </template>
  </Card>
</template>
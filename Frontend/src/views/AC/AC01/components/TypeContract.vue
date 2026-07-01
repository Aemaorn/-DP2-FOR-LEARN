<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Checkbox } from '@/components/forms';
import { ContractInfo, Disbursement } from './Part';
import { computed } from 'vue';
import { useAc01DetailStore } from '@/stores/AC/ac01';
import type { SourceDataContractGuaranteeReturn } from '@/models/ACC/acc001';

const store = useAc01DetailStore();

const sourceData = computed(() => store.body.source.data as SourceDataContractGuaranteeReturn);
</script>

<template>
  <ContractInfo />
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="รายละเอียดคืนหลักประกันสัญญา" />
      <div class="p-2 bg-gray-100 lg:flex justify-between items-center gap-2">
        <p class="flex-1/3">
          เงินสด (เงินโอน) ร้อยละ 5
        </p>

        <div class="flex-2/3">
          <InputNumber v-model="sourceData.returnAmount" rules="required" :min-fraction-digits="2" grouping disabled />
        </div>
      </div>

      <div class="p-2 lg:flex justify-between items-center gap-2">
        <div class="flex-2/3">
          <Checkbox v-model="sourceData.isDeducted" label="หักค่าปรับจากเงินหลักประกันสัญญา" disabled />

        </div>
        <div class="flex-1/3">
          <InputNumber v-model="sourceData.deductedAmount" :rules="`${sourceData.isDeducted ? 'required' : ''}`"
            disabled :min-fraction-digits="2" grouping :maxNumber="sourceData.returnAmount" />
        </div>
      </div>

      <div class="p-2 flex justify-between items-center bg-gray-100">
        <div class="flex-1" />

        <div class="flex-1">
          <p>คืนหลักประกันสัญญาคงเหลือ</p>
        </div>

        <div class="flex-1">
          <InputNumber v-model="sourceData.netReturnAmount" rules="required" :min-fraction-digits="2" grouping disabled />
        </div>
      </div>
    </template>
  </Card>
  <Disbursement v-if="store.state.isEditDisbursement"/>
</template>
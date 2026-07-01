<script setup lang="ts">
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { InputNumber } from '@/components/forms';
import { useMenuStore } from '@/stores/menu';
import type { TP003MedianPriceExpenseDescriptionInfo } from '@/views/PP/models/PP003/pp003Model';

const value = defineModel<TP003MedianPriceExpenseDescriptionInfo>({
  required: true,
});

const props = defineProps<{
  hasTravelCost: boolean,
  states: {
    isEditor: boolean;
    isCommitteeApproval: boolean;
    isCommitteeCurrentApproval: boolean;
    isBossCommitteeApproval: boolean;
    isUnitApproval: boolean;
    isCurrentUnitApproval: boolean;
    isLastUnitApproval: boolean;
    isJorPorSection: boolean;
    isJorPorAssign: boolean;
    isJorPorComment: boolean;
    isAcceptorApproval: boolean;
    isCurrentAcceptorApproval: boolean;
    isLastAcceptorApproval: boolean;
    isMangeMd: boolean;
    currentTemplate: boolean;

  },
}>();

const menuStore = useMenuStore();
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="รายละเอียดค่าใช้จ่าย" />
      <div class="md:grid grid-cols-3 gap-2 mt-10">
        <InputNumber label="ค่าวัสดุอุปกรณ์ (ถ้ามี)" v-model="value.materialCost" :min-fraction-digits="2" grouping
          :disabled="!states.isEditor || !menuStore.hasManage" />
      </div>
      <div class="md:grid grid-cols-3 gap-2 mt-6" v-if="props.hasTravelCost">
        <InputNumber label="ค่าใช้จ่ายในการเดินทางไปต่างประเทศ (ถ้ามี)" v-model="value.overseasTravelCost"
          :min-fraction-digits="2" grouping :disabled="!states.isEditor || !menuStore.hasManage" />
      </div>
      <div class="md:grid grid-cols-3 gap-2 mt-6">
        <InputNumber label="ค่าใช้จ่ายอื่นๆ (ถ้ามี)" v-model="value.otherExpenses" :min-fraction-digits="2" grouping
          :disabled="!states.isEditor || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>

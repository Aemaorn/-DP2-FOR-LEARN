<script setup lang="ts">
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { InputNumber } from '@/components/forms';
import { useMenuStore } from '@/stores/menu';
import type { TP003MedianPriceExpenseDescriptionInfo } from '@/views/PP/models/PP003/pp003Model';

const value = defineModel<TP003MedianPriceExpenseDescriptionInfo>({
  required: true,
});

defineProps<{
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
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mt-10">
        <InputNumber label="ค่า Hardware" v-model="value.hardwareCost" :min-fraction-digits="2" grouping
          :disabled="!states.isEditor || !menuStore.hasManage" />
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mt-4">
        <InputNumber label="ค่า Software" v-model="value.softwareCost" type="number" :min-fraction-digits="2" grouping
          :disabled="!states.isEditor || !menuStore.hasManage" />
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mt-4">
        <InputNumber label="ค่าพัฒนาระบบ" v-model="value.systemDevelopmentCost" type="number" :min-fraction-digits="2"
          grouping :disabled="!states.isEditor || !menuStore.hasManage" />
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mt-4">
        <InputNumber label="ค่าใช้จ่ายอื่น ๆ (ถ้ามี)" v-model="value.otherExpenses" type="number" :min-fraction-digits="2"
          grouping :disabled="!states.isEditor || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>

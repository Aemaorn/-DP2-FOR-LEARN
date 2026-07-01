<script setup lang="ts">
import type { TPP003Body } from '@/views/PP/models/PP003/pp003Model';
import {
  PriceSourceSection,
  QualificationSection,
  ConstructionContSection,
} from './sub/template02';

const value = defineModel<TPP003Body>({
  required: true,
});

const props = defineProps<{
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
</script>

<template>
  <PriceSourceSection v-model="value.budgetAllocations" detailType="refBudget" :states="props.states" />
  <QualificationSection v-if="value.staff" v-model="value.staff" :states="props.states" />
  <ConstructionContSection v-if="value.expenseDescription" v-model="value.expenseDescription" :hasTravelCost="false"
    :states="props.states" />
</template>

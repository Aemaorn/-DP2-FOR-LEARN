<script setup lang="ts">
import type { JorPor04Request } from '@/views/PP/models/PP004/pp004Model';
import { CriteriaConditionSection, ConsiderInformation, ScopeOfWorkList, BudgetProcurement } from '../../PP';
import { usePPDetailStore } from '@/stores/PP/ppStore';
import { computed, watch } from 'vue';
import { checkIsSixty } from '@/helpers/supplyMethod';
import EgpInfoSection from './sub/EgpInfoSection.vue';
import type { PP005Response } from '@/views/PP/models/PP005/pp005Model';
import CommitteeDutySection from './sub/CommitteeDutySection.vue';
import PeriodConsideringPriceSeciton from './sub/PeriodConsideringPriceSeciton.vue';
import { usePP005DetailStore } from '@/views/PP/stores/PP005/PP005Store';
import ProcurementSuppliesDivision from '../../PP/ProcurementSuppliesDivision.vue';

type Props = {
  isOverPrice: boolean;
  item: JorPor04Request;
  isDisabled?: boolean;
  pJp005Number?: string;
};

const props = defineProps<Props>();
const value = defineModel<PP005Response>({
  required: true,
});
const documentDate = defineModel<Date | undefined>('documentDate');

const procurementStore = usePPDetailStore();
const isSixtyOverPrice = computed(() => checkIsSixty(procurementStore.procurementDetail.supplyMethodCode!) && procurementStore.procurementDetail.budget > 100000);
const store = usePP005DetailStore();

watch(
  (): boolean => value.value.procurementCommittees.isCommittee,
  (isCommittee: boolean): void => {
    if (isCommittee) {
      value.value.procurementSuppliesDivision = [];
    }
  }
);
</script>

<template>
  <ConsiderInformation title="จัดทำรายงานขอซื้อขอจ้าง (จพ.005)" :isOverPrice="props.isOverPrice"
    :model-value="props.item.requisition" :is-disabled="props.isDisabled" :number="props.pJp005Number" v-model:document-date="documentDate" />
  <CriteriaConditionSection :model-value="props.item.requisition.evaluationCriteriaCode" isDisabled />
  <BudgetProcurement :model-value="props.item.budgets" disabled :template-code="store.body.torTemplateCode" />
  <ScopeOfWorkList v-if="props.item.scopeOfWorks.length > 0" :model-value="props.item.scopeOfWorks" is-disabled />
  <EgpInfoSection v-if="isSixtyOverPrice" v-model="value.egpProjectNumber" :is-disabled="props.isDisabled" />
  <PeriodConsideringPriceSeciton v-model="value" :is-disabled="props.isDisabled" />
  <ProcurementSuppliesDivision v-if="!value.procurementCommittees.isCommittee"
    v-model="value.procurementSuppliesDivision" label="ผู้จัดทำ" :is-disabled="props.isDisabled" class="mb-4" />
  <CommitteeDutySection label="ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง" v-model="value.procurementCommittees"
    :is-disabled="props.isDisabled" v-model:spacial-option="store.positionProcOptions" person="ผู้จัดซื้อจัดจ้าง" />
  <CommitteeDutySection label="ผู้ตรวจรับ/คณะกรรมการตรวจรับพัสดุ" v-model="value.inspectionCommittees"
    person="ผู้ตรวจรับพัสดุ" :is-disabled="props.isDisabled" v-model:spacial-option="store.positionInspOptions" />
  <CommitteeDutySection label="คณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)"
    v-model="value.maintenanceInspectionCommittee" v-model:is-has="value.isHasMaintenanceInspectionCommittee"
    :is-disabled="props.isDisabled" v-model:spacial-option="store.positionMaOptions"
    person="ผู้ตรวจรับพัสดุงานจ้างบริการบำรุงรักษา" onShowOption />
  <CommitteeDutySection label="ผู้ควบคุมงาน (เฉพาะงานก่อสร้าง)" v-model="value.constructionSupervisor"
    person="ผู้ควบคุมงาน" :is-disabled="props.isDisabled" v-model:is-has="value.isConstructionSupervisor"
    v-model:spacial-option="store.positionSupOptions" onShowOption />
</template>
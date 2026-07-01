<script setup lang="ts">
import type { FineRates, PaymentTerms, PaymentTermsDetail, PP002Detail, TechnicalPeriods, TorCorrectiveMaintenanceModel, TorPreventiveMaintenanceModel, TorTrainingModel, Warranties } from '@/views/PP/models/PP002/pp002Model';
import {
  SourceAndReasonsSection,
  ObjectiveSection,
  QualificationSection,
  SelectionCriteriaSection,
  ParcelSection,
  DeliveryPeriodMaterialSection,
  BudgetProcurement,
  PhaseListSection,
  FineSection,
  WarrantySection,
} from '@/views/PP/components/PP002/components/sub/template05';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { onMounted } from 'vue';
import { PeriodTypeCodeEnum, ProRateTypeCodeEnum } from '@/enums/shared';
import DocumentDescriptionSection from './sub/DocumentDescriptionSection.vue';
import PreventiveMaintenanceSection from './sub/PreventiveMaintenanceSection.vue';
import CorrectiveMaintenanceSection from './sub/CorrectiveMaintenanceSection.vue';
import TrainingSection from './sub/TrainingSection.vue';
import ManuelDescriptionSection from './sub/ManuelDescriptionSection.vue';
import ImpedimentSection from './sub/ImpedimentSection.vue';
import { usePPDetailStore } from '@/stores/PP/ppStore';
import { SupplyMethodTypeConstant } from '@/enums/preProcurement';

const value = defineModel<PP002Detail>({
  required: true,
});

const store = usePP002DetailStore();
const procurementStore = usePPDetailStore();

onMounted(() => {
  onInitTemplate();
});

const onInitTemplate = (): void => {
  value.value = {
    ...value.value,
    technicalPeriods: value.value.technicalPeriods?.length === 0 ? [{ periodTypeCode: PeriodTypeCodeEnum.Day } as TechnicalPeriods] : value.value.technicalPeriods,
    paymentTerms: !value.value.paymentTerms || value.value.paymentTerms.length === 0 ? [{ proRateTypeCode: ProRateTypeCodeEnum.SplitPayment001, isMA: false, totalPeriodTypeCode: PeriodTypeCodeEnum.PeriodType004, details: [{ termNumber: 1 } as PaymentTermsDetail] } as PaymentTerms] : value.value.paymentTerms,
    warranties: value.value.warranties?.length === 0 ? [{} as Warranties] : value.value.warranties,
    preventiveMaintenance: value.value.preventiveMaintenance ?? {} as TorPreventiveMaintenanceModel,
    correctiveMaintenance: value.value.correctiveMaintenance ?? {} as TorCorrectiveMaintenanceModel,
    training: value.value.training ?? {} as TorTrainingModel,
    fineRates: value.value.fineRates?.length === 0 ? [{ sequence: 1, rate: value.value.supplyMethodTypeCode === SupplyMethodTypeConstant.Hire ? 0.1 : 0.2, } as FineRates] : value.value.fineRates,
  };
};
</script>

<template>
  <SourceAndReasonsSection v-model="value.reason" />
  <ObjectiveSection v-model="value.objects" />
  <QualificationSection label="คุณสมบัติของผู้ยื่นข้อเสนอ" v-model="value.qualifications" />
  <DocumentDescriptionSection v-model="value.documentDescription"
    v-if="procurementStore.procurementDetail.budget > 500000" />

  <SelectionCriteriaSection v-model="value.evaluationCriteria" title-name="หลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ" />
  <ParcelSection v-model="value.technicalSpecifications" title-name="รายละเอียดของงาน" />
  <DeliveryPeriodMaterialSection v-model="value.technicalPeriods[0]" title-name="ระยะเวลาดำเนินการ/ส่งมอบงาน"
    label="ระยะเวลาดำเนินการ/ส่งมอบงาน  ภายใน" v-if="value.technicalPeriods" />
  <BudgetProcurement v-model="value.budgets" title-name="วงเงินที่จะจัดซื้อจัดจ้าง"
    :disabled="!store.status.canEditTor" />
  <PhaseListSection v-model="value.paymentTerms" title-name="งวดงานและการจ่ายเงิน" :show-payment-term-ma="false"
    :show-period-type="true" v-if="value.paymentTerms" />
  <FineSection v-model="value.fineRates" title-name="อัตราค่าปรับ" v-if="value.fineRates"
    :supply-method-type-code="value.supplyMethodTypeCode" />
  <WarrantySection v-model="value.warranties[0]" title-name="การรับประกันความชำรุดบกพร่อง และการบำรุงรักษา"
    v-if="value.warranties" />
  <template v-if="procurementStore.procurementDetail.budget > 500000">
    <PreventiveMaintenanceSection v-model="value.preventiveMaintenance" v-model:isPM="value.isPM" />
    <CorrectiveMaintenanceSection v-model="value.correctiveMaintenance" v-model:isCM="value.isCM" />
    <TrainingSection v-model="value.training" v-model:is-training="value.isTraining" />
    <ManuelDescriptionSection v-model="value.manuelDescription" />
    <ImpedimentSection v-model="value.impediments" v-model:is-impediment="value.isImpediment" />
  </template>
</template>

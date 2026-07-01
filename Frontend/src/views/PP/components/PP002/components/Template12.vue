<script setup lang="ts">
import type { FineRates, PaymentTerms, PaymentTermsDetail, PP002Detail, TechnicalPeriods } from '@/views/PP/models/PP002/pp002Model';
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
} from '@/views/PP/components/PP002/components/sub/template12';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { onMounted } from 'vue';
import { PeriodTypeCodeEnum, ProRateTypeCodeEnum } from '@/enums/shared';
import { SupplyMethodTypeConstant } from '@/enums/preProcurement';

const value = defineModel<PP002Detail>({
  required: true,
});

const store = usePP002DetailStore();

onMounted(() => {
  onInitTemplate();
});

const onInitTemplate = (): void => {
  value.value = {
    ...value.value,
    technicalPeriods: value.value.technicalPeriods?.length === 0 ? [{ periodTypeCode: PeriodTypeCodeEnum.Day } as TechnicalPeriods] : value.value.technicalPeriods,
    paymentTerms: !value.value.paymentTerms || value.value.paymentTerms.length === 0 ? [{ proRateTypeCode: ProRateTypeCodeEnum.SplitPayment001, isMA: false, totalPeriodTypeCode: PeriodTypeCodeEnum.PeriodType004, details: [{ termNumber: 1 } as PaymentTermsDetail] } as PaymentTerms] : value.value.paymentTerms,
    fineRates: value.value.fineRates?.length === 0 ? [{ sequence: 1, rate: value.value.supplyMethodTypeCode === SupplyMethodTypeConstant.Hire ? 0.1 : 0.2, } as FineRates] : value.value.fineRates,
  };
};
</script>

<template>
  <SourceAndReasonsSection v-model="value.reason" />
  <ObjectiveSection v-model="value.objects" />
  <QualificationSection label="คุณสมบัติของผู้เสนอราคา" v-model="value.qualifications" />
  <SelectionCriteriaSection v-model="value.evaluationCriteria" title-name="หลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ" />
  <ParcelSection v-model="value.technicalSpecifications" title-name="คุณลักษณะเฉพาะของวงจรสื่อสารข้อมูล" />
  <DeliveryPeriodMaterialSection v-model="value.technicalPeriods[0]" title-name="ระยะเวลาเช่า" label="ระยะเวลาการเช่า"
    v-if="value.technicalPeriods" />
  <BudgetProcurement v-model="value.budgets" title-name="วงเงินในการจัดหา" :disabled="!store.status.canEditTor" />
  <PhaseListSection v-model="value.paymentTerms" title-name="งวดงานและการจ่ายเงิน" v-if="value.paymentTerms"
    :show-payment-term-ma="false" />
  <FineSection v-model="value.fineRates" title-name="อัตราค่าปรับ" v-if="value.fineRates"
    :supply-method-type-code="value.supplyMethodTypeCode" />
</template>

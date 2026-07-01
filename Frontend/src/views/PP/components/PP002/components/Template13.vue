<script setup lang="ts">
import { SupplyMethodTypeConstant } from '@/enums/preProcurement';
import { PeriodTypeCodeEnum, ProRateTypeCodeEnum } from '@/enums/shared';
import {
  BudgetProcurement,
  DeliveryPeriodSection,
  FineSection,
  ObjectiveSection,
  QualificationSection,
  SelectionCriteriaSection,
  SourceAndReasonsSection,
  PhaseListSection,
} from '@/views/PP/components/PP002/components/sub/template13';
import type { FineRates, PaymentTerms, PaymentTermsDetail, PP002Detail, TechnicalPeriods } from '@/views/PP/models/PP002/pp002Model';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { onMounted } from 'vue';
import ParcelSection from './sub/ParcelSection.vue';

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
    paymentTerms: !value.value.paymentTerms || value.value.paymentTerms.length === 0 ? [{ proRateTypeCode: ProRateTypeCodeEnum.SplitPayment002, totalPeriodTypeCode: PeriodTypeCodeEnum.PeriodType004, isMA: false, details: [{ termNumber: 1 } as PaymentTermsDetail] } as PaymentTerms] : value.value.paymentTerms,
    technicalPeriods: value.value.technicalPeriods?.length === 0 ? [{ periodTypeCode: PeriodTypeCodeEnum.Day } as TechnicalPeriods] : value.value.technicalPeriods,
    fineRates: value.value.fineRates?.length === 0 ? [{ sequence: 1, rate: value.value.supplyMethodTypeCode === SupplyMethodTypeConstant.Hire ? 0.1 : 0.2, } as FineRates] : value.value.fineRates,
  };
};
</script>

<template>
  <SourceAndReasonsSection v-model="value.reason" />
  <ObjectiveSection v-model="value.objects" />
  <QualificationSection label="คุณสมบัติของผู้ประสงค์จะเสนอราคา" v-model="value.qualifications" />
  <DeliveryPeriodSection v-model="value.technicalPeriods[0]" title-name="ระยะเวลาการจ้าง"
    v-if="value.technicalPeriods" />
  <BudgetProcurement v-model="value.budgets" title-name="วงเงินที่จะจัดซื้อจัดจ้าง"
    :disabled="!store.status.canEditTor" />
  <PhaseListSection v-model="value.paymentTerms" title-name="งวดงานและการจ่ายเงิน" :show-period-type="true"
    :show-payment-term-ma="false" v-if="value.paymentTerms" />
  <SelectionCriteriaSection v-model="value.evaluationCriteria" title-name="หลักเกณฑ์และสิทธิในการพิจารณา" />
  <ParcelSection v-model="value.technicalSpecifications" title-name="คุณลักษณะงาน (ขอบเขตงาน)" />
  <FineSection v-model="value.fineRates" title-name="อัตราค่าปรับ" v-if="value.fineRates"
    :supply-method-type-code="value.supplyMethodTypeCode" />
</template>

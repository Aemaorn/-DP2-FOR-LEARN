<script setup lang="ts">
import type { FineRates, PaymentTerms, PaymentTermsDetail, PP002Detail, TechnicalPeriods, Warranties } from '@/views/PP/models/PP002/pp002Model';
import {
  SourceAndReasonsSection,
  ObjectiveSection,
  QualificationSection,
  DeliveryPeriodMaterialSection,
  BudgetProcurement,
  PhaseListSection,
  FineSection,
  SelectionCriteriaSection,
} from '@/views/PP/components/PP002/components/sub/template02';
import { onMounted } from 'vue';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
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
    paymentTerms: !value.value.paymentTerms || value.value.paymentTerms.length === 0 ? [{ proRateTypeCode: ProRateTypeCodeEnum.SplitPayment002, totalPeriodTypeCode: PeriodTypeCodeEnum.PeriodType004, isMA: false, details: [{ termNumber: 1 } as PaymentTermsDetail] } as PaymentTerms] : value.value.paymentTerms,
    warranties: value.value.warranties?.length === 0 ? [{} as Warranties] : value.value.warranties,
    fineRates: value.value.fineRates?.length === 0 ? [{ sequence: 1, rate: value.value.supplyMethodTypeCode === SupplyMethodTypeConstant.Hire ? 0.1 : 0.2, } as FineRates] : value.value.fineRates,
  };
};
</script>

<template>
  <SourceAndReasonsSection v-model="value.reason" />
  <ObjectiveSection v-model="value.objects" />
  <QualificationSection label="คุณสมบัติผู้เสนอราคา" v-model="value.qualifications" />
  <DeliveryPeriodMaterialSection v-model="value.technicalPeriods[0]" title-name="ระยะเวลาดำเนินการและการจ่ายเงิน"
    label="ระยะเวลาดำเนินการ/ส่งมอบงาน ภายใน" v-if="value.technicalPeriods" />
  <PhaseListSection v-model="value.paymentTerms" title-name="งวดงานและการจ่ายเงิน" :show-period-type="true"
    :show-payment-term-ma="false" v-if="value.paymentTerms" />
  <BudgetProcurement v-model="value.budgets" title-name="วงเงินในการจัดจ้าง" :disabled="!store.status.canEditTor" />
  <SelectionCriteriaSection v-model="value.evaluationCriteria" title-name="หลักเกณการพิจารณาคัดเลือกข้อเสนอ" />
  <ParcelSection v-model="value.technicalSpecifications" title-name="คุณลักษณะงาน (ขอบเขตงาน)" />
  <FineSection v-model="value.fineRates" title-name="อัตราค่าปรับ" v-if="value.fineRates"
    :supply-method-type-code="value.supplyMethodTypeCode" />
</template>

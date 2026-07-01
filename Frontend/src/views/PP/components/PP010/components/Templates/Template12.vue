<script setup lang="ts">
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { defineAsyncComponent } from 'vue';

type Props = {
  disable?: boolean
}

const props = defineProps<Props>()

const value = defineModel<TContractDraftBody>({
  required: true,
});

const SelesAgreement = defineAsyncComponent(
  (): Promise<typeof import('../Sub/SalesAgreement.vue')> => import('../Sub/SalesAgreement.vue')
);
const PartOfContract = defineAsyncComponent(
  (): Promise<typeof import('../Sub/PartOfContract.vue')> => import('../Sub/PartOfContract.vue')
);
const Payment = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Payment.vue')> => import('../Sub/Payment.vue')
);
const ContractPerformance = defineAsyncComponent(
  (): Promise<typeof import('../Sub/ContractPerformance.vue')> => import('../Sub/ContractPerformance.vue')
);
const Mulct = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Mulct.vue')> => import('../Sub/Mulct.vue')
);
const RetentionPayment = defineAsyncComponent(
  (): Promise<typeof import('../Sub/RetentionPayment.vue')> => import('../Sub/RetentionPayment.vue')
);
const AdvancePayment = defineAsyncComponent(
  (): Promise<typeof import('../Sub/AdvancePayment.vue')> => import('../Sub/AdvancePayment.vue')
);

const store = useContractDraftStore();
</script>

<template>
  <div class="grid grid-rows-1 gap-5">
    <SelesAgreement :body="value" :disable="props.disable" label="สัญญาข้อ 1 ข้อตกลง" />
     <PartOfContract :dropdown="store.dropdown.attacementTypeOptions" :body="value" :disable="props.disable"
      label="สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา" />
    <Payment :body="value" :disable="props.disable" label="สัญญาข้อ 3 ค่าจ้างและการจ่ายเงิน" />
    <AdvancePayment :body="value" :disable="props.disable" label="สัญญาข้อ 4 เงินค่าจ้างล่วงหน้า" />
    <Mulct :body="value" :disable="props.disable" label="สัญญาข้อ 10 ค่าปรับ" />
    <RetentionPayment :body="value" :disable="props.disable" label="สัญญาข้อ 12 (ก) เงินประกันผลงาน" />
    <ContractPerformance :body="value" :disable="props.disable" label="สัญญาข้อ 12 (ข) หลักประกันการปฏิบัติตามสัญญา" />
  </div>
</template>
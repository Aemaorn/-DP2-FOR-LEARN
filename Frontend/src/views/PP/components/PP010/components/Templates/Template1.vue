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
const Delivery = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Delivery.vue')> => import('../Sub/Delivery.vue')
);
const Payment = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Payment.vue')> => import('../Sub/Payment.vue')
);
const Warranty = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Warranty.vue')> => import('../Sub/Warranty.vue')
);
const ContractPerformance = defineAsyncComponent(
  (): Promise<typeof import('../Sub/ContractPerformance.vue')> => import('../Sub/ContractPerformance.vue')
);
const Mulct = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Mulct.vue')> => import('../Sub/Mulct.vue')
);

const store = useContractDraftStore();
</script>

<template>
  <div class="grid grid-rows-1 gap-5">
    <SelesAgreement :body="value" :disable="props.disable" label="สัญญาข้อ 1 ข้อตกลง" />
    <PartOfContract :dropdown="store.dropdown.attacementTypeOptions" :body="value" :disable="props.disable"
      label="สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา" />
    <Delivery :body="value" :disable="props.disable" label="สัญญาข้อ 4 การส่งมอบ" />
    <Payment :body="value" :disable="props.disable" label="สัญญาข้อ 6 การชำระเงิน" />
    <Warranty :body="value" :disable="props.disable" label="สัญญาข้อ 7 การรับประกันความชำรุดบกพร่อง" />
    <ContractPerformance :disable="props.disable" :body="value" label="สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา" />
    <Mulct :body="value" :disable="props.disable" label="สัญญาข้อ 10 ค่าปรับ" />
  </div>
</template>
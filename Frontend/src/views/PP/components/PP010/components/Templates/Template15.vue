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
const ContractPerformance = defineAsyncComponent(
  (): Promise<typeof import('../Sub/ContractPerformance.vue')> => import('../Sub/ContractPerformance.vue')
);
const Mulct = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Mulct.vue')> => import('../Sub/Mulct.vue')
);
const Payment = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Payment.vue')> => import('../Sub/Payment.vue')
);
const Redelivery = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Redelivery.vue')> => import('../Sub/Redelivery.vue')
)
const CarLeaseInfo = defineAsyncComponent(
  (): Promise<typeof import('../Sub/CarLeaseInfo.vue')> => import('../Sub/CarLeaseInfo.vue')
)

const store = useContractDraftStore();
</script>

<template>
  <div class="grid grid-rows-1 gap-5">
    <SelesAgreement :body="value" :disable="props.disable" label="สัญญาข้อ 1 ข้อตกลง" />
    <CarLeaseInfo :body="value" :disable="props.disable" label="สัญญาข้อ 2 ค่าเช่ารถยนต์" />
    <PartOfContract :dropdown="store.dropdown.attacementTypeOptions" :body="value" :disable="props.disable"
      label="สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา" />
    <Payment :body="value" :disable="props.disable" label="สัญญาข้อ 4 การจ่ายเงิน" />
    <Redelivery :body="value" :disable="props.disable" label="สัญญาข้อ 5 การตรวจรับ" />
    <Mulct :body="value" :disable="props.disable" label="สัญญาข้อ 9 ค่าปรับกรณีส่งมอบล่าช้า" />
    <ContractPerformance :body="value" :disable="props.disable" label="สัญญาข้อ 13 หลักประกันการปฏิบัติตามสัญญา" />
  </div>
</template>
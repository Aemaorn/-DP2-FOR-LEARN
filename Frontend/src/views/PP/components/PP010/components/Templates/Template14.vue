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
const ContractPerformance = defineAsyncComponent(
  (): Promise<typeof import('../Sub/ContractPerformance.vue')> => import('../Sub/ContractPerformance.vue')
);
const Mulct = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Mulct.vue')> => import('../Sub/Mulct.vue')
);
const ComputerLeaseInfo = defineAsyncComponent(
  (): Promise<typeof import('../Sub/ComputerLeaseInfo.vue')> => import('../Sub/ComputerLeaseInfo.vue')
);

const store = useContractDraftStore();
</script>

<template>
  <div class="grid grid-rows-1 gap-5">
    <SelesAgreement :body="value" :disable="props.disable" label="สัญญาข้อ 2 ข้อตกลง" />
    <PartOfContract :dropdown="store.dropdown.attacementTypeOptions" :body="value" :disable="props.disable"
      label="สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา" />
    <ComputerLeaseInfo :body="value" :disable="props.disable" label="สัญญาข้อ 4 ระยะเวลาการเช่า" />
    <Delivery :body="value" :disable="props.disable" label="สัญญาข้อ 7 การส่งมอบและติดตั้ง" />
    <ContractPerformance :body="value" :disable="props.disable" label="สัญญาข้อ 14 หลักประกันการปฏิบัติตามสัญญา" />
    <Mulct :body="value" :disable="props.disable" label="สัญญาข้อ 19 ค่าปรับ" />
  </div>
</template>
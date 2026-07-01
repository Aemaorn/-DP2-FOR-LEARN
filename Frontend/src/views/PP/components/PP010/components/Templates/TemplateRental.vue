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

const SalesAgreement = defineAsyncComponent(
  (): Promise<typeof import('../Sub/SalesAgreement.vue')> => import('../Sub/SalesAgreement.vue')
);
const Period = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Period.vue')> => import('../Sub/Period.vue')
);
const RentalFee = defineAsyncComponent(
  (): Promise<typeof import('../Sub/RentalFee.vue')> => import('../Sub/RentalFee.vue')
);
const PartOfContract = defineAsyncComponent(
  (): Promise<typeof import('../Sub/PartOfContract.vue')> => import('../Sub/PartOfContract.vue')
);
const ContractPerformance = defineAsyncComponent(
  (): Promise<typeof import('../Sub/ContractPerformance.vue')> => import('../Sub/ContractPerformance.vue')
);

const store = useContractDraftStore();
</script>

<template>
  <div class="grid grid-rows-1 gap-5">
    <SalesAgreement :body="value" :disable="props.disable" label="สัญญาข้อ 1 ข้อตกลงซื้อขาย" />
    <Period label="สัญญาข้อ 4 ระยะเวลาเช่า" :body="value" :disable="props.disable" />
    <RentalFee label="สัญญาข้อ 5 ค่าเช่า" :body="value" :disable="props.disable" />
    <PartOfContract :dropdown="store.dropdown.attacementTypeOptions" :body="value" :disable="props.disable"
      label="สัญญาข้อ 10 เอกสารอันเป็นส่วนหนึ่งของสัญญา" />
    <ContractPerformance :disable="props.disable" :body="value" label="สัญญาข้อ 16 เงินประกันการเช่า" />
  </div>
</template>

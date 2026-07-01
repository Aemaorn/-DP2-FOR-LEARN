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
const CopierleaseInfo = defineAsyncComponent(
  (): Promise<typeof import('../Sub/CopierLeaseInfo.vue')> => import('../Sub/CopierLeaseInfo.vue')
);
const Warranty = defineAsyncComponent(
  (): Promise<typeof import('../Sub/Warranty.vue')> => import('../Sub/Warranty.vue')
);

const store = useContractDraftStore();
</script>

<template>
  <div class="grid grid-rows-1 gap-5">
    <SelesAgreement :body="value" :disable="props.disable" label="สัญญาข้อ 1 ข้อตกลง" />
    <CopierleaseInfo :body="value" :disable="props.disable" label="สัญญาข้อ 2 ค่าเช่าเครื่องถ่ายเอกสาร" />
    <PartOfContract :dropdown="store.dropdown.attacementTypeOptions" :body="value" :disable="props.disable"
      label="สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา" />
    <Warranty v-model:body="value" :disable="props.disable" label="สัญญาข้อ 7 การบำรุงรักษา" />
    <Delivery :body="value" :disable="props.disable" label="สัญญาข้อ 4 การส่งมอบ" />
    <ContractPerformance :body="value" :disable="props.disable" label="สัญญาข้อ 10 หลักประกันการปฏิบัติตามสัญญา" />
    <Mulct :body="value" :disable="props.disable" label="สัญญาข้อ 12 ค่าปรับกรณีส่งมอบล่าช้า" />
  </div>
</template>
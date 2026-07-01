<script setup lang="ts">
import { useMenuStore } from '@/stores/menu';
import { usePcmContractDraftStore } from '@/stores/PCM/PCM005/pcmContractDraft';
import { defineAsyncComponent } from 'vue';

const SellerInfo = defineAsyncComponent(() => import('@/views/PP/components/PP010/components/Sub/SellerInfo.vue'));
const SalesAgreement = defineAsyncComponent(() => import('@/views/PP/components/PP010/components/Sub/SalesAgreement.vue'));
const PartOfContract = defineAsyncComponent(() => import('@/views/PP/components/PP010/components/Sub/PartOfContract.vue'));
const ContractPerformance = defineAsyncComponent(() => import('@/views/PP/components/PP010/components/Sub/ContractPerformance.vue'));
const Entrepremrur = defineAsyncComponent(() => import('@/views/PP/components/PP010/components/Sub/Entrepremrur.vue'));
const Period = defineAsyncComponent(() => import('@/views/PP/components/PP010/components/Sub/Period.vue'));
const RentalFee = defineAsyncComponent(() => import('@/views/PP/components/PP010/components/Sub/RentalFee.vue'));
const ContractAcceptorSign = defineAsyncComponent(() => import('@/views/PP/components/PP010/components/Sub/ContractAcceptorSign.vue'));

const menuStore = useMenuStore();
const store = usePcmContractDraftStore();
</script>

<template>
  <Entrepremrur :body="store.body" :disable="!store.states.canEdit || !menuStore.hasManage" />
  <!-- <BuyerInfo label="ข้อมูลผู้เช่า" :body="store.body" :disable="!menuStore.hasManage || !store.states.canEdit" /> -->
  <ContractAcceptorSign v-model="store.body.acceptors" :is-disabled="!store.states.canEdit || !menuStore.hasManage"
    label="ลงนามโดย" @set-default-signer="store.api.onGetDefaultAcceptorAsync" />
  <SellerInfo :body="store.body" :disable="!menuStore.hasManage || !store.states.canEdit" />
  <SalesAgreement label="สัญญาข้อ 1 ข้อตกลง" :body="store.body"
    :disable="!menuStore.hasManage || !store.states.canEdit" />
  <Period label="สัญญาข้อ 4 ระยะเวลาเช่า" :body="store.body" :disable="!menuStore.hasManage || !store.states.canEdit" />
  <RentalFee label="สัญญาข้อ 5 ค่าเช่า" :body="store.body" :disable="!menuStore.hasManage || !store.states.canEdit" />
  <PartOfContract :dropdown="store.dropdown.cmRentalTypeAttacement" label="สัญญาข้อ 10 เอกสารอันเป็นส่วนหนึ่งของสัญญา"
    :body="store.body" :disable="!menuStore.hasManage || !store.states.canEdit" />
  <ContractPerformance label="สัญญาข้อ 16 เงินประกันการเช่า" :body="store.body"
    :disable="!menuStore.hasManage || !store.states.canEdit" />
</template>
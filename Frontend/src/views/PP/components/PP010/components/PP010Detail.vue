<script setup lang="ts">
import { useMenuStore } from '@/stores/menu';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { defineAsyncComponent } from 'vue';

const SellerInfo = defineAsyncComponent(() => import('./Sub/SellerInfo.vue'));
const Entrepremrur = defineAsyncComponent(() => import('./Sub/Entrepremrur.vue'));
const ContractAcceptorSign = defineAsyncComponent(() => import('./Sub/ContractAcceptorSign.vue'));

const props = defineProps({
  readonly: { type: Boolean, default: false },
});

const store = useContractDraftStore();
const menuStore = useMenuStore();
</script>

<template>
  <Entrepremrur :body="store.body" :disable="!store.states.canEdit || !menuStore.hasManage || props.readonly" />
  <!-- <BuyerInfo :body="store.body" :disable="!store.states.canEdit || !menuStore.hasManage || props.readonly" /> -->
  <ContractAcceptorSign v-model="store.body.acceptors" :is-disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly"
    label="ลงนามโดย" @set-default-signer="store.api.onGetDefaultAcceptorAsync" />
  <SellerInfo :body="store.body" :disable="!store.states.canEdit || !menuStore.hasManage || props.readonly" />
  <component :is="store.currentTemplate" v-model="store.body" :key="store.body.template"
    :disable="!store.states.canEdit || !menuStore.hasManage || props.readonly" />
</template>
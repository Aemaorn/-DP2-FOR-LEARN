<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Checkbox, InputField, Select, InputArea } from '@/components/forms';
import { useAc01DetailStore } from '@/stores/AC/ac01';
import { useMenuStore } from '@/stores/menu';

const menuStore = useMenuStore();
const store = useAc01DetailStore();
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="รายละเอียดการโอนเข้าบัญชีผู้สำรองจ่าย" />
      <div class="px-4 grid grid-cols-1 lg:grid-cols-4 gap-4 items-baseline">
        <Checkbox label="สำรองจ่าย" v-model="store.body.isAdvance" @update:model-value="() => {
          store.fn.onClearPaymentDetail()
        }" :disabled="!store.state.canEdit || !menuStore.hasManage" />
        <InputField v-if="store.body.isAdvance" class="col-start-1" label="ผู้สำรองจ่าย"
          v-model="store.body.advanceBankAccountName" rules="required"
          :disabled="!store.state.canEdit || !menuStore.hasManage" />
        <Select v-if="store.body.isAdvance" label="ช่องทางการชำระเงิน" v-model="store.body.advancePaymentMethodCode"
          :options="store.paymentMethodDropDown" rules="required"
          :disabled="!store.state.canEdit || !menuStore.hasManage" />
        <Select class="col-start-1" v-if="store.body.isAdvance" label="ธนาคาร" :options="store.bankDropdown"
          v-model="store.body.advanceBankCode" rules="required"
          :disabled="!store.state.canEdit || !menuStore.hasManage" />
        <InputField v-if="store.body.isAdvance" label="เลขที่บัญชี" v-model="store.body.advanceBankAccount"
          rules="required" :disabled="!store.state.canEdit || !menuStore.hasManage" />
        <InputFiel v-if="store.body.isAdvance" d label="สาขา (ถ้ามี)" v-model="store.body.advanceBankBranch"
          :disabled="!store.state.canEdit || !menuStore.hasManage" />
        <InputArea v-if="store.body.isAdvance" label="หมายเหตุ" v-model="store.body.advanceDetail"
          class="col-span-1 lg:col-span-4" :disabled="!store.state.canEdit || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>
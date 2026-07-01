<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
  InputArea,
  InputNumber,
  Datepicker,
  Select,
  Checkbox,
} from '@/components/forms';
import type { ContractDraftTemplate } from '@/enums/contractDraftt';
import { ConfirmDialogType } from '@/enums/dialog';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { usePcmContractDraftStore } from '@/stores/PCM/PCM005/pcmContractDraft';
import { TContractDraftStatus } from '@/views/PP/enums/pp010';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { onMounted, ref, watch } from 'vue';

const store = usePcmContractDraftStore();
const ppContractStore = useContractDraftStore();

const selectKey = ref(0);

const switchTemplate = async (selectedValue: ContractDraftTemplate) => {
  if (store.body.template && !await showConfirmDialogAsync(ConfirmDialogType.ConfirmTemplate)) {
    selectKey.value++;

    return;
  }

  store.body.template = selectedValue;
  selectKey.value = 0;
  store.switchTemplate();
}

onMounted(async () => {
  await ppContractStore.api.getPeriodConditionTypeAsync();
  await store.api.getCmRentalTypeAsync();
  await store.api.getcmRentalTpAsync();
});

watch(() => store.body.template, async (newVal) => {
  await store.api.getCmRentalTypeAttacement(newVal);
});
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="ข้อมูลสัญญา" />
      <div class="grid lg:grid-cols-2 gap-2 mt-10">
        <InputField label="คู่ค้า" v-model="store.body.detail.vendor.name" disabled rules="required" />
        <InputField :disabled="!store.states.canEdit" label="Email คู่ค้า" v-model="store.body.email"
          rules="required" />
      </div>
      <InputArea :disabled="!store.states.canEdit" label="ชื่อสัญญา" class="mt-8" v-model="store.body.contractName" />
      <div class="grid lg:grid-cols-2 gap-2 mt-8 gap-y-8">
        <InputField :disabled="!store.states.canEdit" label="เลขที่สัญญา PO" v-model="store.body.poNumber"
          rules="required" />
        <InputField :disabled="!store.states.canEdit" label="เลขที่สัญญา" v-model="store.body.contractNumber"
          rules="required" />
      </div>
      <div class="grid lg:grid-cols-2 gap-2 mt-8 gap-y-8">
        <InputNumber :disabled="!store.states.canEdit" label="วงเงินตามสัญญา" v-model="store.body.budget"
          rules="required" :min-fraction-digits="2" grouping/>
        <Datepicker v-if="store.body.status == TContractDraftStatus.Approved" :disabled="!store.states.canSaveDateSign"
          label="วันที่ลงนามในสัญญา" v-model="store.body.contractSignedDate" rules="required" />
      </div>
      <div class="grid lg:grid-cols-2 gap-2 mt-8 gap-y-8">
        <Select :options="store.dropdown.cmRentalTypeOptions" label="ประเภทสัญญา" v-model="store.body.contractType"
          disabled rules="required" />
        <Select :disabled="!store.states.canEdit" :options="store.dropdown.cmRentalTpOptions" label="รูปแบบสัญญา"
          :modelValue="store.body.template" @update:model-value="(e) => switchTemplate(e)" rules="required"
          :key="selectKey" />
      </div>
      <div class="grid lg:grid-cols-2 gap-2 mt-8 gap-y-8">
        <Select :disabled="!store.states.canEdit" :options="ppContractStore.dropdown.conditionTypeOptions"
          label="ระยะเวลาเริ่มต้นสัญญา" v-model="store.body.periodConditionType" rules="required" />
        <Checkbox :disabled="!store.states.canEdit" label="เฉพาะวันทำการ" class="mt-2"
          v-model="store.body.isWorkingDayOnly" rules="required" />
      </div>
      <div class="bg-gray-100 rounded-lg py-2 px-4 h-10 mb-6">
        <small><span class="text-red-500">*</span>หมายเหตุ : ระยะเวลาเริ่มต้นสัญญา คือ
          เงื่อนไขสำหรับคำนวนวันที่สิ้นสุดของสัญญา โดยระบบจะคำนวนหลังจากยืนยันวันที่ลงนามแล้ว</small>
      </div>
    </template>
  </Card>
</template>
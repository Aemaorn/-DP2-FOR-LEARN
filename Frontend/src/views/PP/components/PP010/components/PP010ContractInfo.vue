<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
  InputArea,
  Datepicker,
  Select,
  InputNumber,
} from '@/components/forms';
import { ContractDraftTemplate } from '@/enums/contractDraftt';
import { ConfirmDialogType } from '@/enums/dialog';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { useMenuStore } from '@/stores/menu';
import { TContractDraftStatus } from '@/views/PP/enums/pp010';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { onMounted, ref, watch } from 'vue';

const props = defineProps({
  readonly: { type: Boolean, default: false },
});

const menuStore = useMenuStore();
const store = useContractDraftStore();
const selectKey = ref(1);

const switchTemplate = async (selectedValue: ContractDraftTemplate) => {
  try {
    if (store.body.template) {
      const confirmed = await showConfirmDialogAsync(ConfirmDialogType.ConfirmTemplate);
      if (!confirmed) {
        selectKey.value++;
        return;
      }
    }
    store.body.template = selectedValue;
    store.switchTemplate();
    selectKey.value = 0;
    store.dropdown.subTemplateTypeOptions = [];
    store.dropdown.attacementTypeOptions = [];
  } catch (err) {
    console.error('switchTemplate failed', err);
    // keep UI stable: force select re-render or revert as needed
    selectKey.value++;
  }
}

const resetAttachments = async () => {

  store.body.detail.attachments = [];
}

onMounted(async () => {
  await store.api.getPeriodConditionTypeAsync();
  await store.api.getContractTypeAsync();
});

watch(() => store.body.contractType, async (newVal) => {
  await store.api.getTemplateTypeAsync(newVal);
});

watch(() => store.body.template, async (newVal, oldVal) => {
  if (oldVal != undefined && oldVal != newVal) {
    store.body.subTemplate = undefined;
  }

  if (store.dropdown.templateTypeOptions.length == 0) {
    await store.api.getTemplateTypeAsync(store.body.contractType);
  };

  const findItem = store.dropdown.templateTypeOptions.find(f => f.value == newVal);

  if (findItem && findItem.id && typeof (findItem.id) === 'string') {
    await store.api.getSubTemplateTypeAsync(findItem.id);

    const templateIdToUse = store.body.subTemplate
      ? store.dropdown.subTemplateTypeOptions.find(f => f.value == store.body.subTemplate)?.id
      : findItem.id;

    if (templateIdToUse && typeof (templateIdToUse) === 'string') {
      await store.api.getAttacementTypeAsync(templateIdToUse);
    }
  }
});

watch(() => store.body.subTemplate, async (newVal) => {
  if (!newVal) {
    const findItem = store.dropdown.templateTypeOptions.find(f => f.value == store.body.template);
    if (findItem && findItem.id && typeof (findItem.id) === 'string') {
      await store.api.getAttacementTypeAsync(findItem.id);
    }
    return;
  }

  const findSubItem = store.dropdown.subTemplateTypeOptions.find(f => f.value == newVal);
  if (findSubItem && findSubItem.id && typeof (findSubItem.id) === 'string') {
    await store.api.getAttacementTypeAsync(findSubItem.id);
  }
});

onMounted(async () => {
  if (!store.body.template) return;

  const findItem = store.dropdown.templateTypeOptions.find(f => f.value == store.body.template);

  if (findItem && findItem.id && typeof (findItem.id) === 'string') {
    await store.api.getSubTemplateTypeAsync(findItem.id);

    const templateIdToUse = store.body.subTemplate
      ? store.dropdown.subTemplateTypeOptions.find(f => f.value == store.body.subTemplate)?.id
      : findItem.id;

    if (templateIdToUse && typeof (templateIdToUse) === 'string') {
      await store.api.getAttacementTypeAsync(templateIdToUse);
    }
  }
});
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="ข้อมูลสัญญา" />
      <div class="grid lg:grid-cols-2 gap-2 mt-10">
        <Datepicker :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly" label="วันที่เอกสาร"
          v-model="store.body.documentDate" />
      </div>
      <div class="grid lg:grid-cols-2 gap-2 mt-10">
        <InputField :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly" label="คู่ค้า"
          v-model="store.body.detail.vendor.name" readonly rules="required" />
        <InputField :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly" label="Email(คู่ค้า)"
          v-model="store.body.email" rules="required" />
      </div>
      <InputArea class="mt-8" :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly" label="ชื่อสัญญา"
        v-model="store.body.contractName" rules="required" />
      <div class="grid lg:grid-cols-2 gap-y-8 gap-2 mt-8">
        <InputField :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly" label="เลขที่สัญญา PO"
          v-model="store.body.poNumber" rules="required" />
        <InputField :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly" label="เลขที่สัญญา จพ.(สบส.)"
          v-model="store.body.contractNumber" rules="required" />
        <InputNumber :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly" label="วงเงินตามสัญญา"
          v-model="store.body.budget" grouping :min-fraction-digits="2" rules="required" />
        <Datepicker v-if="store.body.status == TContractDraftStatus.Approved"
          :disabled="!menuStore.hasManage || !store.states.canSaveDateSign || props.readonly" label="วันที่ลงนามในสัญญา"
          v-model="store.body.contractSignedDate" rules="required" />
        <Select class="lg:col-start-1" :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly"
          :options="store.dropdown.contractTypeOptions" label="ประเภทสัญญา" v-model="store.body.contractType"
          rules="required" />
        <Select :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly" :options="store.dropdown.templateTypeOptions"
          label="รูปแบบสัญญา" :modelValue="store.body.template" @update:model-value="(e) => switchTemplate(e)"
          rules="required" :key="selectKey" />
        <Select v-if="store.dropdown.subTemplateTypeOptions.length > 0" v-model="store.body.subTemplate"
          :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly" :options="store.dropdown.subTemplateTypeOptions"
          label="รูปแบบสัญญาย่อย" rules="required" @update:model-value="resetAttachments" />
        <InputField v-if="store.body.template == ContractDraftTemplate.CFormat007"
          :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly" v-model="store.body.templateText"
          label="ชื่องาน MA (เช่น: คอมพิวเตอร์, Server อื่นๆ)" rules="required" />
        <Select :disabled="!store.states.canEdit || !menuStore.hasManage || props.readonly" class="lg:col-start-1"
          :options="store.dropdown.conditionTypeOptions" label="ระยะเวลาเริ่มต้นสัญญา"
          v-model="store.body.periodConditionType" rules="required" />
        <Datepicker :disabled="store.states.isPending || !menuStore.hasManage || props.readonly"
          v-if="store.body.periodConditionType === 'CSDPCond003'" label="วันที่เริ่มต้นสัญญา"
          v-model="store.body.startDate"
          :rules="`${store.body.status === TContractDraftStatus.Approved ? 'required' : ''}`"
          :max-date="store.body.endDate" />
        <Datepicker :disabled="store.states.isPending || !menuStore.hasManage || props.readonly"
          v-if="store.body.periodConditionType === 'CSDPCond003'" label="วันที่สิ้นสุดสัญญา"
          v-model="store.body.endDate" :min-date="store.body.startDate"
          :rules="`${store.body.status === TContractDraftStatus.Approved ? 'required' : ''}`" />
      </div>
      <div class="bg-gray-100 rounded-lg py-2 px-4 h-10 mb-6">
        <small>
          <span class="text-red-500">*</span>
          หมายเหตุ : ระยะเวลาเริ่มต้นสัญญา คือ เงื่อนไขสำหรับคำนวนวันที่สิ้นสุดของสัญญา
          โดยระบบจะคำนวนหลังจากยืนยันวันที่ลงนามแล้ว
        </small>
      </div>
    </template>
  </Card>
</template>
<script setup lang="ts">
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { Datepicker, InputField, InputArea, Select, Radio } from '@/components/forms';
import type { TP003MedianPriceExpenseDescriptionInfo, TPP003Body, TPP003Staff, TPP003StaffDetail, TPP003StaffDetailPersonal } from '@/views/PP/models/PP003/pp003Model';
import { usePP003DetailStore } from '@/views/PP/stores/PP003/pp003Store';
import { PP003Template } from '@/views/PP/enums/pp003';
import { Template02, Template03, Template04, Template05 } from '..';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ref } from 'vue';
import { ConfirmDialogType } from '@/enums/dialog';
import { useRouter } from 'vue-router';
import { useMenuStore } from '@/stores/menu';

const value = defineModel<TPP003Body>({
  required: true,
});

const props = defineProps<{
  readonly?: boolean;
  states: {
    isEditor: boolean;
    isCommitteeApproval: boolean;
    isCommitteeCurrentApproval: boolean;
    isBossCommitteeApproval: boolean;
    isUnitApproval: boolean;
    isCurrentUnitApproval: boolean;
    isLastUnitApproval: boolean;
    isJorPorSection: boolean;
    isJorPorAssign: boolean;
    isJorPorComment: boolean;
    isAcceptorApproval: boolean;
    isCurrentAcceptorApproval: boolean;
    isLastAcceptorApproval: boolean;
    isMangeMd: boolean;
    currentTemplate: boolean;
  },
}>();

const menuStore = useMenuStore();
const detailStore = usePP003DetailStore();

const router = useRouter();
const emit = defineEmits(['onSelectTemplate']);
const selectKey = ref(0);

const jobDescriptionTemplateCodes: string[] = [
  PP003Template.MedianPriceCancelBoKor0160,
  PP003Template.MedianPriceCancelBoKor01ForJorporComment60,
  PP003Template.MedianPriceCancelBoKor01ForJorporComment80,
  PP003Template.MedianPriceChangeBoKor01ForJorporComment60,
  PP003Template.MedianPriceChangeBoKor0160,
  PP003Template.MedianPriceBoKor0180,
  PP003Template.MedianPriceChangeBoKor0180,
  PP003Template.MedianPriceChangeBoKor01ForJorporComment80,
  PP003Template.MedianPriceCancelBoKor0180,
  PP003Template.MedianPriceBoKor01ForJorporComment60,
  PP003Template.MedianPriceBoKor0160,
  PP003Template.MedianPriceBoKor01ForJorporComment80,
];

const onSelectTemplate = async (val: string): Promise<void> => {
  if (value.value.medianPriceDocumentTemplateCode && !await showConfirmDialogAsync(ConfirmDialogType.ConfirmTemplate)) {
    selectKey.value++;

    return;
  }

  detailStore.setIsChangeTemplate(true);
  value.value.medianPriceDocumentTemplateCode = val;
  selectKey.value = 0;

  detailStore.onResetbudgetAllocationsDetail();

  const hasStaff = [Template02, Template03, Template04].includes(detailStore.states.currentTemplate);
  const hasExpenseDescription = [Template02, Template03, Template04, Template05].includes(detailStore.states.currentTemplate);

  if (hasStaff) {
    detailStore.body.staff = {
      personnelCount: 0,
      details: [] as TPP003StaffDetail[] | TPP003StaffDetailPersonal[],
    } as TPP003Staff;
  } else {
    delete detailStore.body.staff;
  }

  if (hasExpenseDescription) {
    detailStore.body.expenseDescription = {} as TP003MedianPriceExpenseDescriptionInfo;
  } else {
    delete detailStore.body.expenseDescription;
  }
  emit('onSelectTemplate', val);
};

const onViewTemplate = async (code: string) => {
  if (!await showConfirmDialogAsync(undefined, "ต้องการดูตัวอย่างเอกสาร")) return;

  const selectedTemplateId = detailStore.templateOptions.find(f => f.value === code)?.id;

  if (!selectedTemplateId) return;

  router.push({ name: 'st007Detail', params: { id: selectedTemplateId } });
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="กำหนดราคากลาง (ราคาอ้างอิง)" />
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 gap-y-8 mt-8">
        <InputField label="เลขที่อ้างอิงในระบบ" v-model="value.referenceNumber" disabled />
        <Datepicker label="วันที่เอกสาร" v-model="value.documentDate"
          :disabled="!detailStore.states.isEditor || detailStore.body.isCancel || detailStore.body.isChange || !menuStore.hasManage || props.readonly" />

        <InputField class="lg:col-start-1" label="เบอร์โทร" v-model="detailStore.body.telephone" rules="required"
          :disabled="!detailStore.states.isEditor || detailStore.body.isCancel || detailStore.body.isChange || !menuStore.hasManage || props.readonly" />

        <Select class="lg:col-start-1" label="รูปแบบเอกสารราคากลาง" :model-value="value.medianPriceDocumentTemplateCode"
          @update:model-value="(e) => onSelectTemplate(e)" :options="detailStore.templateOptions" rules="required"
          :key="selectKey"
          :disabled="!detailStore.states.isEditor || detailStore.body.isCancel || detailStore.body.isChange || !menuStore.hasManage || props.readonly" />
        <p class="d-flex align-center m-1.5 cursor-pointer text-blue-500 w-fit"
          v-if="value.medianPriceDocumentTemplateCode"
          @click="() => onViewTemplate(value.medianPriceDocumentTemplateCode!)">
          ดูตัวอย่างเอกสาร</p>

        <InputArea class="lg:col-span-3" label="เหตุผลและความจำเป็นที่จะซื้อหรือจ้างหรือเช่า" v-model="value.reason"
          rules="required" :disabled="!detailStore.states.isEditor || !menuStore.hasManage || props.readonly" />

        <InputArea class="lg:col-span-3" label="วัตถุประสงค์การขอซื้อ" v-model="value.object" rules="required"
          :disabled="!detailStore.states.isEditor || !menuStore.hasManage || props.readonly" />

        <InputArea class="lg:col-span-3"
          v-if="value.torTemplate != 'TorHireCleaning80' && value.torTemplate != 'TorHireCleaning60'"
          label="รายละเอียดคุณลักษณะเฉพาะของพัสดุที่จะซื้อหรือจ้างหรือเช่าแล้วแต่กรณี"
          v-model="value.specialDescription" rules="required"
          :disabled="!detailStore.states.isEditor || !menuStore.hasManage || props.readonly" />

        <InputArea class="lg:col-span-3" label="ลักษณะงาน (โดยสังเขป)"
          v-if="jobDescriptionTemplateCodes.includes(value.medianPriceDocumentTemplateCode!)" rules="required"
          v-model="value.jobDescription" :disabled="!detailStore.states.isEditor || !menuStore.hasManage || props.readonly" />

        <InputArea class="lg:col-span-3" label="เหตุผลการขอเปลี่ยนแปลง" v-model="value.changeReason"
          v-if="value.isChange" rules="required" :disabled="!detailStore.states.isEditor || props.readonly" />
        <InputArea class="lg:col-span-3" label="เหตุผลการขอยกเลิก" v-model="value.cancelReason" v-if="value.isCancel"
          rules="required" :disabled="!detailStore.states.isEditor || props.readonly" />
      </div>
    </template>
  </Card>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา" />
      <Radio
        label="ราคากลางของพัสดุที่จะซื้อหรือจ้างเช่า หรือ ข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคากรณีวงเงิน เกิน 100,000 บาท มีข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา ดังนี้"
        v-model="value.priceReasonablenessInfo" :options="detailStore.medianPriceInfoConsiderOptions" vertical
        rules="required" :disabled="!detailStore.states.isEditor || !menuStore.hasManage || props.readonly" />
    </template>
  </Card>
</template>
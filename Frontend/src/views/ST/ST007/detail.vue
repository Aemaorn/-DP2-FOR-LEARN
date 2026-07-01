<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue';
import { Form as VeeForm } from 'vee-validate';
import { useRoute } from 'vue-router';
import type { MenuItem } from 'primevue/menuitem';
import { useSt007DetailStore } from '@/stores/ST/st007';

import ChEditor from '@/components/Document/ChEditor.vue';
import {
  InputField,
  InputUploadFile,
  Select,
  DragdropFilesSection,
  Checkbox,
  InputNumber,
} from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import type { CustomFileSt007 } from '@/models/ST/st007';
import { storeToRefs } from 'pinia';
import ToastHelper from '@/helpers/toast';

const routeItems = ref([
  { label: 'จัดการรูปแบบเอกสาร', url: '/st/st007' },
  { label: 'รายละเอียด' },
] as MenuItem[]);

const route = useRoute();
const detailStore = useSt007DetailStore();
const {
  groupDropdown,
  supplyMethodDropdown,
  contractTypeOptions,
  templateTypeOptions,
  supplyMethodTypeDropdown,
  pRentalPcpDropdown,
  contractAmendmentDocumentTypeDropdown,
} = storeToRefs(detailStore);

const documentRef = ref<InstanceType<typeof ChEditor> | null>(null);

onMounted(async (): Promise<void> => {
  await detailStore.initialDropdownDataAsync();

  if (route.params.id) {
    await detailStore.onGetByIdAsync(route.params.id as string);
  }

  if (detailStore.body.group === 'CA') {
    await onGetDropdownContractTypeAsync();
  }

  if (detailStore.body.group === 'CAInv') {
    await detailStore.onGetSupplyMethodTypeAsync();
  }

  if (detailStore.body.group === 'PRentalPcp') {
    await detailStore.onGetPRentalPcpAsync();
  }
});

onUnmounted((): void => {
  detailStore.onResetBody();
});

const onSubmitAsync = async (): Promise<void> => {
  if (!detailStore.body.file.id) return ToastHelper.errorDescription("กรุณาอัปโหลดเอกสาร");

  if (route.params.id) {
    await detailStore.onUpdateAsync(route.params.id.toString());

    return;
  }

  await detailStore.onCreateAsync();
};

const onUploadFileAsync = async (file: CustomFileSt007): Promise<void> => {
  if (file.file) {
    await detailStore.onUploadFileAsync(file.file);
  }
};

const showIsChange = (): boolean => {
  switch (detailStore.body.group) {
    case 'Plan':
    case 'Ap':
    case 'PlanAnnment':
    case 'Tor':
      return true;
    default:
      return false;
  }
};

const showIsCancel = (): boolean => {
  switch (detailStore.body.group) {
    case 'Plan':
    case 'Ap':
    case 'PlanAnnment':
    case 'Jp06':
    case 'Tor':
      return true;
    default:
      return false;
  }
};

const showIsJorPorComment = (): boolean => {
  if (detailStore.body.group === 'Tor' && detailStore.body.isApproval) {
    return true;
  }

  if (detailStore.body.group === 'Mdp') {
    return true;
  }

  return false;
};

const showIsFine = (): boolean => {
  switch (detailStore.body.group) {
    case 'CMR':
    case 'CMCltr':
      return true;
    default:
      return false;
  }
};

const showIsWinnerAnnounced = (): boolean => {
  return detailStore.body.group === 'Jp06';
};

const showIsEvaluationReport = (): boolean => {
  return detailStore.body.group === 'Jp06';
};

const showIsAppointmentOrdered = (): boolean => {
  return detailStore.body.group === 'Jp05';
};

const showIsApproval = (): boolean => {
  return ['Tor', 'CA', 'CamContractAmendment'].includes(detailStore.body.group);
};

const showIsPublished = (): boolean => {
  return ['Plan', 'PlanAnnment'].includes(detailStore.body.group);
};

const showIsInYear = (): boolean => {
  return ['Plan', 'PlanAnnment'].includes(detailStore.body.group);
};

const showHasGuarantee = (): boolean => {
  return detailStore.body.group === 'CAInv';
};

const showSupplyMethodType = (): boolean => {
  return detailStore.body.group === 'CAInv';
};

const showIsConfidential = (): boolean => {
  return detailStore.body.group === 'CA';
};

const isDisabled = (): boolean => {
  return route.params.id !== undefined && route.params.id !== null && route.params.id !== '';
};

const switchContractType = async (newVal: string): Promise<void> => {
  await detailStore.onGetTemplateTypeAsync(newVal);
  detailStore.body.contractTemplateCode = undefined;
};

const onGetDropdownContractTypeAsync = async (): Promise<void> => {
  await detailStore.onGetContractTypeAsync();
  await detailStore.onGetTemplateTypeAsync(detailStore.body.contractTemplateType);
};

const switchGroup = async (newVal: string): Promise<void> => {
  detailStore.body.group = newVal;
  detailStore.body.contractTemplateCode = undefined;
  if (newVal !== 'CA') {
    return;
  }
  await onGetDropdownContractTypeAsync();
};

const switchContractAmendmentDocumentType = (newVal: string): void => {
  detailStore.body.contractAmendmentDocumentType = newVal;
};

</script>

<template>
  <VeeForm class="pb-10" @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()">
    <TitleHeader label="จัดการรูปแบบเอกสาร" :route-items="routeItems">
      <template #breadcrumbAction>
        <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
      </template>
    </TitleHeader>
    <Card class="my-4">
      <template #content>
        <div class="my-2">
          <TitleHeader label="รายละเอียดรูปแบบเอกสาร" />
        </div>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4 gap-y-8 mt-10">
          <Select label="กลุ่ม" v-model="detailStore.body.group" :options="groupDropdown"
            @update:model-value="switchGroup" rules="required" />
          <InputField label="รหัส" v-model="detailStore.body.code" :disabled="isDisabled()" rules="required" />
          <InputField label="ชื่อเอกสาร" v-model="detailStore.body.name" rules="required" />
          <Select class="col-start-1" :options="supplyMethodDropdown" label="วิธีการจัดหา"
            v-model="detailStore.body.supplyMethodCode" />
          <Select :options="supplyMethodTypeDropdown" label="ประเภทวิธีการจัดหา"
            v-model="detailStore.body.supplyMethodTypeCode" v-if="showSupplyMethodType()" />
          <Select :model-value="detailStore.body.contractTemplateType" :options="contractTypeOptions"
            label="ประเภทสัญญา" @update:model-value="switchContractType" v-if="detailStore.body.group === 'CA'" />

          <Select :model-value="detailStore.body.principleApprovalTemplateCode" :options="pRentalPcpDropdown"
            label="ประเภทสัญญา" v-if="detailStore.body.group === 'PRentalPcp'" />


          <Select :model-value="detailStore.body.contractAmendmentDocumentType"
            :options="contractAmendmentDocumentTypeDropdown" label="ประเภทบันทึกต่อท้ายสัญญา"
            v-if="detailStore.body.group === 'CamContractAmendment'"
            @update:model-value="switchContractAmendmentDocumentType" />

          <Select :options="templateTypeOptions" label="ประเภทแม่แบบ" v-model="detailStore.body.contractTemplateCode"
            v-if="detailStore.body.group === 'CA'" />
          <InputNumber label="จำนวนเงิน (ตั้งแต่)" v-model="detailStore.body.budgetMin" :min-fraction-digits="2" grouping class="col-start-1" />
          <InputNumber label="จำนวนเงิน (ถึง)" v-model="detailStore.body.budgetMax" :min-fraction-digits="2" grouping />

          <div class="col-span-12 flex gap-4 items-center">
            <Checkbox label="ขอเปลี่ยนแปลง" v-model="detailStore.body.isChange" v-if="showIsChange()" />
            <Checkbox label="ขออนุมัติยกเลิก" v-model="detailStore.body.isCancel" v-if="showIsCancel()" />
            <Checkbox label="ขออนุมัติ" v-model="detailStore.body.isApproval" v-if="showIsApproval()" />
            <Checkbox label="จพ. ให้ความเห็น" v-model="detailStore.body.isJorPorComment" v-if="showIsJorPorComment()" />
            <Checkbox label="มีค่าปรับ" v-model="detailStore.body.isFine" v-if="showIsFine()" />
            <Checkbox label="ประกาศผู้ชนะ" v-model="detailStore.body.isWinnerAnnounced"
              v-if="showIsWinnerAnnounced()" />
            <Checkbox label="รายงานผลการพิจารณาและขออนุมัติ" v-model="detailStore.body.isEvaluationReport"
              v-if="showIsEvaluationReport()" />
            <Checkbox label="คำสั่งแต่งตั้ง" v-model="detailStore.body.isAppointmentOrdered"
              v-if="showIsAppointmentOrdered()" />
            <Checkbox label="เผยแพร่" v-model="detailStore.body.isPublished" v-if="showIsPublished()" />
            <Checkbox label="ระหว่างปีงบประมาณ" v-model="detailStore.body.isInYear" v-if="showIsInYear()" />
            <Checkbox label="มีหลักประกัน" v-model="detailStore.body.hasGuarantee" v-if="showHasGuarantee()" />
            <Checkbox label="สัญญารักษาความลับ" v-model="detailStore.body.isConfidential" v-if="showIsConfidential()" />
          </div>

          <div class="col-start-1 mt-4">
            <InputUploadFile :file-name="detailStore.body.previewPdfFile!.fileName" label="ตัวอย่างเอกสาร"
              v-model="detailStore.body.previewPdfFile!.file" :rules="`${route.params.id ? '' : 'required'}`"
              helper-text="รองรับเฉพาะไฟล์สกุล .pdf" />
          </div>
        </div>
      </template>
    </Card>
    <DragdropFilesSection label="เอกสาร" v-model="detailStore.body.file"
      @on-change="onUploadFileAsync(detailStore.body.file)" :accept="['application/vnd.oasis.opendocument.text']"
      support-text="รองรับเฉพาะไฟล์ .odt และมีขนาดไม่เกิน 10 MB">
      <template #extraRender v-if="detailStore.body.file.id">
        <ChEditor :docId="detailStore.body.file.id" docName="stDoc007" :readonly="false" ref="documentRef"
          :key="detailStore.body.file.id" />
      </template>
    </DragdropFilesSection>
  </VeeForm>
</template>

<style scoped>
.center {
  display: flex;
  align-items: center;
  justify-content: center;
}

.underline {
  text-decoration: underline;
}

.pointer {
  cursor: pointer;
}

.grab {
  cursor: grab;
}
</style>

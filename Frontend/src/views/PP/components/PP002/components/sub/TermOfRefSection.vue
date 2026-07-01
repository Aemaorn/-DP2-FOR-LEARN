<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import type { PP002Detail } from '@/views/PP/models/PP002/pp002Model';
import { Datepicker, InputArea, InputField, InputNumber, Radio, Select } from '@/components/forms';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { computed, onMounted, ref } from 'vue';
import { ConfirmDialogType } from '@/enums/dialog';
import { useMenuStore } from '@/stores/menu';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import ST007Service from '@/services/ST/ST007';
import { DepartmentId } from '@/enums/businessUnit';
import { useAuthenticationStore } from '@/stores/authentication';

const value = defineModel<PP002Detail>({
  required: true,
});

const props = defineProps({
  readonly: { type: Boolean, default: false },
});

const emit = defineEmits(['onChangeTemplate']);

const menuStore = useMenuStore();
const store = usePP002DetailStore();
const authStore = useAuthenticationStore();

const mockRadioData = [
  { value: true, label: 'มี' },
  { value: false, label: 'ไม่มี' },
] as Option[];

const mockRadioStockData = [
  { value: true, label: 'ใช่' },
  { value: false, label: 'ไม่ใช่' },
] as Option[];

const template = ref(0);

const isJorPorDepartmentCode = computed(() => {
  return authStore.profile.departmentCode === DepartmentId.JorPor;
});

onMounted(() => {
  initAsync();
});

const initAsync = async () => {
  await store.onGetTemplateDDLAsync();
};

const onSelectTemplateAsync = async (newTemplateId: string): Promise<void> => {
  if (store.PP002Detail.torDocumentTemplateCode) {
    const res = await showConfirmDialogAsync(ConfirmDialogType.ConfirmTemplate);

    if (!res) {
      template.value++;

      return;
    }
  }

  store.onClearForm();
  store.showDocument = false;
  store.PP002Detail.torDraftDocumentId = undefined;
  store.PP002Detail.torDraftApprovalDocumentId = undefined;
  emit('onChangeTemplate', newTemplateId);
};

const onViewTemplate = async (code: string) => {
  const selectedTemplateId = store.templateDDL.find(f => f.value === code)?.id;

  if (!selectedTemplateId) return;

  ST007Service.downloadFilePdfAsync(selectedTemplateId as string);
};
</script>

<template>
  <Card class="mb-4" data-section-id="term-of-ref" data-section-label="ขอบเขตงาน">
    <template #content>
      <TitleHeader label="ขอบเขตงาน"> </TitleHeader>
      <div class="grid lg:grid-cols-2 gap-2 gap-y-8 mt-8">
        <InputField label="เลขที่จัดทำร่างขอบเขตงาน" v-model="value.referenceNumber" disabled />
        <Datepicker label="วันที่เอกสาร" v-model="value.documentDate"
          :disabled="!store.status.canEditTor || !menuStore.hasManage || props.readonly" />
        <InputField class="lg:col-start-1" label="เบอร์โทร" v-model="value.telephoneNumber" rules="required"
          :disabled="!store.status.canEditTor || !menuStore.hasManage || props.readonly" />
        <Select class="lg:col-start-1" label="รูปแบบเอกสาร" :model-value="value.torDocumentTemplateCode"
          :options="store.templateDDL" rules="required" @update:model-value="(e) => onSelectTemplateAsync(e)"
          :key="template" :disabled="!store.status.canEditTor || !menuStore.hasManage || props.readonly" />
        <p class="d-flex align-center m-1.5 text-link w-fit" v-if="value.torDocumentTemplateCode"
          @click="() => onViewTemplate(value.torDocumentTemplateCode)">ดูตัวอย่างเอกสาร</p>
      </div>

      <Radio class="lg:col-start-1" label="หลักประกันการเสนอราคา" v-model="value.bidGuarantee" :options="mockRadioData"
        rules="required" :disabled="!store.status.canEditTor || !menuStore.hasManage || props.readonly" />

      <Radio class="lg:col-start-1"
        label="(Stock) เป็นรายการวัสดุเครื่องเขียนแบบพิมพ์ และวัสดุของใช้สิ้นเปลืองคลังพัสดุ" v-model="value.isStock"
        :options="mockRadioStockData" rules="required"
        :disabled="!store.status.canEditTor || !menuStore.hasManage || !isJorPorDepartmentCode || props.readonly" />

      <Radio class="lg:col-start-1" label="หลักประกันสัญญา" v-model="value.isContractGuarantee"
        @update:model-value="(val) => value.percentageContract = val ? 5 : undefined" :options="mockRadioData"
        rules="required" :disabled="!store.status.canEditTor || !menuStore.hasManage || props.readonly" />
      <InputNumber v-if="value.isContractGuarantee" class="lg:col-start-1 mt-3" label="ร้อยละ ของข้อมูลค่าสัญญา"
        rules="required" v-model="value.percentageContract" :disabled="!store.status.canEditTor || !menuStore.hasManage || props.readonly"
        :min-fraction-digits="2" :max-fraction-digits="3" grouping />
      <InputArea class="lg:col-span-3" label="เหตุผลการขอเปลี่ยนแปลง" v-model="store.PP002Detail.changeReason"
        v-if="store.PP002Detail.isChange" rules="required"
        :disabled="!store.status.canEditTor || !menuStore.hasManage || props.readonly" />
      <InputArea class="lg:col-span-3" label="เหตุผลการขอยกเลิก" v-model="store.PP002Detail.cancelReason"
        v-if="store.PP002Detail.isCancel" rules="required"
        :disabled="!store.status.canEditTor || !menuStore.hasManage || props.readonly" />
    </template>
  </Card>
</template>

<style scoped lang="scss">
.text-link {
  color: #448aff;
  cursor: pointer;
}
</style>

<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Datepicker, InputField, InputArea, InputNumber, Radio } from '@/components/forms';
import type { VendorInfo } from '@/views/PP/models/PP009/pp009Model';
import type { Option } from '@/models/shared/option';
import { ref, watch, onMounted, computed } from 'vue';
import { HttpStatusCode } from 'axios';
import { EGroupCode } from '@/enums/shared';
import SharedService from '@/services/Shared/dropdown';
import { usePPDetailStore } from '@/stores/PP/ppStore';
import { ProcurementType } from '@/enums/procurement';
import { usePP009DetailStore } from '@/views/PP/stores/PP009/PP009Store';

const radioGroup = [
  { value: true, label: 'มีหลักประกัน' },
  { value: false, label: 'ไม่มีหลักประกัน' },
];

type Props = {
  isValidate?: boolean;
  isDisabled?: boolean;
}
const { isDisabled, isValidate } = defineProps<Props>();

const value = defineModel<VendorInfo>({
  required: true,
});

const detailStore = usePP009DetailStore();
const procurementStore = usePPDetailStore();
const isProcurement = computed(() => procurementStore.procurementDetail.procurementType === ProcurementType.Procurement);

const supplyMethodTypeCodeDDL = ref<Option[]>([]);

onMounted(async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethodType, undefined, true);
  if (status === HttpStatusCode.Ok) {
    supplyMethodTypeCodeDDL.value = data;
  }
});

watch(
  () => [
    value.value.agreedPrice,
    value.value.contractGuaranteePercent,
    value.value.hasContractGuarantee
  ],
  ([agreedPrice]) => {
    const v = value.value;

    if (!agreedPrice || !v.hasContractGuarantee) return;

    if (v.contractGuaranteePercent != null && v.contractGuaranteePercent !== 0) {
      v.guaranteeAmount = v.agreedPrice * (v.contractGuaranteePercent / 100);
      return;
    }
  }
);

watch(
  () => [
    value.value.agreedPrice,
    value.value.guaranteeAmount,
    value.value.hasContractGuarantee
  ],
  ([agreedPrice]) => {
    const v = value.value;

    if (!agreedPrice || !v.hasContractGuarantee) return;

    if (v.guaranteeAmount != null && v.guaranteeAmount !== 0) {
      v.contractGuaranteePercent = (v.guaranteeAmount / v.agreedPrice) * 100;
      return;
    }
  }
);

watch(
  () => value.value.hasContractGuarantee,
  (hasContractGuarantee) => {
    const v = value.value;

    if (hasContractGuarantee) return;

    v.contractGuaranteePercent = 0;
    v.guaranteeAmount = 0;
  }
);
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลสัญญา" />
      <div class="grid lg:grid-cols-2 gap-4 mt-10">
        <Datepicker label="วันที่เอกสาร" v-model="value.documentDate" :disabled="isDisabled" />
      </div>
      <div class="grid lg:grid-cols-2 gap-4 mt-10">
        <InputField label="คู่ค้า" disabled v-model="value.vendorName" />
        <InputField label="Email (คู่ค้า)" v-model="value.email" :rules="`${isValidate ? 'required|email' : 'email'}`"
          :disabled="isDisabled" />
        <Select v-if="isProcurement && detailStore.body.vendors.length > 1" label="รูปแบบสัญญาหนังสือเชิญชวน"
          v-model="value.documentTemplateCode" :options="supplyMethodTypeCodeDDL"
          :rules="`${isValidate ? 'required' : ''}`" :disabled="isDisabled" class="mt-5" />
      </div>
      <InputArea label="ชื่อสัญญา" v-model="value.contractName" :rules="`${isValidate ? 'required' : ''}`"
        :disabled="isDisabled" class="mt-8" />
      <InputField label="เลขที่สัญญา PO" v-model="value.poNumber" :rules="`${isValidate ? 'required' : ''}`"
        :disabled="isDisabled" class="mt-8" />
      <InputField label="เลขที่สัญญา จพ.(สบส.)" v-model="value.contractNumber"
        :rules="`${isValidate ? 'required' : ''}`" :disabled="isDisabled" class="mt-8" />
      <InputNumber label="ราคารวมที่ตกลง" v-model="value.agreedPrice" grouping :minFractionDigits="2"
        :rules="`${isValidate ? 'required' : ''}`" :disabled="isDisabled" class="mt-8" />
      <p class="whitespace-nowrap text-xl! md:text-2xl! font-bold">ข้อมูลหลักประกันสัญญา</p>
      <Radio :options="radioGroup" v-model="value.hasContractGuarantee" class="mt-5" :disabled="isDisabled" />
      <div class="grid lg:grid-cols-2 gap-4 mt-8" v-if="value.hasContractGuarantee">
        <InputNumber label="ร้อยละของราคาค่าจ้างตามสัญญา" v-model="value.contractGuaranteePercent" grouping
          :minFractionDigits="2" :maxFractionDigits="3" :fractionDigits="2" :rules="`${isValidate ? 'required' : ''}`"
          :disabled="isDisabled" />
        <InputNumber label="จำนวนเงินหลักประกัน" v-model="value.guaranteeAmount" grouping :minFractionDigits="2"
          :rules="`${isValidate ? 'required' : ''}`" :disabled="isDisabled" />
      </div>
      <p class="whitespace-nowrap text-xl! md:text-2xl! font-bold">ข้อมูลติดต่อผู้จัดทำสัญญา</p>
      <div class="mt-10 grid lg:grid-cols-3 gap-5">
        <InputField label="ผู้ติดต่อ" v-model="value.contractOfficerName" :rules="`${isValidate ? 'required' : ''}`"
          :disabled="isDisabled" />
        <InputField label="โทรศัพท์" v-model="value.contractOfficerPhone" :rules="`${isValidate ? 'required' : ''}`"
          :disabled="isDisabled" />
        <InputField label="อีเมล" v-model="value.contractOfficerEmail"
          :rules="`${isValidate ? 'required|email' : 'email'}`" :disabled="isDisabled" />
      </div>
    </template>
  </Card>
</template>
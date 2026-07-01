<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Datepicker, InputNumber, InputField, Radio, Select, InputArea } from '@/components/forms';
import type { TContractDraftBody, TGuaranteeInfo } from '@/views/PP/models/PP0010/ContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { computed, onBeforeMount, watch } from 'vue';

type Props = {
  label: string;
  disable?: boolean;
};

const props = defineProps<Props>();

const buyerRadioOption = [
  { value: true, label: 'ยื่น' },
  { value: false, label: 'ไม่ยื่น' },
];

const store = useContractDraftStore();
const body = defineModel<TContractDraftBody>('body', { required: true });

onBeforeMount(async () => {
  await store.api.getBankAsync();
});

const showType1 = computed(() => {
  const type = body.value.detail.guarantee?.typeCode;
  return type === 'PBondType001' || type === 'PBondType004';
});

const showType2 = computed(() => {
  const type = body.value.detail.guarantee?.typeCode;
  return type && !showType1.value && type !== 'PBondType007';
});

const showType3 = computed(() => {
  const type = body.value.detail.guarantee?.typeCode;
  return type === 'PBondType007';
});

watch(
  () => body.value.detail.guarantee?.hasGuarantee,
  (newValue) => {
    if (!newValue) {
      body.value.detail.guarantee = {
        hasGuarantee: false,
      } as TGuaranteeInfo;
    }
  }
);

watch(
  () => body.value.detail.guarantee?.percentage,
  (newPercent) => {
    const g = body.value.detail.guarantee;
    if (!g) return;
    if (newPercent == null) return;

    const total = body.value.detail.agreement.totalAmount ?? 0;
    if (total > 0) {
      g.amount = (total * newPercent) / 100;
    }
  }
);

watch(
  () => body.value.detail.guarantee?.amount,
  (newAmount) => {
    const g = body.value.detail.guarantee;
    if (!g) return;
    if (newAmount == null) return;

    const total = body.value.detail.agreement.totalAmount ?? 0;
    if (total > 0) {
      g.percentage = (newAmount / total) * 100;
    }
  }
);

watch(() => body.value.detail.guarantee?.typeCode, (val) => {
  if (val != 'PBondType007' && body.value.detail.guarantee) {
    body.value.detail.guarantee.otherDetails = undefined;
  }
})
</script>

<template>
  <Card v-if="body.detail.guarantee"
    :pt="{ root: { 'data-section-id': 'contract-performance', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <Radio :disabled="props.disable" label="ต้องยื่นหลักประกันสัญญาหรือไม่" :options="buyerRadioOption" class="mt-5"
        v-model="body.detail.guarantee.hasGuarantee" />
      <div v-if="body.detail.guarantee.hasGuarantee" class="grid lg:grid-cols-3 gap-2 mt-6">
        <Select :disabled="props.disable" label="ประเภทหลักประกัน" :options="store.dropdown.rCCRTypeOptions"
          v-model="body.detail.guarantee.typeCode" rules="required" />
      </div>

      <div v-if="showType1" class="flex flex-col gap-2 mt-4 gap-y-8">
        <div class="grid lg:grid-cols-3 gap-2">
          <InputField :disabled="props.disable" label="เลขที่อ้างอิง" v-model="body.detail.guarantee.referenceNumber"
            rules="required" />
        </div>
        <div class="grid lg:grid-cols-3 gap-2">
          <Datepicker :disabled="props.disable" label="วันที่" v-model="body.detail.guarantee.guaranteeDate"
            rules="required" />
        </div>
      </div>

      <div v-if="showType2" class="flex flex-col gap-8 mt-4">
        <div class="grid lg:grid-cols-3 gap-2 gap-y-8">
          <Select :disabled="props.disable" label="ธนาคาร" :options="store.dropdown.bankOptions"
            v-model="body.detail.guarantee.bankCode" rules="required" />
          <InputField :disabled="props.disable" label="สาขาธนาคาร" v-model="body.detail.guarantee.bankBranch"
            rules="required" />
          <InputField :disabled="props.disable" label="เลขที่" v-model="body.detail.guarantee.bankAccountNumber"
            rules="required" />
        </div>

        <div class="grid lg:grid-cols-3 gap-2 gap-y-8">
          <Datepicker :disabled="props.disable" label="วันหนังสือค้ำประกันเริ่มต้น"
            v-model="body.detail.guarantee.bankCollateralStartDate" rules="required" />
          <Datepicker :disabled="props.disable" label="สิ้นสุด" v-model="body.detail.guarantee.bankCollateralEndDate"
            rules="required" />
        </div>
      </div>

      <section v-if="showType3" class="flex flex-col gap-8 mt-8">
        <div class="grid lg:grid-cols-3 gap-2 gap-y-8">
          <InputNumber :disabled="props.disable" label="จำนวนเงิน" v-model="body.detail.guarantee.amount" grouping
            rules="required" :min-fraction-digits="2" />
        </div>

        <div class="grid lg:grid-cols-3 gap-2 gap-y-8">
          <InputNumber :max-number="100" :disabled="props.disable" label="ร้อยละ (ของราคาทั้งหมดตามสัญญา)"
            v-model="body.detail.guarantee.percentage" rules="required" :min-fraction-digits="2"
            :max-fraction-digits="3" />
        </div>

        <InputArea v-model="body.detail.guarantee.otherDetails" label="รายละเอียด" rules="required" />
      </section>

      <div v-if="body.detail.guarantee.hasGuarantee" class="flex flex-col gap-2 mt-4 gap-y-8">
        <div class="grid lg:grid-cols-3 gap-2">
          <InputNumber :disabled="props.disable" label="จำนวนเงิน" v-model="body.detail.guarantee.amount" grouping
            rules="required" :min-fraction-digits="2" />
        </div>

        <div class="grid lg:grid-cols-3 gap-2">
          <InputNumber :max-number="100" :disabled="props.disable" label="ร้อยละ (ของราคาทั้งหมดตามสัญญา)"
            v-model="body.detail.guarantee.percentage" rules="required" :min-fraction-digits="2"
            :max-fraction-digits="3" />
        </div>
      </div>
    </template>
  </Card>
</template>

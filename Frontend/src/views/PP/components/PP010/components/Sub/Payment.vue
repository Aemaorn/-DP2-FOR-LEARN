<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputNumber,
} from '@/components/forms';
import { ContractDraftTemplate } from '@/enums/contractDraftt';
import { ArrayHelper } from '@/helpers/array';
import type { TAdvancePayment, TContractDraftBody, TPaymentBase, TPaymentTermDetail } from '@/views/PP/models/PP0010/ContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { watch, computed } from 'vue';
import draggable from 'vuedraggable';

type Props = {
  label: string;
  disable?: boolean;
  showContractDates?: boolean;
}

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });

const store = useContractDraftStore();

const { addSequence, reSequence, deleteItemAndReSequence } = ArrayHelper();

const addPaymentDetail = () => {
  if (!body.value.detail.payment) {
    body.value.detail.payment = {} as TPaymentBase;
  }

  if (!body.value.detail.payment.details) {
    body.value.detail.payment.details = [] as TPaymentTermDetail[];
  }

  body.value.detail.payment.details = addSequence(body.value.detail.payment.details, {} as TPaymentTermDetail)
};

const reSequenceData = () => {
  if (!body.value.detail.payment || !body.value.detail.payment.details) return;

  body.value.detail.payment.details = reSequence(body.value.detail.payment.details);
}

const deleteItem = (index: number): void => {
  if (!body.value.detail.payment || !body.value.detail.payment.details) return;

  body.value.detail.payment.details = deleteItemAndReSequence(
    body.value.detail.payment.details as TPaymentTermDetail[],
    index
  ) as TPaymentTermDetail[];
};

const buyerRadioOption = [
  { value: true, label: 'จ่ายเงินล่วงหน้า' },
  { value: false, label: 'ไม่มีเงินจ่ายล่วงหน้า' },
];

watch(() => body.value.detail.advancePayment?.hasAdvancePayment, (newValue) => {
  if (!newValue) {
    body.value.detail.advancePayment = {
      hasAdvancePayment: false,
    } as TAdvancePayment;
  };
});

const onAmountChange = (item: TPaymentTermDetail) => {
  const budget = Number(body.value.budget);
  if (!budget) return;
  const amount = Number(item.amount || 0);
  item.installmentPercentage = Math.round(((amount / budget) * 100) * 100) / 100;
};

const onPercentageChange = (item: TPaymentTermDetail) => {
  const budget = Number(body.value.budget);
  if (!budget) return;
  const percent = Number(item.installmentPercentage || 0);
  item.amount = Math.round(((percent / 100) * budget) * 100) / 100;
};

const getTotalPercent = computed(() => {
  const details = body.value.detail?.payment?.details;
  if (!details || details.length === 0) return 0;

  const total = details.reduce((sum, d) => {
    return sum + Math.round((d.installmentPercentage || 0) * 100);
  }, 0);

  return total / 100;
});

const onChangePaymentTypeCode = (e?: 'PayType001' | 'PayType002') => {
  if (!e) {
    return;
  }

  if (e == 'PayType002' && body.value.detail.payment?.details) {
    body.value.detail.payment.details.splice(1);

    body.value.detail.payment.details.forEach(item => {
      item.installmentPercentage = 100;
    })
  }
};
</script>

<template>
  <Card v-if="body.detail.payment" :pt="{ root: { 'data-section-id': 'payment', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <div v-if="props.showContractDates" class="grid lg:grid-cols-3 gap-2 mt-8">
        <Datepicker label="วันที่เริ่มต้นสัญญา" v-model="body.contractSignedDate" />
        <Datepicker disabled label="จนถึง" v-model="body.contractEndDate" />
      </div>
      <div class="flex flex-col gap-4">
        <div class="grid lg:grid-cols-3 mt-5">
          <Select :disabled="props.disable" :options="store.dropdown.payTypeOptions" label="ประเภทการจ่ายเงิน"
            rules="required" v-model="body.detail.payment.paymentTypeCode"
            @update:model-value="(e) => onChangePaymentTypeCode(e)" />
        </div>
        <div v-if="body.template == ContractDraftTemplate.CFormat003" class="grid lg:grid-cols-3 gap-2">
          <InputNumber :disabled="props.disable" label="ผู้จะซื้อตกลงจะชำระเงินค่าสิ่งของให้แก่ผู้จะขายภายใน (วัน)"
            v-model="body.detail.payment.dueDay" rules="required" />
          <Select :disabled="props.disable" :options="store.dropdown.periodConditionTypeOptions" label="เงื่อนไข"
            v-model="body.detail.payment.redeliveryTypeCode" rules="required" />
        </div>
      </div>

      <div
        v-if="[ContractDraftTemplate.CMRentalTpl001, ContractDraftTemplate.CMRentalTpl002, ContractDraftTemplate.CMRentalTpl003, ContractDraftTemplate.CMRentalTpl004].includes(body.template) && body.detail.advancePayment">
        <p>6.1 เงินล่วงหน้า</p>
        <Radio :disabled="props.disable" label="การชำระเงินให้แก่ผู้ซื้อมีการจ่ายเงินล่วงหน้าหรือไม่"
          :options="buyerRadioOption" class="mt-2" v-model="body.detail.advancePayment.hasAdvancePayment" />
        <div v-if="body.detail.advancePayment.hasAdvancePayment">
          <div class="grid lg:grid-cols-3 gap-2">
            <InputNumber :disabled="props.disable" rules="required" label="จ่ายเงินล่วงหน้าจำนวน"
              v-model="body.detail.advancePayment.amount" grouping :min-fraction-digits="2"/>
          </div>
          <div class="grid lg:grid-cols-3 gap-2">
            <InputNumber :disabled="props.disable" rules="required" label="จ่ายให้ภายใน(วัน)"
              v-model="body.detail.advancePayment.dueDate" use-grouping />
            <Select :options="store.dropdown.periodConditionTypeOptions" :disabled="props.disable" rules="required"
              label="ตั้งแต่" v-model="body.detail.advancePayment.conditionCode" />
          </div>
        </div>
        <div v-if="body.detail.retentionPayment">
          <p>6.2 เงินที่เหลือ</p>
          <div class="grid lg:grid-cols-3 gap-2 mt-10">
            <InputNumber v-if="body.detail.retentionPayment" :disabled="props.disable" label="เงินที่เหลือ จำนวน"
              v-model="body.detail.retentionPayment.amount" :min-fraction-digits="2" rules="required" grouping />
          </div>
        </div>
      </div>

      <TitleHeader label="โดยมีรายละเอียดการมอบดังนี้" :hidden-icon="true">
        <template #action>
          <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined" @click="addPaymentDetail"
            v-if="!props.disable" />
        </template>
      </TitleHeader>

      <p v-if="body.detail.payment.details?.length == 0" class="text-center">ไม่พบข้อมูล</p>
      <!-- Summary bar -->
      <div v-if="body.detail.payment.details && body.detail.payment.details.length > 0"
        class="flex items-center gap-3 rounded-lg bg-gray-50 border border-gray-200 px-4 py-3 mt-4">
        <div class="flex-1">
          <div class="h-2.5 w-full rounded-full bg-gray-200 overflow-hidden">
            <div class="h-full rounded-full transition-all duration-300"
              :class="getTotalPercent === 100 ? 'bg-green-500' : getTotalPercent > 100 ? 'bg-red-500' : 'bg-amber-500'"
              :style="{ width: Math.min(getTotalPercent, 100) + '%' }" />
          </div>
        </div>
        <span class="text-sm font-semibold whitespace-nowrap"
          :class="getTotalPercent === 100 ? 'text-green-600' : getTotalPercent > 100 ? 'text-red-500' : 'text-amber-600'">
          {{ getTotalPercent.toFixed(2) }}%
        </span>
        <span v-if="getTotalPercent === 100"
          class="material-symbols-outlined text-green-600 text-lg">check_circle</span>
        <span v-else class="material-symbols-outlined text-amber-500 text-lg">warning</span>
        <span class="text-xs text-gray-500">{{ body.detail.payment.details.length }} รายการ</span>
      </div>

      <draggable v-model="body.detail.payment.details" group="files" class="mt-2" handle=".drag-handle"
        item-key="sequence" @end="reSequenceData">
        <template #item="{ element: data, index: index }">
          <div class="relative flex p-4 pt-10 bg-gray-100 rounded-2xl flex-col mb-4 gap-2">
            <div class="absolute top-3 right-3 flex items-center gap-4" v-if="!props.disable">
              <i class="pi pi-trash cursor-pointer text-red-500 text-lg"
                v-if="body.detail.payment.details && body.detail.payment.details?.length > 1"
                @click="() => deleteItem(index)"></i>
              <span class="material-symbols-outlined drag-handle cursor-move">
                drag_indicator
              </span>
            </div>
            <div class="grid lg:grid-cols-4 gap-y-8 gap-2">
              <InputNumber :disabled="props.disable" label="งวดที่" v-model="data.no" rules="required" :key="index" />
              <InputNumber :disabled="props.disable" label="ระยะเวลา" v-model="data.leadTime" rules="required"
                :key="index" />
              <Select :disabled="props.disable" :options="store.dropdown.periodTypeOptions"
                v-model="data.periodTypeCode" rules="required" :key="index" />
              <Datepicker disabled label="วันที่ต้องส่งมอบ" v-model="data.deliveryDate" />
              <InputNumber :disabled="props.disable" label="ร้อยละ" :max-number="100" :min-fraction-digits="2"
                v-model="data.installmentPercentage" @update:model-value="() => onPercentageChange(data)"
                rules="required" :key="index" />
              <InputNumber :disabled="props.disable" label="จำนวนเงิน" :min-fraction-digits="2" v-model="data.amount"
                @update:model-value="() => onAmountChange(data)" grouping rules="required" :key="index" />
              <InputNumber :disabled="props.disable" label="หักเงินล่วงหน้า" :min-fraction-digits="2"
                v-model="data.advanceDeductionAmount" grouping :key="index" />
              <InputNumber :disabled="props.disable" label="หักเงินประกันผลงาน" :min-fraction-digits="2"
                v-model="data.performanceDeductionAmount" grouping :key="index" />
            </div>
            <InputArea :disabled="props.disable" label="รายละเอียดการส่งมอบ" v-model="data.description" rules="required"
              :key="index" />
          </div>
        </template>
      </draggable>
    </template>
  </Card>
</template>
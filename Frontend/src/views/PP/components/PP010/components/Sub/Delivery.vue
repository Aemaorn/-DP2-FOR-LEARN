<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputArea,
  Datepicker,
} from '@/components/forms';
import { ContractDraftTemplate } from '@/enums/contractDraftt';
import { ArrayHelper } from '@/helpers/array';
import type { TContractDraftBody, TPaymentBase, TPaymentTermDetail } from '@/views/PP/models/PP0010/ContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { computed } from 'vue';
import draggable from 'vuedraggable';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const store = useContractDraftStore();

const body = defineModel<TContractDraftBody>("body", { required: true });

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
</script>

<template>
  <Card v-if="body.detail.delivery"
    :pt="{ root: { 'data-section-id': 'delivery', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <div
        v-if="[ContractDraftTemplate.CFormat002, ContractDraftTemplate.CMRentalTpl001, ContractDraftTemplate.CMRentalTpl002, ContractDraftTemplate.CMRentalTpl003, ContractDraftTemplate.CMRentalTpl004].includes(body.template)"
        class="flex flex-col gap-8 mt-5">
        <InputArea :disabled="props.disable" label="สถานที่ส่งมอบที่ปรากฎตามสัญญา"
          v-model="body.detail.delivery.address" class="mt-5" rules="required" />
        <div class="grid lg:grid-cols-3 gap-2">
          <Datepicker :disabled="props.disable" label="ผู้ขายจะส่งมอบของภายในวันที่" v-model="body.detail.delivery.date"
            rules="required" />
        </div>
        <div class="grid lg:grid-cols-3 gap-2">
          <InputNumber :disabled="props.disable" label="ผู้ขายจะส่งมอบของภายใน (วัน)"
            v-model="body.detail.delivery.leadTime" />
          <Select v-model="body.detail.delivery.periodTypeCode" :options="store.dropdown.periodOptions"
            :disabled="props.disable" />
          <Select :disabled="props.disable" :options="store.dropdown.conditionTypeOptions" label="เงื่อนไขการนับ"
            v-model="body.detail.delivery.leadTimeTypeCode" />
        </div>
      </div>

      <div v-if="body.template == ContractDraftTemplate.CFormat004" class="flex flex-col gap-4 mt-5">
        <InputArea :disabled="props.disable" label="สถานที่ส่งมอบที่ปรากฎตามสัญญา"
          v-model="body.detail.delivery.address" class="mt-5" rules="required" />
        <div class="grid lg:grid-cols-3 gap-2 mt-4">
          <InputNumber :disabled="props.disable" label="ผู้ขายจะส่งมอบของภายใน (วัน)"
            v-model="body.detail.delivery.leadTime" />
          <Select v-model="body.detail.delivery.periodTypeCode" :options="store.dropdown.periodOptions"
            :disabled="props.disable" />
          <Select :disabled="props.disable" :options="store.dropdown.conditionTypeOptions" label="เงื่อนไขการนับ"
            v-model="body.detail.delivery.leadTimeTypeCode" />
        </div>
        <p class="text-2xl font-bold ml-1">การแจ้งกำหนดเวลาการส่งมอบ</p>
        <div class="grid lg:grid-cols-3 gap-2 mt-2">
          <InputNumber :disabled="props.disable" label="ออกแบบสถานะที่ ติดตั้ง และระบบอื่นๆภายใน"
            v-model="body.detail.delivery.leadOtherTime" rules="required" />
          <Select :disabled="props.disable" :options="store.dropdown.periodTypeOptions" label="ระยะเวลา"
            v-model="body.detail.delivery.leadOtherTimeTypeCode" rules="required" />
        </div>
      </div>

      <div v-if="body.template == ContractDraftTemplate.CFormat005" class="flex flex-col gap-8 mt-5">
        <InputArea :disabled="props.disable" label="สถานที่ส่งมอบที่ปรากฎตามสัญญา"
          v-model="body.detail.delivery.address" class="mt-5" rules="required" />
        <div class="grid lg:grid-cols-3 gap-2">
          <InputNumber :disabled="props.disable" label="ผู้ขายจะส่งมอบของภายใน (วัน)"
            v-model="body.detail.delivery.leadTime" />
          <Select v-model="body.detail.delivery.periodTypeCode" :options="store.dropdown.periodOptions"
            :disabled="props.disable" />
          <Select :disabled="props.disable" :options="store.dropdown.conditionTypeOptions" label="เงื่อนไขการนับ"
            v-model="body.detail.delivery.leadTimeTypeCode" />
        </div>
      </div>

      <div v-if="body.template == ContractDraftTemplate.CFormat011" class="flex flex-col gap-8 mt-5">
        <InputArea :disabled="props.disable" label="สถานที่ส่งมอบที่ปรากฎตามสัญญา"
          v-model="body.detail.delivery.address" class="mt-5" rules="required" />
        <div class="grid lg:grid-cols-3 gap-2">
          <Datepicker :disabled="props.disable" label="ผู้ให้เช่าต้องส่งมอบและติดตั้งภายในวันที่"
            v-model="body.detail.delivery.date" rules="required" />
        </div>
      </div>

      <div v-if="body.template == ContractDraftTemplate.CFormat012" class="flex flex-col gap-8 mt-5">
        <InputArea :disabled="props.disable" label="สถานที่ส่งมอบที่ปรากฎตามสัญญา"
          v-model="body.detail.delivery.address" class="mt-5" rules="required" />
        <div class="grid lg:grid-cols-3 gap-2">
          <Datepicker :disabled="props.disable" label="ผู้รับแลกเปลี่ยนจะส่งมอบของภายในวันที่"
            v-model="body.detail.delivery.date" rules="required" />
        </div>
        <div v-if="body.detail.payment">
          <TitleHeader label="โดยมีรายละเอียดการมอบดังนี้" class="mt-2" :hidden-icon="true">
            <template #action>
              <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
                @click="addPaymentDetail" v-if="!props.disable" />
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

          <draggable v-model="body.detail.payment.details" group="files" class="mt-4" handle=".drag-handle"
            item-key="sequence" @end="reSequenceData" tag="div">
            <template #item="{ element: data, index: index }">
              <div class="relative flex p-4 pt-10 bg-gray-100 rounded-2xl flex-col gap-2 mt-4">
                <div class="absolute top-3 right-3 flex items-center gap-4" v-if="!props.disable">
                  <i class="pi pi-trash cursor-pointer text-red-500 text-lg" @click="() => deleteItem(index)"
                    v-if="index != 0"></i>
                  <span class="material-symbols-outlined drag-handle cursor-move">
                    drag_indicator
                  </span>
                </div>
                <div class="grid lg:grid-cols-8 gap-y-8 gap-2">
                  <InputNumber :disabled="props.disable" label="งวดที่" v-model="data.no" rules="required" />
                  <InputNumber :disabled="props.disable" label="ระยะเวลา" v-model="data.leadTime" rules="required" />
                  <Datepicker disabled label="วันที่ต้องส่งมอบ" v-model="data.deliveryDate" />
                  <InputNumber :disabled="props.disable" :max-number="100" :min-fraction-digits="2"
                    :max-fraction-digits="3" label="ร้อยละ" v-model="data.installmentPercentage"
                    @update:model-value="() => onPercentageChange(data)" rules="required" />
                  <InputNumber :disabled="props.disable" :min-fraction-digits="2" label="จำนวนเงิน"
                    v-model="data.amount" @update:model-value="() => onAmountChange(data)" grouping rules="required" />
                  <InputNumber :disabled="props.disable" :min-fraction-digits="2" label="หักเงินล่วงหน้า"
                    v-model="data.advanceDeductionAmount" grouping rules="required" />
                  <InputNumber :disabled="props.disable" :min-fraction-digits="2" label="หักเงินประกันผลงาน"
                    v-model="data.performanceDeductionAmount" grouping rules="required" />
                </div>
                <InputArea :disabled="props.disable" label="รายละเอียดการส่งมอบ" v-model="data.description"
                  rules="required" class="mt-8" />
              </div>
            </template>
          </draggable>
        </div>
      </div>

      <div v-if="body.template == ContractDraftTemplate.CFormat006" class="flex flex-col gap-8 mt-5">
        <InputArea :disabled="props.disable" label="สถานที่ส่งมอบคอมพิวเตอร์ ณ" v-model="body.detail.delivery.address"
          class="mt-5" rules="required" />
        <div class="grid lg:grid-cols-3 gap-2 gap-y-8">
          <InputNumber :disabled="props.disable" label="ผู้ให้เช่าจะส่งมอบของภายใน"
            v-model="body.detail.delivery.leadOtherTime" rules="required" />
          <Select :disabled="props.disable" :options="store.dropdown.periodTypeOptions"
            v-model="body.detail.delivery.leadTimeTypeCode" />
          <Select :disabled="props.disable" :options="store.dropdown.conditionTypeOptions" label="เงื่อนไขการนับ"
            v-model="body.detail.delivery.countingConditionCode" />
        </div>
        <div v-if="body.detail.payment">
          <TitleHeader label="โดยมีรายละเอียดการมอบดังนี้" :hidden-icon="true">
            <template #action>
              <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
                @click="addPaymentDetail" v-if="!props.disable" />
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

          <draggable v-model="body.detail.payment.details" group="files" class="mt-4" handle=".drag-handle"
            item-key="sequence" @end="reSequenceData">
            <template #item="{ element: data, index: index }">
              <div class="relative flex p-4 pt-10 bg-gray-100 rounded-2xl flex-col gap-2 mt-4">
                <div class="absolute top-3 right-3 flex items-center gap-4" v-if="!props.disable">
                  <i class="pi pi-trash cursor-pointer text-red-500 text-lg" @click="() => deleteItem(index)"
                    v-if="index != 0"></i>
                  <span class="material-symbols-outlined drag-handle cursor-move">
                    drag_indicator
                  </span>
                </div>
                <div class="grid lg:grid-cols-8 gap-y-8 gap-2">
                  <InputNumber :disabled="props.disable" label="งวดที่" v-model="data.no" rules="required" />
                  <InputNumber :disabled="props.disable" label="ระยะเวลา" v-model="data.leadTime" rules="required" />
                  <Datepicker disabled label="วันที่ต้องส่งมอบ" v-model="data.deliveryDate" />
                  <InputNumber :disabled="props.disable" :max-number="100" :min-fraction-digits="2"
                    :max-fraction-digits="3" label="ร้อยละ" v-model="data.installmentPercentage"
                    @update:model-value="() => onPercentageChange(data)" rules="required" />
                  <InputNumber :disabled="props.disable" :min-fraction-digits="2" label="จำนวนเงิน"
                    v-model="data.amount" @update:model-value="() => onAmountChange(data)" grouping rules="required" />
                  <InputNumber :disabled="props.disable" :min-fraction-digits="2" label="หักเงินล่วงหน้า"
                    v-model="data.advanceDeductionAmount" grouping />
                  <InputNumber :disabled="props.disable" :min-fraction-digits="2" label="หักเงินประกันผลงาน"
                    v-model="data.performanceDeductionAmount" grouping />
                </div>
                <InputArea :disabled="props.disable" label="รายละเอียดการส่งมอบ" v-model="data.description"
                  rules="required" class="mt-8" />
              </div>
            </template>
          </draggable>
        </div>
      </div>
    </template>
  </Card>
</template>
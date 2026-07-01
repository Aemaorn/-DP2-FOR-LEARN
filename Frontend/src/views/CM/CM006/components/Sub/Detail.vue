<script setup lang="ts">
import { InputNumber, Checkbox, InputArea } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { watch } from 'vue';
import { useCm006DetailStore } from '@/stores/CM/CM006/cm006.detail';
import { storeToRefs } from 'pinia';
import { formatCurrency } from '@/helpers/currency';
import { Cm006Status } from '@/enums/CM/cm006';

const store = useCm006DetailStore();
const { body: value } = storeToRefs(store);
const { isCanEdit } = storeToRefs(store);

watch(() => [value.value.guaranteeReturn.returnAmount, value.value.guaranteeReturn.deductedAmount], () => {
  value.value.guaranteeReturn.netReturnAmount = (value.value.guaranteeReturn.returnAmount ?? 0) - (value.value.guaranteeReturn.deductedAmount ?? 0);
}, {
  immediate: true
});
</script>

<template>
  <Card class="my-2">
    <template #content>
      <TitleHeader label="รายละเอียดการวางประกันสัญญา" />
      <div class="p-2 bg-gray-100 lg:flex justify-between items-center gap-2">
        <p>
          {{ value.guaranteeReturn.guaranteeReturnDescription }}
        </p>
      </div>
    </template>
  </Card>
  <Card class="my-2">
    <template #content>
      <TitleHeader label="บันทึกผลการพิจารณาคืนหลักประกันสัญญา" />
      <div v-for="(item, index) in value.guaranteeReturn.conditions" :key="index" class="py-1">
        <Checkbox v-model="item.isSatisfied" :label="item.description" :disabled="!isCanEdit" />
      </div>
      <InputArea label="ความเห็นเพิ่มเติม" v-model="value.guaranteeReturn.additionalComment" class="mt-6"
        :disabled="!isCanEdit" />
    </template>
  </Card>
  <Card class="my-2" id="guarantee-return-detail-section">
    <template #content>
      <TitleHeader label="รายละเอียดการคืนหลักประกันสัญญา" />
      <div class="grid grid-cols-1 gap-4 gap-y-8 mt-10">
        <template
          v-if="[Cm006Status.Assigned, Cm006Status.WaitingAcceptance, Cm006Status.Approved].includes(value.guaranteeReturn.status)">
          <InputArea v-model="value.guaranteeReturn.contractDescription" rules="required"
            :disabled="!store.isCanAssign && !store.isConfirmAssigned" label="สัญญาเลขที่และรายละเอียด" />

          <InputArea v-model="value.guaranteeReturn.proofOfPaymentDescription" rules="required"
            label="หลักฐานการจ่ายเงิน (งวดสุดท้ายหรือทั้งหมด)"
            :disabled="!store.isCanAssign && !store.isConfirmAssigned" />

          <InputArea v-model="value.guaranteeReturn.guranteeDescription" rules="required"
            :disabled="!store.isCanAssign && !store.isConfirmAssigned" label="หลักประกันสัญญา" />
        </template>
      </div>
      <div class="p-2 lg:flex justify-between items-center gap-2">
        <div class="flex-2/3">
          <Checkbox v-model="value.guaranteeReturn.isDeducted" label="หักค่าปรับจากเงินหลักประกันสัญญา"
            :disabled="!store.isCanAssign && !store.isConfirmAssigned" />
        </div>
        <div class="flex-1/3">
          <InputNumber v-model="value.guaranteeReturn.deductedAmount"
            :rules="`${value.guaranteeReturn.isDeducted ? 'required' : ''}`"
            :disabled="!(value.guaranteeReturn.isDeducted && value.guaranteeReturn.returnAmount) || (!store.isCanAssign && !store.isConfirmAssigned)"
            :min-fraction-digits="2" grouping :maxNumber="value.guaranteeReturn.returnAmount" />
        </div>
      </div>

      <div class="flex flex-col w-full items-end mt-8">
        <div class="flex items-center gap-4 text-2xl font-bold mb-2">
          <span class="text-right">รวมจำนวนเงิน</span>
          <span class="min-w-[150px] text-right">{{ formatCurrency(value.guaranteeReturn.returnAmount || 0) }}</span>
        </div>

        <div v-if="value.guaranteeReturn.isDeducted"
          class="flex items-center gap-4 text-xl font-bold text-primary-500 mb-4">
          <span class="text-right">ค่าปรับ</span>
          <span class="min-w-[150px] text-right">{{ formatCurrency(value.guaranteeReturn.deductedAmount || 0) }}</span>
        </div>

        <div class="flex items-center gap-4 text-xl font-bold border-t border-gray-300 pt-4">
          <span class="text-right">รวมคืนหลักประกันสัญญาคงเหลือ</span>
          <span class="min-w-[150px] text-right">{{ formatCurrency((value.guaranteeReturn.returnAmount || 0) -
            (value.guaranteeReturn.deductedAmount || 0)) }}</span>
        </div>
      </div>
    </template>
  </Card>
</template>
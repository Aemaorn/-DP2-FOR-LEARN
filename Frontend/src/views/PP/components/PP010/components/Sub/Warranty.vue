<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputNumber, Select } from '@/components/forms';
import { ContractDraftTemplate } from '@/enums/contractDraftt';
import type { TContractDraftBody, TWarrantyInfo } from '@/views/PP/models/PP0010/ContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { onBeforeMount, watch } from 'vue';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const warrantyRadioOption = [
  { value: true, label: 'มี' },
  { value: false, label: 'ไม่มี' },
];

const body = defineModel<TContractDraftBody>("body", { required: true });

const store = useContractDraftStore();

const initalWarrantyByTemplate = () => {
  switch (body.value.template) {
    case ContractDraftTemplate.CFormat004:
      body.value.detail.warranty = {
        ...body.value.detail.warranty,
        warrantyPeriod: body.value.detail.warranty?.warrantyPeriod || {},
        fixingDeadlinePeriod: body.value.detail.warranty?.fixingDeadlinePeriod || {},
      } as TWarrantyInfo;

      break;
    // For more Case in future.

    default:
      break;
  }
}

watch(() => body.value.detail.warranty?.hasWarranty, (newValue) => {
  if (!newValue) {
    body.value.detail.warranty = {
      ...body.value.detail.warranty,
      hasWarranty: false,
      warrantyPeriod: {},
      fixingDeadlinePeriod: {},
      warrantyConditionCode: undefined,
    } as TWarrantyInfo;
  };

  if (newValue && body.value.detail.warranty) {
    if (!body.value.detail.warranty.warrantyPeriod) {
      body.value.detail.warranty.warrantyPeriod = {} as TWarrantyInfo['warrantyPeriod'];
    }
    if (!body.value.detail.warranty.fixingDeadlinePeriod) {
      body.value.detail.warranty.fixingDeadlinePeriod = {} as TWarrantyInfo['fixingDeadlinePeriod'];
    }
    initalWarrantyByTemplate();
  }
}, { immediate: true });

onBeforeMount(async () => {
  if (body.value.detail.warranty) {
    initalWarrantyByTemplate();
  }

  await store.api.getWarrantyTypeAsync();
  await store.api.getPTimeTypeAsync();
  await store.api.getPeriodAsync();
})
</script>

<template>
  <Card v-if="body.detail.warranty"
    :pt="{ root: { 'data-section-id': 'warranty', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <Radio :disabled="props.disable" :options="warrantyRadioOption" v-model="body.detail.warranty.hasWarranty" />

      <div class="flex flex-col gap-10 mt-5">
        <section v-if="body.template == ContractDraftTemplate.CFormat004">
          <div class="flex flex-col gap-6">
            <div v-if="body.detail.warranty.hasWarranty">
              <span class="text-xl">ระยะเวลาการรับประกันความชำรุดบกพร่องหรือขัดข้อง</span>
              <div class="flex gap-4 mt-4">
                <InputNumber class="w-48" :disabled="props.disable" label="ปี" input-class="text-end"
                  v-model="body.detail.warranty.warrantyPeriod.year" />
                <InputNumber class="w-48" :disabled="props.disable" label="เดือน" input-class="text-end"
                  v-model="body.detail.warranty.warrantyPeriod.month" />
                <InputNumber class="w-48" :disabled="props.disable" label="วัน" input-class="text-end"
                  v-model="body.detail.warranty.warrantyPeriod.day" />
              </div>
            </div>

            <div>
              <span class="text-xl">ระยะเวลาให้แก้ไข ซ่อมแซมให้ดีดังเดิมภายใน</span>
              <div class="flex gap-4 mt-4">
                <InputNumber class="w-48" :disabled="props.disable" label="วัน" input-class="text-end"
                  v-model="body.detail.warranty.fixingDeadlinePeriod.day" />
              </div>
            </div>

            <div>
              <span class="text-xl">ระยะเวลาขัดข้องรวมไม่เกิน เดือนละ</span>
              <div class="flex items-end gap-4 mt-4">
                <InputNumber class="w-40" :disabled="props.disable" label="ชั่วโมง" input-class="text-end"
                  v-model="body.detail.warranty.warrantyMonthlyAllowedDowntimeHours" hide-details />
                <InputNumber class="w-48" :disabled="props.disable" label="หรือร้อยละ" input-class="text-end"
                  v-model="body.detail.warranty.warrantyDowntimePercentPerMonth" :max-number="100" grouping :min-fraction-digits="2"
                  :max-fraction-digits="3" hide-details />
                <span class="text-xl whitespace-nowrap mb-2">ของเวลาใช้งานทั้งหมด</span>
              </div>
            </div>

            <div>
              <span class="text-xl">คิดอัตราค่าปรับรายชั่วโมง ในอัตราร้อยละ</span>
              <div class="flex gap-4 mt-4">
                <InputNumber class="w-48" :disabled="props.disable" input-class="text-end" :max-number="100"
                  v-model="body.detail.warranty.maxMonthlyMalfunctionPenaltyPercentageRate" grouping :min-fraction-digits="2"
                  :max-fraction-digits="3" />
              </div>
            </div>

            <div>
              <span class="text-xl">คิดอัตราค่าปรับชั่วโมงละ</span>
              <div class="flex gap-4 mt-4">
                <InputNumber class="w-48" :disabled="props.disable" input-class="text-end" grouping :min-fraction-digits="2"
                  v-model="body.detail.warranty.warrantyPenaltyPerHour" hide-details />
                <span class="text-xl whitespace-nowrap mb-2">บาท</span>
              </div>
            </div>

            <div>
              <span class="text-xl">ชำระค่าปรับภายใน</span>
              <div class="flex gap-4 mt-4">
                <InputNumber class="w-48" :disabled="props.disable" label="วัน" input-class="text-end"
                  v-model="body.detail.warranty.maxMonthlyMalfunctionPenaltyDueDays" hide-details />
                <span class="text-xl whitespace-nowrap mb-2">นับแต่วันที่ได้รับแจ้ง</span>
              </div>
            </div>
          </div>
        </section>

        <section v-else>
          <div
            v-if="body.detail.warranty.hasWarranty && body.detail.warranty.warrantyPeriod && ![ContractDraftTemplate.CMRentalTpl001, ContractDraftTemplate.CMRentalTpl002, ContractDraftTemplate.CMRentalTpl003, ContractDraftTemplate.CMRentalTpl004, ContractDraftTemplate.CFormat011, ContractDraftTemplate.CFormat007].includes(body.template)"
            class="flex flex-col gap-6">
            <span class="text-xl">ระยะเวลาการรับประกันความชำรุดบกพร่องหรือขัดข้อง</span>
            <div class="flex gap-4">
              <InputNumber class="w-48" :disabled="props.disable" label="ปี" input-class="text-end"
                v-model="body.detail.warranty.warrantyPeriod.year" />
              <InputNumber class="w-48" :disabled="props.disable" label="เดือน" input-class="text-end"
                v-model="body.detail.warranty.warrantyPeriod.month" />
              <InputNumber class="w-48" :disabled="props.disable" label="วัน" input-class="text-end"
                v-model="body.detail.warranty.warrantyPeriod.day" />
            </div>
          </div>

          <div v-if="body.detail.warranty.warrantyPeriod && body.template === ContractDraftTemplate.CFormat007"
            class="flex flex-col gap-6">
            <section class="flex flex-col gap-4">
              <span class="text-xl">ต้องครวจสอบบำรุงรักษาคอมพิวเตอร์อย่างน้อย</span>
              <div class="flex items-end gap-4">
                <InputNumber class="w-48" :disabled="props.disable" label="จำนวน" input-class="text-end"
                  v-model="body.detail.warranty.warrantyMaintenanceCount" hide-details />
                <Select class="w-48" :disabled="props.disable" label="หน่วย"
                  :options="store.dropdown.periodTypeOptions" v-model="body.detail.warranty.warrantyMaintenanceTypeCode"
                  hide-details />
              </div>
            </section>

            <section class="flex flex-col gap-4">
              <span class="text-xl">คอมพิวเตอร์ขัดข้อง ใช้การไม่ได้ตามปกติ ต้องจัดการซ่อมแซมแก้ไขฯ ภายใน</span>
              <div class="flex items-end gap-4">
                <InputNumber class="w-48" :disabled="props.disable" label="ชั่วโมง" input-class="text-end"
                  v-model="body.detail.warranty.downtimeResolutionHours" hide-details />
                <InputNumber class="w-48" :disabled="props.disable" label="วัน" input-class="text-end"
                  v-model="body.detail.warranty.downtimeResolutionDay" hide-details />
                <span class="text-xl whitespace-nowrap mb-2">นับตั้งแต่เวลาที่ได้รับแจ้ง</span>
              </div>
            </section>

            <section class="flex flex-col gap-4">
              <span class="text-xl">ผู้รับจ้างจะต้องซ่อมแซมแก้ไข หรือเปลี่ยนสิ่งที่จำเป็นให้เสร็จเรียบร้อย ภายใน</span>
              <div class="flex items-end gap-4">
                <InputNumber class="w-48" :disabled="props.disable" label="ชั่วโมง" input-class="text-end"
                  v-model="body.detail.warranty.repairCompletionHours" hide-details />
                <InputNumber class="w-48" :disabled="props.disable" label="วัน" input-class="text-end"
                  v-model="body.detail.warranty.repairCompletionDay" hide-details />
                <span class="text-xl whitespace-nowrap mb-2">นับตั้งแต่เวลาที่ได้รับแจ้ง</span>
              </div>
            </section>

            <section class="flex flex-col gap-4">
              <span class="text-xl">ผู้รับจ้างไม่เข้ามาซ่อมแซมแก้ไขภายในวันกำหนด</span>
              <div class="flex items-end gap-4">
                <InputNumber class="w-48" :disabled="props.disable" label="คิดค่าปรับในอัตราร้อยละ" input-class="text-end"
                  v-model="body.detail.warranty.repairDelayPenaltyPercentPerHour" hide-details grouping :min-fraction-digits="2"/>
                <span class="text-xl whitespace-nowrap mb-2">ต่อชั่วโมง ของค่าจ้าง (รายงวด)</span>
              </div>
            </section>
          </div>
          <div v-if="body.detail.warranty.warrantyPeriod
            && [ContractDraftTemplate.CMRentalTpl001, ContractDraftTemplate.CMRentalTpl002, ContractDraftTemplate.CMRentalTpl003, ContractDraftTemplate.CMRentalTpl004].includes(body.template)
            && body.template != ContractDraftTemplate.CFormat007" class="flex flex-col gap-6">
            <span class="text-xl">ระยะเวลาการรับประกันความชำรุดบกพร่องหรือขัดข้อง</span>
            <div class="flex gap-4">
              <InputNumber class="w-48" :disabled="props.disable" label="ปี" input-class="text-end"
                v-model="body.detail.warranty.warrantyPeriod.year" />
              <InputNumber class="w-48" :disabled="props.disable" label="เดือน" input-class="text-end"
                v-model="body.detail.warranty.warrantyPeriod.month" />
              <InputNumber class="w-48" :disabled="props.disable" label="วัน" input-class="text-end"
                v-model="body.detail.warranty.warrantyPeriod.day" />
              <Select class="w-48" :disabled="props.disable" label="เงื่อนไข" :options="store.dropdown.warrantyOptions"
                v-model="body.detail.warranty.warrantyConditionCode" />
            </div>
          </div>

          <div v-if="body.template != ContractDraftTemplate.CFormat007" class="flex flex-col gap-6 mt-5">
            <span class="text-xl">ระยะเวลาให้แก้ไข ภายในกำหนด</span>
            <div class="flex gap-4">
              <InputNumber class="w-48" :disabled="props.disable" label="ปี" input-class="text-end"
                v-model="body.detail.warranty.fixingDeadlinePeriod.year" />
              <InputNumber class="w-48" :disabled="props.disable" label="เดือน" input-class="text-end"
                v-model="body.detail.warranty.fixingDeadlinePeriod.month" />
              <InputNumber class="w-48" :disabled="props.disable" label="วัน" input-class="text-end"
                v-model="body.detail.warranty.fixingDeadlinePeriod.day" />
            </div>
          </div>
        </section>
      </div>
    </template>
  </Card>
</template>
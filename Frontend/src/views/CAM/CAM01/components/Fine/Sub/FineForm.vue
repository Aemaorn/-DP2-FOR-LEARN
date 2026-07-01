<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Select, InputNumber, Checkbox } from '@/components/forms';
import { useCam01FineStore } from '@/stores/CAM/CAM01/Fine/cam01.fine';
import { storeToRefs } from 'pinia';
import type { Cam01FineBody } from '@/models/CAM/CAM01/cam.fine';

const store = useCam01FineStore();
const { options, isCanEdit } = storeToRefs(store);

const value = defineModel<Cam01FineBody>({
  required: true,
});

const onChangeIsWaive = (val: boolean) => {
  if (val) {
    value.value.penaltyNew.penaltyTypeCode = undefined;
    value.value.penaltyNew.rate = undefined;
    value.value.penaltyNew.amount = undefined;
    value.value.penaltyNew.rateTypeCode = undefined;
  }
};
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="รายละเอียดแก้ไขสัญญา" />
      <div class="my-2">
        <div class="bg-gray-50 py-2 px-4">
          <TitleHeader label="(จากเดิม)" :hidden-icon="true" />
          <div class="grid lg:grid-cols-3 gap-2">
            <Select label="ประเภทค่าปรับ" :options="options.fineType" v-model="value.penaltyOld.penaltyTypeCode"
              disabled />
            <InputNumber label="ค่าปรับอัตราร้อยละ" class="lg:col-start-1" v-model="value.penaltyOld.rate" grouping
              :min-fraction-digits="2" :max-fraction-digits="3" disabled />
            <InputNumber label="อัตราค่าปรับคันละ" v-model="value.penaltyOld.amount" grouping :min-fraction-digits="2"
              disabled />
            <Select label="ต่อ" :options="options.period" v-model="value.penaltyOld.rateTypeCode" disabled />
          </div>
        </div>

        <div class="mt-2 bg-gray-50 py-2 px-4">
          <TitleHeader label="(แก้ไขเป็น)" :hidden-icon="true" />
          <div class="grid grid-cols-3 gap-2">
            <Checkbox label="งดเว้นค่าปรับทั้งหมด" v-model="value.waiveAll" @onChange="onChangeIsWaive" class="mb-4"
              :disabled="!isCanEdit" />

            <Select label="ประเภทค่าปรับ" :options="options.fineType" v-model="value.penaltyNew.penaltyTypeCode"
              class="col-start-1" :disabled="!isCanEdit || value.waiveAll"
              :rules="`${!value.waiveAll ? 'required' : ''}`" />
            <InputNumber :currency-display="100" label="ค่าปรับอัตราร้อยละ" v-model="value.penaltyNew.rate"
              class="col-start-1" :disabled="!isCanEdit || value.waiveAll" grouping :min-fraction-digits="2" :max-fraction-digits="3"
              :rules="`${!value.waiveAll ? 'required' : ''}`" />
            <InputNumber label="อัตราค่าปรับคันละ" v-model="value.penaltyNew.amount" :min-fraction-digits="2"
              :disabled="!isCanEdit || value.waiveAll" grouping :rules="`${!value.waiveAll ? 'required' : ''}`" />
            <Select label="ต่อ" :options="options.period" v-model="value.penaltyNew.rateTypeCode"
              :disabled="!isCanEdit || value.waiveAll" :rules="`${!value.waiveAll ? 'required' : ''}`" />
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>
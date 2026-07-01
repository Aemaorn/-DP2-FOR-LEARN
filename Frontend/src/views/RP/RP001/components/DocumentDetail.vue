<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Datepicker } from '@/components/forms';
import { useRp001DetailStore } from '@/stores/RP/rp001';
import { watch } from 'vue';

const store = useRp001DetailStore();

watch(
  [() => store.body.signStartDate, () => store.body.signEndDate],
  async ([startDate, endDate]) => {
    if (startDate && endDate) {
      store.body.details = [];
      await store.api.getContractDraftVendorList();
    }
  }
);
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="ข้อมูลเอกสาร" />
      <div class="grid lg:grid-cols-3 gap-2 gap-y-8 mt-10">
        <InputField disabled label="เลขที่เอกสาร" v-model="store.body.documentNumber" />
        <Datepicker :disabled="!store.state.canEdit" rules="required" label="วันที่ทำเอกสาร"
          v-model="store.body.documentDate" />

        <Datepicker :disabled="!store.state.canEdit" rules="required" label="วันที่ส่งรายงาน"
          v-model="store.body.deliveryDate" />

        <Datepicker rules="required" :disabled="!store.state.canEdit"
          label="วันที่ลงนามในสัญญา ตั้งแต่" v-model="store.body.signStartDate" :max-date="store.body.signEndDate" />
        <Datepicker rules="required" :disabled="!store.state.canEdit" label="ถึงวันที่"
          v-model="store.body.signEndDate" :min-date="store.body.signStartDate" />
      </div>

    </template>
  </Card>
</template>
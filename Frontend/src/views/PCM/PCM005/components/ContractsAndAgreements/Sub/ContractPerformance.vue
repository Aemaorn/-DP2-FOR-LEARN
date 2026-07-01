<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
} from '@/components/forms';
import { ref } from 'vue';

const buyerRadioOption = [
  { value: 'y', label: 'ยื่น' },
  { value: 'n', label: 'ไม่ยื่น' },
];

const mock = ref();
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา" />
      <Radio label="ผู้ขายต้องยื่นหลักประกันสัญญาหรือไม่" :options="buyerRadioOption" class="mt-5" />
      <p>ระยะเวลาการรับประกันความชำรุดบกพร่องหรือขัดข้อง</p>
      <div class="grid lg:grid-cols-4 gap-2">
        <Select label="ประเภทหลักประกัน" :options="[]" v-model="mock" />
      </div>
      <p>ระยะเวลาให้แก้ไขซ่อมแซมให้ดีดังเดิมภายใน</p>
      <div class="grid lg:grid-cols-3 gap-2">
        <InputField label="จำนวนเงิน" v-model="mock" />
        <InputField label="ร้อยละ (ของราคาทั้งหมดตามสัญญา)" v-model="mock" />
      </div>
    </template>
  </Card>
</template>
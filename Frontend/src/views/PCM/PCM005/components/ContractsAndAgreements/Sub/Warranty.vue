<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
} from '@/components/forms';
import { ref } from 'vue';

const warrantyRadioOption = [
  { value: 'y', label: 'มี' },
  { value: 'n', label: 'ไม่มี' },
];

const mock = ref();
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="สัญญาข้อ 7 การรับประกันความชำรุดบกพร่อง" />
      <div class="grid lg:grid-cols-3 gap-2 mt-5">
        <Select label="ประเภทการจ่ายเงิน" :options="[]" v-model="mock" />
      </div>
      <Radio label="การชำระเงินให้แก่ผู้ซื้อมีการจ่ายเงินล่วงหน้าหรือไม่" :options="warrantyRadioOption" />
      <p>ระยะเวลาการรับประกันความชำรุดบกพร่องหรือขัดข้อง</p>
      <div class="grid lg:grid-cols-4 gap-2">
        <InputField v-model="mock" />
        <InputField v-model="mock" />
        <InputField v-model="mock" />
        <InputField v-model="mock" />
      </div>
      <p>ระยะเวลาให้แก้ไขซ่อมแซมให้ดีดังเดิมภายใน</p>
      <div class="grid lg:grid-cols-3 gap-2">
        <InputField v-model="mock" />
        <Select :options="[]" v-model="mock" />
        <Select :options="[]" v-model="mock" />
      </div>
    </template>
  </Card>
</template>
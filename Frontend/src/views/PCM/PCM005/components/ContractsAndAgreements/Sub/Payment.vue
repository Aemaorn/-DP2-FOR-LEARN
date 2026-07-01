<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
} from '@/components/forms';
import { ref } from 'vue';

const radioOption = [
  { value: 'y', label: 'จ่ายเงินล่วงหน้า' },
  { value: 'n', label: 'ไม่จ่ายเงินล่วงหน้า' },
];

const mock = ref();
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="สัญญาข้อ 6 การชำระเงิน" />
      <div class="grid lg:grid-cols-3 mt-5">
        <Select :options="[]" label="ประเภทการจ่ายเงิน" v-model="mock" />
      </div>
      <p class="mb-5">6.1 เงินล่วงหน้า</p>
      <Radio label="การชำระเงินให้แก่ผู้ซื้อมีการจ่ายเงินล่วงหน้าหรือไม่" :options="radioOption" />
      <div class="grid lg:grid-cols-3 gap-2">
        <InputField label="จ่ายเงินล่วงหน้าจำนวน" v-model="mock" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2">
        <InputField label="จะจ่ายให้ภายใน" v-model="mock" />
        <Select :options="[]" label="วัน" v-model="mock" />
      </div>
      <p class="mb-5">6.1 เงินที่เหลือ</p>
      <div class="grid lg:grid-cols-3 gap-2 mt-10">
        <InputField label="เงินที่เหลือจำนวน" v-model="mock" />
      </div>
      <p>โดยมีรายละเอียดการส่งมอบ ดังนี้</p>
      <div class="flex justify-end">
        <Button label="แก้ไขรายการ" severity="primary" variant="outlined" />
      </div>

      <DataTable :value="[]">
        <Column field="name">
          <template #header>
            <p class="w-full font-bold text-center">งวดเงิน</p>
          </template>
        </Column>
        <Column field="message">
          <template #header>
            <p class="w-full font-bold text-center">งวดงาน</p>
          </template>
        </Column>
        <Column field="count">
          <template #header>
            <p class="w-full font-bold text-center">ระยะเวลา(วัน)</p>
          </template>
        </Column>
        <Column field="file">
          <template #header>
            <p class="w-full font-bold text-center">วันที่ต้องส่งมอบ</p>
          </template>
        </Column>
        <Column field="file">
          <template #header>
            <p class="w-full font-bold text-center">ร้อยละ</p>
          </template>
        </Column>
        <Column field="file">
          <template #header>
            <p class="w-full font-bold text-center">จำนวนเงิน</p>
          </template>
        </Column>
        <Column field="file">
          <template #header>
            <p class="w-full font-bold text-center">หักเงินล่วงหน้า</p>
          </template>
        </Column>
        <Column field="file">
          <template #header>
            <p class="w-full font-bold text-center">หักเงินประกันผลงาน</p>
          </template>
        </Column>
        <Column field="file">
          <template #header>
            <p class="w-full font-bold text-center">รายละเอียด</p>
          </template>
        </Column>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataTable>
    </template>
  </Card>
</template>
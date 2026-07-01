<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
  InputArea,
} from '@/components/forms';
import { ref } from 'vue';

const mock = ref();
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="สัญญาข้อ 1 ข้อตกลงซื้อขาย" />
      <InputArea label="รายการสินค้าพัสดุที่ตกลงซื้อขาย" v-model="mock" class="mt-5" />
      <div class="grid lg:grid-cols-3 gap-2">
        <InputNumber label="จำนวน" v-model="mock" />
        <InputField v-model="mock" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2">
        <InputNumber label="อัตราภาษีมูลค่าเพิ่ม" v-model="mock" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2">
        <InputNumber label="ราคาที่ตกลงกัน (บาท)" v-model="mock" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2">
        <InputNumber label="ราคาที่ตกลงกัน (บาท)" v-model="mock" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2">
        <InputNumber label="รวมราคาทั้งสิ้น  (บาท)" v-model="mock" />
      </div>
    </template>
  </Card>
</template>
<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
} from '@/components/forms';
import { ref } from 'vue';

const mock = ref();
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="สัญญาข้อ 10 ค่าปรับ" />
      <div class="grid lg:grid-cols-3 gap-2 mt-5">
        <InputField label="ประเภทค่าปรับ" v-model="mock" />
      </div>
      <p>ระยะเวลาให้แก้ไขซ่อมแซมให้ดีดังเดิมภายใน</p>
      <div class="grid lg:grid-cols-3 gap-2">
        <InputField label="ค่าปรับอัตราร้อยละ" v-model="mock" />
        <InputField label="อัตราค่าปรับอัตราเครื่องละ" v-model="mock" />
        <Select label="ต่อ" :options="[]" v-model="mock" />
      </div>
    </template>
  </Card>
</template>
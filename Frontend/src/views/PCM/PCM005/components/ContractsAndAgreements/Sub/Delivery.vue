<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
  InputArea,
  Datepicker,
} from '@/components/forms';
import { ref } from 'vue';

const mock = ref();
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="สัญญาข้อ 4 การส่งมอบ" />
      <InputArea label="สถานที่ส่งมอบที่ปรากฎตามสัญญา" v-model="mock" class="mt-5" />
      <div class="grid lg:grid-cols-3 gap-2">
        <Datepicker label="ผู้ขายจะส่งมอบของภายในวันที่" v-model="mock" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2">
        <InputField label="เลขที่" v-model="mock" />
        <Select :options="[]" label="วัน" v-model="mock" />
      </div>
    </template>
  </Card>
</template>
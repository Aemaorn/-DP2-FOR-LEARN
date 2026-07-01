<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
} from '@/components/forms';
import type { MockContract } from '@/models/PCM/PCM005/principle';
import { ref } from 'vue';

const mockDataList = ref<MockContract[]>([{} as MockContract]);
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="สัญญาข้อ3 เอกสารอันเป็นส่วนหนึ่งของสัญญา">
        <template #action>
          <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined" />
        </template>
      </TitleHeader>
      <p>เอกสารแนบท้ายสัญญาดังต่อไปนี้ ให้ถือเป็นส่วนหนึ่งของสัญญานี้</p>
      <DataTable :value="mockDataList">
        <Column field="name">
          <template #header>
            <p class="w-full font-bold text-center">ชื่อเอกสาร</p>
          </template>
          <template #body="{ data }">
            <InputField v-model="data.name" hide-details />
          </template>
        </Column>
        <Column field="message">
          <template #header>
            <p class="w-full font-bold text-center">ข้อความอ้างอิง</p>
          </template>
          <template #body="{ data }">
            <InputField v-model="data.message" hide-details />
          </template>
        </Column>
        <Column field="count">
          <template #header>
            <p class="w-full font-bold text-center">จำนวน(หน้า)</p>
          </template>
          <template #body="{ data }">
            <InputField v-model="data.count" hide-details />
          </template>
        </Column>
        <Column field="file">
          <template #header>
            <p class="w-full font-bold text-center">เอกสารแนบ</p>
          </template>
          <template #body>
            <Button label="แนบเอกสาร" icon="pi pi-paperclip" severity="primary" variant="outlined" class="w-full" />
          </template>
        </Column>
        <Column field="control">
          <template #body>
            <i class="pi pi-trash mt-3 text-red-600 cursor-pointer" />
          </template>
        </Column>
        <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false" bodyStyle="padding-top: 25px" />
      </DataTable>
    </template>
  </Card>
</template>
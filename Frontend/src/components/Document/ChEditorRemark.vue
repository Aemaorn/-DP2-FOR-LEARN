<script setup lang="ts">
import ToastHelper from '@/helpers/toast';
import { ref } from 'vue';

const tags = ref<string[]>(['table_acceptor', 'table_committee', 'table_acceptor_committee']);

const remarks = [
  'ข้อความดังกล่าวจะแสดงอยู่บนหัวตารางของเอกสารบางฉบับ',
  'เมื่อดำเนินการส่งเห็นชอบ/อนุมัติ หรือดำเนินการสำเร็จแล้ว ข้อความนี้จะหายไป',
  'หากข้อความถูกลบออก กรุณาคัดลอกข้อความดังกล่าว ไปวางที่ตำแหน่งเดิม',
];

const copyText = (text: string): void => {
  navigator.clipboard
    .writeText(text)
    .then((): void => {
      ToastHelper.success('คัดลอกข้อความ', `คัดลอกข้อความ '${text}' สำเร็จ`);
    })
    .catch((): void => {
      ToastHelper.error('คัดลอกข้อความ', 'คัดลอกข้อความไม่สำเร็จ');
    });
};
</script>

<template>
  <div class="flex gap-2 bg-gray-200 p-5 rounded-[5px]">
    <div class="text-[14px] w-4/12">
      <p>*หมายเหตุ: ห้ามลบข้อความที่ปรากฏอยู่ในเอกสาร ดังนี้</p>
      <div class="ps-5">
        <p v-for="(tag, index) in tags" :key="index">
          {{ tag }}
          <button @click="copyText(tag)" class="text-blue-500 hover:underline">คัดลอก</button>
        </p>
      </div>
    </div>
    <div class="vertical-line"></div>
    <div class="text-[24px]">
      <div class="list-disc">
        <ol>
          <li v-for="(remark, index) in remarks" :key="index">
            {{ remark }}
          </li>
        </ol>
      </div>
    </div>
  </div>
</template>

<style scoped>
li {
  display: list-item;
  margin-bottom: 5px;
}

.vertical-line {
  border-left: 2px solid var(--color-gray-300);
  margin: 0 20px;
}
</style>

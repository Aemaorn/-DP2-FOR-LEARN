<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField } from '@/components/forms';
import { ref } from 'vue';
import Pagination from '@/components/Pagination.vue';

const show = defineModel('show', { default: false });

const mockingData = ref([
  {
    type: 'บุคคลธรรมดา',
    entrepreneurType: 'ห้างหุ้นส่วนสามุญนิติบุคคล',
    taxId: '1234567890123',
    name: 'บริษัท อยู่แล้วรวย จำกัด',
  }
]);

const onCloseDialog = () => {
  show.value = false;
};
</script>

<template>
  <Dialog v-model:visible="show" modal :style="{ width: '70vw' }" :breakpoints="{ '1199px': '100px', '575px': '90vw' }"
    class="p-4">
    <template #container>
      <TitleHeader label="ข้อมูลคู่ค้า">
        <template #action>
          <Button icon="pi pi-times" severity="secondary" class="font-bold" variant="text" @click="onCloseDialog" />
        </template>
      </TitleHeader>

      <div class="md:grid grid-cols-3 gap-2">
        <InputField label="คำค้นหา" model-value="" class="col-span-2" />
      </div>
      <div class="grid md:grid-cols-4 gap-2 items-start">
        <Select label="ประเภท" :options="[]" model-value="" />
        <Select label="ประเภทผู้ประกอบการ" :options="[]" model-value="" />

        <div class="md:col-start-4 md:flex gap-2 justify-end">
          <Button label="ค้นหา" icon="pi pi-search" class="w-full md:w-auto" />
          <Button label="ล้าง" icon="pi pi-eraser" variant="outlined" class="w-full md:w-auto mt-3 md:mt-0" />
        </div>
      </div>

      <DataView class="mt-4" :value="mockingData" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in items" :key="index" class="p-4 border border-gray-300 rounded-xl mt-4">
            <div class="flex items-center justify-between">
              <div class="flex gap-5">
                <div>
                  <p>ประเภท</p>
                  <p>ประเภทผู้ประกอบการ</p>
                  <p>เลขประจำตัวผู้เสียภาษีอากร เลขประจำตัวประชาชน</p>
                  <p>ชื่อบริษัท/ชื่อ - นามสกุล</p>
                </div>

                <div>
                  <p>{{ data.type }}</p>
                  <p>{{ data.entrepreneurType }}</p>
                  <p>{{ data.taxId }}</p>
                  <p class="font-bold">{{ data.name }}</p>
                </div>
              </div>

              <div class="flex items-center justify-end">
                <Button label="เลือก" severity="primary" variant="outlined" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <h5 class="text-center ">
            ไม่พบข้อมูล
          </h5>
        </template>
      </DataView>

      <Pagination :page-number="1" :page-size="10" :total-record="100" />
    </template>
  </Dialog>
</template>

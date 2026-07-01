<script setup lang="ts">
import type { Cam001ContractCriteria, Cam001ContractDialog } from '@/models/CAM/CAM01/cam01';
import type { TDataTableResult } from '@/models/shared/paginated';
import { Pagination } from '@/components';
import { TitleHeader } from '@/components/cosmetic';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { InputField } from '@/components/forms';
import Cam01Service from '@/services/CAM/CAM01/cam01';
import { HttpStatusCode } from 'axios';
import { Button, Dialog } from 'primevue';
import { ref, watch } from 'vue';
import { Form } from 'vee-validate';
import { ToDateOnly } from '@/helpers/dateTime';
import { formatCurrency } from '@/helpers/currency';

const emit = defineEmits<(event: 'onSelected', value: Cam001ContractDialog) => void>();

const isShow = defineModel<boolean>({
  required: true,
  default: false,
});

const table = ref<TDataTableResult<Cam001ContractDialog>>({
  data: [],
  totalRecords: 0,
});

const criteria = ref<Cam001ContractCriteria>({
  pageNumber: 1,
  pageSize: 10,
})

const onResetTable = () => {
  table.value = {
    data: [],
    totalRecords: 0,
  };
};

const onResetCriteria = () => {
  criteria.value = {
    pageNumber: 1,
    pageSize: 10,
  }
};

const onAfterHidden = () => {
  onResetTable();
  onResetCriteria();
};

const onChangePageSize = async (pageNumber: number, pageSize: number) => {
  criteria.value.pageNumber = pageNumber;
  criteria.value.pageSize = pageSize;

  await onGetListAsync();
};

const onGetListAsync = async () => {
  const { data, status } = await Cam01Service.onGetContractDialogAsync(criteria.value);

  if (status === HttpStatusCode.Ok) {
    table.value = data;

  }
};

const onSelectedItem = (item: Cam001ContractDialog) => {
  emit('onSelected', item);
  isShow.value = false;
};

watch(() => isShow.value, async (val: boolean) => {
  if (val) {
    await onGetListAsync();
  }
});
</script>

<template>
  <Dialog v-model:visible="isShow" modal maximizable :close-on-escape="false" :style="{ width: '80vw' }"
    :draggable="false" :breakpoints="{ '575px': '90vw' }" @after-hide="onAfterHidden">
    <template #container="{ closeCallback, maximizeCallback }">
      <div class="flex flex-col bg-white rounded-2xl max-h-[90vh] overflow-hidden">
        <!-- Header -->
        <div class="flex items-center justify-between p-4 shrink-0">
          <TitleHeader label="ค้นหาสัญญา"></TitleHeader>
          <div class="flex items-center gap-2">
            <span
              class="material-symbols-outlined text-gray-500 border-[0.5px] border-gray-500 rounded-md cursor-pointer"
              @click="maximizeCallback">
              expand_content
            </span>
            <span class="material-symbols-outlined cursor-pointer" @click="closeCallback">close</span>
          </div>
        </div>

        <!-- Results (scrollable) -->
        <div class="flex-1 overflow-y-auto px-4 min-h-0">
          <Card>
            <template #content>
              <Form @submit="onGetListAsync">
                <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5">
                  <InputField label="คำค้นหา" class="lg:col-span-2" v-model.trim="criteria.keyword" />
                </div>
                <div class="flex gap-2 justify-start lg:justify-end items-center mt-5">
                  <Button class="lg:w-fit w-full" label="ค้นหา" icon="pi pi-search" type="submit" />
                  <Button class="lg:w-fit w-full" label="ล้าง" icon="pi pi-eraser" variant="outlined"
                    @click="onResetCriteria" />
                </div>
              </Form>
            </template>
          </Card>

          <p class="text-primary font-bold text-lg mt-4 mb-2">ผลการค้นหา</p>

          <Card v-for="(item, index) in (table.data as Cam001ContractDialog[])" :key="index"
            class="mt-2 border border-gray-300">
            <template #content>
              <div class="grid lg:grid-cols-12 gap-x-4 gap-y-2">
                <div class="lg:col-span-8">
                  <InfoRow label="เลขที่สัญญา">
                    <p class="text-blue-500 underline">{{ item.contractNumber }}</p>
                  </InfoRow>
                  <InfoRow label="เลขที่ PO (SAP)">
                    <p class="text-blue-500 underline">{{ item.poNumber }}</p>
                  </InfoRow>
                  <InfoRow label="วันที่ลงนามในสัญญา">
                    <p class="font-bold">{{ ToDateOnly(item.contractSignedDate) }}</p>
                  </InfoRow>
                  <InfoRow label="คู่สัญญา">
                    {{ `${item.entrepreneurName} : ${item.entrepreneurName}` }}
                  </InfoRow>
                  <InfoRow label="ชื่อสัญญา">
                    {{ item.contractName }}
                  </InfoRow>
                  <InfoRow label="วงเงินตามสัญญา">
                    {{ formatCurrency(item.budget) }}
                  </InfoRow>
                  <InfoRow label="ประเภทสัญญา">
                    {{ item.contractTypeLabel ?? '-' }}
                  </InfoRow>
                </div>
                <div class="lg:col-span-4 flex flex-col items-end justify-center gap-2">
                  <Button class="text-[#f9a825] border-[#f9a825] bg-white hover:bg-[#f9a825] hover:text-white"
                    label="เลือก" @click="() => onSelectedItem(item)" />
                </div>
              </div>
            </template>
          </Card>
          <p v-if="!table.data?.length" class="text-center mt-4">ไม่พบข้อมูล</p>
        </div>

        <!-- Pagination (fixed) -->
        <div class="px-4 py-2 shrink-0">
          <Pagination :page-number="criteria.pageNumber" :page-size="criteria.pageSize"
            :total-record="table.totalRecords" @change="onChangePageSize" />
        </div>
      </div>
    </template>
  </Dialog>
</template>
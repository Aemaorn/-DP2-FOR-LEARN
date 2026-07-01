<script setup lang="ts">
import { Pagination } from '@/components';
import { TitleHeader, InfoRow } from '@/components/cosmetic';
import { InputField, Select } from '@/components/forms';
import { useSt005DetailStore } from '@/stores/ST/st005';
import { Button, DataView } from 'primevue';
import { watch } from 'vue';
import Dialog from 'primevue/dialog';
import { Form as VeeForm } from 'vee-validate';

const visible = defineModel('visible', { default: false });

const detailStore = useSt005DetailStore();

const onClose = (): void => {
  visible.value = false;
  detailStore.clearCriteria();
};

const initAsync = async (): Promise<void> => {
  await detailStore.getDropDownGroup();
  await detailStore.getUserDialog();
};

const onChangeGroup = (value?: string): void => {
  detailStore.dropdowns.line = [];
  detailStore.userCriteria.lineWork = undefined;

  if (value) {
    detailStore.getDropDownLine(value);
  }
};

const onChangeLine = (value?: string): void => {
  detailStore.dropdowns.department = [];
  detailStore.userCriteria.department = undefined;

  if (value) {
    detailStore.getDropDownDepartment(value);
  }
};

const onSelectedUser = (index: number): void => {
  detailStore.onSelectUser(index);
  onClose();
};

watch(
  (): number[] => [detailStore.userCriteria.pageNumber, detailStore.userCriteria.pageSize],
  async (): Promise<void> => {
    await detailStore.getUserDialog();
  }
);

watch((): boolean => visible.value, (newValue: boolean): void => {
  if (newValue) {
    initAsync();
  }
});
</script>

<template>
  <Dialog v-model:visible="visible" modal :style="{ width: '70vw' }"
    :breakpoints="{ '1199px': '100px', '575px': '90vw' }" class="p-4">
    <template #container>
      <TitleHeader label="รายชื่อ">
        <template #action>
          <Button icon="pi pi-times" severity="secondary" class="font-bold" variant="text" @click="() => onClose()" />
        </template>
      </TitleHeader>
      <VeeForm @submit="detailStore.getUserDialog">

        <div class="grid lg:grid-cols-4 gap-4 mt-8">
          <InputField class="lg:col-span-2" label="คำค้นหา" v-model.trim="detailStore.userCriteria.keyword"
            hide-details />

          <Select class="lg:col-start-1 hidden" label="กลุ่มงาน" v-model="detailStore.userCriteria.groupWork"
            hide-details :options="detailStore.dropdowns.group" @enter-close="detailStore.getUserDialog"
            @on-select="onChangeGroup" />

          <Select class="hidden" label="สายงาน" v-model="detailStore.userCriteria.lineWork" hide-details
            :options="detailStore.dropdowns.line" @enter-close="detailStore.getUserDialog" @on-select="onChangeLine"
            :disabled="!detailStore.userCriteria.groupWork" />

          <Select class="hidden" label="ฝ่าย/ภาคเขต" v-model="detailStore.userCriteria.department" hide-details
            :options="detailStore.dropdowns.department" @enter-close="detailStore.getUserDialog"
            :disabled="!detailStore.userCriteria.lineWork" />
        </div>

        <div class="flex justify-end gap-4">
          <Button label="ค้นหา" icon="pi pi-search" type="submit" />
          <Button label="ล้าง" icon="pi pi-eraser" variant="outlined" @click="detailStore.clearCriteria" />
        </div>
      </VeeForm>
      <DataView class="mt-4 h-full overflow-y-scroll hide-scrollbar" :value="detailStore.userSourceList.data"
        data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm p-3 mb-4">
            <div class="grid lg:grid-cols-12 px-2">
              <div class="lg:col-span-8">
                <InfoRow label="ชื่อ-นามสกุล ">
                  <p class="font-semibold">{{ data.fullName }}</p>
                </InfoRow>
                <InfoRow label="ตำแหน่ง">
                  <p class="">{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p class="">{{ data.fullPositionName }}</p>
                </InfoRow>
              </div>
              <div class="flex flex-col justify-center items-end lg:col-span-4">
                <Button label="เลือก" severity="primary" variant="outlined"
                  @click="() => onSelectedUser(index as number)" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center ">
            ไม่พบข้อมูล
          </p>
        </template>
      </DataView>

      <Pagination :page-number="detailStore.userCriteria.pageNumber" :page-size="detailStore.userCriteria.pageSize"
        :total-record="detailStore.userSourceList.totalRecords" @change="detailStore.onChangePageSize" />
    </template>
  </Dialog>
</template>

<style lang="css" scoped>
.hide-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
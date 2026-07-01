<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Datepicker } from '@/components/forms';
import { onMounted, watch } from 'vue';
import { ButtonSearch, ButtonClear } from '@/components/Button';
import { Pagination } from '@/components';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { useST008Store } from '@/stores/ST/st008';
import { storeToRefs } from 'pinia';
import { Form as VeeForm } from 'vee-validate';
import type { ST008List } from '@/models/ST/st008';
import { ToDateTimeFully } from '@/helpers/dateTime';


const store = useST008Store();
const { searchCriteria, table } = storeToRefs(store);
const { onGetListAsync, onResetCriteria, onChangePageSize } = store;

onMounted(async (): Promise<void> => {
  await onGetListAsync();

  onWatching();
});

const onChangeStartDate = (e?: Date): void => {
  if (!e) {
    searchCriteria.value.endDate = undefined;
  }
};

const onWatching = (): void => {
  watch((): number[] => [searchCriteria.value.pageNumber, searchCriteria.value.pageSize], async (): Promise<void> => {
    await onGetListAsync();
  });
};
</script>

<template>
  <TitleHeader label="ประวัติการใช้งาน" />
  <Card>
    <template #content>
      <VeeForm @submit="onGetListAsync">
        <div class="mt-10 space-y-8 lg:space-y-10">
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
            <InputField label="คำค้นหา" v-model.trim="searchCriteria.keyword" hide-details />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Datepicker label="วันที่เริ่มต้น" v-model="searchCriteria.startDate"
              @on-selected="(e) => onChangeStartDate(e)" hide-details />
            <Datepicker label="วันที่สิ้นสุด" v-model="searchCriteria.endDate" :min-date="searchCriteria.startDate"
              :disabled="!searchCriteria.startDate" hide-details />
          </div>
          <div class="grid grid-cols-2 gap-2 lg:flex lg:items-center lg:justify-end">
            <ButtonSearch type="submit" class="lg:w-fit w-full" />
            <ButtonClear @click="() => onResetCriteria()" class="lg:w-fit w-full" />
          </div>
        </div>
      </VeeForm>
    </template>
  </Card>

  <Card class="mt-5">
    <template #content>
      <DataView :value="table.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in (items as Array<ST008List>)" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="grid lg:grid-cols-12 p-1">
              <div class="lg:col-span-4">
                <InfoRow label="วันที่">
                  <p>{{ ToDateTimeFully(data.timeStamp) }}</p>
                </InfoRow>
                <InfoRow label="ชื่อผู้ใช้">
                  <p>{{ data.userName }}</p>
                </InfoRow>
              </div>
              <div class="lg:col-span-4">
                <InfoRow label="โปรแกรม">
                  <p>{{ data.programName }}</p>
                </InfoRow>
                <InfoRow label="IP">
                  <p>{{ data.ipAddress ?? '-' }}</p>
                </InfoRow>
                <InfoRow label="ข้อความ">
                  <p>{{ data.message }}</p>
                </InfoRow>
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center font-bold">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="searchCriteria.pageNumber" :page-size="searchCriteria.pageSize"
        :total-record="table.totalRecords" @change="onChangePageSize" />
    </template>
  </Card>
</template>
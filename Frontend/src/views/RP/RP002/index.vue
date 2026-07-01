<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { ButtonSearch, ButtonClear } from '@/components/Button';
import { InputField, Select } from '@/components/forms';
import { useRouter } from 'vue-router';
import { ToDateOnly } from '@/helpers/dateTime';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { formatCurrency } from '@/helpers/currency';
import { useRP002ListStore } from '@/stores/RP/RP002/list.store';
import { storeToRefs } from 'pinia';
import { Form as VeeForm } from 'vee-validate';
import { YearOptions, QuarterOptions } from '@/constants/date';
import { onMounted, watch } from 'vue';
import RP002Constants from '@/constants/RP/rp002';
import type { RP002List } from '@/models/RP/rp002';
import { RP002Status } from '@/enums/RP/rp002';


const router = useRouter();
const { StatusColorLabel } = RP002Constants;
const store = useRP002ListStore();
const { searchCriteria, table, statusOptionBadge } = storeToRefs(store);
const { onResetCriteria, onGetListAsync, onChangePageSize, onDeleteByIdAsync } = store;

onMounted(async (): Promise<void> => {
  await onGetListAsync();

  onWatching();
});

const navagateToDetail = (id: string): void => {
  router.push({ 'name': 'rp002Detail', params: { id } });
};

const onWatching = (): void => {
  watch((): (number | RP002Status | undefined)[] => [
    searchCriteria.value.pageNumber,
    searchCriteria.value.pageSize,
    searchCriteria.value.status,
  ], async (): Promise<void> => await onGetListAsync());
};
</script>

<template>
  <TitleHeader label="รายงานสัญญาแล้วเสร็จตามไตรมาส">
    <template #action>
      <Button icon="pi pi-plus" label="สร้างรายการ" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'rp002Detail' })" />
    </template>
  </TitleHeader>
  <Card class="mb-4">
    <template #content>
      <VeeForm @submit="onGetListAsync">
        <div class="grid lg:grid-cols-5 gap-2 mt-2">
          <InputField class="lg:col-span-3" label="คำค้นหา" v-model.trim="searchCriteria.keyword" />
          <Select class="lg:col-start-1" label="ปี" :options="YearOptions" v-model="searchCriteria.year"
            @enterClose="onGetListAsync" />
          <Select label="ไตรมาส" :options="QuarterOptions" v-model="searchCriteria.quarter" @enterClose="onGetListAsync" />

          <div class="col-start-auto lg:col-start-5 flex gap-2 items-center justify-start lg:justify-end">
            <ButtonSearch class="w-full lg:w-fit" type="submit" />
            <ButtonClear class="w-full lg:w-fit" @click="onResetCriteria" />
          </div>
        </div>
      </VeeForm>
    </template>
  </Card>
  <Card class="mb-4">
    <template #content>
      <StatusGroupButton v-model="searchCriteria.status" :optionBadges="statusOptionBadge" />
      <DataView :value="table.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in (items as Array<RP002List>)" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="grid lg:grid-cols-12 px-2">
              <div class="lg:col-span-8">
                <InfoRow label="เลขที่เอกสาร">
                  <p class="underline text-blue-400 cursor-pointer" @click="() => navagateToDetail(data.id)">
                    {{ data.documentNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="วันที่ทำรายงาน">
                  <p>
                    {{ ToDateOnly(data.documentDate) }}
                  </p>
                </InfoRow>
                <InfoRow label="ปีงบประมาณ">
                  <p>{{ data.year }}</p>
                </InfoRow>
                <InfoRow label="ไตรมาส">
                  <p>{{ QuarterOptions.find(q => q.value === data.quarter)?.label ?? `ไตรมาสที่ ${data.quarter}` }}</p>
                </InfoRow>
                <InfoRow label="ข้อมูลตั้งแต่วันที่ - ถึงวันที่">
                  <p>{{ `${ToDateOnly(data.signStartDate)} - ${ToDateOnly(data.signEndDate)}` }}</p>
                </InfoRow>
                <InfoRow label="จำนวนสัญญา (ฉบับ)">
                  <p>
                    {{ `${data.detailCount} ฉบับ` }}
                  </p>
                </InfoRow>
                <InfoRow label="วงเงินตามสัญญารวม (บาท)">
                  <p>
                    {{ `${formatCurrency(data.totalAmount)} บาท` }}
                  </p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <p class="text-sm">สถานะ :</p>
                  <div class="text-center w-fit">
                    <BadgeStatus :color="StatusColorLabel(data.status).color"
                      :label="StatusColorLabel(data.status).label" />
                  </div>
                </div>

                <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  size="small" variant="text" @click="() => navagateToDetail(data.id)" />
                <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text" v-if="data.status === RP002Status.Draft" @click="() => onDeleteByIdAsync(data.id)" />
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
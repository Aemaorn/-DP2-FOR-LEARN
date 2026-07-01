<script setup lang="ts">
import { onMounted, watch } from 'vue';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { TitleHeader } from '@/components/cosmetic';
import { CriteriaGroupButton, Datepicker, InputField, StatusGroupButton } from '@/components/forms';
import ProcurementConstants from '@/constants/procurement';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { useRouter } from 'vue-router';
import { useRp001ListStore } from '@/stores/RP/rp001';
import { ToDateOnly } from '@/helpers/dateTime';
import { Form } from 'vee-validate';
import rp001Constant from '@/constants/RP/rp001'
import { BadgeStatus as BadgeComponent } from '@/components';
import { rp001Status } from '@/enums/RP/rp001';
import { formatCurrency } from '@/helpers/currency';

const { BadgeStatus } = rp001Constant;

const router = useRouter();
const listStore = useRp001ListStore();

onMounted(async () => {
  await listStore.getListAsync();
})

const routeToDetail = (id?: string): void => {
  const route = '/rp/rp001/detail';
  const finalRoute = id ? `${route}/${id}` : route;

  router.push(finalRoute);
};

watch(() => [listStore.criteria.pageNumber, listStore.criteria.pageSize, listStore.criteria.status, listStore.criteria.workProcess], async () => {
  await listStore.getListAsync();
})
</script>

<template>
  <div class="flex flex-col gap-4">
    <TitleHeader label="รายงานสำนักงานการตรวจเงินแผ่นดินและกรมสรรพากร">
      <template #action>
        <Button label="สร้างรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => routeToDetail()" />
      </template>
    </TitleHeader>

    <Card>
      <template #content>
        <Form @submit="listStore.getListAsync()">
          <CriteriaGroupButton :options="ProcurementConstants.ProcurementWorkProcessOptions"
            v-model:model-value="listStore.criteria.workProcess" />
          <div class="grid md:grid-cols-2 gap-2 mt-8">
            <InputField label="คำค้นหา" v-model.trim="listStore.criteria.keyword" hide-details />
          </div>
          <div class="grid md:grid-cols-4 gap-2 gap-y-8 mt-10 items-start">
            <Datepicker label="วันที่ทำรายงาน" v-model="listStore.criteria.documentDate" hide-details />
            <Datepicker label="ข้อมูลตั้งแต่วันที่" v-model="listStore.criteria.signStartDate" hide-details />
            <Datepicker label="ถึง" v-model="listStore.criteria.signEndDate" hide-details />

            <div class="md:col-start-5 md:flex gap-2 justify-end">
              <ButtonSearch type="submit" />
              <ButtonClear @click="listStore.clearCriteria" />
            </div>
          </div>
        </Form>
      </template>
    </Card>

    <Card>
      <template #content>
        <StatusGroupButton :optionBadges="listStore.statusOptionBadge" v-model="listStore.criteria.status" />
        <DataView :value="listStore.table?.data.data" data-key="id">
          <template #list="{ items }">
            <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
              <div class="grid lg:grid-cols-12 px-2">
                <div class="lg:col-span-8">
                  <InfoRow label="เลขที่เอกสาร">
                    <p class="underline text-blue-400" @click="() => routeToDetail(data.id)">
                      {{ data.documentNumber }}
                    </p>
                  </InfoRow>
                  <InfoRow label="วันที่ทำรายงาน">
                    <p>
                      {{ ToDateOnly(data.documentDate) }}
                    </p>
                  </InfoRow>
                  <InfoRow label="ข้อมูลตั้งแต่วันที่ - ถึงวันที่">
                    <p>
                      {{ ToDateOnly(data.signStartDate) }} - {{ ToDateOnly(data.signEndDate) }}
                    </p>
                  </InfoRow>
                  <InfoRow label="จำนวนสัญญา (ฉบับ)">
                    <p>
                      {{ data.detailCount }}
                    </p>
                  </InfoRow>
                  <InfoRow label="วงเงินตามสัญญารวม (บาท)">
                    <p>
                      {{ formatCurrency(data.totalAmount) }}
                    </p>
                  </InfoRow>
                </div>
                <div class="flex items-start justify-end gap-1.5 lg:col-span-4">
                  <div class="flex items-center gap-2 mt-2 mr-2">
                    <BadgeComponent :label="BadgeStatus(data.status).label" :color="BadgeStatus(data.status).color" />
                  </div>
                  <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                    size="small" variant="text" @click="() => routeToDetail(data.id)" />
                  <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!"
                    size="small" variant="text"
                    v-if="[rp001Status.Draft, rp001Status.Edit, rp001Status.Rejected].includes(data.status)"
                    @click="() => listStore.deleteByIdAsync(data.id)" />
                </div>
              </div>
            </div>
          </template>
          <template #empty>
            <p class="text-center">ไม่พบข้อมูล</p>
          </template>
        </DataView>
        <Pagination :page-number="listStore.criteria.pageNumber" :page-size="listStore.criteria.pageSize"
          :total-record="listStore.table?.data.totalRecords" @change="listStore.onChangePageSize" />
      </template>
    </Card>
  </div>
</template>
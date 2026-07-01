<script setup lang="ts">
import { onBeforeMount, onMounted, watch } from 'vue';
import { Button, Card, DataView } from 'primevue';
import { useRouter } from 'vue-router';
import ST001Service from '@/services/ST/ST001';
import { useSt001ListStore } from '@/stores/ST/st001';
import { InputField, Datepicker, AutoCompleteMultiField } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import Pagination from '@/components/Pagination.vue';
import { ToDateOnly } from '@/helpers/dateTime';
import { useMenuStore } from '@/stores/menu';
import { Form } from 'vee-validate';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';

const router = useRouter();
const menuStore = useMenuStore();
const listStore = useSt001ListStore();

onBeforeMount((): void => {
  listStore.onResetCriteria();
});

onMounted(async (): Promise<void> => {
  await Promise.all([
    listStore.onGetEntrepreneurTypeOptionsAsync(),
    listStore.onGetListData(),
    listStore.getDropDownDepartment(),
    listStore.getDropDownGroup(),
    listStore.getDropDownLine(),
  ]);
});

watch(
  (): number[] => [listStore.searchCriteria.pageNumber, listStore.searchCriteria.pageSize],
  async (): Promise<void> => {
    await listStore.onGetListData();
  }
);

const onDeleteAsync = async (id: string): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  await listStore.onDeleteByIdAsync(id);
};

const downloadReport = async (): Promise<void> => {
  ST001Service.exportReport('รายงานการมอบหมายให้ปฏิบัติหน้าที่แทน', listStore.searchCriteria);
};
</script>

<template>
  <TitleHeader label="กำหนดผู้รับมอบอำนาจ">
    <template #action>
      <Button label="เพิ่มผู้รับมอบอำนาจ" icon="pi pi-plus" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'st001Detail' })" />
    </template>
  </TitleHeader>
  <Card class="my-4">
    <template #content>
      <Form @submit="listStore.onGetListData">
        <div class="mt-6 space-y-4 lg:space-y-2">
          <div class="grid grid-cols-2 lg:grid-cols-6 gap-8 lg:gap-2">
            <AutoCompleteMultiField label="กลุ่มงาน/สายงาน/ฝ่าย/ศูนย์/สาขา"
              v-model="listStore.searchCriteria.businessUnitIds" :options="listStore.businessUnitDropdown"
              class="lg:col-span-3" />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-6 gap-8 lg:gap-2">
            <InputField label="ชื่อ - นามสกุล ผู้ปฏิบัติหน้าที่"
              v-model.trim="listStore.searchCriteria.delegatorName" />
            <InputField label="ตำแหน่งผู้ปฏิบัติหน้าที่" v-model.trim="listStore.searchCriteria.delegatorPositionName" />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-6 gap-8 lg:gap-2">
            <InputField label="ชื่อ - นามสกุล ผู้ปฏิบัติหน้าที่แทน"
              v-model.trim="listStore.searchCriteria.delegateeName" />
            <InputField label="ตำแหน่งผู้ปฏิบัติหน้าที่แทน"
              v-model.trim="listStore.searchCriteria.delegateePositionName" />
            <Datepicker label="วันที่มอบหมาย ตั้งแต่" v-model="listStore.searchCriteria.delegatorStartDate"
              :max-date="listStore.searchCriteria.delegatorEndDate" />
            <Datepicker label="ถึงวันที่" v-model="listStore.searchCriteria.delegatorEndDate"
              :min-date="listStore.searchCriteria.delegatorStartDate" />
            <div class="grid grid-cols-2 gap-2 lg:flex lg:items-center lg:justify-end lg:col-span-2 lg:col-end-7">
              <ButtonSearch class="lg:w-fit w-full" type="submit" />
              <ButtonClear class="lg:w-fit w-full" @click="() => listStore.onResetCriteria()" />
            </div>
          </div>
        </div>
      </Form>
    </template>
  </Card>
  <Card>
    <template #content>
      <div class="flex items-center justify-end">
        <Button label="พิมพ์รายงานการมอบหมายให้ปฏิบัติหน้าที่แทน" icon="pi pi-file" severity="primary"
          variant="outlined" class="bg-white! hover:bg-red-50! my-4" @click="downloadReport" />
      </div>
      <DataView :value="listStore.table.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12">
              <div class="lg:col-span-8">
                <InfoRow label="ผู้ปฏิบัติหน้าที่">
                  <p class="font-extrabold">{{ data.delegatorName }}</p>
                </InfoRow>
                <InfoRow label="ตำแหน่ง ผู้ปฏิบัติหน้าที่">
                  <p>{{ data.delegatorPositionName }}</p>
                </InfoRow>
                <InfoRow label="วันที่มอบหมาย">
                  <p>
                    {{ ToDateOnly(data.delegatorStartDate) }} -
                    {{ ToDateOnly(data.delegatorEndDate) }}
                  </p>
                </InfoRow>
                <InfoRow label="ผู้ปฏิบัติหน้าที่แทน">
                  <p class="font-extrabold">{{ data.delegateeName }}</p>
                </InfoRow>
                <InfoRow label="ปฏิบัติหน้าที่แทนตำแหน่ง">
                  <p>{{ data.delegateePositionName }}</p>
                </InfoRow>
                <InfoRow label="วันที่แก้ไข">
                  <p>{{ ToDateOnly(data.updatedAt) }}</p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  size="small" variant="text"
                  @click="() => router.push({ name: 'st001Detail', params: { id: data.id } })"
                  v-if="menuStore.hasPermission" />
                <Button icon="pi pi-trash" class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text" @click="() => onDeleteAsync(data.id)" v-if="menuStore.hasPermission" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="listStore.searchCriteria.pageNumber" :page-size="listStore.searchCriteria.pageSize"
        :total-record="listStore.table.totalRecords" @change="listStore.onChangePageSize" />
    </template>
  </Card>
</template>

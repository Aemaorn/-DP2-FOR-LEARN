<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { useRP003ListStore } from '@/stores/RP/rp003';
import { ToDateOnly } from '@/helpers/dateTime';
import { formatCurrency } from '@/helpers/currency';
import { onMounted, ref, watch } from 'vue';
import { Form } from 'vee-validate';
import { DataTable } from 'primevue';
import type { rp003SupplyMethod } from '@/enums/RP/rp003';
import ExportFieldsDialog from './components/ExportFieldsDialog.vue';
import { YearOptions } from '@/constants/date';

const listStore = useRP003ListStore();
const showExportDialog = ref(false);

onMounted(async (): Promise<void> => {
  await listStore.onGetDropdownAsync();

  await listStore.onGetProcurementListData();
});

watch((): (number | rp003SupplyMethod | undefined)[] => [
  listStore.searchCriteria.pageNumber,
  listStore.searchCriteria.pageSize,
  listStore.searchCriteria.supplyMethodCode
], async (): Promise<void> => {
  await listStore.onGetProcurementListData();
});
</script>

<template>
  <TitleHeader label="รายงานบริหารสัญญา" />
  <Card class="my-4">
    <template #content>
      <Form @submit="listStore.onGetProcurementListData">
        <div class="grid grid-cols-4 gap-2 gap-y-10 mt-8">
          <Select class="col-start-1" label="ปีงบประมาณ" v-model="listStore.searchCriteria.budgetYear" hide-details
            :options="YearOptions" />
          <Select label="ฝ่าย/ภาคเขต" v-model="listStore.searchCriteria.departmentCode" hide-details
            :options="listStore.deparmentDropdown" />
          <div class="flex gap-2 items-center justify-start md:justify-end col-start-4">
            <Button label="ค้นหา" icon="pi pi-search" @click="listStore.onGetProcurementListData" />
            <Button label="ล้าง" icon="pi pi-eraser" variant="outlined" @click="listStore.onResetCriteria" />
          </div>
        </div>
      </Form>
    </template>
  </Card>
  <ExportFieldsDialog v-model="showExportDialog" @confirm="listStore.exporeExcelAsync($event)" />
  <Card>
    <template #content>
      <div class="w-full flex justify-end">
        <Button label="พิมพ์รายการบริหารสัญญา" icon="pi pi-file" severity="primary" variant="outlined"
          class="bg-white! hover:bg-red-50! mb-4" @click="showExportDialog = true" />
      </div>
      <StatusGroupButton :option-badges="listStore.statusOptions" v-model="listStore.searchCriteria.supplyMethodCode" class="!mb-0" />
      <DataTable :value="listStore.procurementListTable.data.data" tableStyle="min-width: 80rem" scrollable scrollHeight="700px" showGridlines
        :pt="{ headerRow: { class: 'bg-gray-200 text-gray-900' } }">
        <Column bodyStyle="vertical-align: top" headerStyle="vertical-align: top" style="width: 8rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center whitespace-nowrap">เลขที่สัญญา</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-center font-bold">{{ data.contractNumber }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top" headerStyle="vertical-align: top" style="width: 7rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center whitespace-nowrap">เลขที่ PO</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-center">{{ data.poNumber }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top" headerStyle="vertical-align: top" style="width: 12rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center whitespace-nowrap">วิธีจัดหา</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-start">{{ data.supplyMethodName }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top" headerStyle="vertical-align: top" style="width: 10rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center whitespace-nowrap">ฝ่าย/ภาคเขต</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-start">{{ data.departmentName }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top" headerStyle="vertical-align: top" style="width: 7rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center whitespace-nowrap">ปีงบประมาณ</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-center">{{ data.budgetYear }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top" headerStyle="vertical-align: top" style="width: 9rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center whitespace-nowrap">วันที่ลงนามในสัญญา</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-center">{{ ToDateOnly(data.contractSignedDate) }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top" headerStyle="vertical-align: top" style="width: 8rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center whitespace-nowrap">ประเภทสัญญา</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-start">{{ data.contractTypeName }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top" headerStyle="vertical-align: top" style="width: 14rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center whitespace-nowrap">ชื่อสัญญา</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-start">{{ data.contractName }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top" headerStyle="vertical-align: top" style="width: 10rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center whitespace-nowrap">บริษัทคู่สัญญา</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-start">{{ data.vendorName }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top" headerStyle="vertical-align: top" style="width: 9rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center whitespace-nowrap">วงเงินตามสัญญา</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-end">{{ formatCurrency(data.budget) }} บาท</p>
            </div>
          </template>
        </Column>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataTable>
      <Pagination :page-number="listStore.searchCriteria.pageNumber" :page-size="listStore.searchCriteria.pageSize"
        :totalRecord="listStore.procurementListTable.data.totalRecords" @change="listStore.onChangePageSize" />
    </template>
  </Card>
</template>

<style scoped>
:deep(.p-datatable-thead > tr > th),
:deep(.p-datatable-thead > tr > th p),
:deep(thead > tr > th),
:deep(thead > tr > th p) {
  color: #000 !important;
  font-weight: 700 !important;
}

:deep(.p-datatable-thead > tr > th),
:deep(.p-datatable-tbody > tr > td),
:deep(thead > tr > th),
:deep(tbody > tr > td) {
  vertical-align: top !important;
  padding-top: 4px !important;
}

:deep(.p-datatable-tbody > tr > td p),
:deep(.p-datatable-thead > tr > th p) {
  margin: 0 !important;
}
</style>
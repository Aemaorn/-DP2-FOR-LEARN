<script setup lang="ts">
import { onBeforeMount, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { DataTable, Column, Button } from 'primevue';
import { InputField, Select, Datepicker, StatusGroupButton } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import Pagination from '@/components/Pagination.vue';
import { Form } from 'vee-validate';
import { YearOptions } from '@/constants/date';
import { ToDateOnly } from '@/helpers/dateTime';
import { formatCurrency } from '@/helpers/currency';
import ToastHelper from '@/helpers/toast';
import FileHelper from '@/helpers/file';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { useAnnouncementInfoListStore } from '@/stores/ANN/ANN001';

const ALLOWED_EXTENSIONS = ['.xlsx', '.xls', '.csv'];
const MAX_FILE_SIZE_MB = 10;

const importFileInput = ref<HTMLInputElement | null>(null);
const selectedImportFile = ref<File | null>(null);

const onImportFileChange = (event: Event): void => {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (!file) return;

  const ext = '.' + file.name.split('.').pop()?.toLowerCase();
  if (!ALLOWED_EXTENSIONS.includes(ext)) {
    ToastHelper.error('ไฟล์ไม่ถูกต้อง', 'รองรับเฉพาะไฟล์ .xlsx, .xls และ .csv เท่านั้น');
    if (importFileInput.value) importFileInput.value.value = '';
    return;
  }

  if (file.size > MAX_FILE_SIZE_MB * 1024 * 1024) {
    ToastHelper.error('ไฟล์ใหญ่เกินไป', `ขนาดไฟล์ต้องไม่เกิน ${MAX_FILE_SIZE_MB} MB`);
    if (importFileInput.value) importFileInput.value.value = '';
    return;
  }

  selectedImportFile.value = file;
};

const onClearSelectedFile = (): void => {
  selectedImportFile.value = null;
  if (importFileInput.value) importFileInput.value.value = '';
};

const router = useRouter();
const listStore = useAnnouncementInfoListStore();

const onImportConfirmAsync = async (): Promise<void> => {
  if (!selectedImportFile.value) return;
  await listStore.onImportAsync(selectedImportFile.value);
  selectedImportFile.value = null;
  if (importFileInput.value) importFileInput.value.value = '';
};

const formatMonthYear = (val?: string): string => {
  if (!val) return '';
  return new Intl.DateTimeFormat('th', { month: 'long', year: 'numeric' }).format(new Date(val));
};

onBeforeMount((): void => {
  listStore.onResetCriteria();
});

onMounted(async (): Promise<void> => {
  await Promise.all([
    listStore.onGetSupplyMethodOptionsAsync(),
    listStore.onGetAnnouncementCategoryOptionsAsync(),
    listStore.onGetListAsync(),
  ]);
});

watch(
  (): (number | string | undefined)[] => [
    listStore.criteria.pageNumber,
    listStore.criteria.pageSize,
    listStore.criteria.supplyMethodCode,
  ],
  async (): Promise<void> => {
    await listStore.onGetListAsync();
  }
);

const onDeleteAsync = async (id: string): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;
  await listStore.onDeleteAsync(id);
};
</script>

<template>
  <div class="flex flex-col gap-4">
    <TitleHeader label="ประกาศข้อมูลการจัดซื้อจัดจ้าง">
      <template #action>
        <Button
          label="ไฟล์ตัวอย่าง"
          icon="pi pi-download"
          severity="secondary"
          variant="outlined"
          @click="listStore.onDownloadTemplate()"
        />
        <div
            class="flex items-stretch rounded-lg border overflow-hidden bg-white"
            :class="selectedImportFile ? 'border-red-400' : 'border-red-300'"
          >
            <div class="flex items-center gap-2 px-3 flex-1">
              <i
                class="text-sm"
                :class="selectedImportFile ? 'pi pi-file-excel text-gray-600' : 'pi pi-file-import text-gray-400'"
              />
              <button
                type="button"
                class="text-sm max-w-48 truncate outline-none"
                :class="selectedImportFile ? 'font-medium text-gray-700' : 'text-gray-400 hover:text-gray-600 cursor-pointer'"
                :title="selectedImportFile?.name"
                @click="!selectedImportFile && importFileInput?.click()"
              >
                {{ selectedImportFile?.name ?? 'เลือกไฟล์อัปโหลดข้อมูล...' }}
              </button>
              <Button
                v-if="selectedImportFile"
                icon="pi pi-times"
                text rounded
                severity="danger"
                class="!w-8 !h-8 shrink-0"
                @click="onClearSelectedFile"
              />

            </div>
            <Button
              :label="selectedImportFile ? 'นำเข้า' : undefined"
              :icon="selectedImportFile ? 'pi pi-upload' : 'pi pi-folder-open'"
              severity="danger"
              class="!rounded-none"
              :loading="listStore.isImporting"
              @click="selectedImportFile ? onImportConfirmAsync() : importFileInput?.click()"
            />
          </div>
        <Button
          label="เพิ่มรายการ"
          icon="pi pi-plus" severity="primary" variant="outlined"
          @click="router.push({ name: 'ann001Detail' })"
        />
      </template>
    </TitleHeader>
    <Card>
      <template #content>
        <Form @submit="listStore.onGetListAsync">
          <div class="mt-10 space-y-8 lg:space-y-10">
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <InputField
                label="คำค้นหา"
                v-model.trim="listStore.criteria.keyword"
                hide-details
                class="lg:col-span-3"
              />
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Select
                label="ประเภทประกาศ"
                v-model="listStore.criteria.announcementCategoryCode"
                :options="listStore.announcementCategoryOptions"
                @enterClose="listStore.onGetListAsync"
                hide-details
              />
              <Select
                label="ปีงบประมาณ"
                v-model="listStore.criteria.budgetYear"
                :options="YearOptions"
                @enterClose="listStore.onGetListAsync"
                hide-details
              />
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Datepicker
                label="วันที่เผยแพร่ ตั้งแต่"
                v-model="listStore.criteria.announcementDateFrom"
                :max="listStore.criteria.announcementDateTo"
                hide-details
              />
              <Datepicker
                label="ถึงวันที่"
                v-model="listStore.criteria.announcementDateTo"
                :min="listStore.criteria.announcementDateFrom"
                hide-details
              />
              <div class="lg:col-span-3 flex items-end justify-end gap-2">
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
        <div class="flex justify-end mb-4">
          <input
            ref="importFileInput"
            type="file"
            accept=".xlsx,.xls,.csv"
            class="hidden"
            @change="onImportFileChange"
          />
        </div>

        <StatusGroupButton
          :option-badges="listStore.supplyMethodBadges"
          v-model="listStore.criteria.supplyMethodCode"
          class="!mb-0"
        />

        <DataTable
          :value="listStore.table.data"
          tableStyle="min-width: 60rem"
          scrollable
          scrollHeight="700px"
          showGridlines
          :pt="{ headerRow: { class: 'bg-gray-200 text-gray-900' } }"
        >
          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 10rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center whitespace-nowrap">ประเภทประกาศ</p>
            </template>
            <template #body="{ data }">
              <p>{{data.announcementCategory}}</p>
            </template>
          </Column>

          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 12rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center whitespace-nowrap">อ้างอิง</p>
            </template>
            <template #body="{ data }">
              <p>{{ data.supplyMethod }}</p>
            </template>
          </Column>

          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 8rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center whitespace-nowrap">วันที่เผยแพร่</p>
            </template>
            <template #body="{ data }">
              <p class="text-center">{{ ToDateOnly(data.announcementDate) }}</p>
            </template>
          </Column>

          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 18rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center whitespace-nowrap">ประกาศ</p>
            </template>
            <template #body="{ data }">
              <a
                v-if="data.documentId || data.documentUrl"
                :href="data.documentId ? FileHelper.getFileUrl(data.documentId) : data.documentUrl"
                target="_blank"
                rel="noopener noreferrer"
                class="font-bold text-blue-600 underline hover:text-blue-800"
              >{{ data.announcementName }}</a>
              <p v-else class="font-bold">{{ data.announcementName }}</p>
            </template>
          </Column>

          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 10rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center whitespace-nowrap">วงเงินงบประมาณ</p>
            </template>
            <template #body="{ data }">
              <p class="text-end">
                {{ data.budgetAmount != null ? formatCurrency(data.budgetAmount) : '-' }}
              </p>
            </template>
          </Column>

          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 10rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center">คาดว่าจะประกาศ<br />จัดซื้อจัดจ้าง (เดือน/ปี)</p>
            </template>
            <template #body="{ data }">
              <p class="text-center">{{ formatMonthYear(data.expectedDate) }}</p>
            </template>
          </Column>

          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 5rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center whitespace-nowrap"></p>
            </template>
            <template #body="{ data }">
              <div class="flex justify-center">
                <Button
                  icon="pi pi-pen-to-square"
                  class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  text
                  rounded
                  @click="router.push({ name: 'ann001Detail', params: { id: data.id } })"
                />
                <Button icon="pi pi-trash" class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text" @click="onDeleteAsync(data.id)" />
              </div>
            </template>
          </Column>
          <template #empty>
            <p class="text-center">ไม่พบข้อมูล</p>
          </template>
        </DataTable>

        <Pagination
          :page-number="listStore.criteria.pageNumber"
          :page-size="listStore.criteria.pageSize"
          :total-record="listStore.table.totalRecords"
          @change="listStore.onChangePageSize"
        />
      </template>
    </Card>
  </div>
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

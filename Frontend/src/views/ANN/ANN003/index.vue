<script setup lang="ts">
import { onBeforeMount, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { DataTable, Column, Button } from 'primevue';
import { Select } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import Pagination from '@/components/Pagination.vue';
import { Form } from 'vee-validate';
import { YearOptions, MonthOptions } from '@/constants/date';
import FileHelper from '@/helpers/file';
import ToastHelper from '@/helpers/toast';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { useAnnouncementSorKorRorListStore } from '@/stores/ANN/ANN003';

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
const listStore = useAnnouncementSorKorRorListStore();

const onImportConfirmAsync = async (): Promise<void> => {
  if (!selectedImportFile.value) return;
  await listStore.onImportAsync(selectedImportFile.value);
  selectedImportFile.value = null;
  if (importFileInput.value) importFileInput.value.value = '';
};

const MONTH_LABEL = Object.fromEntries(MonthOptions.map((m): [number, string] => [m.value, m.label]));

const onDeleteAsync = async (id: string): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;
  await listStore.onDeleteAsync(id);
};

onBeforeMount((): void => {
  listStore.onResetCriteria();
});

onMounted(async (): Promise<void> => {
  await Promise.all([
    listStore.onGetDepartmentTypeOptionsAsync(),
    listStore.onGetListAsync(),
  ]);
});

watch(
  (): (number | string | undefined)[] => [
    listStore.criteria.pageNumber,
    listStore.criteria.pageSize,
  ],
  async (): Promise<void> => {
    await listStore.onGetListAsync();
  }
);
</script>

<template>
  <div class="flex flex-col gap-4">
    <TitleHeader label="สรุปผลการจัดซื้อจัดจ้าง ตามแบบ สขร. 1">
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
          icon="pi pi-plus"
          severity="primary"
          variant="outlined"
          @click="router.push({ name: 'ann003Detail' })"
        />
      </template>
    </TitleHeader>

    <input
      ref="importFileInput"
      type="file"
      accept=".xlsx,.xls,.csv"
      class="hidden"
      @change="onImportFileChange"
    />

    <Card>
      <template #content>
        <Form @submit="listStore.onGetListAsync">
          <div class="mt-10 space-y-8 lg:space-y-10">
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Select
                label="ประเภทหน่วยงาน"
                v-model="listStore.criteria.departmentTypeCode"
                :options="listStore.departmentTypeOptions"
                @enterClose="listStore.onGetListAsync"
                hide-details
              />
              <Select
                label="ปี"
                v-model="listStore.criteria.year"
                :options="YearOptions"
                @enterClose="listStore.onGetListAsync"
                hide-details
              />
              <Select
                label="เดือน"
                v-model="listStore.criteria.month"
                :options="MonthOptions"
                @enterClose="listStore.onGetListAsync"
                hide-details
              />
              <div class="lg:col-span-2 flex items-end justify-end gap-2">
                <ButtonSearch class="lg:w-fit w-full" type="submit" />
                <ButtonClear class="lg:w-fit w-full" @click="listStore.onResetCriteria()" />
              </div>
            </div>
          </div>
        </Form>
      </template>
    </Card>

    <Card>
      <template #content>
        <DataTable
          :value="listStore.table.data"
          tableStyle="min-width: 50rem"
          scrollable
          scrollHeight="700px"
          showGridlines
          :pt="{ headerRow: { class: 'bg-gray-200 text-gray-900' } }"
        >
          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 7rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center whitespace-nowrap">ประเภทหน่วยงาน</p>
            </template>
            <template #body="{ data }">
              <p>{{ data.departmentType ?? '-' }}</p>
            </template>
          </Column>

          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 7rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center whitespace-nowrap">ปี พ.ศ.</p>
            </template>
            <template #body="{ data }">
              <p class="text-center">{{ data.year ?? '-' }}</p>
            </template>
          </Column>

          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 7rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center whitespace-nowrap">เดือน</p>
            </template>
            <template #body="{ data }">
              <p class="text-center">{{ data.month ? MONTH_LABEL[data.month] : '-' }}</p>
            </template>
          </Column>

          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 8rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center whitespace-nowrap">จำนวน (ฉบับ)</p>
            </template>
            <template #body="{ data }">
              <p class="text-center">{{ data.amount ?? '-' }}</p>
            </template>
          </Column>

          <Column
            bodyStyle="vertical-align: top"
            headerStyle="vertical-align: top"
            style="width: 14rem"
            headerClass="bg-gray-200 !text-black font-bold"
          >
            <template #header>
              <p class="w-full font-bold text-center whitespace-nowrap">เอกสารแนบ</p>
            </template>
            <template #body="{ data }">
              <a
                v-if="data.documentId || data.documentUrl"
                :href="data.documentId ? FileHelper.getFileUrl(data.documentId) : data.documentUrl"
                target="_blank"
                rel="noopener noreferrer"
                class="font-bold text-blue-600 underline hover:text-blue-800"
              >{{ data.documentName ?? data.documentUrl }}</a>
              <p v-else-if="data.documentName">{{ data.documentName }}</p>
              <p v-else class="text-gray-400">-</p>
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
                  @click="router.push({ name: 'ann003Detail', params: { id: data.id } })"
                />
                <Button
                  icon="pi pi-trash"
                  class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!"
                  text
                  rounded
                  @click="onDeleteAsync(data.id)"
                />
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

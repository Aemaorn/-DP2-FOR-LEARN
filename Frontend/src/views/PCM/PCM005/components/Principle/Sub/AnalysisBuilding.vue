<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField, InputNumber } from '@/components/forms';
import { RentalAnalysisType } from '@/enums/PCM005/principle';
import { useMenuStore } from '@/stores/menu';
import { usePcm005PrincipleStore } from '@/stores/PCM/PCM005/principle';
import { computed, ref } from 'vue';

const analysisData = [
  {
    title: 'NPV',
  },
  {
    title: 'Payback Period',
  },
  {
    title: 'Discounted Payback Period',
  },
];

const fileRef = ref();
const menuStore = useMenuStore();
const store = usePcm005PrincipleStore();
const { onImportAnalysisBuildingAsync, onExportAnalysisAsync } = store;

const yearList = computed(() => {
  const allYears = store.body.rentalAnalyses?.flatMap(
    item => (item.details ?? []).map(detail => detail.year)
  );

  const uniqueYears = Array.from(new Set(allYears)).sort((a, b) => a - b);

  return uniqueYears;
});

const onSelectedFile = async (event: HTMLInputElement) => {
  if (event.files?.length === 0) return;
  const fileListType = event.files as FileList;

  const selectedFile = Array.from(fileListType)[0];

  if (selectedFile) {
    await onImportAnalysisBuildingAsync(selectedFile);
  }

  event.value = '';
};
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="การวิเคราะห์ความคุ้มค่าของการเช่าอาคารสถานที่">
        <template #action v-if="store.status.canEdit && menuStore.hasManage">
          <Button label="Export" icon="pi pi-file-export" severity="danger" variant="outlined"
            @click="onExportAnalysisAsync(store.body.id)" />
          <Button label="Import" icon="pi pi-file-import" severity="warn" @click="fileRef.click()" />
          <input type="file" class="hidden" ref="fileRef"
            accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            @change="(e) => onSelectedFile(e.target as HTMLInputElement)" />
        </template>
      </TitleHeader>
      <DataTable :value="store.body.rentalAnalyses?.filter(r => r.type === RentalAnalysisType.General)"
        tableStyle="min-width: 50rem">
        <Column>
          <template #header>
            <p class="text-center font-bold w-full">รายการ</p>
          </template>
          <template #body="{ data }">
            <p>{{ data.description }}</p>
          </template>
        </Column>
        <Column v-for="(year, index) in yearList" :key="year">
          <template #header>
            <p class="text-center font-bold w-full">{{ year }}</p>
          </template>
          <template #body="{ data }">
            <InputNumber
              v-if="data.details && data.details[index]"
              v-model="data.details[index].amount"
              hide-details
              grouping
              :min-fraction-digits="2"
              :disabled="!store.status.canEdit || !menuStore.hasManage"
            />
          </template>
        </Column>
      </DataTable>

      <div class="mt-5">
        <div class="flex items-center gap-3">
          <p>งบกำไรขาดทุน</p>
          <div class="h-px bg-gray-300 flex-1" />
        </div>
        <DataTable :value="store.body.rentalAnalyses?.filter(r => r.type === RentalAnalysisType.ProfitAndLoss)"
          tableStyle="min-width: 50rem">
          <Column>
            <template #header>
              <p class="text-center font-bold w-full">งบกำไรขาดทุน</p>
            </template>
            <template #body="{ data }">
              <p>{{ data.description }}</p>
            </template>
          </Column>
          <Column v-for="(year, index) in yearList" :key="year">
            <template #header>
              <p class="text-center font-bold w-full">{{ year }}</p>
            </template>
            <template #body="{ data }">
              <InputNumber
                v-if="data.details && data.details[index]"
                v-model="data.details[index].amount"
                hide-details
                grouping
                :fractionDigits="2"
                :disabled="!store.status.canEdit || !menuStore.hasManage"
              />
            </template>
          </Column>
        </DataTable>
      </div>

      <div class="mt-5">
        <div class="flex items-center gap-3">
          <p>สรุปผลการวิเคราะห์ความคุ้มค่าของการเช่าอาคารสถานที่</p>
          <div class="h-px bg-gray-300 flex-1" />
        </div>
        <DataTable :value="analysisData" tableStyle="min-width: 50rem">
          <Column>
            <template #header>
              <p class="text-center font-bold w-full">ความคุ้มค่าโครงการ</p>
            </template>
            <template #body="{ data }">
              <p>{{ data.title }}</p>
            </template>
          </Column>
          <Column>
            <template #header>
              <p class="text-center font-bold w-full">จำนวน</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-model="store.body.analysisSummaryNpv" hide-details grouping input-class="text-end"
                v-if="data.title === 'NPV'" :disabled="!store.status.canEdit || !menuStore.hasManage" />
              <InputNumber v-model="store.body.analysisSummaryPaybackYearPeriod" hide-details grouping
                input-class="text-end" v-if="data.title === 'Payback Period'"
                :disabled="!store.status.canEdit || !menuStore.hasManage" />
              <InputNumber v-model="store.body.analysisSummaryDiscountedPaybackYearPeriod" hide-details grouping
                input-class="text-end" v-if="data.title === 'Discounted Payback Period'"
                :disabled="!store.status.canEdit || !menuStore.hasManage" />
            </template>
          </Column>
          <Column>
            <template #header>
              <p class="text-center font-bold w-full">หน่วย</p>
            </template>
            <template #body="{ data }">
              <InputField model-value="ล้านบาท" hide-details readonly v-if="data.title === 'NPV'" />
              <InputField model-value="ปี" hide-details readonly v-if="data.title === 'Payback Period'" />
              <InputField model-value="ปี" hide-details readonly v-if="data.title === 'Discounted Payback Period'" />
            </template>
          </Column>
        </DataTable>
      </div>
    </template>
  </Card>
</template>

<style lang="scss" scoped>
:deep(th),
:deep(td) {
  background-color: oklch(96.7% 0.003 264.542);
}
</style>

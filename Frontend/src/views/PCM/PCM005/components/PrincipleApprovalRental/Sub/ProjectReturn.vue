<script setup lang="ts">
import type { RoiPerfResults } from '@/models/PCM/PCM005/principleApprovalRental';
import { BudgetYearSelect, InputNumber } from '@/components/forms';
import { PerformanceResultGroup } from '@/enums/PCM005/principle';
import { Button } from 'primevue';
import { usePcm005PrinApproveRentStore } from '@/stores/PCM/PCM005/principleApprovalRental';
import { useMenuStore } from '@/stores/menu';

const menuStore = useMenuStore();
const store = usePcm005PrinApproveRentStore();

const PerformanceResultGroupLabels: Record<PerformanceResultGroup, string> = {
  [PerformanceResultGroup.DepositRemaining]: 'ผลการดำเนินการ - เงินฝากคงเหลือ',
  [PerformanceResultGroup.LoanExisting]: 'ผลการดำเนินการ - สินเชื่อคงเหลือ',
  [PerformanceResultGroup.LoanNew]: 'ผลการดำเนินการ - สินเชื่อปล่อยใหม่',
};

const getPerformanceResultGroupOptions = (): { value: PerformanceResultGroup; label: string }[] =>
  Object.values(PerformanceResultGroup).map(value => ({
    value,
    label: PerformanceResultGroupLabels[value],
  }));

const addRoiResult = (group: PerformanceResultGroup) => {
  store.body.roiPerfResults?.push({
    year: (new Date().getFullYear() + 543),
    sequence: store.body.roiPerfResults?.filter(r => r.performanceResultGroup === group)?.length + 1,
    performanceResultGroup: group,
  } as RoiPerfResults);
};

const deleteRoiResult = (group: PerformanceResultGroup, index: number) => {
  store.body.roiPerfResults?.splice(index, 1);

  store.body.roiPerfResults
    ?.filter(f => f.performanceResultGroup == group)
    .forEach((item, i) => {
      item.sequence = i + 1;
    });
};
</script>

<template>
  <div>
    <div class="flex items-center gap-3">
      <p>ปริมาณสินเชื่อปล่อยใหม่ และเงินฝากสะสม</p>
      <div class="h-px bg-gray-300 flex-1" />
    </div>
    <DataTable :value="store.body.roiLoanAndDepositSummaries" tableStyle="min-width: 50rem">
      <Column field="list">
        <template #header>
          <p class="w-full font-bold text-center">รายการ</p>
        </template>
        <template #body="{ data }">
          <p>{{ data.activityDescription }}</p>
        </template>
      </Column>
      <Column field="first">
        <template #header>
          <div class="w-full font-bold text-center">
            <p>ปีที่ 1</p>
            <p>ม.ค. - ธ.ค. 2564</p>
          </div>
        </template>
        <template #body="{ data }">
          <InputNumber v-model="data.amountYear1" hideDetails grouping :min-fraction-digits="2"
            :disabled="!store.status.canEdit || !menuStore.hasManage" rules="required" />
        </template>
      </Column>
      <Column field="second">
        <template #header>
          <div class="w-full font-bold text-center">
            <p>ปีที่ 2</p>
            <p>ม.ค. - ธ.ค. 2565</p>
          </div>
        </template>
        <template #body="{ data }">
          <InputNumber v-model="data.amountYear2" hideDetails grouping :min-fraction-digits="2"
            :disabled="!store.status.canEdit || !menuStore.hasManage" rules="required" />
        </template>
      </Column>
      <Column field="thrid">
        <template #header>
          <div class="w-full font-bold text-center">
            <p>ปีที่ 3</p>
            <p>ม.ค. - ธ.ค. 2566</p>
          </div>
        </template>
        <template #body="{ data }">
          <InputNumber v-model="data.amountYear3" hideDetails grouping :min-fraction-digits="2"
            :disabled="!store.status.canEdit || !menuStore.hasManage" rules="required" />
        </template>
      </Column>
    </DataTable>
  </div>

  <div class="mt-5" v-for="(data, index) in getPerformanceResultGroupOptions()" :key="index">
    <div class="flex items-center gap-3">
      <p>{{ data.label }}</p>
      <div class="h-px bg-gray-300 flex-1" />
      <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
        class="bg-white! hover:bg-red-50!" @click="() => addRoiResult(data.value)"
        v-if="store.status.canEdit && menuStore.hasManage" />
    </div>
    <DataTable :value="store.body.roiPerfResults?.filter(r => r.performanceResultGroup === data.value)"
      tableStyle="min-width: 50rem" class="mt-5">
      <ColumnGroup type="header">
        <Row>
          <Column :rowspan="12">
            <template #header>
              <p class="text-center w-full font-bold">ปี</p>
            </template>
          </Column>
          <Column :colspan="3">
            <template #header>
              <p class="text-center w-full font-bold">ปีที่ 1</p>
            </template>
          </Column>
          <Column :colspan="3">
            <template #header>
              <p class="text-center w-full font-bold">จำนวนเงิน (ล้านบาท)</p>
            </template>
          </Column>
          <Column>
          </Column>
        </Row>
        <Row>
          <Column>
            <template #header>
              <p class="text-center w-full font-bold">ทำได้จริง</p>
            </template>
          </Column>
          <Column>
            <template #header>
              <p class="text-center w-full font-bold">Growth (%)</p>
            </template>
          </Column>
          <Column>
            <template #header>
              <p class="text-center w-full font-bold">เป้าหมาย</p>
            </template>
          </Column>
          <Column>
            <template #header>
              <p class="text-center w-full font-bold">ทำได้จริง</p>
            </template>
          </Column>
          <Column>
            <template #header>
              <p class="text-center w-full font-bold">คิดเป็น</p>
            </template>
          </Column>
          <Column>
            <template #header>
              <p class="text-center w-full font-bold">Growth (%)</p>
            </template>
          </Column>
          <Column>
          </Column>
        </Row>
      </ColumnGroup>
      <Column field="year" bodyClass="min-w-[200px]">
        <template #body="{ data }">
          <BudgetYearSelect v-model="data.year" rules="required" label="ปี" hideDetails
            :disabled="!store.status.canEdit || !menuStore.hasManage" />
        </template>
      </Column>
      <Column field="firstYearTrue" bodyClass="min-w-[200px]">
        <template #body="{ data }">
          <InputNumber v-model="data.accountActual" hideDetails grouping :min-fraction-digits="2" rules="required"
            :disabled="!store.status.canEdit || !menuStore.hasManage" />
        </template>
      </Column>
      <Column field="firstYearGrowh" bodyClass="min-w-[200px]">
        <template #body="{ data }">
          <InputNumber v-model="data.accountGrowth" hideDetails grouping :min-fraction-digits="2" rules="required"
            :disabled="!store.status.canEdit || !menuStore.hasManage" />
        </template>
      </Column>
      <Column field="firstYearPoint" bodyClass="min-w-[200px]">
        <template #body="{ data }">
          <InputNumber v-model="data.amountTarget" hideDetails grouping :min-fraction-digits="2" rules="required"
            :disabled="!store.status.canEdit || !menuStore.hasManage" />
        </template>
      </Column>
      <Column field="moneyTrue" bodyClass="min-w-[200px]">
        <template #body="{ data }">
          <InputNumber v-model="data.amountActual" hideDetails grouping :min-fraction-digits="2" rules="required"
            :disabled="!store.status.canEdit || !menuStore.hasManage" />
        </template>
      </Column>
      <Column field="moneyThing" bodyClass="min-w-[200px]">
        <template #body="{ data }">
          <InputNumber v-model="data.amountRate" hideDetails grouping :min-fraction-digits="2" rules="required"
            :disabled="!store.status.canEdit || !menuStore.hasManage" />
        </template>
      </Column>
      <Column field="amountGrowth" bodyClass="min-w-[200px]">
        <template #body="{ data }">
          <InputNumber v-model="data.amountGrowth" hideDetails grouping :min-fraction-digits="2" rules="required"
            :disabled="!store.status.canEdit || !menuStore.hasManage" />
        </template>
      </Column>
      <Column bodyClass="min-w-[100px]">
        <template #body="{ index }">
          <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
            v-if="store.status.canEdit && menuStore.hasManage" @click="deleteRoiResult(data.value, index)" />
        </template>
      </Column>
    </DataTable>
  </div>
</template>

<style lang="scss" scoped>
:deep(th),
:deep(td) {
  background-color: oklch(96.7% 0.003 264.542);
}
</style>

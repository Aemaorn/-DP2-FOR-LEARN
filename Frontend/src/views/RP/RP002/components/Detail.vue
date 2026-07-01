<script setup lang="ts">
import type { Option, OptionBadge } from '@/models/shared/option';
import { Card, Button, SelectButton, Badge, DataView } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Datepicker, BudgetYearSelect, InputArea, Select } from '@/components/forms';
import { QuarterOptions } from '@/constants/date';
import { computed, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useRP002DetailStore } from '@/stores/RP/RP002/detail';
import { ToDateOnly } from '@/helpers/dateTime';
import { formatCurrency } from '@/helpers/currency';
import { BadgeStatus } from '@/components';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import DataTableDialog from './DataTableDialog.vue';
import type { RP002ContractComplete } from '@/models/RP/rp002';
import RP002Service from '@/services/RP/rp002';
import { HttpStatusCode } from 'axios';

const store = useRP002DetailStore();
const router = useRouter();

const openPpDetail = (procurementId: string): void => {
  const url = router.resolve({ path: `/pp/detail/${procurementId}` }).href;
  window.open(url, '_blank');
};

const showModal = ref(false);
const statusValue = ref("All");
const options = ref<Array<OptionBadge>>([
  {
    label: 'ทั้งหมด',
    count: 0,
    value: "All",
    bgColorClass: 'bg-gray-300',
    textColorClass: 'text-black',
  },
  {
    label: 'สัญญาเช่า',
    count: 0,
    value: "CMType003",
    bgColorClass: 'bg-yellow-300',
    textColorClass: 'text-white',
  },
  {
    label: 'สัญญาซื้อขาย',
    count: 0,
    value: "CMType001",
    bgColorClass: 'bg-yellow-300',
    textColorClass: 'text-white'
  },
  {
    label: 'สัญญาจ้าง',
    count: 0,
    value: "CMType002",
    bgColorClass: 'bg-yellow-300',
    textColorClass: 'text-white'
  },
]);

const filterData = computed((): Array<RP002ContractComplete> | undefined => {
  if (statusValue.value === 'All') return store.body.detail;

  return store.body.detail?.filter((d): boolean => d.contractTypeCode === statusValue.value);
});

const usedQuarters = ref<number[]>([]);

const quarterOptionsWithDisabled = computed((): Option[] =>
  QuarterOptions.map((opt): Option => ({
    ...opt,
    disabled: usedQuarters.value.includes(opt.value as number),
  }))
);

const fetchUsedQuarters = async (year: number): Promise<void> => {
  if (!year) return;
  const { data, status } = await RP002Service.GetListAsync({ year, pageNumber: 1, pageSize: 4 });
  if (status === HttpStatusCode.Ok) {
    const currentId = store.body.id;
    usedQuarters.value = data.data.data
      .filter((r): boolean => !currentId || r.id !== currentId)
      .map((r): number => r.quarter);
  }
};

watch([(): number => store.body.year, (): string | undefined => store.body.id], ([year]): void => {
  if (year) fetchUsedQuarters(year);
}, { immediate: true });

const quarterMonthRanges: Record<number, { startMonth: number; endMonth: number; lastDay: number }> = {
  1: { startMonth: 1, endMonth: 3, lastDay: 31 },
  2: { startMonth: 4, endMonth: 6, lastDay: 30 },
  3: { startMonth: 7, endMonth: 9, lastDay: 30 },
  4: { startMonth: 10, endMonth: 12, lastDay: 31 },
};

watch([(): number => store.body.quarter, (): number => store.body.year], ([quarter, buddhistYear]): void => {
  if (!store.status.canEdit || !quarter || !buddhistYear) return;
  const ceYear = buddhistYear - 543;
  const range = quarterMonthRanges[quarter];
  if (!range) return;
  store.body.signStartDate = new Date(ceYear, range.startMonth - 1, 1);
  store.body.signEndDate = new Date(ceYear, range.endMonth - 1, range.lastDay);
});

watch((): Array<RP002ContractComplete> | undefined => store.body.detail, (newValue): void => {
  if (newValue) {
    options.value[0].count = newValue.length;
    options.value[1].count = newValue.filter((n): boolean => n.contractTypeCode === 'CMType003').length;
    options.value[2].count = newValue.filter((n): boolean => n.contractTypeCode === 'CMType001').length;
    options.value[3].count = newValue.filter((n): boolean => n.contractTypeCode === 'CMType002').length;
  }
});
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลเอกสาร" />
      <div class="mb-4 px-4 grid grid-cols-1 lg:grid-cols-3 gap-y-8 mt-8 gap-2">
        <InputField label="เลขที่เอกสาร" :modelValue="undefined" disabled />
        <Datepicker label="วันที่ทำเอกสาร" rules="required" v-model="store.body.documentDate"
          :disabled="!store.status.canEdit" />
        <BudgetYearSelect class="lg:col-start-1 col-start-auto" label="ปี" v-model="store.body.year" rules="required"
          :disabled="!store.status.canEdit" />
        <Select label="ไตรมาส" rules="required" :options="quarterOptionsWithDisabled" v-model="store.body.quarter" :disabled="!store.status.canEdit" />
        <Datepicker class="lg:col-start-1 col-start-auto" label="วันที่ลงนามในสัญญา ตั้งแต่" rules="required"
          v-model="store.body.signStartDate" :max-date="store.body.signEndDate" :disabled="!store.status.canEdit" />
        <Datepicker label="ถึงวันที่" rules="required" v-model="store.body.signEndDate"
          :min-date="store.body.signStartDate" :disabled="!store.status.canEdit" />
      </div>
    </template>
  </Card>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="รายการสัญญา">
        <template #action>
          <Button icon="pi pi-file-excel" label="ส่งออกข้อมูลสัญญา" severity="success" variant="outlined"
            class="bg-white hover:bg-green-50" @click="store.exportDetailAsync()" v-if="store.body.id" />
        </template>
      </TitleHeader>
      <div class="my-4 px-2 flex justify-end gap-2">
        <Button icon="pi pi-refresh" label="ดึงรายการสัญญาเพิ่มเติม" severity="primary" variant="outlined"
          @click="() => store.getContractCompleteAsync(store.body.signStartDate, store.body.signEndDate)"
          v-if="store.status.canEdit" />
      </div>
      <div class="bg-gray-200 mt-5 py-2 px-5 rounded-sm flex justify-between items-center">
        <SelectButton v-model="statusValue" :allowEmpty="false" :options="options" option-label="label"
          v-if="options.length > 0" option-value="value" unstyled>
          <template #option="slotProps">
            <div class="cursor-pointer duration-100 flex! flex-nowrap! items-center gap-2 mr-3"
              :class="`${slotProps.option.value == statusValue ? 'border-b-4 px-2 border-primary/90 font-bold' : 'border-b-4 border-transparent'}`">
              <small>
                {{ slotProps.option.label }}
              </small>
              <Badge :value="slotProps.option.count"
                :class="`${slotProps.option.bgColorClass} ${slotProps.option.textColorClass}`" />
            </div>
          </template>
        </SelectButton>
        <Button variant="outlined" severity="primary" class="bg-white hover:bg-yellow-50" v-if="store.body.id"
          @click="() => { store.getContractSummaryAsync(store.body.id!); showModal = true; }">ตารางสรุปการทำสัญญา</Button>
      </div>
      <DataView :value="filterData" data-key="id" class="mt-5">
        <template #list="{ items }">
          <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="grid lg:grid-cols-12 px-2">
              <div class="lg:col-span-8">
                <InfoRow label="เลขที่สัญญา">
                  <p class="underline text-blue-400 cursor-pointer" @click="openPpDetail(data.procurementId!)">
                    {{ data.contractNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อสัญญา">
                  <p class="font-bold">
                    {{ data.contractName }}
                  </p>
                </InfoRow>
                  <InfoRow label="ประเภทสัญญา">
                  <p class="font-bold">
                    {{ data.contractTypeName }}
                  </p>
                </InfoRow>
                <InfoRow label="วันที่ลงนามในสัญญา">
                  <p>
                    {{ ToDateOnly(data.contractSignedDate) }}
                  </p>
                </InfoRow>
                <InfoRow label="คู่สัญญา">
                  <p>
                    {{ data.entrepreneurName }}
                  </p>
                </InfoRow>
                <InfoRow label="วงเงินตามสัญญา">
                  <p>
                    {{ formatCurrency(data.budget) }}
                  </p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4">
                <div class="flex items-center gap-2 mt-2 mr-2" v-if="data.overdue !== undefined">
                  <BadgeStatus color="green" label="สัญญาภายในไตรมาส" v-if="data.overdue" />
                  <BadgeStatus color="red" label="สัญญานอกไตรมาส" v-if="!data.overdue" />
                </div>
                <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text" @click="store.onRemoveDetail(index as number)" v-if="store.status.canEdit" />
              </div>
            </div>

            <InputArea label="หมายเหตุ" class="mt-8 px-2" hide-details v-model="data.description"
              :disabled="!store.status.canEdit" />
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <DataTableDialog v-model="showModal" />
    </template>
  </Card>
</template>
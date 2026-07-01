<script setup lang="ts">
import type { JorPor04PaymentTerm } from '@/views/PP/models/PP004/pp004Model';
import { TitleHeader } from '@/components/cosmetic';
import { InputNumber, InputArea, Select } from '@/components/forms';
import { Card, Button, DataTable, type DataTableRowReorderEvent } from 'primevue';
import { DatatableHelper } from '@/helpers/datable';
import { usePP004Store } from '@/views/PP/stores/PP004/pp004Store';
import { useMenuStore } from '@/stores/menu';
import { onMounted, ref, computed, h, type Ref } from 'vue';
import { addMonths, addYears, differenceInCalendarDays } from 'date-fns';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode, PeriodTypeCodeEnum, ProRateTypeCodeEnum } from '@/enums/shared';
import type { Option } from '@/models/shared/option';
import { HttpStatusCode } from 'axios';
import { Form } from 'vee-validate';
import { PP002DocumentTemplate } from '@/views/PP/enums/pp002';

const { onRowReorder } = DatatableHelper();

const pp004Store = usePP004Store();
const menuStore = useMenuStore();
const dropdown = ref<Option[]>([]);
const periodDropdown = ref<Option[]>([]);

onMounted(async (): Promise<void> => {

  await Promise.all([getDropdownAsync(dropdown, EGroupCode.SplitPayment), getDropdownAsync(periodDropdown, EGroupCode.PeriodType),]);

  if (pp004Store.body.paymentTerms && pp004Store.body.paymentTerms.length === 0) {
    handleAddItem(ProRateTypeCodeEnum.SplitPayment001);
  }
});

const getDropdownAsync = async (target: Ref<Array<Option>>, group: EGroupCode): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(group, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const handleAddItem = (paymentType?: string) => {
  const nonMATerms = pp004Store.body.paymentTerms.filter(x => !x.isMA);
  pp004Store.body.paymentTerms.push({
    description: '',
    sequence: nonMATerms.length + 1,
    termNumber: nonMATerms.length + 1,
    isMA: false,
    paymentTypeCode: paymentType,
  } as JorPor04PaymentTerm);
};

const handleAddItemIsMA = () => {
  pp004Store.body.paymentTerms.push({
    description: '',
    sequence: pp004Store.body.paymentTerms.filter(x => x.isMA).length + 1,
    isMA: true,
    paymentTypeCode: ProRateTypeCodeEnum.SplitPayment002,
    totalPeriodTypeCode: PeriodTypeCodeEnum.PeriodType004,
  } as JorPor04PaymentTerm);
};

const deleteItem = (index: number): void => {
  const nonMATerms = pp004Store.body.paymentTerms.filter(x => !x.isMA);
  const itemToDelete = nonMATerms[index];
  const originalIndex = pp004Store.body.paymentTerms.indexOf(itemToDelete);
  if (originalIndex !== -1) {
    pp004Store.body.paymentTerms.splice(originalIndex, 1);
  }

  pp004Store.body.paymentTerms.filter(x => !x.isMA).forEach((t, i) => {
    t.sequence = i + 1;
  });
};

const deleteItemIsMA = (index: number): void => {
  const maTerms = pp004Store.body.paymentTerms.filter(x => x.isMA);
  const itemToDelete = maTerms[index];
  const originalIndex = pp004Store.body.paymentTerms.indexOf(itemToDelete);
  if (originalIndex !== -1) {
    pp004Store.body.paymentTerms.splice(originalIndex, 1);
  }

  pp004Store.body.paymentTerms.filter(x => x.isMA).forEach((t, i) => {
    t.sequence = i + 1;
  });
};

const onProRateTypeChange = (val: string): void => {
  pp004Store.body.paymentTerms = pp004Store.body.paymentTerms.filter(x => x.isMA);
  handleAddItem(val);
};

const handleReOrder = (event: DataTableRowReorderEvent): void => {
  pp004Store.body.paymentTerms = onRowReorder(event);
};

const nonMaTerms = computed(() =>
  pp004Store.body.paymentTerms.filter(x => !x.isMA && x.paymentTypeCode === pp004Store.body.paymentTypeCode)
);

const getTotalPercent = computed((): number => {
  if (!nonMaTerms.value || nonMaTerms.value.length === 0) return 0;
  return nonMaTerms.value.reduce((sum, d) => sum + (d.percent || 0), 0);
});

// Dialog ProRateTypeCode -> SplitPayment001

const DialogButton = (show: boolean) => {
  if (show) {
    return h(Button, {
      label: 'กำหนดงวดงานและการจ่ายเงิน',
      rounded: true,
      outlined: true,
      icon: 'pi pi-plus',
      'onClick': () => {
        dialogItem.value = {
          period: 1,
        };

        isShow.value = !isShow.value;
      },
    })
  }

  return undefined;
}

type DialogItemType = {
  period: number;
  amount: number;
  type: 'day' | 'month' | 'year';
  startDate: Date;
};

const typeOptions: Option[] = [
  { value: 'day', label: 'วัน' },
  { value: 'month', label: 'เดือน' },
  { value: 'year', label: 'ปี' },
];

const isShow = ref(false);

const dialogItem = ref<Partial<DialogItemType>>({} as Partial<DialogItemType>);

const generatePaymentTermsDetails = (
  period: number,
  amount: number,
  type: 'day' | 'month' | 'year',
  startDate?: Date
): JorPor04PaymentTerm[] => {
  if (!Number.isInteger(period) || period < 1) {
    throw new Error('period must be a positive integer');
  }

  if (amount < 1) {
    throw new Error('amount must be at least 1');
  }

  if ((type === 'month' || type === 'year') && !startDate) {
    throw new Error('startDate is required for month/year calculations');
  }

  const basePercent = Math.floor((10000 / period)) / 100;
  const remainder = Math.round((100 - basePercent * period) * 100) / 100;

  const details: JorPor04PaymentTerm[] = [];

  for (let i = 0; i < period; i++) {
    let cumulativeDays = 0;

    if (type === 'day') {
      cumulativeDays = amount * (i + 1);
    } else if (type === 'month') {
      const termEnd = addMonths(startDate as Date, amount * (i + 1));
      cumulativeDays = differenceInCalendarDays(termEnd, startDate as Date);
    } else {
      const termEnd = addYears(startDate as Date, amount * (i + 1));
      cumulativeDays = differenceInCalendarDays(termEnd, startDate as Date);
    }

    details.push({
      termNumber: i + 1,
      percent: i === period - 1 ? basePercent + remainder : basePercent,
      period: cumulativeDays,
      description: '',
      sequence: i + 1,
      isMA: false,
      paymentTypeCode: ProRateTypeCodeEnum.SplitPayment001,
    } as JorPor04PaymentTerm);
  }

  return details;
};

const onDialogSubmit = (): void => {
  const { period, amount, type, startDate } = dialogItem.value;

  if (!period || !amount || !type) return;

  if ((type === 'month' || type === 'year') && !startDate) return;

  const details = generatePaymentTermsDetails(period, amount, type, startDate as Date | undefined);

  // Replace existing non-MA SplitPayment001 terms with generated ones
  pp004Store.body.paymentTerms = [
    ...pp004Store.body.paymentTerms.filter(x => x.isMA || x.paymentTypeCode !== ProRateTypeCodeEnum.SplitPayment001),
    ...details,
  ];

  isShow.value = false;
};

const isBuyAndHireTor = new Set([
  PP002DocumentTemplate.TorBuyWithHire60,
  PP002DocumentTemplate.TorBuyWithHire80,
  PP002DocumentTemplate.TorHireWithHire60,
  PP002DocumentTemplate.TorHireWithHire80,
]);
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="งวดงานและการจ่ายเงิน">
        <template #action>
          <component
            :is="DialogButton(pp004Store.body.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment001 && pp004Store.IsEdit && menuStore.hasManage)" />
          <Button
            v-if="pp004Store.IsEdit && menuStore.hasManage && pp004Store.body.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment001"
            icon="pi pi-plus" label="เพิ่มข้อมูล" variant="outlined"
            @click="() => handleAddItem(pp004Store.body.paymentTypeCode)" />
        </template>
      </TitleHeader>
      <div class="mt-10">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-2">
          <Select label="การแบ่งจ่าย" v-model="pp004Store.body.paymentTypeCode" :options="dropdown" rules="required"
            :disabled="!pp004Store.IsEdit || !menuStore.hasManage"
            @update:model-value="(val: string) => onProRateTypeChange(val)" />
        </div>

        <!-- Summary bar -->
        <div
          v-if="(pp004Store.body.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment001 || pp004Store.body.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment002) && nonMaTerms.length > 0"
          class="flex items-center gap-3 rounded-t-lg bg-gray-50 border border-b-0 border-gray-200 px-4 py-3">
          <div class="flex-1">
            <div class="h-2.5 w-full rounded-full bg-gray-200 overflow-hidden">
              <div class="h-full rounded-full transition-all duration-300"
                :class="getTotalPercent === 100 ? 'bg-green-500' : getTotalPercent > 100 ? 'bg-red-500' : 'bg-amber-500'"
                :style="{ width: Math.min(getTotalPercent, 100) + '%' }" />
            </div>
          </div>
          <span class="text-sm font-semibold whitespace-nowrap"
            :class="getTotalPercent === 100 ? 'text-green-600' : getTotalPercent > 100 ? 'text-red-500' : 'text-amber-600'">
            {{ getTotalPercent.toFixed(2) }}%
          </span>
          <span v-if="getTotalPercent === 100"
            class="material-symbols-outlined text-green-600 text-lg">check_circle</span>
          <span v-else class="material-symbols-outlined text-amber-500 text-lg">warning</span>
          <span class="text-xs text-gray-500">{{ nonMaTerms.length }} รายการ</span>
        </div>

        <DataTable
          v-if="pp004Store.body.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment001 || pp004Store.body.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment002"
          :value="pp004Store.body.paymentTerms.filter(x => !x.isMA && x.paymentTypeCode === pp004Store.body.paymentTypeCode)"
          @row-reorder="handleReOrder">
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">ลำดับ</p>
            </template>
            <template #body="{ index }">
              <p class="text-center">{{ index + 1 }}</p>
            </template>
          </Column>
          <Column v-if="pp004Store.body.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment001"
            bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">งวดที่</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-model="data.termNumber" rules="required|min_value:1"
                :disabled="!pp004Store.IsEdit || !menuStore.hasManage" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">จำนวน %</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-model="data.percent" rules="required|max_value:100"
                :disabled="!pp004Store.IsEdit || !menuStore.hasManage" :min-fraction-digits="2" grouping />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">จำนวนวัน</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-model="data.period" rules="required" :disabled="!pp004Store.IsEdit || !menuStore.hasManage"
                grouping />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">รายละเอียด</p>
            </template>
            <template #body="{ data }">
              <InputArea v-model="data.description" rules="required"
                :disabled="!pp004Store.IsEdit || !menuStore.hasManage" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" v-if="pp004Store.IsEdit && menuStore.hasManage">
            <template #body="{ index }">
              <Button v-if="pp004Store.body.paymentTerms.filter(x => !x.isMA).length > 1" icon="pi pi-trash"
                class="mt-1" severity="danger" variant="text" @click="() => deleteItem(index)" />
            </template>
          </Column>
          <template #empty>
            <p class="text-center text-gray-500">ไม่มีข้อมูล</p>
          </template>
        </DataTable>
        <div v-else-if="pp004Store.body.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment003">
          <div
            v-for="(item, index) in pp004Store.body.paymentTerms.filter(x => !x.isMA && x.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment003)"
            :key="index" class="md:grid grid-cols-8 gap-2 mt-10">
            <InputNumber class="col-span-2" label="ชำระทุกๆ (วัน/เดือน/ปี)" v-model="item.period" rules="required"
              :disabled="!pp004Store.IsEdit || !menuStore.hasManage" grouping />
            <Select class="col-span-2" v-model="item.periodTypeCode" :options="periodDropdown" rules="required"
              :disabled="!pp004Store.IsEdit || !menuStore.hasManage" />
            <InputNumber class="col-span-2" label="รวม" v-model="item.totalPeriod" rules="required"
              :disabled="!pp004Store.IsEdit || !menuStore.hasManage" grouping :max-number="60" />
            <Select class="col-span-2" v-model="item.totalPeriodTypeCode" :options="periodDropdown" rules="required"
              :disabled="!pp004Store.IsEdit || !menuStore.hasManage" />
          </div>
        </div>
      </div>

      <section v-if="isBuyAndHireTor.has(pp004Store.body.torTemplateCode as any)">
        <TitleHeader label="สัญญาจ้างบริการบำรุงรักษาฯ (ถ้ามี)" :hidden-icon="true">
          <template #action>
            <Button
              v-if="pp004Store.IsEdit && menuStore.hasManage && pp004Store.body.paymentTerms.filter(x => x.isMA && x.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment002).length === 0"
              icon="pi pi-plus" label="เพิ่มข้อมูล" severity="warn" variant="outlined"
              @click="() => handleAddItemIsMA()" />
          </template>
        </TitleHeader>
        <div class="mt-10"
          v-if="pp004Store.body.paymentTerms?.filter(x => x.isMA && x.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment002)?.length > 0">
          <div
            v-for="(item, index) in pp004Store.body.paymentTerms.filter(x => x.isMA && x.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment002)"
            :key="index" class="md:grid grid-cols-9 gap-2 mt-10">
            <InputNumber class="col-span-2" label="ชำระทุกๆ (วัน/เดือน/ปี)" v-model="item.period" rules="required"
              :disabled="!pp004Store.IsEdit || !menuStore.hasManage" grouping />
            <Select class="col-span-2" v-model="item.periodTypeCode" :options="periodDropdown" rules="required"
              :disabled="!pp004Store.IsEdit || !menuStore.hasManage" />
            <InputNumber class="col-span-2" label="รวม" v-model="item.totalPeriod" rules="required"
              :disabled="!pp004Store.IsEdit || !menuStore.hasManage" grouping :max-number="60" />
            <Select class="col-span-2" v-model="item.totalPeriodTypeCode" :options="periodDropdown" rules="required"
              :disabled="!pp004Store.IsEdit || !menuStore.hasManage" />
            <div class="flex items-center justify-center">
              <Button
                v-if="pp004Store.IsEdit && menuStore.hasManage && pp004Store.body.paymentTerms.filter(x => x.paymentTypeCode === ProRateTypeCodeEnum.SplitPayment002).length > 0"
                icon="pi pi-trash" severity="danger" variant="text" @click="() => deleteItemIsMA(index)" />
            </div>
          </div>
        </div>
      </section>
    </template>
  </Card>

  <Form v-slot="{ handleSubmit }">
    <Dialog v-model:visible="isShow" modal maximizable header="กำหนดงวดงานและการจ่ายเงิน" :draggable="false"
      @after-hide="() => {
        dialogItem = {} as Partial<DialogItemType>
      }" :close-on-escape="false" class="min-w-[65rem]" :breakpoints="{ '1199px': '75vw', '575px': '90vw' }">
      <template #default>
        <Divider class="mb-0 mt-0" />
        <div class="lg:grid grid-cols-3 gap-4 mt-10">
          <InputNumber label="จำนวนงวดทั้งหมด" v-model="dialogItem.period" rules="required" />

          <InputNumber class="lg:col-start-1" label="ระยะเวลา" v-model="dialogItem.amount" rules="required" />
          <Select label="ประเภท (วัน/เดือน/ปี)" v-model="dialogItem.type" :options="typeOptions" rules="required" />

          <Datepicker v-if="dialogItem.type && dialogItem.type != 'day'" label="นับตั้งแต่วันที่"
            v-model="dialogItem.startDate" rules="required" />
        </div>
      </template>
      <template #footer>
        <div class="flex flex-col w-full">
          <Divider class="mt-0" />
          <Button label="ยืนยัน" rounded fluid @click="handleSubmit(() => onDialogSubmit())" />
        </div>
      </template>
    </Dialog>
  </Form>
</template>
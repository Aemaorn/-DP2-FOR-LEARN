<script setup lang="ts">
import type { PaymentTerms, PaymentTermsDetail, TorPaymentTermPeriods } from '@/views/PP/models/PP002/pp002Model';
import type { Option } from '@/models/shared/option';
import { ArrayHelper } from '@/helpers/array';
import { InputNumber, InputArea, Select, Radio } from '@/components/forms';
import { Card, Button, DataTable, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { onMounted, ref, type Ref, watch, computed, h } from 'vue';
import { addMonths, addYears, differenceInCalendarDays } from 'date-fns';
import { EGroupCode, PeriodTypeCodeEnum, ProRateTypeCodeEnum } from '@/enums/shared';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { HttpStatusCode } from 'axios';
import { useMenuStore } from '@/stores/menu';
import defaultProps from '@/helpers/defaultProps';
import SharedService from '@/services/Shared/dropdown';
import { PP002DocumentTemplate } from '@/views/PP/enums/pp002';
import { Form } from 'vee-validate';

const props = defineProps({
  titleName: defaultProps(''),
  titlePaymentTermMa: defaultProps('สัญญาจ้างบริการบำรุงรักษาฯ'),
  showPeriodType: defaultProps(true),
  showPaymentTermMa: defaultProps(true),
});

const value = defineModel<PaymentTerms[]>({
  default: () => [],
  required: true,
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();
const { reSequence, addSequence } = ArrayHelper();

const resolvedTemplateCode = computed(() => store.PP002Detail.torDocumentTemplateCode);

const isHireTemplate = computed(() => ([
  PP002DocumentTemplate.TorBuyWithHire60,
  PP002DocumentTemplate.TorBuyWithHire80,
  PP002DocumentTemplate.TorHireWithHire60,
  PP002DocumentTemplate.TorHireWithHire80,
] as string[]).includes(resolvedTemplateCode.value));

const contractLabel = computed(() => {
  const templateCode = store.PP002Detail.torDocumentTemplateCode;
  if (['TorHireWithHire80', 'TorHireWithHire60'].includes(templateCode)) {
    return 'สัญญาจ้าง';
  }
  return 'สัญญาซื้อขาย';
});

const dropdown = ref<Option[]>([]);
const mPeriodTypeDropdown = ref<Array<Option>>([]);
const periodDropdown = ref<Option[]>([]);

const mockRadioData = [
  { value: true, label: 'มี' },
  { value: false, label: 'ไม่มี' },
] as Option[];

onMounted(async () => {
  await Promise.all([getDropdownAsync(dropdown, EGroupCode.SplitPayment), getDropdownAsync(mPeriodTypeDropdown, EGroupCode.MPeriodType), getDropdownAsync(periodDropdown, EGroupCode.PeriodType),]);

  if (isHireTemplate.value && value.value && value.value.length === 1 && !store.PP002Detail.isMigration) {
    addPaymentTerms();
  }
});

watch(() => store.PP002Detail.isMA, (newValue) => {
  if ((store.PP002Detail.isMigration && newValue) && (!store.PP002Detail?.paymentTermPeriods || store.PP002Detail?.paymentTermPeriods?.length === 0)) {
    addItemPaymentTermPeriods();
  }
});

const getDropdownAsync = async (target: Ref<Array<Option>>, group: EGroupCode): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(group, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const addItemPaymentTermPeriods = (): void => {

  store.PP002Detail.paymentTermPeriods = addSequence(store.PP002Detail.paymentTermPeriods, {} as TorPaymentTermPeriods);
};

const deleteItemPaymentTermPeriod = (index: number): void => {
  store.PP002Detail.paymentTermPeriods?.splice(index, 1);

  store.PP002Detail.paymentTermPeriods?.forEach((item, index) => {
    item.sequence = index + 1;
  });
};

const getNonMaDetails = (item: PaymentTerms): PaymentTermsDetail[] => item.details || [];

const onProRateTypeChange = (item: PaymentTerms, val?: string): void => {
  item.details = [{ termNumber: 1 } as PaymentTermsDetail];
  item.period = undefined;
  item.periodTypeCode = undefined;
  item.totalPeriod = undefined;
  item.totalPeriodTypeCode = undefined;

  if (val === ProRateTypeCodeEnum.SplitPayment002) {
    item.details.forEach((value) => {
      value.percent = 100;
    });
  }
};

const addPaymentTerms = (): void => {
  value.value.push({ proRateTypeCode: ProRateTypeCodeEnum.SplitPayment003, isMA: true, totalPeriodTypeCode: PeriodTypeCodeEnum.PeriodType004, details: [{ termNumber: 1 } as PaymentTermsDetail] } as PaymentTerms);
};

const addItem = (item: PaymentTerms): void => {
  if (item.details) {
    const newItem = {
      termNumber: item.details.length + 1,
    } as PaymentTermsDetail;

    item.details.push(newItem);

    return;
  }

  item.details = [{ termNumber: 1 } as PaymentTermsDetail];
};

const deleteItem = (item: PaymentTerms, index: number): void => {
  const details = item.details || [];

  details.splice(index, 1);
  details.forEach((d: PaymentTermsDetail, i: number) => {
    d.termNumber = i + 1;
  });

  item.details = details;
};

const onRowReorder = (item: PaymentTerms, event: DataTableRowReorderEvent): void => {
  const reordered = reSequence(event.value);

  reordered.forEach((d: PaymentTermsDetail, index: number) => {
    d.termNumber = index + 1;
  });

  item.details = reordered;
};

const onRowReorderPaymentTermPeriods = (event: DataTableRowReorderEvent): void => {
  store.PP002Detail.paymentTermPeriods = reSequence(event.value);
};

const getTotalPercent = (item: PaymentTerms): number => {
  if (!item.details || item.details.length === 0) return 0;
  return item.details.reduce((sum, d) => sum + (d.percent || 0), 0);
};


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
  defaultDescription: string;
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
  startDate?: Date,
  defaultDescription?: string
): PaymentTermsDetail[] => {
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

  const details: PaymentTermsDetail[] = [];

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
      description: defaultDescription ?? '',
    });
  }

  return details;
};

const onDialogSubmit = (): void => {
  const { period, amount, type, startDate } = dialogItem.value;

  if (!period || !amount || !type) return;

  if ((type === 'month' || type === 'year') && !startDate) return;

  const details = generatePaymentTermsDetails(period, amount, type, startDate as Date | undefined, dialogItem.value.defaultDescription);

  const targetItem = value.value.find(
    (item) => !item.isMA && item.proRateTypeCode === ProRateTypeCodeEnum.SplitPayment001
  );

  if (targetItem) {
    targetItem.details = details;
  }

  isShow.value = false;
};

// Per-row inline edit toggle (tracked by row id; rows without id are always editable)
const tableEditableIds = ref<Set<string>>(new Set());

const isRowEditable = (data: PaymentTermsDetail): boolean => {
  if (store.status.canEditTor && menuStore.hasManage) return true;
  if (!data.id) return true;
  return tableEditableIds.value.has(data.id);
};

const toggleRowEdit = (data: PaymentTermsDetail): void => {
  if (!data.id) return;
  const next = new Set(tableEditableIds.value);
  if (next.has(data.id)) { next.delete(data.id); } else { next.add(data.id); }
  tableEditableIds.value = next;
};
</script>

<template>
  <Card class="mb-4" data-section-id="phase-list" :data-section-label="props.titleName">
    <template #content>
      <TitleHeader :label="props.titleName" />
      <div v-for="(item, idx) in value" :key="idx" class="mt-10">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-2" v-if="!item.isMA && !store.PP002Detail.isMigration">
          <Select label="การแบ่งจ่าย" v-model="item.proRateTypeCode" :options="dropdown" rules="required"
            :disabled="!store.status.canEditTor || !menuStore.hasManage"
            @update:model-value="(val: string) => onProRateTypeChange(item, val)" />
        </div>

        <div v-if="!props.showPeriodType && props.showPaymentTermMa" class="flex justify-end gap-x-2">
          <TitleHeader :label="idx === 0 ? contractLabel : props.titlePaymentTermMa" :hidden-icon="true">
            <template #action>
              <component
                :is="DialogButton((item.proRateTypeCode === ProRateTypeCodeEnum.SplitPayment001 && !item.isMA))"
                :key="idx"
                v-if="store.status.canEditTor && menuStore.hasManage && item.proRateTypeCode === ProRateTypeCodeEnum.SplitPayment001" />
              <Button label="เพิ่มข้อมูล" icon="pi pi-plus" severity="primary" variant="outlined"
                class="bg-white! hover:bg-red-50!" @click="addItem(item)"
                v-if="store.status.canEditTor && menuStore.hasManage && item.proRateTypeCode === ProRateTypeCodeEnum.SplitPayment001" />
            </template>
          </TitleHeader>
        </div>

        <div v-if="!props.showPaymentTermMa" class="flex justify-end gap-x-2">
          <component :is="DialogButton(item.proRateTypeCode === ProRateTypeCodeEnum.SplitPayment001 && !item.isMA)"
            :key="idx"
            v-if="store.status.canEditTor && menuStore.hasManage && item.proRateTypeCode === ProRateTypeCodeEnum.SplitPayment001" />
          <Button label="เพิ่มข้อมูล" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="addItem(item)"
            v-if="store.status.canEditTor && menuStore.hasManage && item.proRateTypeCode === ProRateTypeCodeEnum.SplitPayment001" />
        </div>

        <!-- Summary bar -->
        <div
          v-if="((item.proRateTypeCode !== ProRateTypeCodeEnum.SplitPayment003) || store.PP002Detail.isMigration) && item.details && item.details.length > 0"
          class="flex items-center gap-3 rounded-t-lg bg-gray-50 border border-b-0 border-gray-200 px-4 py-3 mt-2">
          <div class="flex-1">
            <div class="h-2.5 w-full rounded-full bg-gray-200 overflow-hidden">
              <div class="h-full rounded-full transition-all duration-300"
                :class="getTotalPercent(item) === 100 ? 'bg-green-500' : getTotalPercent(item) > 100 ? 'bg-red-500' : 'bg-amber-500'"
                :style="{ width: Math.min(getTotalPercent(item), 100) + '%' }" />
            </div>
          </div>
          <span class="text-sm font-semibold whitespace-nowrap"
            :class="getTotalPercent(item) === 100 ? 'text-green-600' : getTotalPercent(item) > 100 ? 'text-red-500' : 'text-amber-600'">
            {{ getTotalPercent(item).toFixed(2) }}%
          </span>
          <span v-if="getTotalPercent(item) === 100"
            class="material-symbols-outlined text-green-600 text-lg">check_circle</span>
          <span v-else class="material-symbols-outlined text-amber-500 text-lg">warning</span>
          <span class="text-xs text-gray-500">{{ item.details.length }} รายการ</span>
        </div>
        <DataTable
          v-if="(item.proRateTypeCode !== ProRateTypeCodeEnum.SplitPayment003) || (store.PP002Detail.isMigration)"
          :value="getNonMaDetails(item)" @row-reorder="(event: DataTableRowReorderEvent) => onRowReorder(item, event)"
          striped-rows table-class="min-w-[50rem]" scrollable scroll-height="clamp(250px, calc(100vh - 10rem), 900px)">
          <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
            <template #header>
              <p class="w-full font-bold text-center">ลำดับ</p>
            </template>
            <template #body="{ data, index }">
              <p class="text-center" :class="isRowEditable(data) ? 'mt-8' : ''">{{ index + 1 }}</p>
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" class="min-w-[80px] max-w-[100px]"
            v-if="item.proRateTypeCode === ProRateTypeCodeEnum.SplitPayment001">
            <template #header>
              <p class="w-full font-bold text-center">งวดที่</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-if="isRowEditable(data)" class="mt-8" v-model="data.termNumber" rules="required" hide-details
                :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
              <p v-else class="text-center">{{ data.termNumber ?? '-' }}</p>
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" class="min-w-[80px] max-w-[100px]">
            <template #header>
              <p class="w-full font-bold text-center">จำนวนเงิน (%)</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-if="isRowEditable(data)" class="mt-8" v-model="data.percent" default-zero-when-empty hide-details
                :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping :min-fraction-digits="2"
                :max-fraction-digits="3" :max-number="100" />
              <p v-else class="text-center">{{ data.percent != null ? data.percent.toFixed(2) : '-' }}</p>
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" class="min-w-[80px] max-w-[100px]">
            <template #header>
              <p class="w-full font-bold text-center">จำนวนวัน</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-if="isRowEditable(data)" class="mt-8" default-zero-when-empty v-model="data.period" rules="required"
                hide-details :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
              <p v-else class="text-center">{{ data.period ?? '-' }}</p>
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" class="min-w-[150px] max-w-[250px]">
            <template #header>
              <p class="w-full font-bold text-center">รายละเอียด</p>
            </template>
            <template #body="{ data }">
              <InputArea v-if="isRowEditable(data)" class="mt-8" label="รายละเอียด" v-model="data.description" rules="required"
                hide-details :disabled="!store.status.canEditTor || !menuStore.hasManage" />
              <p v-else class="whitespace-pre-wrap">{{ data.description || '-' }}</p>
            </template>
          </Column>
          <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top"
            v-if="store.status.canEditTor && menuStore.hasManage">
            <template #body="{ data, index }">
              <div class="flex gap-1" :class="isRowEditable(data) ? 'mt-5' : ''">
                <Button v-if="data.id && !isRowEditable(data)"
                  icon="pi pi-pencil" severity="secondary"
                  variant="text" @click="toggleRowEdit(data)" />
                <Button v-if="isRowEditable(data) && item.details && item.details.length > 1"
                  icon="pi pi-trash" severity="danger" variant="text"
                  @click="() => deleteItem(item, index)" />
              </div>
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
            bodyStyle="vertical-align: top;padding-top: 25px"
            v-if="item.details && item.details.length > 1 && store.status.canEditTor">
            <template #rowreordericon>
              <span class="material-symbols-outlined cursor-pointer mt-5" :draggable="true">
                drag_indicator
              </span>
            </template>
          </Column>
        </DataTable>

        <div class="md:grid grid-cols-8 gap-2 mt-10"
          v-if="item.proRateTypeCode === ProRateTypeCodeEnum.SplitPayment003 && !store.PP002Detail.isMigration">
          <InputNumber class="col-span-2" label="ชำระทุกๆ (วัน/เดือน/ปี)" v-model="item.period" rules="required"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
          <Select class="col-span-2" v-model="item.periodTypeCode" :options="periodDropdown" rules="required"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <InputNumber class="col-span-2" label="รวม" v-model="item.totalPeriod" rules="required"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping :max-number="60" />
          <Select class="col-span-2" v-model="item.totalPeriodTypeCode" :options="periodDropdown" rules="required"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>

        <div v-if="store.PP002Detail.isMigration && store.PP002Detail.paymentTermPeriods?.length">
          <small class="whitespace-nowrap font-bold">เงื่อนไขการชำระเงินกรณีมี (MA) (ถ้ามี)</small>
          <Radio class="lg:col-start-1"
            label="เงื่อนไขการชำระเงิน (ระยะเวลา เช่นงานจ้าง บำรุงรักษา จ้างแม่บ้าน จ้างรปภ เช่า)"
            v-model="store.PP002Detail.isMA" :options="mockRadioData"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <div class="mt-4" v-if="store.PP002Detail.isMA">

            <div class="md:grid grid-cols-12 gap-2 gap-y-8 mt-10">
              <InputArea class="col-span-4" v-model="item.description" label="รายละเอียด" rules="required"
                :disabled="!store.status.canEditTor || !menuStore.hasManage" />
              <InputNumber class="col-span-2" label="ชำระทุกๆ (วัน/เดือน/ปี)" v-model="item.period" rules="required"
                :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
              <Select class="col-span-2" v-model="item.periodTypeCode" :options="periodDropdown" rules="required"
                :disabled="!store.status.canEditTor || !menuStore.hasManage" />
              <InputNumber class="col-span-2" label="รวม" v-model="item.totalPeriod" rules="required"
                :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping :max-number="60" />
              <Select class="col-span-2" v-model="item.totalPeriodTypeCode" :options="periodDropdown" rules="required"
                :disabled="!store.status.canEditTor || !menuStore.hasManage" />
            </div>
          </div>
          <div
            v-if="store.PP002Detail.isMA && store.PP002Detail.paymentTermPeriods && store.PP002Detail.paymentTermPeriods.length > 0"
            class="mt-4">
            <div class="flex gap-2 md:gap-4 items-center">
              <small class="whitespace-nowrap font-bold">รายละเอียดเงื่อนไขการชำระเงิน</small>
              <div class="h-px bg-gray-300 flex-1" />
              <Button label="เพิ่มข้อมูล" icon="pi pi-plus" severity="primary" variant="outlined"
                class="bg-white! hover:bg-red-50!" @click="addItemPaymentTermPeriods"
                v-if="store.status.canEditTor && menuStore.hasManage" />
            </div>
            <DataTable class="mt-4" :value="store.PP002Detail.paymentTermPeriods"
              @row-reorder="onRowReorderPaymentTermPeriods" striped-rows table-class="min-w-[50rem]" scrollable
              scroll-height="clamp(250px, calc(100vh - 20rem), 600px)">
              <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
                <template #header>
                  <p class="w-full font-bold text-center">ลำดับ</p>
                </template>
                <template #body="{ index }">
                  <p class="text-center mt-8">{{ index + 1 }}</p>
                </template>
              </Column>
              <Column bodyStyle="vertical-align: top">
                <template #header>
                  <p class="w-full font-bold text-center">รายละเอียด</p>
                </template>
                <template #body="{ data }">
                  <InputArea class="mt-8" v-model="data.description" rules="required"
                    :disabled="!store.status.canEditTor || !menuStore.hasManage" />
                </template>
              </Column>
              <Column bodyStyle="vertical-align: top" class="max-w-[75px]">
                <template #header>
                  <p class="w-full font-bold text-center">จำนวน</p>
                </template>
                <template #body="{ data }">
                  <InputNumber class="mt-8" v-model="data.quantity" default-zero-when-empty
                    :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping :max-number="100" />
                </template>
              </Column>
              <Column bodyStyle="vertical-align: top" class="max-w-[100px]">
                <template #header>
                  <p class="w-full font-bold text-center">ระยะเวลา</p>
                </template>
                <template #body="{ data }">
                  <Select class="mt-8" v-model="data.periodTypeCode" :options="periodDropdown" rules="required"
                    :disabled="!store.status.canEditTor || !menuStore.hasManage" />
                </template>
              </Column>
              <Column bodyStyle="vertical-align: top" class="max-w-[75px]">
                <template #header>
                  <p class="w-full font-bold text-center">จำนวนรวม</p>
                </template>
                <template #body="{ data }">
                  <InputNumber class="mt-8" v-model="data.totalQuantity" default-zero-when-empty
                    :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping :max-number="100" />
                </template>
              </Column>
              <Column bodyStyle="vertical-align: top" class="max-w-[100px]">
                <template #header>
                  <p class="w-full font-bold text-center">ระยะเวลารวม</p>
                </template>
                <template #body="{ data }">
                  <Select class="mt-8" v-model="data.totalPeriodTypeCode" :options="periodDropdown" rules="required"
                    :disabled="!store.status.canEditTor || !menuStore.hasManage" />
                </template>
              </Column>
              <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top">
                <template #body="{ index }">
                  <Button icon="pi pi-trash" class="mt-5" severity="danger" variant="text"
                    @click="() => deleteItemPaymentTermPeriod(index)"
                    v-if="store.PP002Detail?.paymentTermPeriods && store.PP002Detail?.paymentTermPeriods?.length > 1 && store.status.canEditTor" />
                </template>
              </Column>
              <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
                bodyStyle="vertical-align: top;padding-top: 25px">
                <template #rowreordericon>
                  <span class="material-symbols-outlined cursor-pointer mt-5" :draggable="true"
                    v-if="store.PP002Detail?.paymentTermPeriods && store.PP002Detail?.paymentTermPeriods?.length > 1 && store.status.canEditTor">
                    drag_indicator
                  </span>
                </template>
              </Column>
            </DataTable>
          </div>
        </div>
      </div>
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
          <InputArea class="lg:col-span-3" label="รายละเอียด" v-model="dialogItem.defaultDescription" />
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

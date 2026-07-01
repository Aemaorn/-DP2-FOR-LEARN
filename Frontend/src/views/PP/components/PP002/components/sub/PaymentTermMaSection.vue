<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import type { PaymentTerms, PaymentTermsDetail, TorPaymentTermPeriods } from '@/views/PP/models/PP002/pp002Model';
import { InputNumber, Select } from '@/components/forms';
import { EGroupCode, ProRateTypeCodeEnum } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { onMounted, ref, watch, type Ref } from 'vue';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import defaultProps from '@/helpers/defaultProps';
import SharedService from '@/services/Shared/dropdown';
import type { DataTableRowReorderEvent } from 'primevue';
import { ArrayHelper } from '@/helpers/array';
import InputArea from '@/components/forms/InputArea.vue';

const props = defineProps({
  titleName: defaultProps(''),
});

const { reSequence, addSequence } = ArrayHelper();

const value = defineModel<PaymentTerms>({
  default: () => ({
    proRateTypeCode: '',
    totalPeriodTypeCode: 'PeriodType004'
  } as PaymentTerms),
});

const mockRadioData = [
  { value: true, label: 'มี' },
  { value: false, label: 'ไม่มี' },
] as Option[];

const menuStore = useMenuStore();
const store = usePP002DetailStore();

const dropdown = ref<Option[]>([]);
const splitPaymentDropdown = ref<Option[]>([]);
const mPeriodTypeDropdown = ref<Array<Option>>([]);
const periodDropdown = ref<Option[]>([]);

onMounted(() => {
  onInitAsync();

  if (!value.value?.details || value.value.details?.length === 0) {
    addItem();
  }
});

const addItem = (): void => {
  if (value.value.details) {
    const newItem = { termNumber: value.value.details.length + 1 } as PaymentTermsDetail;

    if (value.value.proRateTypeCode === ProRateTypeCodeEnum.SplitPayment002) {
      newItem.termNumber = value.value.details.length + 1;
      newItem.percent = 100;
    }

    value.value.details.push(newItem);

    return;
  }

  value.value = { details: [{ termNumber: 1 } as PaymentTermsDetail] } as PaymentTerms;
};

const deleteItem = (index: number): void => {
  value.value.details.splice(index, 1);

  value.value.details.forEach((item, index) => {
    item.termNumber = index + 1;
  });
};

const onInitAsync = async (): Promise<void> => {
  await Promise.all([getDropdownAsync(dropdown, EGroupCode.PeriodType), getDropdownAsync(splitPaymentDropdown, EGroupCode.SplitPayment), getDropdownAsync(mPeriodTypeDropdown, EGroupCode.MPeriodType), getDropdownAsync(periodDropdown, EGroupCode.PeriodType),]);

  value.value.proRateTypeCode = undefined;
};

const getDropdownAsync = async (target: Ref<Array<Option>>, group: EGroupCode): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(group, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
    value.value.totalPeriodTypeCode = 'PeriodType004';
  }
};

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  value.value.details = reSequence(event.value);

  value.value.details.forEach((item, index) => {
    item.termNumber = index + 1;
  });
};

watch(() => store.PP002Detail.isMA, (newValue) => {
  if ((store.PP002Detail.isMigration && newValue) && (!store.PP002Detail?.paymentTermPeriods || store.PP002Detail?.paymentTermPeriods?.length === 0)) {
    addItemPaymentTermPeriods();
  }
});

const recalculateDetails = (): void => {
  const totalPeriod = value.value.totalPeriod;
  if (!totalPeriod || totalPeriod <= 0) return;

  const currentLength = value.value.details?.length || 0;

  if (totalPeriod > currentLength) {
    const itemsToAdd = totalPeriod - currentLength;
    for (let i = 0; i < itemsToAdd; i++) {
      addItem();
    }
  } else if (totalPeriod < currentLength) {
    value.value.details = value.value.details.slice(0, totalPeriod);
  }

  value.value.details?.forEach((item, index) => {
    item.termNumber = index + 1;
    if (value.value.period) {
      let periodInDays = value.value.period;

      if (value.value.periodTypeCode === 'PeriodType002') {
        periodInDays = value.value.period * 30;
      } else if (value.value.periodTypeCode === 'PeriodType003') {
        periodInDays = value.value.period * 365;
      }

      item.period = periodInDays * (index + 1);
    }
  });
};

watch(() => value.value.totalPeriod, recalculateDetails);
watch(() => value.value.period, recalculateDetails);
watch(() => value.value.periodTypeCode, recalculateDetails);

const addItemPaymentTermPeriods = (): void => {

  store.PP002Detail.paymentTermPeriods = addSequence(store.PP002Detail.paymentTermPeriods, {} as TorPaymentTermPeriods);
};

const deleteItemPaymentTermPeriod = (index: number): void => {
  store.PP002Detail.paymentTermPeriods?.splice(index, 1);

  store.PP002Detail.paymentTermPeriods?.forEach((item, index) => {
    item.sequence = index + 1;
  });
};

</script>

<template>
  <Card class="mb-4" data-section-id="payment-term-ma" :data-section-label="props.titleName">
    <template #content>
      <TitleHeader :label="props.titleName" />
      <div class="md:grid grid-cols-8 gap-2 mt-10">
        <InputNumber class="col-span-3" label="ชำระทุกๆ (วัน/เดือน/ปี)" v-model="value.period" rules="required"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />

        <Select v-model="value.periodTypeCode" :options="dropdown" rules="required"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" />

        <InputNumber class="col-span-3" label="รวม" v-model="value.totalPeriod" rules="required" :max-number="60"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />

        <Select v-model="value.totalPeriodTypeCode" :options="dropdown" rules="required" :disabled="true" />
      </div>
      <div class="flex gap-2 md:gap-4 items-center">
        <small class="whitespace-nowrap font-bold">รายละเอียดเงื่อนไขการชำระเงิน</small>
        <div class="h-px bg-gray-300 flex-1" />
        <Button label="เพิ่มข้อมูล" icon="pi pi-plus" severity="primary" variant="outlined"
          class="bg-white! hover:bg-red-50!" @click="addItem" v-if="store.status.canEditTor && menuStore.hasManage" />
      </div>
      <div v-if="value.details && value.details.length > 0" class="mt-10">
        <div class="mt-4">
          <DataTable :value="value.details" @row-reorder="onRowReorder" striped-rows table-class="min-w-[50rem]">
            <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
              <template #header>
                <p class="w-full font-bold text-center">ลำดับ</p>
              </template>
              <template #body="{ index }">
                <div>
                  <p class="text-center mt-8">{{ index + 1 }}</p>
                </div>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" class="max-w-[50px]">
              <template #header>
                <p class="w-full font-bold text-center">งวดที่</p>
              </template>
              <template #body="{ data }">
                <InputNumber class="mt-8" label="งวดที่" v-model="data.termNumber" rules="required"
                  :disabled="(!store.status.canEditTor || !menuStore.hasManage)" grouping />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" class="max-w-[75px]">
              <template #header>
                <p class="w-full font-bold text-center">จำนวนเงิน (%)</p>
              </template>
              <template #body="{ data }">
                <InputNumber class="mt-8" v-model="data.percent" default-zero-when-empty
                  :disabled="(!store.status.canEditTor || !menuStore.hasManage)" grouping :min-fraction-digits="2" :max-fraction-digits="3"
                  :max-number="100" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" class="max-w-[100px]">
              <template #header>
                <p class="w-full font-bold text-center">จำนวนวัน</p>
              </template>
              <template #body="{ data }">
                <InputNumber class="mt-8" label="จำนวนวัน" default-zero-when-empty v-model="data.period"
                  rules="required" :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">รายละเอียด</p>
              </template>
              <template #body="{ data }">
                <InputArea class="mt-8" label="รายละเอียด" v-model="data.description" rules="required"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" />
              </template>
            </Column>
            <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top">
              <template #body="{ index }">
                <Button icon="pi pi-trash" class="mt-5" severity="danger" variant="text"
                  @click="() => deleteItem(index)" v-if="value.details && value.details.length > 1" />
              </template>
            </Column>
            <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
              bodyStyle="vertical-align: top;padding-top: 25px">
              <template #rowreordericon>
                <span class="material-symbols-outlined cursor-pointer mt-5" :draggable="true"
                  v-if="value.details && value.details.length > 1">
                  drag_indicator
                </span>
              </template>
            </Column>
          </DataTable>
        </div>
      </div>
      <div v-if="store.PP002Detail.isMigration && store.PP002Detail.paymentTermPeriods?.length">
        <small class="whitespace-nowrap font-bold">เงื่อนไขการชำระเงินกรณีมี (MA) (ถ้ามี)</small>
        <Radio class="lg:col-start-1"
          label="เงื่อนไขการชำระเงิน (ระยะเวลา เช่นงานจ้าง บำรุงรักษา จ้างแม่บ้าน จ้างรปภ เช่า)"
          v-model="store.PP002Detail.isMA" :options="mockRadioData"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" />
      </div>
      <div
        v-if="(store.PP002Detail.isMigration && store.PP002Detail.isMA) && store.PP002Detail.paymentTermPeriods && store.PP002Detail.paymentTermPeriods.length > 0"
        class="mt-10">
        <div class="flex gap-2 md:gap-4 items-center">
          <div class="h-px bg-gray-300 flex-1" />
          <Button label="เพิ่มข้อมูล" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="addItemPaymentTermPeriods"
            v-if="store.status.canEditTor && menuStore.hasManage" />
        </div>
        <div class="mt-4">
          <DataTable :value="store.PP002Detail.paymentTermPeriods" @row-reorder="onRowReorder" striped-rows
            table-class="min-w-[50rem]">
            <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
              <template #header>
                <p class="w-full font-bold text-center">ลำดับ</p>
              </template>
              <template #body="{ index }">
                <div>
                  <p class="text-center mt-8">{{ index + 1 }}</p>
                </div>
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
                  :disabled="(!store.status.canEditTor || !menuStore.hasManage)" grouping :max-number="100" />
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
                  :disabled="(!store.status.canEditTor || !menuStore.hasManage)" grouping :max-number="100" />
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
                  v-if="store.PP002Detail?.paymentTermPeriods && store.PP002Detail?.paymentTermPeriods?.length > 1" />
              </template>
            </Column>
            <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
              bodyStyle="vertical-align: top;padding-top: 25px">
              <template #rowreordericon>
                <span class="material-symbols-outlined cursor-pointer mt-5" :draggable="true"
                  v-if="store.PP002Detail?.paymentTermPeriods && store.PP002Detail?.paymentTermPeriods?.length > 1">
                  drag_indicator
                </span>
              </template>
            </Column>
          </DataTable>
        </div>
      </div>
    </template>
  </Card>
</template>

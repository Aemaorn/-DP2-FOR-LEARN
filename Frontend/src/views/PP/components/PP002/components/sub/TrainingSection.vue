<script setup lang="ts">
import { Card, Button, DataTable, Column, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';
import type { TorTrainingItemModel, TorTrainingModel } from '@/views/PP/models/PP002/pp002Model';
import { HttpStatusCode } from 'axios';
import SharedService from '@/services/Shared/dropdown';
import type { Option } from '@/models/shared/option';
import { onMounted, ref, watch, type Ref } from 'vue';
import { EGroupCode } from '@/enums/shared';
import { ArrayHelper } from '@/helpers/array';
import { InputArea, Radio } from '@/components/forms';

const props = defineProps<{
  label?: string;
}>();

const value = defineModel<TorTrainingModel>({
  default: () => ({} as TorTrainingModel),
  required: false,
});

const isTraining = defineModel<boolean | undefined>('isTraining', {
  default: undefined,
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();

const mockRadioData = [
  { label: 'มี', value: true },
  { label: 'ไม่มี', value: false },
];
const { reSequence } = ArrayHelper();

const dwcUnitDropdown = ref<Option[]>([]);
const periodDropdown = ref<Array<Option>>([]);

onMounted(async () => {
  await Promise.all([getDropdownAsync(periodDropdown, EGroupCode.PeriodType), getDropdownAsync(dwcUnitDropdown, EGroupCode.DWCUnit)]);
});

const getDropdownAsync = async (target: Ref<Array<Option>>, group: EGroupCode) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(group, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const addItem = (): void => {
  if (store.PP002Detail.trainingItems) {

    store.PP002Detail.trainingItems.push({ sequence: store.PP002Detail.trainingItems.length + 1 } as TorTrainingItemModel);

    return;
  }

  store.PP002Detail.trainingItems = [{ sequence: 1 } as TorTrainingItemModel];
};

const removeItem = (index: number): void => {

  if (store.PP002Detail.trainingItems != null && store.PP002Detail.trainingItems.length > 0) {
    store.PP002Detail.trainingItems.splice(index, 1);
  }
};

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  store.PP002Detail.trainingItems = reSequence(event.value);
};

watch(isTraining, (val) => {
  if (val && (!store.PP002Detail.trainingItems || store.PP002Detail.trainingItems.length === 0)) {
    addItem();
  }

  if (!val) {
    value.value = {} as TorTrainingModel;
    store.PP002Detail.trainingItems = [];
  }
});
</script>

<template>
  <Card class="mb-4" data-section-id="training" :data-section-label="props.label ?? 'การฝึกอบรม (IT)'">
    <template #content>
      <TitleHeader :label="props.label ?? 'การฝึกอบรม (IT)'" />
      <div class="md:grid grid-cols-3 gap-2">
        <Radio :label="props.label ?? 'การฝึกอบรม'" v-model="isTraining" :options="mockRadioData"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" rules="required" />
      </div>
      <template v-if="isTraining">
      <div class="my-4">
        <div class="md:grid grid-cols-6 gap-2 mt-8">
          <InputNumber label="ต้องจัดการฝึกอบรมภายในเวลา" class="col-span-2" v-model="value.trainingCount" grouping
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <Select class="col-span-2" v-model="value.trainingCountUnit" :options="periodDropdown"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
          <Select class="col-span-2" v-model="value.trainingUnitId" :options="dwcUnitDropdown"
            :disabled="!store.status.canEditTor || !menuStore.hasManage" />
        </div>
        <div class="flex gap-2 md:gap-4 items-center">
          <small class="whitespace-nowrap font-bold">รายการหลักสูตร</small>
          <div class="h-px bg-gray-300 flex-1" />
          <Button icon="pi pi-plus" label="เพิ่มรายการ" variant="outlined" severity="primary" @click="addItem"
            v-if="store.status.canEditTor && menuStore.hasManage" />
        </div>
        <div v-if="store.PP002Detail.trainingItems && store.PP002Detail.trainingItems?.length > 0" class="mt-4">
          <DataTable :value="store.PP002Detail.trainingItems" dataKey="sequence" @row-reorder="onRowReorder">
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">ลำดับ</p>
              </template>
              <template #body="{ index }">
                <p class="text-center">{{ index + 1 }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" body-class="min-w-[300px]">
              <template #header>
                <p class="w-full font-bold text-center">ชื่อหลักสูตร</p>
              </template>
              <template #body="{ data }">
                <InputArea v-model="data.courseName" rules="required"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" body-class="min-w-[175px]">
              <template #header>
                <p class="w-full font-bold text-center">จำนวนวัน</p>
              </template>
              <template #body="{ data }">
                <InputNumber v-model="data.periodDay" rules="required"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" body-class="min-w-[300px]">
              <template #header>
                <p class="w-full font-bold text-center">สถานที่อบรม</p>
              </template>
              <template #body="{ data }">
                <InputArea v-model="data.place" rules="required"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" body-class="min-w-[175px]">
              <template #header>
                <p class="w-full font-bold text-center">จำนวนครั้ง</p>
              </template>
              <template #body="{ data }">
                <InputNumber v-model="data.trainingCount" rules="required"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" body-class="min-w-[175px]">
              <template #header>
                <p class="w-full font-bold text-center">จำนวนคนต่อครั้ง</p>
              </template>
              <template #body="{ data }">
                <InputNumber v-model="data.totalPersonPerTime" rules="required"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping />
              </template>
            </Column>
            <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top">
              <template #body="{ index }">
                <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                  @click="() => removeItem(index)" v-if="store.status.canEditTor && menuStore.hasManage" />
              </template>
            </Column>
            <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
              bodyStyle="vertical-align: top;padding-top: 25px">
              <template #rowreordericon>
                <span class="material-symbols-outlined cursor-pointer" :draggable="true"
                  v-if="store.status.canEditTor && menuStore.hasManage">
                  drag_indicator
                </span>
              </template>
            </Column>
          </DataTable>
        </div>
      </div>
      </template>
    </template>
  </Card>
</template>
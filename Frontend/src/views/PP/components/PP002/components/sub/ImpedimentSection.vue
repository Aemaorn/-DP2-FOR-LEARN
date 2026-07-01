<script setup lang="ts">
import { Card, Button, DataTable, Column, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';
import type { TorImpedimentModel } from '@/views/PP/models/PP002/pp002Model';
import { HttpStatusCode } from 'axios';
import SharedService from '@/services/Shared/dropdown';
import type { Option } from '@/models/shared/option';
import { onMounted, ref, watch } from 'vue';
import { EGroupCode } from '@/enums/shared';
import { ArrayHelper } from '@/helpers/array';
import { InputField, Radio } from '@/components/forms';

const value = defineModel<TorImpedimentModel[]>({
  default: () => [],
});

const isImpediment = defineModel<boolean | undefined>('isImpediment', {
  default: undefined,
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();

const mockRadioData = [
  { label: 'มี', value: true },
  { label: 'ไม่มี', value: false },
];
const { reSequence, addSequence, deleteItemAndReSequence } = ArrayHelper();

const dropdown = ref<Option[]>([]);
const periodDropdown = ref<Array<Option>>([]);

onMounted(() => {
  getDropdownAsync();
});

const getDropdownAsync = async () => {
  const [split, period] = await Promise.all([SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SplitPayment, undefined, true), SharedService.onGetParameterByGroupCodeAsync(EGroupCode.MPeriodType, undefined, true)]);

  if (split.status === HttpStatusCode.Ok) {
    dropdown.value = split.data;
  }

  if (period.status === HttpStatusCode.Ok) {
    periodDropdown.value = period.data;
  }
};

const addItem = (): void => {

  value.value = addSequence(value.value, {} as TorImpedimentModel);
};

const removeItem = (index: number): void => {
  value.value = deleteItemAndReSequence(value.value, index);
};

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  value.value = reSequence(event.value);
};

watch(isImpediment, (val) => {
  if (val && value.value.length === 0) {
    addItem();
  }

  if (!val) {
    value.value = [];
  }
});

</script>

<template>
  <Card class="mb-4" data-section-id="impediment" data-section-label="การกำหนดตัวถ่วง (IT)">
    <template #content>
      <TitleHeader label="การกำหนดตัวถ่วง (IT)" />
      <div class="md:grid grid-cols-3 gap-2">
        <Radio label="การกำหนดตัวถ่วง" v-model="isImpediment" :options="mockRadioData"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" rules="required" />
      </div>
      <template v-if="isImpediment">
        <div class="my-4">
          <div class="flex gap-2 md:gap-4 items-center">
            <small class="whitespace-nowrap font-bold">รายการตัวถ่วง</small>
            <div class="h-px bg-gray-300 flex-1" />
            <Button icon="pi pi-plus" label="เพิ่มรายการ" variant="outlined" severity="primary" @click="addItem"
              v-if="store.status.canEditTor && menuStore.hasManage" />
          </div>
          <div v-if="value && value?.length > 0" class="mt-4">
            <DataTable :value="value" dataKey="sequence" @row-reorder="onRowReorder">
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
                  <p class="w-full font-bold text-center">รายการ</p>
                </template>
                <template #body="{ data }">
                  <InputField v-model="data.description" rules="required"
                    :disabled="!store.status.canEditTor || !menuStore.hasManage" />
                </template>
              </Column>
              <Column bodyStyle="vertical-align: top" body-class="min-w-[175px]">
                <template #header>
                  <p class="w-full font-bold text-center">ค่าตัวถ่วง</p>
                </template>
                <template #body="{ data }">
                  <InputNumber v-model="data.impedimentValue" rules="required" :min-fraction-digits="2"
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
<script setup lang="ts">
import type { TechnicalPeriods, TechnicalPeriodsDetail } from '@/views/PP/models/PP002/pp002Model';
import { InputField, Datepicker } from '@/components/forms';
import { Card, Button, DataTable } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { computed } from 'vue';
import { formatNumber } from '@/helpers/currency';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';
import defaultProps from '@/helpers/defaultProps';

const props = defineProps({
  titleName: defaultProps(''),
});

const value = defineModel<TechnicalPeriods>({
  default: () => ({} as TechnicalPeriods),
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();

const sumPersonal = computed(() => {
  if (!value.value.details) return 0;

  return value.value.details.reduce((x, y) => x + parseInt(y.personalCount ?? 0), 0);
});

const addItem = (): void => {
  value.value.details.push({} as TechnicalPeriodsDetail);
};

const removeItem = (index: number): void => {
  value.value.details.splice(index, 1);
};

const getDiffDate = (data: TechnicalPeriodsDetail): string => {
  if (data.startDate && data.endDate) {
    const startDate = new Date(data.startDate);
    const endDate = new Date(data.endDate);
    endDate.setHours(23, 59);

    const diffInMs = Math.abs(startDate.getTime() - endDate.getTime());
    const diffInDays = Math.ceil(diffInMs / (1000 * 60 * 60 * 24));

    return `${diffInDays} วัน`;
  }

  return '-';
};
</script>

<template>
  <Card class="mb-4" data-section-id="processing-time" :data-section-label="props.titleName">
    <template #content>
      <TitleHeader :label="props.titleName">
        <template #action>
          <Button label="เพิ่มสาขา" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="() => addItem()"
            v-if="store.status.canEditTor && menuStore.hasManage" />
        </template>
      </TitleHeader>
      <div v-if="value.details && value.details.length > 0">
        <div class="mt-4">
          <DataTable :value="value.details">
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
                <p class="w-full font-bold text-center min-w-[200px]">สาขา</p>
              </template>
              <template #body="{ data }">
                <InputField class="mt-8" label="รายละเอียด" v-model="data.branch"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center min-w-[200px]">จำนวน (คน)</p>
              </template>
              <template #body="{ data }">
                <InputField class="mt-8" v-model="data.personalCount"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">วันที่เริ่มต้นสัญญา</p>
              </template>
              <template #body="{ data }">
                <Datepicker class="mt-8" v-model="data.startDate"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">วันที่สิ้นสุดสัญญา</p>
              </template>
              <template #body="{ data }">
                <Datepicker class="mt-8" v-model="data.endDate"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" :min-date="new Date(data.startDate)" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center min-w-[100px]">ระยะเวลา</p>
              </template>
              <template #body="{ data }">
                <p class="w-full text-center mt-9">{{ getDiffDate(data) }}</p>
              </template>
            </Column>
            <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top">
              <template #body="{ index }">
                <Button icon="pi pi-trash" class="mt-9" severity="danger" variant="text"
                  @click="() => removeItem(index)" v-if="store.status.canEditTor && menuStore.hasManage" />
              </template>
            </Column>
            <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
              bodyStyle="vertical-align: top;padding-top: 25px">
              <template #rowreordericon>
                <span class="material-symbols-outlined cursor-pointer mt-9" :draggable="true"
                  v-if="store.status.canEditTor && menuStore.hasManage">
                  drag_indicator
                </span>
              </template>
            </Column>
          </DataTable>
          <p class="mt-3 font-bold">รวม
            <span class="ml-8">{{ formatNumber(sumPersonal) }}</span>
            <span class="ml-8">คน</span>
          </p>
        </div>
      </div>
    </template>
  </Card>
</template>

<style lang="css" scoped>
:deep(.p-datatable-table-container) {
  overflow: unset !important;
}
</style>
<script setup lang="ts">
import { formatCurrency } from '@/helpers/currency';
import type { planSelected } from '@/models/PL/pl002';
import { usePl002DetailStore } from '@/stores/PL/pl002';
import { computed, ref } from 'vue';
import { Select } from '@/components/forms';
import { getYearOptionsWithValue } from '@/constants/date';

const store = usePl002DetailStore();

const visible = ref(false);
const availableItems = ref<planSelected[]>([]);
const selectedItems = ref<planSelected[]>([]);
const filterYear = ref<number | undefined>(undefined);
const filterSupplyMethodCode = ref<string | undefined>(undefined);

const yearOptions = computed(() => getYearOptionsWithValue(filterYear.value));

const isAllSelected = computed(
  () => availableItems.value.length > 0 && selectedItems.value.length === availableItems.value.length
);

const toggleSelectAll = (checked: boolean): void => {
  selectedItems.value = checked ? [...availableItems.value] : [];
};

const isSelected = (item: planSelected): boolean =>
  selectedItems.value.some((s: planSelected): boolean => s.planId === item.planId);

const toggleItem = (item: planSelected, checked: boolean): void => {
  if (checked) {
    selectedItems.value = [...selectedItems.value, item];
  } else {
    selectedItems.value = selectedItems.value.filter((s: planSelected): boolean => s.planId !== item.planId);
  }
};

const confirm = (): void => {
  if (selectedItems.value.length === 0) return;
  store.body.year = filterYear.value as number;
  store.body.supplyMethodCode = filterSupplyMethodCode.value as string;
  store.applySelectedPlans(selectedItems.value);
  visible.value = false;
};

const reload = async (): Promise<void> => {
  if (!filterYear.value) {
    availableItems.value = [];
    selectedItems.value = [];
    return;
  }

  const list = await store.fetchAnnualPlanList(filterYear.value, filterSupplyMethodCode.value);
  const existingIds = new Set(store.body.planSelected.map(p => p.planId));
  availableItems.value = list.filter(p => !existingIds.has(p.planId));
  selectedItems.value = [];
};

const open = async (): Promise<void> => {
  filterYear.value = store.body.year;
  filterSupplyMethodCode.value = store.body.supplyMethodCode;
  await reload();
  visible.value = true;
};

defineExpose({ open });
</script>

<template>
  <Dialog v-model:visible="visible" modal header="เลือกรายการจัดซื้อจัดจ้าง" :style="{ width: '80vw' }">
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-4">
      <Select label="ปีงบประมาณ" :options="yearOptions" v-model="filterYear"
        @update:model-value="reload" />
      <Select label="วิธีการจัดหา" :options="store.supplyMethodCodeDDL" v-model="filterSupplyMethodCode"
        @update:model-value="reload" />
    </div>
    <div class="flex items-center gap-2 mb-4">
      <Checkbox :model-value="isAllSelected" binary input-id="selectAllPlans"
        :disabled="availableItems.length === 0"
        @update:model-value="toggleSelectAll" label="เลือกทั้งหมด" />
    </div>
    <DataTable :value="availableItems" data-key="planId">
      <Column header-style="width: 3rem">
        <template #header />
        <template #body="{ data }">
          <Checkbox :model-value="isSelected(data)" binary
            @update:model-value="(v: boolean) => toggleItem(data, v)" />
        </template>
      </Column>
      <Column header="เลขที่รายการจัดซื้อจัดจ้าง" header-class="text-center font-bold">
        <template #body="{ data }">
          <p class="text-center">{{ data.planNumber }}</p>
        </template>
      </Column>
      <Column header="ชื่อโครงการ" header-class="text-center font-bold">
        <template #body="{ data }">
          <p>{{ data.planTitle }}</p>
        </template>
      </Column>
      <Column header="ฝ่าย/ภาคเขต" header-class="text-center font-bold">
        <template #body="{ data }">
          <p>{{ data.departmentName }}</p>
        </template>
      </Column>
      <Column header="วิธีจัดหา" header-class="text-center font-bold">
        <template #body="{ data }">
          <p>{{ `${data.supplyMethodName}${data.supplyMethodTypeName ? ` : ${data.supplyMethodTypeName}` : ''}` }}</p>
        </template>
      </Column>
      <Column header="วงเงินงบประมาณ" header-class="text-center font-bold">
        <template #body="{ data }">
          <p class="text-right">{{ formatCurrency(data.budget) }}</p>
        </template>
      </Column>
      <template #empty>
        <p class="text-center">ไม่พบรายการเพิ่มเติม</p>
      </template>
    </DataTable>
    <template #footer>
      <Button label="ยกเลิก" severity="secondary" variant="outlined" @click="visible = false" />
      <Button label="ยืนยัน" severity="primary" :disabled="selectedItems.length === 0" @click="confirm" />
    </template>
  </Dialog>
</template>

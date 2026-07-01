<script setup lang="ts">
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import type { ReimbursementItem } from '@/models/PCM/pcm006';
import { usePcm006DetailStore } from '@/stores/PCM/pcm006';
import { computed, ref } from 'vue';

const store = usePcm006DetailStore();

const visible = ref(false);
const availableItems = ref<ReimbursementItem[]>([]);
const selectedItems = ref<ReimbursementItem[]>([]);

const isAllSelected = computed(
  () => availableItems.value.length > 0 && selectedItems.value.length === availableItems.value.length
);

const toggleSelectAll = (checked: boolean): void => {
  selectedItems.value = checked ? [...availableItems.value] : [];
};

const isSelected = (item: ReimbursementItem): boolean =>
  selectedItems.value.some((s: ReimbursementItem): boolean => s.pettyCashGlAccountId === item.pettyCashGlAccountId);

const toggleItem = (item: ReimbursementItem, checked: boolean): void => {
  if (checked) {
    selectedItems.value = [...selectedItems.value, item];
  } else {
    selectedItems.value = selectedItems.value.filter((s: ReimbursementItem): boolean => s.pettyCashGlAccountId !== item.pettyCashGlAccountId);
  }
};

const confirm = (): void => {
  if (selectedItems.value.length === 0) return;
  store.api.applySelectedPettyCashItems(selectedItems.value);
  visible.value = false;
};

const open = async (): Promise<void> => {
  availableItems.value = await store.api.fetchGlAccounts();
  selectedItems.value = [];
  visible.value = true;
};

defineExpose({ open });
</script>

<template>
  <Dialog v-model:visible="visible" modal header="เลือกรายการข้อมูลเบิกจ่าย" :style="{ width: '80vw' }">
    <div class="flex items-center gap-2 mb-4">
      <Checkbox :model-value="isAllSelected" binary input-id="selectAllPettyCash"
        @update:model-value="toggleSelectAll" label="เลือกทั้งหมด"/>
    </div>
    <DataTable :value="availableItems" data-key="pettyCashGlAccountId">
      <Column header-style="width: 3rem">
        <template #header />
        <template #body="{ data }">
          <Checkbox :model-value="isSelected(data)" binary
            @update:model-value="(v: boolean) => toggleItem(data, v)" />
        </template>
      </Column>
      <Column header="วันที่" header-class="text-center font-bold">
        <template #body="{ data }">
          <p class="text-center">{{ ToDateOnly(data.pettyCashDate) }}</p>
        </template>
      </Column>
      <Column header="เลขที่อ้างอิง" header-class="text-center font-bold">
        <template #body="{ data }">
          <p class="text-center">{{ data.pettyCashNumber }}</p>
        </template>
      </Column>
      <Column header="รายการ" header-class="text-center font-bold">
        <template #body="{ data }">
          <p>{{ data.pettyCashSubject }}</p>
        </template>
      </Column>
      <Column header="ศูนย์ต้นทุน" header-class="text-center font-bold">
        <template #body="{ data }">
          <p>{{ data.departmentName }}</p>
        </template>
      </Column>
      <Column header="รหัสบัญชี" header-class="text-center font-bold">
        <template #body="{ data }">
          <p>{{ data.glAccountLabel }}</p>
        </template>
      </Column>
      <Column header="จำนวนเงิน" header-class="text-center font-bold">
        <template #body="{ data }">
          <p class="text-right">{{ formatCurrency(data.amount) }}</p>
        </template>
      </Column>
      <template #empty>
        <p class="text-center">ไม่พบข้อมูล</p>
      </template>
    </DataTable>
    <template #footer>
      <Button label="ยกเลิก" severity="secondary" variant="outlined" @click="visible = false" />
      <Button label="ยืนยัน" severity="primary" :disabled="selectedItems.length === 0" @click="confirm" />
    </template>
  </Dialog>
</template>

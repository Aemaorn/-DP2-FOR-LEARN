<script setup lang="ts">
import { Card, Button, DataTable, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputField } from '@/components/forms';
import type { TP004TorInfo } from '@/views/PP/models/PP004/pp004Model';
import { ArrayHelper } from '@/helpers/array';
import { DatatableHelper } from '@/helpers/datable';
import { useMenuStore } from '@/stores/menu';

const { addSequence, deleteItemAndReSequence } = ArrayHelper();
const { onRowReorder } = DatatableHelper();
const menuStore = useMenuStore();

const value = defineModel<Array<TP004TorInfo>>({
});

const handleAdd = () => {
  value.value = addSequence(value.value, {
    sequence: 0,
    description: '',
  } as TP004TorInfo);
};

const deleteItem = (index: number): void => {
  value.value = deleteItemAndReSequence(value.value, index);
};

const handleReOrder = (event: DataTableRowReorderEvent): void => {
  value.value = onRowReorder(event);
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลร่างขอบเขตของงาน (TOR)">
        <template #action>
          <Button icon="pi pi-plus" label="เพิ่มข้อมูล" severity="primary" variant="outlined" @click="handleAdd"
            v-if="menuStore.hasManage" />
        </template>
      </TitleHeader>
      <div class="mt-4">
        <DataTable :value="value" @row-reorder="handleReOrder">
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">ลำดับ</p>
            </template>
            <template #body="{ data }">
              <p class="text-center">{{ data.sequence }}</p>
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">รายละเอียด</p>
            </template>
            <template #body="{ data }">
              <InputField v-model="data.description" :disabled="!menuStore.hasManage" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" class="max-w-[10px]" v-if="menuStore.hasManage">
            <template #body="{ index }">
              <Button icon="pi pi-trash" severity="danger" variant="text" @click="() => deleteItem(index)" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
            bodyStyle="vertical-align: top;padding-top: 22px" v-if="menuStore.hasManage">
            <template #rowreordericon>
              <span class="material-symbols-outlined cursor-pointer" :draggable="true">
                drag_indicator
              </span>
            </template>
          </Column>
        </DataTable>
      </div>
    </template>
  </Card>
</template>
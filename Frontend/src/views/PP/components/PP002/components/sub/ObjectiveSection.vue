<script setup lang="ts">
import type { SequenceDescription } from '@/views/PP/models/PP002/pp002Model';
import { ArrayHelper } from '@/helpers/array';
import { InputArea } from '@/components/forms';
import { Card, Button, DataTable, Column, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';

const defaultItem: SequenceDescription = {
  sequence: 1,
  description: '',
  isDefault: true,
};

const value = defineModel<SequenceDescription[]>({
  default: () => [],
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();
const { addSequence, reSequence, deleteItemAndReSequence } = ArrayHelper();

if (!value.value || value.value.length === 0) {
  value.value = [{ ...defaultItem }];
}

const addItem = (): void => {
  value.value = addSequence(value.value, {} as SequenceDescription);
};

const deleteItem = (index: number): void => {
  value.value = deleteItemAndReSequence(value.value, index);
};

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  value.value = reSequence(event.value);
};
</script>

<template>
  <Card class="mb-4" data-section-id="objective" data-section-label="วัตถุประสงค์">
    <template #content>
      <TitleHeader label="วัตถุประสงค์">
        <template #action>
          <Button label="เพิ่มวัตถุประสงค์" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="addItem" v-if="store.status.canEditTor && menuStore.hasManage" />
        </template>
      </TitleHeader>

      <div v-if="value && value.length > 0" class="mt-4">
        <DataTable :value="value" @row-reorder="onRowReorder">
          <!-- ลำดับ -->
          <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
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
              <InputArea :row="1" v-model="data.description" rules="required"
                :disabled="!store.status.canEditTor || !menuStore.hasManage" />
            </template>
          </Column>

          <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top">
            <template #body="{ index }">
              <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text" @click="() => deleteItem(index)"
                v-if="store.status.canEditTor && menuStore.hasManage && value.length > 1" />
            </template>
          </Column>

          <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
            bodyStyle="vertical-align: top;padding-top: 25px">
            <template #rowreordericon>
              <span class="material-symbols-outlined cursor-pointer" :draggable="true"
                v-if="store.status.canEditTor && menuStore.hasManage && value.length > 1">
                drag_indicator
              </span>
            </template>
          </Column>
        </DataTable>
      </div>
    </template>
  </Card>
</template>

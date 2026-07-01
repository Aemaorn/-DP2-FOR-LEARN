<script setup lang="ts">
import { ButtonAddUserOutlined } from '@/components/Button';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { Card, type DataTableRowReorderEvent } from 'primevue';
import type { ProcurementSuppliesDivision } from '../../models/PP005/pp005Model';
import { DatatableHelper } from '@/helpers/datable';
import { ArrayHelper } from '@/helpers/array';
import { showUserDialogAsync } from '@/helpers/dialog';
import { DepartmentId } from '@/enums/businessUnit';

type Props = {
  label: string;
  isDisabled?: boolean;
};

const { addSequence, deleteItemAndReSequence } = ArrayHelper();
const { onRowReorder } = DatatableHelper();

const props = defineProps<Props>();
const value = defineModel<ProcurementSuppliesDivision[]>({
  required: true,
});

const addItemAsync = async () => {
  const selectedData = await showUserDialogAsync(DepartmentId.JorPor);

  if (!selectedData) {
    return;
  }

  if (!value.value) {
    value.value = [];
  }

  const currentLength = Array.isArray(value.value) ? value.value.length : 0;

  value.value = addSequence(value.value, {
    userId: selectedData.id,
    fullName: selectedData.name,
    fullPositionName: selectedData.positionName?.trim(),
    sequence: currentLength + 1,
  }) as ProcurementSuppliesDivision[];
};

const reOrderDatatable = (event: DataTableRowReorderEvent) => {
  value.value = onRowReorder(event) as ProcurementSuppliesDivision[];
}

const deleteItem = (index: number) => {
  value.value = deleteItemAndReSequence(value.value as any, index) as any;
};
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader :label="props.label">
        <template #action>
          <ButtonAddUserOutlined type="button" :disabled="props.isDisabled" @click="addItemAsync" />
        </template>
      </TitleHeader>
      <div class="px-4 my-4">
        <DataTable :value="value" @row-reorder="(e) => reOrderDatatable(e)">
          <Column>
            <template #header>
              <p class="w-full font-bold text-center">ชื่อ-นามสกุล/ตำแหน่ง</p>
            </template>
            <template #body="{ data }">
              <div>
                <p>{{ data.fullName }}</p>
                <small class="text-gray-400">{{ data.fullPositionName }}</small>
              </div>
            </template>
          </Column>
          <Column class="max-w-[10px]" v-if="!props.isDisabled">
            <template #body="{ index }">
              <Button icon="pi pi-trash" severity="danger" variant="text"
                @click="deleteItem(index)" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" bodyStyle="padding-top: 20px" v-if="!props.isDisabled">
            <template #body>
              <span class="material-symbols-outlined cursor-pointer" :draggable="!props.isDisabled">
                drag_indicator
              </span>
            </template>
          </Column>
          <template #empty>
            <p class="text-center font-bold">ไม่พบข้อมูล</p>
          </template>
        </DataTable>
      </div>
    </template>
  </Card>
</template>
<script setup lang="ts">
import { Radio, Select } from '@/components/forms';
import { DataTable, type DataTableRowReorderEvent } from 'primevue';
import { PreProcurementCommitteeType } from '@/enums/preProcurement';
import type { Option } from '@/models/shared/option';
import type { TPP004Procurement, TPP004UserListDetail } from '@/views/PP/models/PP004/pp004Model';
import { onMounted, ref } from 'vue';
import { ArrayHelper } from '@/helpers/array';
import { DatatableHelper } from '@/helpers/datable';
import { usePP004Store } from '@/views/PP/stores/PP004/pp004Store';
import { useMenuStore } from '@/stores/menu';

const { addSequence, deleteItemAndReSequence } = ArrayHelper();
const { onRowReorder } = DatatableHelper();
const menuStore = useMenuStore();

const radioOption = ref([{
  label: 'ผู้จัดซื้อจัดจ้าง',
  value: PreProcurementCommitteeType.SingleCommittee,
},
{
  label: 'คณะกรรมการจัดซื้อจัดจ้าง',
  value: PreProcurementCommitteeType.MultipleCommittee,
}] as Option[]);

const value = defineModel<TPP004Procurement>({
  required: true,
});

const openUserDialog = () => {
  // TODO: Open dialog user and assign to list.
  value.value.detail = addSequence(value.value.detail, {
    name: '',
    position: '',
    positionProcurement: '',
  } as TPP004UserListDetail);
};

const deleteItem = (index: number): void => {
  value.value.detail = deleteItemAndReSequence(value.value.detail, index);
};

const handleReOrder = (event: DataTableRowReorderEvent): void => {
  value.value.detail = onRowReorder(event);
};

const pp004Store = usePP004Store();

onMounted((): void => {
  pp004Store.fetchPositionProcurementOptions();
});
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <div class="w-full my-4 z-50">
        <div class="flex gap-2 md:gap-4 items-center">
          <div>
            <Radio v-model="value.committeeType" :options="radioOption" vertical hide-details rules="required"
              :disabled="!menuStore.hasManage" />
          </div>
          <div class="h-px bg-gray-300 flex-1" />
          <Button icon="pi pi-plus" label="เพิ่มรายชื่อ" severity="primary" variant="outlined"
            v-if="value.committeeType && menuStore.hasManage" @click="openUserDialog" />
        </div>
      </div>
      <div class="mt-4">
        <DataTable :value="value.detail" @row-reorder="handleReOrder" v-if="value.committeeType">
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">ชื่อ-นามสกุล/ตำแหน่ง</p>
            </template>
            <template #body="{ data }">
              <div>
                <p class="text-center">{{ data.name }}</p>
                <small class="text-gray-400">{{ data.position }}</small>
              </div>
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">ตำแหน่งในคณะกรรมการ</p>
            </template>
            <template #body="{ data }">
              <Select v-model="data.positionProcurement" :options="pp004Store.positionProcurementOptions"
                :disabled="!menuStore.hasManage" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" class="max-w-[10px]" v-if="menuStore.hasManage">
            <template #body="{ index }">
              <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                @click="() => deleteItem(index)" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
            bodyStyle="vertical-align: top;padding-top: 25px" v-if="menuStore.hasManage">
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
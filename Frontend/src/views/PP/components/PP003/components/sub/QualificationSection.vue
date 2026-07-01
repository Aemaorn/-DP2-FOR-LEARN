<script setup lang="ts">
import { Button, type DataTableRowReorderEvent } from 'primevue';
import { ArrayHelper } from '@/helpers/array';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { InputField, InputNumber } from '@/components/forms';
import type { TPP003Staff, TPP003StaffDetailPersonal } from '@/views/PP/models/PP003/pp003Model';
import { useMenuStore } from '@/stores/menu';

const value = defineModel<TPP003Staff>({
  required: true,
});

defineProps<{
  states: {
    isEditor: boolean;
    isCommitteeApproval: boolean;
    isCommitteeCurrentApproval: boolean;
    isBossCommitteeApproval: boolean;
    isUnitApproval: boolean;
    isCurrentUnitApproval: boolean;
    isLastUnitApproval: boolean;
    isJorPorSection: boolean;
    isJorPorAssign: boolean;
    isJorPorComment: boolean;
    isAcceptorApproval: boolean;
    isCurrentAcceptorApproval: boolean;
    isLastAcceptorApproval: boolean;
    isMangeMd: boolean;
    currentTemplate: boolean;

  },
}>();

const { addSequence, reSequence, deleteItemAndReSequence } = ArrayHelper();
const menuStore = useMenuStore();

const addPriceSourceSequence = (): void => {
  value.value.details = addSequence(value.value.details, {
    type: 'Personal',
    description: '',
    personCount: 0,
  } as unknown as TPP003StaffDetailPersonal);
};

const deleteItem = (index: number): void => {
  value.value.details = deleteItemAndReSequence(value.value.details, index);
};

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  value.value.details = reSequence(event.value);
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="รายละเอียดบุคลากร" />
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mt-10">
        <InputNumber label="ค่าตอบแทนบุคลากร (บาท)" v-model="value.personnelCompensation"
          rules="required|min_value:0.01" :min-fraction-digits="2" grouping
          :disabled="!states.isEditor || !menuStore.hasManage" />
      </div>
      <div class="mt-4 flex justify-between">
        <p class="text-xl font-bold">รายการบุคลากร</p>
        <Button label="เพิ่มข้อมูล" icon="pi pi-plus" severity="primary" variant="outlined"
          class="bg-white! hover:bg-red-50! text-nowrap" @click="addPriceSourceSequence"
          v-if="states.isEditor && menuStore.hasManage" />
      </div>
      <div class="grid grid-cols-1 gap-2">
        <DataTable :value="value.details" @row-reorder="onRowReorder">
          <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
            <template #header>
              <p class="w-full font-bold text-center">ลำดับ</p>
            </template>
            <template #body="{ data }">
              <div>
                <p class="text-center">{{ data.sequence }}</p>
              </div>
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">รายละเอียด</p>
            </template>
            <template #body="{ data }">
              <InputField v-model="data.description" :disabled="!states.isEditor || !menuStore.hasManage"
                rules="required" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">จำนวนคน</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-model="data.personnelCount" :disabled="!states.isEditor || !menuStore.hasManage"
                rules="required|min_value:1" />
            </template>
          </Column>
          <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top"
            v-if="states.isEditor && menuStore.hasManage">
            <template #body="{ index }">
              <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                @click="() => deleteItem(index)" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
            bodyStyle="vertical-align: top;padding-top: 25px" v-if="menuStore.hasManage">
            <template #rowreordericon>
              <span class="material-symbols-outlined"
                :class="`${states.isEditor ? 'cursor-pointer' : 'cursor-default'}`" :draggable="states.isEditor">
                drag_indicator
              </span>
            </template>
          </Column>
          <template #empty>
            <div class="p-4 text-center text-gray-500">ไม่มีข้อมูลบุคลากร</div>
          </template>
        </DataTable>
      </div>
    </template>
  </Card>
</template>

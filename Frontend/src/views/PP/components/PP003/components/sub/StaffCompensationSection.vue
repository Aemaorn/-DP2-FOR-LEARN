<script setup lang="ts">
import { Button, type DataTableRowReorderEvent } from 'primevue';
import { ArrayHelper } from '@/helpers/array';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { InputArea, InputNumber } from '@/components/forms';
import type {
  TPP003Staff,
  TPP003StaffDetail,
} from '@/views/PP/models/PP003/pp003Model';
import { ref } from 'vue';
import { DatatableHelper } from '@/helpers/datable';
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

const { addSequence, reSequence } = ArrayHelper();
const { onRowReorder } = DatatableHelper();
const menuStore = useMenuStore();

const addItem = (type: 'ConsultantTypes' | 'ConsultantQualifications') => {
  const filterItem = ref(value.value.details.filter(r => r.type === type));
  filterItem.value = addSequence(filterItem.value, {
    type: type,
    description: '',
  } as TPP003StaffDetail);

  value.value.details = [
    ...value.value.details.filter(r => r.type !== type).sort(s => s.sequence),
    ...filterItem.value.sort(s => s.sequence),
  ];
};

const handleDeleteItem = (type: 'ConsultantTypes' | 'ConsultantQualifications', index: number): void => {
  const remainItems = ref(value.value.details.filter(r => r.type === type));
  remainItems.value.splice(index, 1);
  remainItems.value = reSequence(remainItems.value);

  value.value.details = [
    ...value.value.details.filter(r => r.type !== type).sort(s => s.sequence),
    ...remainItems.value.sort(s => s.sequence),
  ];

};

const handleReorder = (
  type: 'ConsultantTypes' | 'ConsultantQualifications',
  event: DataTableRowReorderEvent
): void => {
  const temp = onRowReorder(event) as unknown as TPP003StaffDetail[];

  value.value.details = [
    ...value.value.details.filter(r => r.type !== type).sort(s => s.sequence),
    ...temp.sort(s => s.sequence),
  ];
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="รายละเอียดบุคลากร" />
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mt-8">
        <InputNumber label="ค่าตอบแทนบุคลากร (บาท)" v-model="value.personnelCompensation" rules="required"
          :min-fraction-digits="2" grouping :disabled="!states.isEditor || !menuStore.hasManage" />
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mt-8">
        <InputNumber label="จำนวนที่ปรึกษา (คน)" v-model="value.personnelCount" rules="required|min_value:1"
          :disabled="!states.isEditor || !menuStore.hasManage" />
      </div>
      <div class="mt-4 flex justify-between">
        <p class="text-xl font-bold">รายการประเภทที่ปรึกษา</p>
        <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="bg-white! hover:bg-red-50!" @click="() => addItem('ConsultantTypes')"
          v-if="states.isEditor && menuStore.hasManage" />
      </div>
      <div class="grid grid-cols-1 gap-2">
        <DataTable :value="value.details.filter(r => r.type === 'ConsultantTypes')"
          @row-reorder="(event) => handleReorder('ConsultantTypes', event)">
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
              <InputArea v-model="data.description" :disabled="!states.isEditor || !menuStore.hasManage" />
            </template>
          </Column>
          <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top"
            v-if="states.isEditor && menuStore.hasManage">
            <template #body="{ index }">
              <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                @click="() => handleDeleteItem('ConsultantTypes', index)" />
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
            <div class="p-4 text-center text-gray-500">ไม่มีข้อมูลประเภทที่ปรึกษา</div>
          </template>
        </DataTable>
      </div>
      <div class="mt-4 flex justify-between">
        <p class="text-xl font-bold">รายการคุณสมบัติที่ปรึกษา</p>
        <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="bg-white! hover:bg-red-50!" @click="() => addItem('ConsultantQualifications')"
          v-if="states.isEditor && menuStore.hasManage" />
      </div>
      <div class="grid grid-cols-1 gap-2">
        <DataTable :value="value.details.filter(r => r.type === 'ConsultantQualifications')"
          @row-reorder="(event) => handleReorder('ConsultantQualifications', event)">
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
              <InputArea v-model="data.description" :disabled="!states.isEditor || !menuStore.hasManage" />
            </template>
          </Column>
          <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top">
            <template #body="{ index }">
              <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                @click="() => handleDeleteItem('ConsultantQualifications', index)"
                v-if="states.isEditor && menuStore.hasManage" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
            bodyStyle="vertical-align: top;padding-top: 25px">
            <template #rowreordericon>
              <span class="material-symbols-outlined"
                :class="`${states.isEditor ? 'cursor-pointer' : 'cursor-default'}`" :draggable="states.isEditor">
                drag_indicator
              </span>
            </template>
          </Column>
          <template #empty>
            <div class="p-4 text-center text-gray-500">ไม่มีข้อมูลคุณสมบัติที่ปรึกษา</div>
          </template>
        </DataTable>
      </div>
    </template>
  </Card>
</template>

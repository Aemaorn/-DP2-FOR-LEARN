<script setup lang="ts">
import { Button, type DataTableRowReorderEvent } from 'primevue';
import { ArrayHelper } from '@/helpers/array';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { InputField, InputNumber, Datepicker } from '@/components/forms';
import type { TPP003BudgetAllocations, TPP003BudgetAllocationsDetail, TPP003BudgetAllocationsDetailRefBudget } from '@/views/PP/models/PP003/pp003Model';
import { useMenuStore } from '@/stores/menu';
import { computed, watch } from 'vue';
import { usePP003DetailStore } from '@/views/PP/stores/PP003/pp003Store';
import { storeToRefs } from 'pinia';

type DetailType = 'basic' | 'refBudget';

const value = defineModel<TPP003BudgetAllocations>({
  required: true,
});

const props = defineProps<{
  detailType: DetailType,
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
const store = usePP003DetailStore();
const { body } = storeToRefs(store);

const addDetail = (): void => {
  if (props.detailType === 'refBudget') {
    value.value.details = addSequence(value.value.details, {
      type: 'With',
      source: '',
      referenceBudge: 0,
    } as TPP003BudgetAllocationsDetailRefBudget);

    return;
  }

  value.value.details = addSequence(value.value.details, {
    type: 'Without',
    source: '',
  } as TPP003BudgetAllocationsDetail);
};

const deleteItem = (index: number): void => {
  value.value.details = deleteItemAndReSequence(
    value.value.details,
    index
  );
};

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  value.value.details = reSequence(event.value);
};

const minReferenceBudget = computed(() => {
  if (!value.value.details || value.value.details.length === 0) return 0;

  const temp = value.value.details as TPP003BudgetAllocationsDetailRefBudget[];

  const budgets = temp
    .filter((detail) => detail.referenceBudge != null && detail.referenceBudge > 0)
    .map(detail => detail.referenceBudge || 0);

  return budgets.length > 0 ? Math.min(...budgets) : 0;
});

const minBasicRefBudget = computed(() => {
  const budget = (body.value.staff?.personnelCompensation ?? 0) + (body.value.expenseDescription?.materialCost ?? 0) + (body.value.expenseDescription?.overseasTravelCost ?? 0)
    + (body.value.expenseDescription?.otherExpenses ?? 0) + (body.value.expenseDescription?.hardwareCost ?? 0) + (body.value.expenseDescription?.softwareCost ?? 0) + (body.value.expenseDescription?.systemDevelopmentCost ?? 0);

  return budget;
});

watch(minReferenceBudget, (newMinValue) => {
  if (props.detailType === 'refBudget') {
    value.value.referenceMedianPrice = newMinValue;
  }
}, { immediate: true });

watch(minBasicRefBudget, (newVal) => {
  if (props.detailType === 'basic') {
    value.value.referenceMedianPrice = newVal;
  }
}, { immediate: true });
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="กำหนดราคากลาง (ราคาอ้างอิง)" class="mb-6" />
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mt-10">
        <Datepicker label="วันที่กำหนดราคากลาง (ราคาอ้างอิง)" v-model="value.referenceDate" :disabled=true />
        <InputNumber label="วงเงินงบประมาณ (บาท)" v-model="value.budget" rules="required" :min-fraction-digits="2" grouping
          disabled />
        <InputNumber label="ราคากลางอ้างอิง" v-model="value.referenceMedianPrice" rules="required" :min-fraction-digits="2" grouping
         disabled :min-number="1"/>
      </div>
      <div class="mt-4 flex items-center justify-end">
        <Button label="เพิ่มข้อมูล" icon="pi pi-plus" severity="primary" variant="outlined"
          class="bg-white! hover:bg-red-50!" @click="addDetail" v-if="states.isEditor && menuStore.hasManage" />
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
              <p class="w-full font-bold text-center">แหล่งที่มา</p>
            </template>
            <template #body="{ data }">
              <InputField v-model="data.source" rules="required" :disabled="!states.isEditor || !menuStore.hasManage" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" v-if="props.detailType === 'refBudget'">
            <template #header>
              <p class="w-full font-bold text-center">ราคาอ้างอิง</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-model="data.referenceBudge" rules="required|min_value:0.01" :min-fraction-digits="2" grouping
                :disabled="!states.isEditor || !menuStore.hasManage" />
            </template>
          </Column>
          <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top"
            v-if="states.isEditor && menuStore.hasManage">
            <template #body="{ index }">
              <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                @click="() => deleteItem(index)" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" bodyStyle="vertical-align: top;padding-top: 25px"
            v-if="menuStore.hasManage">
            <template #rowreordericon>
              <span class="material-symbols-outlined"
                :class="`${states.isEditor ? 'cursor-pointer' : 'cursor-default'}`" :draggable="states.isEditor">
                drag_indicator
              </span>
            </template>
          </Column>
          <template #empty>
            <p class="text-center">ไม่พบข้อมูล</p>
          </template>
        </DataTable>
      </div>
    </template>
  </Card>
</template>

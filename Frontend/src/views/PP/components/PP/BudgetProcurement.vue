<script setup lang="ts">
import type { Budgets, BudgetsDetail } from '../../models/PP002/pp002Model';
import type { Option } from '@/models/shared/option';
import { Card, Button, DataTable, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputNumber, InputField, Select } from '@/components/forms';
import { ArrayHelper } from '@/helpers/array';
import { computed, onMounted, ref, watch } from 'vue';
import { EGroupCode } from '@/enums/shared';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import { HttpStatusCode } from 'axios';
import { useMenuStore } from '@/stores/menu';
import draggable from 'vuedraggable';
import SharedService from '@/services/Shared/dropdown';
import { PP002DocumentTemplate } from '../../enums/pp002';
import { usePP002DetailStore } from '../../stores/PP002/pp002Store';

const value = defineModel<Budgets[]>({
  default: () => [],
});

const props = defineProps({
  titleName: {
    type: String,
    default: 'วงเงินงบประมาณที่จะจัดซื้อจ้าง',
  },
  disabled: {
    type: Boolean,
    default: false,
  },
  canAddBudget: {
    type: Boolean,
    default: false,
  },
  templateCode: {
    type: String,
    default: undefined,
  },
});

const pp002DetailStore = usePP002DetailStore();
const menuStore = useMenuStore();
const procuementStore = usePPDetailStore();
const { reSequence } = ArrayHelper();

onMounted(() => {
  getDepartmentDropdownAsync();
  getBudgetTypeDropdownAsync();
  getAccountCodeDropdownAsync();

  if (value.value.length === 0) {
    addItem();
    if (isHireTemplate.value) {
      addItem();
    }
  }
});

const departmentDropdown = ref<Option[]>([]);

const resolvedTemplateCode = computed(() => props.templateCode ?? pp002DetailStore.PP002Detail.torDocumentTemplateCode);

const isHireTemplate = computed(() => ([
  PP002DocumentTemplate.TorBuyWithHire60,
  PP002DocumentTemplate.TorBuyWithHire80,
  PP002DocumentTemplate.TorHireWithHire60,
  PP002DocumentTemplate.TorHireWithHire80,
] as string[]).includes(resolvedTemplateCode.value));

const isHireOnlyTemplate = computed(() => ([
  PP002DocumentTemplate.TorHireWithHire60,
  PP002DocumentTemplate.TorHireWithHire80,
] as string[]).includes(resolvedTemplateCode.value));

const isBuyHireOnlyTemplate = computed(() => ([
  PP002DocumentTemplate.TorBuyWithHire60,
  PP002DocumentTemplate.TorBuyWithHire60,
] as string[]).includes(resolvedTemplateCode.value));

const conditionLabel = (index: number) => {
  if (index != 0) {
    return 'สัญญาจ้างบริการบำรุงรักษาฯ';
  }

  if (isHireOnlyTemplate.value) {
    return 'สัญญาจ้าง';
  }

  if (isBuyHireOnlyTemplate.value) {
    return 'สัญญาซื้อ';
  }

  return '';
}

const getDepartmentDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SolId, undefined, true);

  if (status === HttpStatusCode.Ok) {
    departmentDropdown.value = data;
  }
};

const budgetTypeDropdown = ref<Option[]>([]);

const getBudgetTypeDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.BudgetTyp, undefined, true);

  if (status === HttpStatusCode.Ok) {
    budgetTypeDropdown.value = data;
  }
};

const accountCodeDropdown = ref<Option[]>([]);

const getAccountCodeDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.GLAcc, undefined, true);

  if (status === HttpStatusCode.Ok) {
    accountCodeDropdown.value = data;
  }
};

const addItem = (): void => {
  value.value.push({
    sequence: value.value.length + 1,
    budgetAmount: 0,
    description: procuementStore.procurementDetail.planName ?? '',
    details: [
      { sequence: 1 }
    ] as BudgetsDetail[]
  } as Budgets);
};

const addItemDetail = (index: number): void => {
  const data = value.value[index];

  data.details.push({
    sequence: data.details.length + 1,
  } as BudgetsDetail);


  calculateBudgetAmount(index);
};

const deleteItem = (index: number): void => {
  value.value.splice(index, 1);
};

const deleteSubItem = (mainIndex: number, index: number): void => {
  value.value[mainIndex].details.splice(index, 1);
  value.value[mainIndex].details = reSequence(value.value[mainIndex].details);

  calculateBudgetAmount(mainIndex);
};

const containerRef = ref<HTMLElement | null>(null);

const onResequence = (): void => {
  value.value = reSequence(value.value);
};

const onGroupDragOver = (e: DragEvent): void => {
  if (!containerRef.value?.contains(e.target as Node) && e.dataTransfer) {
    e.dataTransfer.dropEffect = 'none';
  }
};

const onGroupDragStart = (): void => document.addEventListener('dragover', onGroupDragOver);
const onGroupDragEnd = (): void => {
  document.removeEventListener('dragover', onGroupDragOver);
  onResequence();
};

const onRowReorder = (mainIndex: number, event: DataTableRowReorderEvent): void => {
  value.value[mainIndex].details = reSequence(event.value);
};

const onChangeBudgetType = (e: string | undefined, groupIndex: number, index: number) => {
  if (e && e === 'BudgetType001') {
    value.value[groupIndex].details[index].projectCode = undefined;
  }
};

const onBudgetChange = (groupIndex: number): void => {
  calculateBudgetAmount(groupIndex);
};

const calculateBudgetAmount = (groupIndex: number): void => {
  if (!value.value[groupIndex] || !value.value[groupIndex].details) return;

  const totalBudget = value.value[groupIndex].details.reduce((sum, detail) => {
    return sum + (detail.budget || 0);
  }, 0);

  value.value[groupIndex].budgetAmount = totalBudget;
};

watch(
  () => value.value,
  (newValue) => {
    if (newValue) {
      newValue.forEach((_, index) => {
        calculateBudgetAmount(index);
      });
    }
  },
  { deep: true }
);
</script>

<template>
  <Card class="mb-4" data-section-id="budget-procurement" :data-section-label="props.titleName">
    <template #content>
      <TitleHeader :label="props.titleName" />
      <div class="my-4 mt-9">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-2">
          <InputNumber label="วงเงินทั้งสิ้น" v-model="procuementStore.procurementDetail.budget" disabled grouping
            :min-fraction-digits="2" />
        </div>
        <div class="flex gap-2 md:gap-4 items-center">
          <small class="whitespace-nowrap font-bold">รายละเอียดวงเงินงบประมาณที่จะจัดซื้อจัดจ้าง</small>
          <div class="h-px bg-gray-300 flex-1" />
          <Button icon="pi pi-plus" label="เพิ่มวงเงิน" variant="outlined" severity="primary" @click="addItem"
            v-if="!props.disabled && menuStore.hasManage && props.canAddBudget && (isHireTemplate && value.length < 2)" />
        </div>
        <div ref="containerRef">
        <draggable v-model="value" handle=".drag-group" group="budget-groups" itemKey="sequence" @start="onGroupDragStart" @end="onGroupDragEnd">
          <template #item="{ element: budgetGroup, index: groupIndex }: { element: Budgets, index: number }">
            <div class="p-1 pt-5 bg-gray-100 mt-2 mb-4">
              <div class="px-4 mb-2">
                <p class="font-bold">{{ conditionLabel(groupIndex) }}</p>
              </div>
              <div class="flex justify-end items-center mt-8">
                <div class="flex gap-2 mb-2">
                  <Button icon="pi pi-trash" severity="danger" variant="text"
                    class="bg-transparent transition hover:scale-110 duration-300" @click="() => deleteItem(groupIndex)"
                    v-if="!props.disabled && groupIndex !== 0 && menuStore.hasManage" />
                  <span class="mt-1.75 material-symbols-outlined cursor-pointer text-gray-500 drag-group"
                    v-if="!props.disabled && menuStore.hasManage">
                    drag_indicator
                  </span>
                </div>
              </div>
              <div class="px-4 grid grid-cols-2 gap-2">
                <InputField label="รายละเอียด" v-model="budgetGroup.description" rules="required"
                  :disabled="props.disabled || !menuStore.hasManage" />
                <InputNumber label="จำนวนเงิน" v-model="budgetGroup.budgetAmount"
                  :rules="`required|max_value:${procuementStore.procurementDetail.budget}`" disabled grouping
                  :min-fraction-digits="2" />
              </div>
              <div class="px-4 py-2">
                <div class="flex gap-2 items-cetner justify-between">
                  <p class="font-bold">ข้อมูลรหัสบัญชีและการใช้งบประมาณของฝ่าย</p>
                  <Button icon="pi pi-plus" label="เพิ่มข้อมูล" variant="outlined" severity="primary"
                    class="bg-white! hover:bg-red-50!" @click="() => addItemDetail(groupIndex)"
                    v-if="!props.disabled && menuStore.hasManage" />
                </div>
                <div class="mt-4">
                  <DataTable :value="value[groupIndex].details" @row-reorder="(e) => onRowReorder(groupIndex, e)"
                    v-if="value[groupIndex].details?.length > 0">
                    <Column bodyStyle="vertical-align: top">
                      <template #header>
                        <p class="w-full font-bold text-center">ลำดับ</p>
                      </template>
                      <template #body="{ data }">
                        <p class="text-center">{{ data.sequence }}</p>
                      </template>
                    </Column>
                    <Column bodyStyle="vertical-align: top" body-class="min-w-[200px]">
                      <template #header>
                        <p class="w-full font-bold text-center">ศูนย์ต้นทุน</p>
                      </template>
                      <template #body="{ data }">
                        <Select v-model="data.department" :options="departmentDropdown" rules="required"
                          :disabled="props.disabled || !menuStore.hasManage" />
                      </template>
                    </Column>
                    <Column bodyStyle="vertical-align: top" body-class="min-w-[300px]">
                      <template #header>
                        <p class="w-full font-bold text-center">ประเภทงบประมาณ</p>
                      </template>
                      <template #body="{ data, index }">
                        <Select v-model="data.budgetType" :options="budgetTypeDropdown" rules="required"
                          :disabled="props.disabled || !menuStore.hasManage"
                          @on-select="(e: string | undefined) => onChangeBudgetType(e, groupIndex, index)" />
                      </template>
                    </Column>
                    <Column bodyStyle="vertical-align: top" body-class="min-w-[300px]">
                      <template #header>
                        <p class="w-full font-bold text-center">รหัสโครงการ</p>
                      </template>
                      <template #body="{ data }">
                        <InputField v-model="data.projectCode"
                          :rules="`${data.budgetType === 'BudgetType001' ? '' : 'required'}`"
                          :disabled="props.disabled || !menuStore.hasManage || data.budgetType === 'BudgetType001'" />
                      </template>
                    </Column>
                    <Column bodyStyle="vertical-align: top" body-class="min-w-[300px]">
                      <template #header>
                        <p class="w-full font-bold text-center">รหัสบัญชี</p>
                      </template>
                      <template #body="{ data }">
                        <Select v-model="data.accountNo" :options="accountCodeDropdown" rules="required"
                          :disabled="props.disabled || !menuStore.hasManage" />
                      </template>
                    </Column>
                    <Column bodyStyle="vertical-align: top" body-class="min-w-[300px]">
                      <template #header>
                        <p class="w-full font-bold text-center">จำนวนเงิน</p>
                      </template>
                      <template #body="{ data }">
                        <InputNumber v-model="data.budget" rules="required"
                          :disabled="props.disabled || !menuStore.hasManage" grouping :min-fraction-digits="2"
                          :max-fraction-digits="3" @update:model-value="() => onBudgetChange(groupIndex)" />
                      </template>
                    </Column>
                    <Column bodyStyle="vertical-align: top" body-class="max-w-[50px]">
                      <template #body="{ index }">
                        <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                          @click="() => deleteSubItem(groupIndex, index)"
                          v-if="!props.disabled && index !== 0 && menuStore.hasManage" />
                      </template>
                    </Column>
                    <Column rowReorder headerStyle="width: 3rem" body-class="max-w-[50px]"
                      bodyStyle="vertical-align: top;padding-top: 25px">
                      <template #rowreordericon>
                        <span class="material-symbols-outlined cursor-pointer" :draggable="true"
                          v-if="!props.disabled && menuStore.hasManage">
                          drag_indicator
                        </span>
                      </template>
                    </Column>
                  </DataTable>
                </div>
              </div>
            </div>
          </template>
        </draggable>
        </div>
      </div>
    </template>
  </Card>
</template>
<script setup lang="ts">
import type { Budget, BudgetDetail } from '@/models/PCM/PCM005/principle';
import { Card, Button, DataTable } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputNumber, InputField, Select } from '@/components/forms';
import { ArrayHelper } from '@/helpers/array';
import { computed } from 'vue';
import { usePcm005PrincipleStore } from '@/stores/PCM/PCM005/principle';
import { useMenuStore } from '@/stores/menu';
import draggable from 'vuedraggable';
import { usePcm005DetailStore } from '@/stores/PCM/PCM005/pcm005';

const menuStore = useMenuStore();
const store = usePcm005PrincipleStore();
const storeDetail = usePcm005DetailStore();
const { reSequence, addSequence, deleteItemAndReSequence } = ArrayHelper();

const budgetSum = computed((): number => {
  return store.body.budgets?.reduce((sum, _, index) => {
    return sum + getBudgetGroupAmount(index);
  }, 0) || 0;
});

const getBudgetGroupAmount = (groupIndex: number): number => {
  return store.body.budgets[groupIndex].details?.reduce((sum, detail) => {
    const valid = typeof detail.budget === 'number' && !isNaN(sum);
    return valid ? sum + detail.budget : sum;
  }, 0) || 0;
};

const addBudget = (): void => {
  store.body.budgets = addSequence(store.body.budgets, {
    sequence: store.body.budgets?.length + 1,
    description: storeDetail.body.planName || '',
    details: [
      {
        sequence: 1,
      }
    ]
  } as Budget);
};

const removeBudget = (index: number): void => {
  store.body.budgets = deleteItemAndReSequence(store.body.budgets, index);
};

const reSequenceBudget = (): void => {
  store.body.budgets = reSequence(store.body.budgets);
};

const addBudgetDetail = (budgetIndex: number): void => {
  store.body.budgets[budgetIndex].details = addSequence(store.body.budgets[budgetIndex].details, {
    sequence: store.body.budgets?.length + 1,
  } as BudgetDetail);
};

const removeBudgetDetail = (budgetIndex: number, index: number): void => {
  store.body.budgets[budgetIndex].details = deleteItemAndReSequence(store.body.budgets[budgetIndex].details, index);
};

const reSequenceBudgetDetail = (budgetIndex: number, details: BudgetDetail[]): void => {
  store.body.budgets[budgetIndex].details = reSequence(details);
};

const onChangeBudgetType = (e: string | undefined, groupIndex: number, index: number) => {
  if (e && e === 'BudgetType001' && store.body.budgets[groupIndex].details) {
    store.body.budgets[groupIndex].details[index].projectCode = undefined;
  }
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="งบประมาณ" />
      <div class="my-4">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mt-8">
          <InputNumber label="วงเงินทั้งสิ้น" v-model="budgetSum" disabled grouping :min-fraction-digits="2" />
        </div>
        <div class="flex gap-2 md:gap-4 items-center">
          <small class="whitespace-nowrap font-bold">รายละเอียดวงเงินงบประมาณที่จะจัดซื้อจัดจ้าง</small>
          <div class="h-px bg-gray-300 flex-1" />
          <Button icon="pi pi-plus" label="เพิ่มวงเงิน" variant="outlined" severity="primary" @click="addBudget"
            v-if="store.status.canEdit && menuStore.hasManage" />
        </div>
        <draggable v-model="store.body.budgets" handle=".drag-group" group="group" itemKey="sequence"
          @end="reSequenceBudget">
          <template #item="{ element: budgetGroup, index: groupIndex }: { element: Budget, index: number }">
            <div class="p-1 bg-gray-100 mt-2 mb-4">
              <div class="flex justify-end items-center">
                <div class="flex gap-2 mb-2" v-if="store.status.canEdit && menuStore.hasManage">
                  <Button icon="pi pi-trash" severity="danger" variant="text"
                    class="bg-transparent transition hover:scale-110 duration-300"
                    @click="() => removeBudget(groupIndex)" v-if="groupIndex !== 0" />
                  <span class="mt-1.75 material-symbols-outlined cursor-pointer text-gray-500 drag-group">
                    drag_indicator
                  </span>
                </div>
              </div>
              <div class=" px-4 grid grid-cols-2 gap-2 mt-9">
                <InputField label="รายละเอียด" v-model="budgetGroup.description" rules="required"
                  :disabled="!store.status.canEdit || !menuStore.hasManage" />
                <InputNumber label="จำนวนเงิน" :model-value="getBudgetGroupAmount(groupIndex)" disabled grouping
                  :min-fraction-digits="2" />
              </div>
              <div class="px-4 py-2">
                <div class="flex gap-2 items-center justify-between">
                  <p class="font-bold">ข้อมูลรหัสบัญชีและการใช้งบประมาณของฝ่าย</p>
                  <Button icon="pi pi-plus" label="เพิ่มข้อมูล" variant="outlined" severity="primary"
                    class="bg-white! hover:bg-red-50!" @click="() => addBudgetDetail(groupIndex)"
                    v-if="store.status.canEdit && menuStore.hasManage" />
                </div>
                <div class="mt-4">
                  <DataTable :value="store.body.budgets[groupIndex].details"
                    @row-reorder="(e) => reSequenceBudgetDetail(groupIndex, e.value)"
                    v-if="store.body.budgets[groupIndex].details && store.body.budgets[groupIndex].details?.length > 0">
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
                        <p class="w-full font-bold text-center">ศูนย์ต้นทุน</p>
                      </template>
                      <template #body="{ data }">
                        <Select v-model="data.department" :options="store.departmentDDL" rules="required"
                          :disabled="!store.status.canEdit || !menuStore.hasManage" />
                      </template>
                    </Column>
                    <Column bodyStyle="vertical-align: top">
                      <template #header>
                        <p class="w-full font-bold text-center">ประเภทงบประมาณ</p>
                      </template>
                      <template #body="{ data, index }">
                        <Select v-model="data.budgetType" :options="store.budgetTypeDDL" rules="required"
                          :disabled="!store.status.canEdit || !menuStore.hasManage"
                          @on-select="(e) => onChangeBudgetType(e, groupIndex, index)" />
                      </template>
                    </Column>
                    <Column bodyStyle="vertical-align: top">
                      <template #header>
                        <p class="w-full font-bold text-center min-w-[150px]">รหัสโครงการ</p>
                      </template>
                      <template #body="{ data }">
                        <InputField v-model="data.projectCode"
                          :rules="`${data.budgetType === 'BudgetType001' ? '' : 'required'}`"
                          :disabled="!store.status.canEdit || !menuStore.hasManage || data.budgetType === 'BudgetType001'" />
                      </template>
                    </Column>
                    <Column bodyStyle="vertical-align: top">
                      <template #header>
                        <p class="w-full font-bold text-center">รหัสบัญชี</p>
                      </template>
                      <template #body="{ data }">
                        <Select v-model="data.accountNo" :options="store.accountCodeDDL" rules="required"
                          :disabled="!store.status.canEdit || !menuStore.hasManage" />
                      </template>
                    </Column>
                    <Column bodyStyle="vertical-align: top">
                      <template #header>
                        <p class="w-full font-bold text-center min-w-[150px]">จำนวนเงิน</p>
                      </template>
                      <template #body="{ data }">
                        <InputNumber :min-fraction-digits="2" v-model="data.budget" rules="required" grouping
                          :disabled="!store.status.canEdit || !menuStore.hasManage" />
                      </template>
                    </Column>
                    <Column bodyStyle="vertical-align: top" v-if="store.status.canEdit && menuStore.hasManage">
                      <template #body="{ index }">
                        <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                          @click="() => removeBudgetDetail(groupIndex, index)" v-if="index != 0" />
                      </template>
                    </Column>
                    <Column rowReorder headerStyle="width: 3rem" bodyStyle="vertical-align: top;padding-top: 25px"
                      v-if="store.status.canEdit && menuStore.hasManage">
                      <template #rowreordericon>
                        <span class="material-symbols-outlined cursor-pointer" :draggable="true">
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
    </template>
  </Card>
</template>

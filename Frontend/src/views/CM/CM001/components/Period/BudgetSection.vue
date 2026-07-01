<script setup lang="ts">
import { EGroupCode } from "@/enums/shared";
import { ArrayHelper } from "@/helpers/array";
import type { Option } from "@/models/shared/option";
import SharedService from "@/services/Shared/dropdown";
import { formatCurrency } from "@/helpers/currency";
import type { BudgetsDetail } from "@/views/PP/models/PP002/pp002Model";
import { HttpStatusCode } from "axios";
import { Button, Card, DataTable, type DataTableRowReorderEvent } from "primevue";
import { computed, onBeforeMount, ref } from "vue";

type Props = {
  title?: string;
  disabled?: boolean;
};

const { reSequence } = ArrayHelper();
const { disabled } = defineProps<Props>();

const value = defineModel<Array<BudgetsDetail>>({
  required: true,
});

const addItemDetail = (): void => {
  const data = value.value;

  data.push({
    sequence: data.length + 1,
  } as BudgetsDetail);
};

onBeforeMount(() => {
  getDepartmentDropdownAsync();
  getBudgetTypeDropdownAsync();
  getAccountCodeDropdownAsync();

  if (!value.value || value.value.length === 0) {
    value.value = [{
      sequence: 1,
    } as BudgetsDetail];
  }
});

const departmentDropdown = ref<Option[]>([]);

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

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  value.value = reSequence(event.value);
};

const deleteSubItem = (index: number): void => {
  value.value.splice(index, 1);
  value.value = reSequence(value.value);
};

const totalBudget = computed<number>(() =>
  (value.value ?? []).reduce((sum, s) => sum + (s.budget || 0), 0)
);

const onChangeBudgetType = (e: string | undefined, index: number) => {
  if (e && e === 'BudgetType001') {
    value.value[index].projectCode = undefined;
  }
};

</script>

<template>
  <Card>
    <template #content>
      <div class="flex gap-2 items-center justify-between">
        <TitleHeader :label="title ?? 'ข้อมูลรหัสบัญชีและการใช้งบประมาณของฝ่าย'">
          <template #action>
            <Button icon="pi pi-plus" label="เพิ่มข้อมูล" variant="outlined" severity="primary"
              class="bg-white! hover:bg-red-50!" @click="() => addItemDetail()" v-if="!disabled" />
          </template>
        </TitleHeader>
      </div>
      <div class="mt-4">
        <DataTable :value="value" @row-reorder="(e) => onRowReorder(e)" v-if="value?.length > 0">
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
              <Select v-model="data.department" :options="departmentDropdown" rules="required" :disabled="disabled" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" body-class="min-w-[175px]">
            <template #header>
              <p class="w-full font-bold text-center">ประเภทงบประมาณ</p>
            </template>
            <template #body="{ data, index }">
              <Select v-model="data.budgetType" :options="budgetTypeDropdown" rules="required"
                @on-select="(e: string | undefined) => onChangeBudgetType(e, index)" :disabled="disabled" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" body-class="min-w-[175px]">
            <template #header>
              <p class="w-full font-bold text-center">รหัสโครงการ</p>
            </template>
            <template #body="{ data }">
              <InputField v-model="data.projectCode" :rules="`${data.budgetType === 'BudgetType001' ? '' : 'required'}`"
                :disabled="disabled || data.budgetType === 'BudgetType001'" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" body-class="min-w-[175px]">
            <template #header>
              <p class="w-full font-bold text-center">รหัสบัญชี</p>
            </template>
            <template #body="{ data }">
              <Select v-model="data.accountNo" :options="accountCodeDropdown" rules="required" :disabled="disabled" />
            </template>
            <template #footer>
              <p class="text-end font-bold">รวมทั้งหมด</p>
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" body-class="min-w-[200px]">
            <template #header>
              <p class="w-full font-bold text-center">จำนวนเงิน</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-model="data.budget" rules="required" :disabled="disabled" grouping
                :min-fraction-digits="2" />
            </template>
            <template #footer>
              <p class="text-end font-bold">{{ formatCurrency(totalBudget) }} บาท</p>
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" body-class="max-w-[50px]">
            <template #body="{ index }">
              <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                @click="() => deleteSubItem(index)" v-if="!disabled && value.length != 1" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" body-class="max-w-[50px]"
            bodyStyle="vertical-align: top;padding-top: 25px">
            <template #rowreordericon>
              <span class="material-symbols-outlined cursor-pointer" :draggable="true" v-if="!disabled">
                drag_indicator
              </span>
            </template>
          </Column>
        </DataTable>
      </div>
    </template>
  </Card>
</template>
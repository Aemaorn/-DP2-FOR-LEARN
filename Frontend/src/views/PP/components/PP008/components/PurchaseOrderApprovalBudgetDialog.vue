<script setup lang="ts">
import { Dialog } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { ref, watch, type PropType } from 'vue';
import { ButtonSave } from '@/components/Button';
import { Form } from 'vee-validate';
import ToastHelper from '@/helpers/toast';
import { usePP008DetailStore } from '@/views/PP/stores/PP008/PP008Store';
import type { Budgets, ContractGroup } from '@/views/PP/models/PP008/pp008model';
import { InputArea, InputNumber } from '@/components/forms';

const show = defineModel({
  type: Boolean,
  default: false,
  required: true,
});

const props = defineProps({
  selected: { type: Object as PropType<ContractGroup | null>, required: false },
});

const store = usePP008DetailStore();

const budget = ref<Budgets>({
  id: '',
  description: '',
  budgetAmount: 0,
} as Budgets);

const onSubmitAsync = async (): Promise<void> => {
  if (!budget.value.description || !budget.value.budgetAmount) {
    return ToastHelper.warning('เพิ่มวงเงินที่จัดซื้อจัดจ้าง', 'กรุณาระบุวงเงินที่จัดซื้อจัดจ้าง');
  }

  const sumBudget = store.detail.contractBudgetGroups?.reduce((sum, item) => {
    if (item.budgetId === budget.value.id) return sum;
    return sum + (item.budget ?? 0);
  }, 0) ?? 0;
  const sumBudgetAmount = sumBudget + budget.value.budgetAmount;
  if (sumBudgetAmount > store.detail.procurementBudget) {
    return ToastHelper.warning('เพิ่มวงเงินที่จัดซื้อจัดจ้าง', 'วงเงินที่จัดซื้อจัดจ้างรวมกันต้องไม่เกินวงเงินงบประมาณ');
  }

  if (budget.value.id) {
    await store.onUpdateApprovalBudgetAsync(budget.value);
  } else {
    await store.onCreateApprovalBudgetAsync(budget.value);
  }

  show.value = false;
};

const onClose = (): void => {
  show.value = false;
};

watch(() => show.value, (newValue) => {
  if (newValue && props.selected) {
    budget.value = { description: props.selected.budgetDescription, budgetAmount: props.selected.budget, id: props.selected.budgetId } as Budgets;
  }

  if (!newValue) {
    budget.value = { description: '', budgetAmount: 0 } as Budgets;
  }
});
</script>

<template>
  <Dialog v-model:visible="show" modal :style="{ width: '80vw' }" :draggable="false" :breakpoints="{ '575px': '90vw' }">
    <template #container>
      <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" class="p-5 overflow-auto">
        <TitleHeader label="วงเงินที่จัดซื้อจัดจ้าง">
          <template #action>
            <i class="pi pi-times cursor-pointer" @click="onClose"></i>
          </template>
        </TitleHeader>

        <div class="grid lg:grid-cols-1 gap-2 gap-y-8 mt-10">
          <InputArea label="รายการจัดซื้อจัดจ้าง" rules="required" v-model="budget.description" />
          <InputNumber label="วงเงิน (บาท)" grouping rules="required" v-model="budget.budgetAmount" :min-fraction-digits="2" />
        </div>

        <div class="mt-5 flex gap-2 justify-end items-center">
          <Button severity="secondary" variant="outlined" label="ยกเลิก" @click="onClose" />
          <ButtonSave type="submit" />
        </div>
      </Form>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Checkbox, InputArea, InputField, InputNumber } from '@/components/forms';
import { souceType } from '@/enums/CM/cm001';
import { ArrayHelper } from '@/helpers/array';
import { formatCurrency } from '@/helpers/currency';
import type { Cm001PaymentTerm } from '@/models/CM/cm001';
import { useCM001PeriodStore } from '@/stores/CM/cm001Period';
import { useMenuStore } from '@/stores/menu';
import { Button, Card, Column, DataTable } from 'primevue';
import { computed, ref, watch } from 'vue';

type Props = {
  disabled?: boolean;
};

const props = defineProps<Props>();

const { reSequence, deleteItemAndReSequence } = ArrayHelper();

const store = useCM001PeriodStore();
const menuStore = useMenuStore();

const componentKey = ref(0);

watch(() => store.body.id, () => {
  componentKey.value++;
});

const totalAmount = computed(() => {
  return store.body.paymentTerms.reduce((sum, item) => sum + (item.amount || 0), 0);
});

watch(totalAmount, (newTotal) => {
  if (store.isLoading) return;

  if (!store.states.isEdit) {
    return;
  }

  if (store.body.budgetDetails?.length === 1) {
    store.body.budgetDetails[0].budget = newTotal;

    return;
  }
  if (store.body.budgetDetails && store.body.budgetDetails.length > 1) {
    const hasValues = store.body.budgetDetails.some(item => item.budget);
    if (!hasValues) {
      store.body.budgetDetails.forEach(item => item.budget = undefined as any);
    }
  }
});

const totalDeductions = computed(() => {
  let deductions = 0;
  if (store.body.hasInvoiceSlip) {
    deductions += store.body.invoiceSlipAmount || 0;
  }
  return deductions;
});

const finalAmount = computed(() => {
  return totalAmount.value - totalDeductions.value;
});

const addItem = () => {
  const data = store.body.paymentTerms;

  data.push({
    sequence: data.length + 1,
    paymentTerm: data.length + 1,
    description: '',
    amount: 0,
  } as Cm001PaymentTerm);
};

const removeItem = (index: number) => {
  store.body.paymentTerms = deleteItemAndReSequence(store.body.paymentTerms, index);
};

const onRowReorder = (event: { dragIndex: number; dropIndex: number }) => {
  const { dragIndex, dropIndex } = event;
  const items = store.body.paymentTerms;
  const [movedItem] = items.splice(dragIndex, 1);
  items.splice(dropIndex, 0, movedItem);
  reSequence(items);

  store.body.paymentTerms = reSequence(items);
};

const onContractBudgetChange = async () => {
  await store.fn.getDefaultAcceptor();
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="บันทึกตรวจรับ" />
      <div :key="componentKey" class="mt-10 grid grid-cols-1 lg:grid-cols-4 gap-y-8 gap-4">
        <InputField disabled v-model="store.body.acceptanceNumber" label="เลขที่ตรวจรับ" />
        <Datepicker label="วันที่เอกสาร" v-model="store.body.documentDate"
          :disabled="props.disabled" />
        <InputField v-model="store.body.phoneNumber" rules="required" label="เบอร์โทรศัพท์"
          :disabled="props.disabled" />
        <InputNumber v-model="store.body.contractBudgetAmount" rules="required|min_value:0.01" :min-fraction-digits="2"
          grouping class="col-start-1" label="วงเงินตามสัญญา"
          :disabled="props.disabled || store.body.cm001Info?.sourceType == souceType.ContractDraftVendor"
          @update:model-value="onContractBudgetChange" />
        <InputArea v-model="store.body.objectiveDescription" rules="required" class="lg:col-span-4" label="วัตถุประสงค์"
          :disabled="props.disabled" />
        <InputArea v-model="store.body.description" rules="required" class="lg:col-span-4" label="การพิจารณา"
          :disabled="props.disabled" />
      </div>
      <div class="bg-gray-100 p-4 rounded">
        <TitleHeader label="รายการตรวจรับพัสดุ">
          <template #action>
            <Button v-if="!props.disabled && menuStore.hasManage" icon="pi pi-plus" label="เพิ่มรายการ"
              variant="outlined" severity="primary" class="bg-white! hover:bg-red-50!" @click="() => addItem()"
              :disabled="props.disabled || !menuStore.hasManage" />
          </template>
        </TitleHeader>
        <DataTable :value="store.body.paymentTerms" @row-reorder="onRowReorder"
          v-if="store.body.paymentTerms.length > 0">
          <Column bodyStyle="vertical-align: top" body-class="min-w-[150px] max-w-[200px]">
            <template #header>
              <p class="w-full font-bold text-center">งวดการชำระ</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-model="data.paymentTerm" rules="required"
                :disabled="props.disabled || !menuStore.hasManage" input-class="text-center" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" body-class="min-w-[400px]">
            <template #header>
              <p class="w-full font-bold text-center">รายละเอียด</p>
            </template>
            <template #body="{ data }">
              <InputArea v-model="data.description" rules="required" :rows="3"
                :disabled="props.disabled || !menuStore.hasManage" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" body-class="min-w-[300px]">
            <template #header>
              <p class="w-full font-bold text-center">จำนวนเงิน</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-model="data.amount" rules="required|min_value:1"
                :disabled="props.disabled || !menuStore.hasManage" grouping :min-fraction-digits="2"
                class="text-center" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" body-class="min-w-[50px] w-[50px]"
            v-if="!props.disabled && menuStore.hasManage && store.body.paymentTerms.length > 1">
            <template #body="{ index }">
              <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                @click="() => removeItem(index)" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" body-class="min-w-[50px] w-[50px]"
            v-if="!props.disabled && menuStore.hasManage" bodyStyle="vertical-align: top;padding-top: 25px">
            <template #rowreordericon>
              <span class="material-symbols-outlined cursor-pointer" :draggable="true">
                drag_indicator
              </span>
            </template>
          </Column>
        </DataTable>
        <p v-else class="text-center text-gray-500 py-4">-- ไม่มีรายการ --</p>
      </div>

      <div class="bg-gray-100 p-4 py-8 rounded grid grid-cols-4 mt-8 gap-y-8 gap-4 items-center">
        <Checkbox label="มีค่าปรับ" v-model="store.body.hasDeduction" @update:model-value="() => {
          store.body.deductionDescription = undefined;
          store.body.deductionAmount = undefined;
        }" :disabled="props.disabled" />
        <InputField v-model="store.body.deductionDescription" class="col-span-2" label="รายละเอียดการค่าปรับ"
          :disabled="!store.body.hasDeduction || props.disabled" :rules="store.body.hasDeduction ? 'required' : ''" />
        <InputNumber v-model="store.body.deductionAmount" class="col-span-1" label="จำนวนเงิน" :min-fraction-digits="2"
          grouping :disabled="!store.body.hasDeduction || props.disabled"
          :rules="store.body.hasDeduction ? 'required' : ''" />

        <Checkbox label="มีใบลดหนี้" v-model="store.body.hasInvoiceSlip" :disabled="props.disabled" @update:model-value="() => {
          store.body.invoiceSlipDescription = undefined;
          store.body.invoiceSlipAmount = undefined;
        }" />
        <InputField v-model="store.body.invoiceSlipDescription" class="col-span-2" label="รายละเอียดการใบลดหนี้"
          :disabled="!store.body.hasInvoiceSlip || props.disabled"
          :rules="store.body.hasInvoiceSlip ? 'required' : ''" />
        <InputNumber v-model="store.body.invoiceSlipAmount" class="col-span-1" label="จำนวนเงิน"
          :min-fraction-digits="2" grouping :disabled="!store.body.hasInvoiceSlip || props.disabled"
          :rules="store.body.hasInvoiceSlip ? 'required' : ''" />
      </div>

      <div class="flex flex-col w-full items-end mt-8">
        <div class="flex items-center gap-4 text-2xl font-bold mb-2">
          <span class="text-right">รวมจำนวนเงิน</span>
          <span class="min-w-[150px] text-right">{{ formatCurrency(totalAmount) }}</span>
        </div>

        <div v-if="store.body.hasInvoiceSlip" class="flex items-center gap-4 text-xl font-bold text-primary-500 mb-4">
          <span class="text-right">ใบลดหนี้</span>
          <span class="min-w-[150px] text-right">{{ formatCurrency(store.body.invoiceSlipAmount || 0) }}</span>
        </div>

        <div class="flex items-center gap-4 text-xl font-bold border-t border-gray-300 pt-4">
          <span class="text-right">รวมจำนวนเงินทั้งสิ้น</span>
          <span class="min-w-[150px] text-right">{{ formatCurrency(finalAmount) }}</span>
        </div>
      </div>
    </template>
  </Card>
</template>
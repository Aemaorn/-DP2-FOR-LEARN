<script setup lang="ts">
import type { InstallmentRequest } from '@/models/CM/cm004';
import { Card } from 'primevue';
import { Datepicker, InputArea, InputNumber } from '@/components/forms';
import { formatCurrency } from '@/helpers/currency';
import { computed, ref, watch } from 'vue';
import { useCm004DetailStore } from '@/stores/CM/cm004';
import { ToDateOnly } from '@/helpers/dateTime';
import { useMenuStore } from '@/stores/menu';
import DisbursementDialog from './DisbursementDialog.vue';
import { CmDisbursementApprovalStatus } from '@/enums/CM/cm004';
import BudgetSection from '../../CM001/components/Period/BudgetSection.vue';

const menuStore = useMenuStore();
const store = useCm004DetailStore();
const showModal = ref<boolean>(false);

const summary = computed<number>((): number => {
  if (!store.body.installments || store.body.installments.length === 0) return 0;

  return store.body.installments.reduce((sum: number, s: InstallmentRequest): number => sum + (s.amount || 0), 0);
});

watch((): number => summary.value, (val: number): void => {
  store.body.netClaimAmount = val;
});

const onSelectFromDialog = (dataList: InstallmentRequest[]): void => {
  const dupData = (store.body.installments && store.body.installments.length > 0) ? [...store.body.installments] : [];

  store.body.installments = [...dupData, ...dataList];
};

const onDelete = (index: number): void => {
  if (store.body.installments && store.body.installments.length > 0) {
    store.body.installments.splice(index, 1);
  };
};

const isRequired = computed(() => {
  return store.body.status != CmDisbursementApprovalStatus.Draft;
});
</script>

<template>
  <Card>
    <template #content>
      <div class="mt-8">
        <div class="grid grid-cols-4">
          <Datepicker label="วันที่ขออนุมัติ" :rules="isRequired ? 'required' : ''" v-model="store.body.requestDate"
            :disabled="!store.status.canEdit || !menuStore.hasManage" />
        </div>
        <div class="grid grid-cols-4 mt-8">
          <InputArea label="เรื่อง" :rules="isRequired ? 'required' : ''" class="col-span-3"
            v-model="store.body.subject" :disabled="!store.status.canEdit || !menuStore.hasManage" />
        </div>
        <div class="grid grid-cols-4 mt-8">
          <InputArea label="รายละเอียด" :rules="isRequired ? 'required' : ''" class="col-span-3"
            v-model="store.body.description" :disabled="!store.status.canEdit || !menuStore.hasManage" />
        </div>
      </div>
    </template>
  </Card>
  <Card class="my-5">
    <template #content>
      <div class="flex gap-5 items-center">
        <p class="font-bold">รายละเอียดการขออนุมัติเบิกจ่าย</p>
        <Divider class="flex-1" />
        <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="hover:bg-yellow-50 bg-white" @click="() => showModal = true"
          v-if="store.status.canEdit && menuStore.hasManage" />
      </div>
      <DataTable :value="store.body.installments" tableStyle="min-width: 100%" class="mt-5">
        <Column bodyStyle="vertical-align: top">
          <template #header>
            <p class="w-full font-bold text-center min-w-20">งวดที่</p>
          </template>
          <template #body="{ index }">
            <div>
              <p class="text-center">{{ index + 1 }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: center">
          <template #header>
            <p class="w-full font-bold text-center min-w-20">วันที่ส่งมอบ</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-center">{{ ToDateOnly(data.deliveryDetail[0].deliveryDate) }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: center">
          <template #header>
            <p class="w-full font-bold text-center min-w-20">การพิจารณา</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-center">{{ data.deliveryDetail[0].considerationResult }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top">
          <template #header>
            <p class="w-full font-bold text-center min-w-32">จำนวนเงินตรวจรับ</p>
          </template>
          <template #body="{ data }">
            <div>
              <p class="text-end">{{ formatCurrency(data.amount) }}</p>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top">
          <template #header>
            <p class="w-full font-bold text-center min-w-60">รายละเอียดการส่งมอบและตรวจรับ</p>
          </template>
          <template #body="{ data }">
            <div v-for="(d, i) in data.deliveryDetail" :key="i">
              <div>
                <div v-for="(v, i) in d.items" :key="i">
                  <p>{{ v.description }} จำนวน {{ v.quantity }} ราคา {{ formatCurrency(v.price) }} บาท</p>
                </div>
              </div>
            </div>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top;padding-top: 8px" v-if="store.status.canEdit && menuStore.hasManage">
          <template #body="{ index }">
            <Button icon="pi pi-trash" severity="danger" variant="text" @click="() => onDelete(index)" />
          </template>
        </Column>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
        <template #footer>
          <div class="flex justify-end gap-4 font-bold">
            <span>รวมทั้งหมด</span>
            <span>{{ formatCurrency(summary) }} บาท</span>
          </div>
        </template>
      </DataTable>
      <p class="text-end">วงเงินคงเหลือ {{ formatCurrency(summary) }}</p>
      <div class="bg-gray-100 pt-10 px-5 mt-5">
        <div class="grid grid-cols-3 gap-2">
          <InputNumber v-model="summary" label="จำนวนเงินเบิกจ่าย" class="col-start-3 w-full" input-class="text-end"
            grouping :min-fraction-digits="2" :disabled="!store.status.canEdit || !menuStore.hasManage" />
        </div>
      </div>
    </template>
  </Card>
  <BudgetSection v-model="store.body.budgetDetails" :disabled="!store.status.canEdit || !menuStore.hasManage" />
  <DisbursementDialog v-model:show="showModal" @on-select="onSelectFromDialog" />
</template>

<style scoped lang="scss">
:deep(tbody),
:deep(td) {
  border-width: 0 0 0 0;
}
</style>

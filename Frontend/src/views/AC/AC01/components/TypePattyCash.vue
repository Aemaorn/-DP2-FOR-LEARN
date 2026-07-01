<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField } from '@/components/forms';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import type { SourceDataPettyCashReimbursement } from '@/models/ACC/acc001';
import { useAc01DetailStore } from '@/stores/AC/ac01';
import { Card, DataTable } from 'primevue';
import { computed } from 'vue';
import { Disbursement } from './Part';

const store = useAc01DetailStore();

const sourceData = computed(() => store.body.source.data as SourceDataPettyCashReimbursement);

const totalAmount = computed(() => {
  if (!sourceData.value.items || sourceData.value.items.length === 0) return 0
  return sourceData.value.items.reduce((sum, item) => sum + (item.amount ?? 0), 0)
})

</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="รายละเอียดการเบิกจ่าย" />
      <DataTable :value="sourceData.items">
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">ลำดับ</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">{{ data.sequence }}</p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">วันที่</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">{{ ToDateOnly(data.pettyCashDate) }}</p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">เลขที่อ้างอิง</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">{{ data.pettyCashNumber }}</p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">รายการ</p>
          </template>
          <template #body="{ data }">
            <p class="text-start">{{ data.subject }}</p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">ศูนย์ต้นทุน</p>
          </template>
          <template #body="{ data }">
            <p class="text-start">{{ data.departmentName }}</p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">รหัสบัญชี</p>
          </template>
          <template #body="{ data }">
            <p class="text-start">{{ data.glAccountLabel }}</p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">จำนวนเงิน</p>
          </template>
          <template #body="{ data }">
            <p class="text-end">{{ formatCurrency(data.amount) }}</p>
          </template>
        </Column>
      </DataTable>
      <div class="mt-6 flex items-center justify-end gap-4 text-primary font-bold">
        <p>จำนวนเงินรวมทั้งสิ้น</p>
        <p>{{ Intl.NumberFormat('th', { minimumFractionDigits: 2 }).format(totalAmount) }}</p>
      </div>
    </template>
  </Card>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="การรับเงิน" />
      <div class="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-4">
        <InputField label="ชื่อบัญชี" v-model="sourceData.bankAccountName" disabled />
        <InputField label="หมายเลขบัญชี" v-model="sourceData.bankAccountNumber" disabled />
      </div>
    </template>
  </Card>
  <Disbursement v-if="store.state.isEditDisbursement"/>
</template>
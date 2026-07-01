<script setup lang="ts">
import { InputNumber, Select } from '@/components/forms';
import { getMonthOptions } from '@/helpers/dateTime';
import { useMenuStore } from '@/stores/menu';
import { usePcm005PrinApproveRentStore } from '@/stores/PCM/PCM005/principleApprovalRental';

const menuStore = useMenuStore();
const store = usePcm005PrinApproveRentStore();
const monthDDL = getMonthOptions();
</script>

<template>
  <div class="grid grid-cols-4 gap-4 mt-10" v-if="store.body.perfSupportData">
    <InputNumber label="ปริมาณทำธุรกรรม" v-model="store.body.perfSupportData.transactionVolume" grouping
      :fractionDigits="2" :disabled="!store.status.canEdit || !menuStore.hasManage" />
    <InputNumber label="ระยะเวลา/ปี" v-model="store.body.perfSupportData.periodYear" grouping
      :disabled="!store.status.canEdit || !menuStore.hasManage" />
    <Select :options="monthDDL" label="ตั้งแต่เดือน" v-model="store.body.perfSupportData.startMonth"
      :disabled="!store.status.canEdit || !menuStore.hasManage" />
    <Select :options="monthDDL" label="ถึงเดือน" v-model="store.body.perfSupportData.endMonth"
      :disabled="!store.status.canEdit || !menuStore.hasManage" />
  </div>

  <DataTable :value="store.body.perfSupportDataDetails" tableStyle="min-width: 50rem">
    <ColumnGroup type="header">
      <Row>
        <Column :rowspan="12">
          <template #header>
            <p class="text-center w-full font-bold">รายการ</p>
          </template>
        </Column>
        <Column :colspan="2">
          <template #header>
            <p class="text-center w-full font-bold">ม.ค. - ธ.ค. 2565</p>
          </template>
        </Column>
        <Column :colspan="3">
          <template #header>
            <p class="text-center w-full font-bold">ม.ค. - ธ.ค. 2566</p>
          </template>
        </Column>
      </Row>
      <Row>
        <Column>
          <template #header>
            <p class="text-center w-full font-bold">จำนวนบัญชี</p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="text-center w-full font-bold">จำนวนเงิน (ลบ.)</p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="text-center w-full font-bold">จำนวนบัญชี</p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="text-center w-full font-bold">จำนวนเงิน (ลบ.)</p>
          </template>
        </Column>
      </Row>
    </ColumnGroup>
    <Column field="title">
      <template #body="{ data }">
        <p>{{ data.activityDescription }}</p>
      </template>
    </Column>
    <Column field="account1">
      <template #body="{ data }">
        <InputNumber v-model="data.accountCountYear1" hideDetails grouping :min-fraction-digits="2" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
      </template>
    </Column>
    <Column field="price1">
      <template #body="{ data }">
        <InputNumber v-model="data.amountYear1" hideDetails grouping :min-fraction-digits="2" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
      </template>
    </Column>
    <Column field="account2">
      <template #body="{ data }">
        <InputNumber v-model="data.accountCountYear2" hideDetails grouping :min-fraction-digits="2" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
      </template>
    </Column>
    <Column field="price2">
      <template #body="{ data }">
        <InputNumber v-model="data.amountYear2" hideDetails grouping :min-fraction-digits="2" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
      </template>
    </Column>
  </DataTable>
</template>

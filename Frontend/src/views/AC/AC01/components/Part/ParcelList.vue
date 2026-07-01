<script setup lang="ts">
import { Radio } from '@/components/forms';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { DataTable, Card } from 'primevue';
import { ToDateOnly } from '@/helpers/dateTime';
import { computed, ref } from 'vue';
import { useAc01DetailStore } from '@/stores/AC/ac01';
import type { SourceDataClause79_2, SourceDataW119 } from '@/models/ACC/acc001';
import { formatCurrency } from '@/helpers/currency';
import type { Option } from '@/models/shared/option';

const store = useAc01DetailStore();
const sorceData = ref(store.body.source.data as SourceDataClause79_2 | SourceDataW119)

const options = ref<Array<Option>>([
  { label: " นิติบุคคล", value: '0' },
  { label: "บุคคลธรรมดา", value: '1' },
]);

const totalBeforeVat = computed(() => {
  return sorceData.value.vendors.reduce((vendorSum, vendor) => {
    return vendorSum + vendor.parcels.reduce((parcelSum, parcel) => {
      return parcelSum + (parcel.totalPrice * parcel.quantity)
    }, 0)
  }, 0)
});

const totalVat = computed(() => totalBeforeVat.value * 0.07);

const totalAmount = computed(() => totalBeforeVat.value + totalVat.value);
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="รายการของพัสดุ" />
      <div v-for="(items, index) in sorceData.vendors" :key="index" class="px-4">
        <Radio disabled :options="options" v-model="items.vendorType" />
        <div class="grid grid-cols-1 lg:grid-cols-4 gap-4">
          <InfoItem title="ผู้ค้า" :content="items.vendorName" />
          <InfoItem title="เลขประจำตัวผู้เสียภาษี ผู้ค้า" :content="items.taxNumber" />
          <InfoItem title="เลขที่สาขา" :content="items.vendorBranchNumber" />
          <InfoItem title="ประเภทภาษี" :content="items.billTypeLabel" />
          <InfoItem title="ประเภทเอกสาร" :content="items.vatIncludeTypeLabel" />
          <InfoItem title="เลขที่ประเภทเอกสาร" :content="items.billBookNo" />
          <InfoItem title="วันที่เอกสาร" :content="ToDateOnly(items.billDate)" />
        </div>
        <div class="mt-6 px-4">
          <p class="font-bold">ข้อมูลรหัสบัญชีและการใช้งานงบประมาณของฝ่าย</p>
          <DataTable :value="items.parcels" v-if="items.parcels.length > 0">
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">ลำดับ</p>
              </template>
              <template #body="{ data: row }">
                <p class="text-center">{{ row.sequence }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">รายการ</p>
              </template>
              <template #body="{ data: row }">
                <p class="text-start">{{ row.item }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">จำนวน/หน่วย</p>
              </template>
              <template #body="{ data: row }">
                <p class="text-center">{{ row.quantity }} {{ row.unitLabel }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">ราคา/หน่วย (รวม VAT)</p>
              </template>
              <template #body="{ data: row }">
                <p class="text-end">{{ formatCurrency(row.totalPrice) }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">ราคารวม (รวม VAT)</p>
              </template>
              <template #body="{ data: row }">
                <p class="text-end">{{ formatCurrency((row.totalPrice * row.quantity) + ((row.totalPrice * row.quantity) * 0.07)) }}</p>
              </template>
            </Column>
            <template #empty>
              <p class="text-center font-bold">ไม่พบข้อมูล</p>
            </template>
          </DataTable>
        </div>
      </div>
      <div class="flex justify-end items-center mt-6 mr-10">
        <div class="grid grid-cols-2 gap-4">
          <p class="text-end">ราคารวมก่อน VAT</p>
          <p class="text-end">{{ formatCurrency(totalBeforeVat) }}</p>
          <p class="text-end">ราคา VAT</p>
          <p class="text-end">{{ formatCurrency(totalVat) }}</p>
          <p class="text-end text-red-500">จำนวนเงินรวมทั้งสิ้น</p>
          <p class="text-end text-red-500">
            {{ formatCurrency(totalAmount) }}
          </p>
        </div>
      </div>
    </template>
  </Card>
</template>
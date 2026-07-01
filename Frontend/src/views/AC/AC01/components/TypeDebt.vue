<script setup lang="ts">
import { DataTable } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Disbursement } from './Part';
import { useAc01DetailStore } from '@/stores/AC/ac01';
import { computed } from 'vue';
import type { SourceDataDisbursement } from '@/models/ACC/acc001';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import cm004Constant from '@/constants/CM/cm004';
import type { CmDisbursementApprovalStatus } from '@/enums/CM/cm004';
import { useRouter } from 'vue-router';
import { InputNumber } from '@/components/forms';

const store = useAc01DetailStore();

const router = useRouter();

const { cm004StatusColor, cm004StatusName } = cm004Constant;

const sourceData = computed<SourceDataDisbursement>((): SourceDataDisbursement => store.body.source.data as SourceDataDisbursement);

const summary = computed<number>((): number => {
  if (!sourceData.value.installments || sourceData.value.installments.length === 0) return 0;

  return sourceData.value.installments.reduce(
    (sum: number, s: { amount?: number | null }): number => sum + (s.amount || 0),
    0
  );
});

const summaryFineAmount = computed<number>((): number => {
  if (!sourceData.value.installments || sourceData.value.installments.length === 0) return 0;

  return sourceData.value.installments.reduce(
    (sum: number, s: { fineAmount?: number | null }): number => sum + (s.fineAmount || 0),
    0
  );
});

const routeToCM004 = (id: string, detailId: string): void => {
  router.push({ name: 'cm004DisbursementDetail', params: { id: id, disbursementId: detailId } });
}
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลสัญญา" />
      <div class="px-4 mt-2 grid md:grid-cols-2 lg:grid-cols-3 gap-4">
        <InfoItem title="คู้ค่า"
          :content="`${sourceData.contractDraftVendor.contractDraftNumber} : ${sourceData.contractDraftVendor.contractName}`" />
        <InfoItem class="col-start-1" title="เลขที่สัญญา" :content="sourceData.contractDraftVendor.contractNumber" />
        <InfoItem title="เลขที่ PO (SAP)" :content="sourceData.contractDraftVendor.poNumber" />
        <InfoItem title="วงเงินตามสัญญา" :content="formatCurrency(sourceData.contractDraftVendor.budget)" />
        <InfoItem class="col-start-1" title="วันที่ลงนามในสัญญา"
          :content="ToDateOnly(sourceData.contractDraftVendor.contractSignedDate)" />
        <InfoItem title="กำหนดส่งมอบภายใน"
          :content="sourceData.contractDraftVendor.deliveryLeadTime ? `${sourceData.contractDraftVendor.deliveryLeadTime} วัน` : '-'" />
        <InfoItem title="ครบกำหนดส่งมอบงานวันที่" :content="ToDateOnly(sourceData.contractDraftVendor.deliveryDate)" />
        <InfoItem title="ระยะเวลาประกัน"
          :content="sourceData.contractDraftVendor.deliveryLeadTime && sourceData.contractDraftVendor.deliveryLeadTimeTypeLabel ? `${sourceData.contractDraftVendor.deliveryLeadTime} วัน ${sourceData.contractDraftVendor.deliveryLeadTimeTypeLabel}` : '-'" />
      </div>
    </template>
  </Card>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลการเบิกจ่าย" />
      <DataTable :value="sourceData.installments">
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">งวดที่</p>
          </template>
          <template #body="{ index }">
            <p class="text-center">
              {{ index + 1 }}
            </p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">ระยะเวลา (วัน)</p>
          </template>
          <template #body>
            <p class="text-center">
              {{ sourceData.contractDraftVendor.deliveryLeadTime ?? '-' }}
            </p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">ครบกำหนดส่งมอบ งานในวันที่</p>
          </template>
          <template #body>
            <p class="text-center">
              {{ ToDateOnly(sourceData.contractDraftVendor.deliveryDate) }}
            </p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">เปอร์เซ็นต์ %</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">
              {{ data.amount / sourceData.contractDraftVendor.budget * 100 }} %
            </p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">จำนวนเงิน</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">
              {{ formatCurrency(data.amount) }}
            </p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">วันที่ตรวจรับ</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">
              {{ ToDateOnly(data.receiveDate) }}
            </p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">รายละเอียดการตรวจรับ</p>
          </template>
          <template #body="{ data }">
            <div class="text-start">
              <div v-for="(value, index) in data.deliveryDetail" :key="index">
                วันที่ส่งมอบ {{ ToDateOnly(value.deliveryDate) }}
                <div v-for="(item, itemIndex) in value.items" :key="itemIndex">
                  {{ `${item.description} จำนวน ${item.quantity} ราคารวม ${formatCurrency(item.total)} บาท` }}
                </div>
              </div>
            </div>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold"></p>
          </template>
          <template #body>
            <div class="flex-col">
              <div class="flex gap-2 justify-end items-center">
                <Button icon="pi pi-sign-out" class="text-nowrap" severity="success" variant="outlined"
                  label="ดูรายละเอียด" @click="routeToCM004(sourceData.cm004Id, sourceData.id)" />
                <BadgeStatus class="text-nowrap"
                  :label="cm004StatusName(sourceData.status as CmDisbursementApprovalStatus)"
                  :color="cm004StatusColor(sourceData.status as CmDisbursementApprovalStatus)?.color"
                  v-if="sourceData.status" />
              </div>
            </div>
          </template>
        </Column>
        <template #empty>
          <p class="text-center font-bold">ไม่พบข้อมูล</p>
        </template>
      </DataTable>
      <div>
        <p class="text-end mt-4">วงเงินคงเหลือ: {{ formatCurrency(summary) }} บาท</p>
      </div>
      <div class="mt-6 flex items-center justify-end gap-4 text-primary font-bold">
        <p class="text-red-500 text-3xl">ค่าปรับ: {{ formatCurrency(summaryFineAmount) }} บาท</p>
      </div>
      <div class="mt-6 flex items-center gap-4">
        <p class="font-bold">ข้อมูลการเบิกจ่าย</p>
        <div class="h-[2px] bg-gray-300 flex-1" />
      </div>
      <div class="grid grid-cols-3 gap-2 mt-8">
        <InputNumber v-model="summary" label="จำนวนเงินเบิกจ่าย" class="col-start-3 w-full" input-class="text-end"
          grouping :min-fraction-digits="2" disabled />
      </div>
    </template>
  </Card>
  <Disbursement v-if="store.state.isEditDisbursement" />
</template>
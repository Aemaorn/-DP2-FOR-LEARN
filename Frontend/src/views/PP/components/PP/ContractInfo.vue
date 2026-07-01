<script setup lang="ts">
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { ToDateOnly } from '@/helpers/dateTime';
import type { PPContractInfo } from '../../../../models/PP/ppModel';

const value = defineModel<PPContractInfo>({
  required: true,
});
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลสัญญา" />
      <div class="px-4 mt-2 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <InfoItem title="คู่ค้า" :content="value.establishmentName" />
        <InfoItem title="Email" :content="value.email" />
        <InfoItem class="col-start-auto lg:col-start-1" title="เลขที่สัญญา" :content="value.contractName" />
        <InfoItem title="เลขที่ PO (SAP)" :content="value.pONumber" />
        <InfoItem title="วงเงินตามสัญญา"
          :content="Intl.NumberFormat('th', { minimumFractionDigits: 2 }).format(value.contractAmount)" />
        <InfoItem title="ชื่อสัญญา" :content="value.contractName" />
        <InfoItem title="ประเภทสัญญา" :content="value.contractType" />
        <InfoItem title="รูปแบบสัญญา" :content="value.contractTemplate" />
        <InfoItem title="วันที่ลงนามในสัญญา" :content="ToDateOnly(value.contractDate)" />
        <InfoItem title="กำหนดส่งมอบภายใน" :content="`${value.deliveryDueDays} วัน`" />
        <InfoItem title="ครบกำหนดส่งมอบงาน วันที่" :content="ToDateOnly(value.deliveryDueDate)" />
        <InfoItem title="ระยะเวลารับประกัน" :content="value.warrantyDate" />
      </div>
    </template>
  </Card>
</template>
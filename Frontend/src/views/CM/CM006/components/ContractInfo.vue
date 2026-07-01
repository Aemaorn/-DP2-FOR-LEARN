<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import type { Cm006Detail } from '@/models/CM/cm006';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
type Props = {
  data: Cm006Detail;
};

const { data } = defineProps<Props>();
</script>

<template>
  <Card class="my-6">
    <template #content>
      <TitleHeader label="ข้อมูลสัญญา" hidden-icon />
      <div class="px-4 mt-2">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-center">
          <InfoItem title="คู่ค้า" :content="`${data.taxId}: ${data.entrepreneurName}`" />
          <InfoItem title="Email" :content="data.entrepreneurEmail" />

          <InfoItem title="เลขที่สัญญา" :content="data.entrepreneurEmail" class="lg:col-start-1" />
          <InfoItem title="เลขที่ PO (SAP)" :content="data.poNumber" />
          <InfoItem title="วงเงินตามสัญญา" :content="formatCurrency(data.budget)" />

          <InfoItem title="ชื่อสัญญา" :content="data.contractName" class="lg:col-start-1" />
          <InfoItem title="ประเภทสัญญา" :content="data.contractType" />
          <InfoItem title="รูปแบบสัญญา" :content="data.contractTemplate" />

          <InfoItem title="วันที่ลงนามในสัญญา" :content="ToDateOnly(data.contractSignedDate)" class="lg:col-start-1" />
          <InfoItem title="กำหนดส่งมอบภายใน" :content="`${data.deliveryLeadTime} วัน`" />
          <InfoItem title="ครบกำหนดส่งมอบงาน วันที่" :content="ToDateOnly(data.deliveryDate)" />

          <InfoItem title="ระยะเวลารับประกัน" :content="data.deliveryLeadTimeTypeLabel" class="lg:col-start-1" />
        </div>
      </div>
    </template>
  </Card>
</template>
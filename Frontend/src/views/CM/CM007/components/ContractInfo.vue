<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import type { Cm007Detail } from '@/models/CM/cm007';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';

type Props = {
  data: Cm007Detail;
};

const { data } = defineProps<Props>();
</script>

<template>
  <Card class="my-4">
    <template #content>
      <TitleHeader label="ข้อมูลสัญญา" hidden-icon />
      <div class="px-4 mt-2">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-center">
          <InfoItem title="เลขที่สัญญา" :content="data.contractNumber" />
          <InfoItem title="เลขที่ PO (SAP)" :content="data.poNumber" />
          <InfoItem title="ชื่อสัญญา" :content="data.contractName" />

          <InfoItem title="วงเงินตามสัญญา" :content="formatCurrency(data.budget)" class="lg:col-start-1" />
          <InfoItem title="วันที่ลงนามสัญญา" :content="ToDateOnly(data.contractSignedDate)" />
          <InfoItem title="วันที่สิ้นสุดสัญญา" :content="ToDateOnly(data.contractEndDate)" />

          <InfoItem title="ประเภทสัญญา" :content="data.contractTypeLabel ?? '-'" class="lg:col-start-1" />
          <InfoItem title="รูปแบบสัญญา" :content="data.templateLabel ?? data.templateCode ?? '-'" />
        </div>
      </div>
    </template>
  </Card>
</template>

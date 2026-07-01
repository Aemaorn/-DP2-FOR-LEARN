<script setup lang="ts">
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { ToDateOnly } from '@/helpers/dateTime';
import type { SourceDataContractGuaranteeReturn } from '@/models/ACC/acc001';
import { useAc01DetailStore } from '@/stores/AC/ac01';
import { computed } from 'vue';

const store = useAc01DetailStore();

const sourceData = computed(() => store.body.source.data as SourceDataContractGuaranteeReturn);
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลสัญญา" />
      <div class="px-4 mt-2 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <InfoItem title="คู่ค้า"
          :content="`${sourceData.contractDraftVendor.contractDraftNumber} : ${sourceData.contractDraftVendor.contractName}`" />
        <InfoItem title="Email" :content="sourceData.contractDraftVendor.email" />
        <InfoItem class="col-start-auto lg:col-start-1" title="เลขที่สัญญา"
          :content="sourceData.contractDraftVendor.contractNumber" />
        <InfoItem title="เลขที่ PO (SAP)" :content="sourceData.contractDraftVendor.poNumber" />
        <InfoItem title="วงเงินตามสัญญา"
          :content="Intl.NumberFormat('th', { minimumFractionDigits: 2 }).format(sourceData.contractDraftVendor.budget)" />
        <InfoItem title="ชื่อสัญญา" :content="sourceData.contractDraftVendor.contractName" />
        <InfoItem title="ประเภทสัญญา" :content="sourceData.contractDraftVendor.contractTypeLabel" />
        <InfoItem title="รูปแบบสัญญา" :content="sourceData.contractDraftVendor.templateLabel" />
        <InfoItem title="วันที่ลงนามในสัญญา" :content="ToDateOnly(sourceData.contractDraftVendor.contractSignedDate)" />
        <InfoItem title="กำหนดส่งมอบภายใน"
          :content="sourceData.contractDraftVendor.deliveryLeadTime ? `${sourceData.contractDraftVendor.deliveryLeadTime} วัน` : '-'" />
        <InfoItem title="ครบกำหนดส่งมอบงาน วันที่" :content="ToDateOnly(sourceData.contractDraftVendor.deliveryDate)" />
        <InfoItem title="ระยะเวลารับประกัน"
          :content="sourceData.contractDraftVendor.deliveryLeadTime && sourceData.contractDraftVendor.deliveryLeadTimeTypeLabel ? `${sourceData.contractDraftVendor.deliveryLeadTime} วัน ${sourceData.contractDraftVendor.deliveryLeadTimeTypeLabel}` : '-'" />
      </div>
    </template>
  </Card>
</template>
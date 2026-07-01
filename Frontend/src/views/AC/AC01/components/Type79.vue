<script setup lang="ts">
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { PaymentDetail, ParcelList, Disbursement } from './Part';
import { useAc01DetailStore } from '@/stores/AC/ac01';
import { computed } from 'vue';
import type { SourceDataClause79_2 } from '../../../../models/ACC/acc001';

const store = useAc01DetailStore();

const sourceData = computed(() => store.body.source.data as SourceDataClause79_2);
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="รายละเอียด 79 วรรค 2" />
      <div class="px-4 mt-2 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <InfoItem title="ปีงบประมาณ" :content="sourceData.budgetYear" />
        <InfoItem title="วิธีจัดหา" :content="sourceData.supplyMethod" />
        <InfoItem title="" :content="sourceData.supplyMethodType" />
        <InfoItem title="" :content="sourceData.supplyMethodSpecialType" />
        <InfoItem title="ความเป็นมา" :content="sourceData.source" class="col-span-1 md:col-span-4" />
        <InfoItem title="ข้อ 1 กรณีจำเป็นและเร่งด่วน" :content="sourceData.reasonItem1"
          class="col-span-1 md:col-span-4" />
        <InfoItem title="ข้อ 2 ไม่อาจดำเนินการตามปกติได้ทัน" :content="sourceData.reasonItem2"
          class="col-span-1 md:col-span-4" />
        <InfoItem title="เหตุผลและความจำเป็น" :content="sourceData.reasonItem3" class="col-span-1 md:col-span-4" />
      </div>
    </template>
  </Card>
  <PaymentDetail />
  <ParcelList />
  <Disbursement v-if="store.state.isEditDisbursement"/>
</template>
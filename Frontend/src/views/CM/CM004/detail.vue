<script setup lang="ts">
import type { MenuItem } from 'primevue/menuitem';
import { Card } from 'primevue';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { onMounted, } from 'vue';
import { useRoute } from 'vue-router';
import { useCm004DetailStore } from '@/stores/CM/cm004';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import DetailList from './components/DetailList.vue';
import router from '@/router';
import ToastHelper from '@/helpers/toast';

const route = useRoute();
const store = useCm004DetailStore();
const id = route.params?.id as string;
const routeItems: MenuItem[] = [
  { label: 'รายการขออนุมัติเบิกจ่าย', url: '/cm/cm004' },
  { label: 'รายละเอียดการขออนุมัติเบิกจ่าย' },
];

onMounted(() => {
  initAsync();
});

const initAsync = async (): Promise<void> => {
  if (id) {
    await store.getDetailAsync(id);
  }
};

const routeToDisbursement = (disbursementId?: string): void => {
  if (!disbursementId && store.detail.isCompleted) {
    return ToastHelper.error('ขออนุมัติเบิกจ่าย', 'ไม่สามารถขออนุมัติเบิกจ่ายได้เนื่องจากงวดครบแล้ว');
  }

  const baseRoute = '/cm/cm004/detail/' + id + '/disbursement';
  const finalRoute = disbursementId ? baseRoute + '/' + disbursementId : baseRoute;

  router.push(finalRoute);
};
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="ข้อมูลสัญญา" />
      <div class="grid grid-cols-12 gap-y-4 px-8">
        <InfoItem title="คู่ค้า" :content="store.detail.entrepreneurName" class="col-span-4" />
        <InfoItem title="Email" :content="store.detail.entrepreneurEmail" class="col-span-8" />
        <InfoItem title="เลขที่สัญญา" :content="store.detail.contractNumber" class="col-span-4" />
        <InfoItem title="เลขที่ PO (SAP)" :content="store.detail.poNumber" class="col-span-4" />
        <InfoItem title="วงเงินตามสัญญา" :content="formatCurrency(store.detail.budget)" class="col-span-4" />
        <InfoItem title="ชื่อสัญญา" :content="store.detail.contractName" class="col-span-4" />
        <InfoItem title="ประเภทสัญญา" :content="store.detail.contractType" class="col-span-4" />
        <InfoItem title="รูปแบบสัญญา" :content="store.detail.contractTemplate" class="col-span-4" />
        <InfoItem title="วันที่ลงนามในสัญญา" :content="ToDateOnly(store.detail.contractSignedDate)"
          class="col-span-4" />
        <InfoItem title="กำหนดส่งมอบภายใน"
          :content="store.detail.deliveryLeadTime ? `${store.detail.deliveryLeadTime} วัน` : '-'" class="col-span-4" />
        <InfoItem title="ครบกำหนดส่งมอบงาน วันที่" :content="ToDateOnly(store.detail.deliveryDate)"
          class="col-span-4" />
        <InfoItem title="ระยะเวลารับประกัน"
          :content="store.detail.deliveryLeadTime && store.detail.deliveryLeadTimeTypeLabel ? `${store.detail.deliveryLeadTime} วัน ${store.detail.deliveryLeadTimeTypeLabel}` : '-'"
          class="col-span-4" />
      </div>
    </template>
  </Card>
  <TitleHeader label="รายการขออนุมัติเบิกจ่าย" :route-items="routeItems" />
  <Card class="mb-20">
    <template #content>
      <div>
        <div class="flex gap-5 items-center">
          <p>รายละเอียดการขออนุมัติเบิกจ่าย</p>
          <Divider class="flex-1" />
          <Button label="สร้างขออนุมัติเบิกจ่าย" icon="pi pi-plus" severity="primary" variant="outlined"
            @click="() => routeToDisbursement()" />
        </div>
        <DetailList :data-list="store.detail.disbursementApprovals" @route="(e) => routeToDisbursement(e)" />
      </div>
    </template>
  </Card>
</template>

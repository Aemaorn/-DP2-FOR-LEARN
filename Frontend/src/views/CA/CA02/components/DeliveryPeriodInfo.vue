<script setup lang="ts">
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import type { CA02Delivery, CA02DeliveryAcceptancePeriodInfo } from '@/models/CA/ca02';
import { TitleHeader } from '@/components/cosmetic';
import { DataTable, Card } from 'primevue';
import { useRouter } from 'vue-router';
import { showConfirmDialogAsync } from '@/helpers/dialog';

type Props = {
  data: Array<CA02DeliveryAcceptancePeriodInfo>;
  disabled?: boolean;
};

const { data, disabled } = defineProps<Props>();
const router = useRouter();

const onRouteToDeliverPeriod = async (id: string) => {
  if (!disabled && !await showConfirmDialogAsync(undefined, "กรุณาบันทึกข้อมูลก่อนการ \"ยืนยัน\" หากไม่ทำการบันทึกข้อมูลจะทำให้ข้อมูลสูญหาย")) return;

  router.push({ name: 'cm001Detail', params: { id: id } });
}
</script>

<template>

  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลการตรวจรับพัสดุ" />
      <DataTable :value="data">
        <Column bodyStyle="vertical-align: top">
          <template #header>
            <p class="w-full font-bold text-center">ลำดับ</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">{{ data.sequence }}</p>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top">
          <template #header>
            <p class="w-full font-bold text-center">ส่งมอบงานภายในวันที่</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">{{ ToDateOnly(data.deliveryDate) }}</p>
          </template>
        </Column>
        <Column field="unit" bodyStyle="vertical-align: top">
          <template #header>
            <p class="w-full font-bold text-center">ระยะเวลา (วัน)</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">{{ data.leadTime }}</p>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top">
          <template #header>
            <p class="w-full font-bold text-center">เปอร์เซ็นต์ %</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">{{ `${data.installmentPercentage} %` }}</p>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top">
          <template #header>
            <p class="w-full font-bold text-center">จำนวนเงิน</p>
          </template>
          <template #body="{ data }">
            <p class="text-end">{{ formatCurrency(data.amount) }}</p>
          </template>
        </Column>
        <Column bodyStyle="vertical-align: top">
          <template #header>
            <p class="w-full font-bold text-center">ข้อมูลรายละเอียดการส่งมอบงาน</p>
          </template>
          <template #body="{ data }">
            <div v-for="(item, index) in (data.deliveries as Array<CA02Delivery>)" :key="index">
              <div class="flex flex-col">
                <p>{{ `วันที่ผู้มีอำนาจเห็นชอบ: ${ToDateOnly(data.deliveryAcceptanceDate)}` }}</p>
                <p class="ml-2">{{ `${item.sequence}. วันที่ส่งมอบ ${ToDateOnly(item.deliveryDate)}` }}</p>
                <div v-for="(detailItem, detailIndex) in item.deliveryItems" :key="detailIndex">
                  <p>{{ `${detailItem.description} จำนวน ${detailItem.quantity} ราคา ${formatCurrency(detailItem.price)}
                    บาท` }}</p>
                </div>
              </div>
            </div>
          </template>
        </Column>
        <Column body-class="text-end" bodyStyle="vertical-align: top">
          <template #body="{ data }">
            <Button icon="pi pi-sign-out" severity="success" variant="outlined" label="ดูรายละเอียด"
              @click="() => onRouteToDeliverPeriod(data.id)" />
          </template>
        </Column>
      </DataTable>
    </template>
  </Card>
</template>
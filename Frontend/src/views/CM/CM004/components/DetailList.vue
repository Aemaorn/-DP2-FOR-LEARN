<script setup lang="ts">
import { BadgeStatus } from '@/components';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import cm004Constant from '@/constants/CM/cm004';

const props = defineProps({
  dataList: { type: Array as any, required: true },
});

const emit = defineEmits(['route']);

const { cm004StatusColor, cm004StatusName } = cm004Constant;
</script>

<template>
  <DataTable :value="props.dataList" tableStyle="min-width: 100%" class="mt-5">
    <Column bodyStyle="vertical-align: top" class="w-10">
      <template #header>
        <p class="w-full font-bold text-center">วันที่ขออนุมัติ</p>
      </template>
      <template #body="{ data }">
        <div>
          <p class="text-center">{{ ToDateOnly(data.requestDate) }}</p>
        </div>
      </template>
    </Column>
    <Column bodyStyle="vertical-align: top" class="w-100">
      <template #header>
        <p class="w-full font-bold text-center">เรื่อง</p>
      </template>
      <template #body="{ data }">
        <div>
          <p>{{ data.subject }}</p>
        </div>
      </template>
    </Column>
    <Column bodyStyle="vertical-align: top" class="w-10">
      <template #header>
        <p class="w-full font-bold text-center">จำนวนเงิน</p>
      </template>
      <template #body="{ data }">
        <div>
          <p class="text-center">{{ formatCurrency(data.netClaimAmount) }}</p>
        </div>
      </template>
    </Column>
    <Column bodyStyle="vertical-align: top" class="w-10">
      <template #body="{ data }">
        <div class="flex gap-5 items-center justify-end">
          <BadgeStatus :label="cm004StatusName(data.status)" :color="cm004StatusColor(data.status).color" />
          <Button label="ไปยังบันทึกส่งมอบและตรวจรับ" icon="pi pi-sign-out" severity="success" variant="outlined"
            @click="emit('route', data.id)" class="w-60" size="small" />
        </div>
      </template>
    </Column>
    <template #empty>
      <p class="text-center">ไม่พบข้อมูล</p>
    </template>
  </DataTable>
</template>

<style scoped lang="scss">
:deep(tbody),
:deep(td) {
  border-width: 0 0 0 0;
}
</style>

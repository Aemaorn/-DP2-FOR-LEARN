<script setup lang="ts">
import type { InstallmentRequest } from '@/models/CM/cm004';
import { Checkbox } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { formatCurrency } from '@/helpers/currency';
import { ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { HttpStatusCode } from 'axios';
import { ToDateOnly } from '@/helpers/dateTime';
import { useCm004DetailStore } from '@/stores/CM/cm004';
import cm004Service from '@/services/CM/cm004';
import ToastHelper from '@/helpers/toast';

const show = defineModel('show', {
  type: Boolean, default: false, required: true,
});

const emit = defineEmits<(event: 'onSelect', data: InstallmentRequest[]) => void>();

const store = useCm004DetailStore();
const route = useRoute();
const id = route.params?.id as string;
const listData = ref<InstallmentRequest[]>([]);

const initAsync = async () => {
  await onGetDataListAsync();
};

const onGetDataListAsync = async (): Promise<void> => {
  const { data, status } = await cm004Service.getPaymentTermAsync(id);

  if (status === HttpStatusCode.Ok) {
    const convert = data as InstallmentRequest[];

    listData.value = convert.map(c => ({
      ...c,
      cmDeliveryAcceptancePeriodId: c.id,
      id: undefined,
    }));

    filterDupData();
  }
};

const filterDupData = () => {
  if (store.body.installments && store.body.installments.length > 0) {
    const result = [
      ...store.body.installments.filter(a => !listData.value.some(b => b.cmDeliveryAcceptancePeriodId === a.cmDeliveryAcceptancePeriodId)),
      ...listData.value.filter(b => !store.body.installments?.some(a => a.cmDeliveryAcceptancePeriodId === b.cmDeliveryAcceptancePeriodId))
    ];

    listData.value = result;
  }
};

const onCloseModal = (): void => {
  show.value = false;
};

const onSelect = () => {
  const findDataSelect = listData.value.filter(v => v.isSelected);

  if (!findDataSelect || findDataSelect.length === 0) {
    ToastHelper.warning('ข้อมูลไม่ถูกต้อง', 'กรุณาเลือกรายการส่งมอบและตรวจรับ');

    return;
  };

  emit('onSelect', findDataSelect);

  onCloseModal();
};

watch(() => show.value, (newValue) => {
  if (newValue) {
    initAsync();
  }
});
</script>

<template>
  <Dialog v-model:visible="show" modal :style="{ width: '70vw' }" :draggable="false" :breakpoints="{ '575px': '90vw' }">
    <template #container>
      <Card>
        <template #content>
          <TitleHeader label="รายการส่งมอบและตรวจรับ" />
          <DataTable :value="listData" tableStyle="min-width: 100%" class="mt-5">
            <Column bodyStyle="vertical-align: center">
              <template #body="{ data }">
                <div>
                  <Checkbox v-model="data.isSelected" hide-details />
                </div>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: center">
              <template #header>
                <p class="w-full font-bold text-center min-w-20">งวดที่</p>
              </template>
              <template #body="{ data }">
                <div>
                  <p class="text-center">{{ data.installmentNo }}</p>
                </div>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: center">
              <template #header>
                <p class="w-full font-bold text-center min-w-20">วันที่ส่งมอบ</p>
              </template>
              <template #body="{ data }">
                <div>
                  <p class="text-center">{{ ToDateOnly(data.deliveryDetail[0].deliveryDate) }}</p>
                </div>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: center">
              <template #header>
                <p class="w-full font-bold text-center min-w-20">การพิจารณา</p>
              </template>
              <template #body="{ data }">
                <div>
                  <p class="text-center">{{ data.deliveryDetail[0].considerationResult }}</p>
                </div>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: center">
              <template #header>
                <p class="w-full font-bold text-center min-w-32">จำนวนเงินตรวจรับ</p>
              </template>
              <template #body="{ data }">
                <div>
                  <p class="text-end">{{ formatCurrency(data.amount) }}</p>
                </div>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: center">
              <template #header>
                <p class="w-full font-bold text-center min-w-60">รายละเอียดการส่งมอบและตรวจรับ</p>
              </template>
              <template #body="{ data }">
                <div v-for="(d, i) in data.deliveryDetail" :key="i">
                  <div>
                    <p>วันที่ส่งมอบ {{ ToDateOnly(d.deliveryDate) }}</p>
                    <div v-for="(v, i) in d.items" :key="i">
                      <p>{{ v.description }} จำนวน {{ v.quantity }} ราคา {{ formatCurrency(v.price) }} บาท</p>
                    </div>
                  </div>
                </div>
              </template>
            </Column>
            <template #empty>
              <p class="text-center">ไม่พบข้อมูล</p>
            </template>
          </DataTable>
          <div class="flex w-full justify-end items-center gap-2 mt-5">
            <Button label="ยกเลิก" variant="outlined" severity="contrast" @click="onCloseModal" />
            <Button label="เลือก" variant="da" severity="outlined" @click="onSelect" />
          </div>
        </template>
      </Card>
    </template>
  </Dialog>
</template>

<style scoped lang="scss">
:deep(tbody),
:deep(td) {
  border-width: 0 0 0 0;
}
</style>

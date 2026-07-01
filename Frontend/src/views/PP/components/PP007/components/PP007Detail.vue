<script setup lang="ts">
import { Button, Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { StatusChip } from '@/components';
import { usePurchaseOrder } from '@/views/PP/stores/PP007/PP007Store';
import PriceDetailDialog from '@/views/PP/components/PP007/components/PriceDetailDialog.vue'
import { ref, computed } from 'vue';
import CheckDialog from './sub/CheckDialog.vue'
import CheckedUICard from './sub/CheckedUICard.vue'
import EntrepreneurDialog from './EntrepreneurDialog.vue'
import draggable from 'vuedraggable';
import { ArrayHelper } from '@/helpers/array';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import InputField from '@/components/forms/InputField.vue';
import { Datepicker } from '@/components/forms';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';

const props = defineProps({
  readonly: { type: Boolean, default: false },
});

const { reSequence } = ArrayHelper();
const store = usePurchaseOrder();

const showModal = ref(false);
const showDialog = ref<boolean>(false);
const entrepreneurIndex = ref<number>(0);
const showCheckModal = ref(false);
const entrepreneurSelected = ref<number>();
const isPriceDetail = ref<boolean>(true);
const titleCheckModal = ref('');

const onShowVendorModal = async (): Promise<void> => {
  entrepreneurSelected.value = undefined;
  showModal.value = true;
};

const selectedVendor = computed(() => {
  return store.body.entrepreneurs[entrepreneurSelected.value!] || {};
});

const openPriceDetail = (index: number): void => {

  if (store.body.entrepreneurs[index].priceDetails.length === 0) {
    store.body.entrepreneurs[index].priceDetails = store.body.priceDetails?.map(x => ({ ...x })) ?? [];
  }

  showDialog.value = true;
  entrepreneurIndex.value = index;
  isPriceDetail.value = true;
};

const openConsideration = (index: number): void => {
  showDialog.value = true;
  entrepreneurIndex.value = index;
  isPriceDetail.value = false;
};

const onShowModalCheck = async (title: string, index: number): Promise<void> => {
  showCheckModal.value = true;
  titleCheckModal.value = title;
  entrepreneurSelected.value = index;
};

const updateVendor = (updatedVendor: any) => {
  store.body.entrepreneurs[entrepreneurSelected.value!] = updatedVendor;
};

const onShowModal = async (index?: number): Promise<void> => {
  if (index) {
    entrepreneurSelected.value = index;
  }

  showModal.value = true;
};


const onRemove = async (index: number): Promise<void> => {
  const confirmed = await showConfirmDialogAsync(ConfirmDialogType.Delete);
  if (!confirmed) return;

  store.body.entrepreneurs.splice(index, 1);

  if (store.body.jp006Id) {
    await store.onUpdateJp006Async();
  } else {
    await store.onCreateJp006Async();
  }
};

const onRequence = (): void => {
  store.body.entrepreneurs = reSequence(store.body.entrepreneurs);
};

</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="ข้อมูลขออนุมัติสั่งซื้อ/สั่งจ้าง (จพ.006)" />
      <div class="grid lg:grid-cols-2 mt-10 gap-4">
        <InputField label="เลขที่ขออนุมัติสั่งซื้อ/สั่งจ้าง " v-model="store.body.purchaseOrderNumber" :disabled="true || props.readonly" />
        <Datepicker label="วันที่เอกสาร" v-model="store.body.documentDate" :disabled="!store.canEdit || props.readonly" />
      </div>
    </template>
  </Card>
  <Card class="my-5">
    <template #content>
      <TitleHeader label="ผู้ประกอบการเสนอราคา">
        <template #action>
          <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="() => onShowVendorModal()" v-if="store.canEdit && !props.readonly" />
        </template>
      </TitleHeader>
      <draggable v-model="store.body.entrepreneurs" group="group" handle=".drag-data" itemKey="sequence"
        @end="onRequence">
        <template #item="{ element: item, index: i }">
          <div class="border-1 border-gray-300 rounded-lg px-3 py-2 mt-5" :key="i">
            <div class="flex items-center justify-between gap-2">
              <div class="flex items-center gap-4">
                <p class="font-bold">ข้อมูลผู้ประกอบการ</p>
                <span class="material-symbols-outlined text-orange-400 cursor-pointer" @click="() => onShowModal(i)" v-if="!props.readonly">
                  border_color
                </span>
              </div>
              <div class="flex h-full gap-4 items-start justify-end">
                <Button icon="pi pi-trash" severity="danger" variant="text" @click="() => onRemove(i)"
                  v-if="store.canEdit && !props.readonly" />
                <span class="material-symbols-outlined drag-data cursor-move mt-2"
                  v-if="store.canEdit && !props.readonly">drag_indicator</span>
              </div>
            </div>

            <div class="grid lg:grid-cols-5 mb-5 gap-4">
              <div class="lg:col-span-5">
                <InfoRow label="เลขประจำตัวผู้เสียภาษี">
                  <p>{{ item.entrepreneurTaxId }}</p>
                </InfoRow>

                <InfoRow label="ประเภทผู้ประกอบการ">
                  <p>{{ item.entrepreneurType }}</p>
                </InfoRow>

                <InfoRow label="ชื่อสถานประกอบการ">
                  <p class="font-bold">{{ item.entrepreneurName }}</p>
                </InfoRow>

                <InfoRow label="รหัสสาขา">
                  <p>{{ item.sapBranchNumber || '-' }}</p>
                </InfoRow>

                <InfoRow label="อีเมล">
                  <p>{{ item.entrepreneurEmail }}</p>
                </InfoRow>

                <InfoRow label="ผลพิจารณา">
                  <div>
                    <StatusChip v-if="item.isWinner" label="ผู้ชนะ" icon="pi pi-check-circle" color="Success"
                      size="Small" />
                    <p v-else>-</p>
                  </div>
                </InfoRow>

                <InfoRow label="หมายเหตุ">
                  <p>{{ item.remark }}</p>
                </InfoRow>
              </div>
              <div class="lg:col-span-7 justify-center items-stretch flex gap-6">
                <CheckedUICard type="Coi"       :idx="i" :item="item.coi"       @check="onShowModalCheck" />
                <CheckedUICard type="Watchlist" :idx="i" :item="item.watchlist" @check="onShowModalCheck" />
                <CheckedUICard type="Egp"       :idx="i" :item="item.egp"       @check="onShowModalCheck" />

                <div class="flex flex-col items-center h-full">
                  <p class="font-bold">เสนอราคา</p>
                  <Button v-if="!item.priceDetails?.length || !item.isBidding" label="รอบันทึกรายละเอียดราคา" severity="warn" class="mt-auto"
                    @click="openPriceDetail(i)" />
                  <Button v-else label="บันทึกรายละเอียดราคาแล้ว" severity="success" class="mt-auto" @click="openPriceDetail(i)" />
                </div>

                <div class="flex flex-col items-center h-full">
                  <p class="font-bold">ผลพิจารณา</p>
                  <Button v-if="item.isWinner && item.isBidding" label="บันทึกผลพิจารณา" severity="success" class="mt-auto"
                    @click="openConsideration(i)" />
                  <Button v-if="!item.isWinner && item.isBidding" label="บันทึกผลพิจารณา" severity="warn" class="mt-auto"
                    @click="openConsideration(i)" />
                  <Button v-if="!item.isBidding" label="บันทึกผลพิจารณา" variant="outlined" severity="contrast" class="mt-auto"
                    disabled />
                </div>
              </div>
            </div>
          </div>
        </template>
      </draggable>
    </template>
  </Card>
  <EntrepreneurDialog v-model="showModal" :selected="entrepreneurSelected" :readonly="props.readonly" />
  <CheckDialog v-model="showCheckModal" :title="titleCheckModal" :vendor="selectedVendor"
    @update:vendor="updateVendor" :readonly="props.readonly" />
  <PriceDetailDialog v-model="showDialog" v-model:index="entrepreneurIndex" :is-price-detail="isPriceDetail" :readonly="props.readonly" />
</template>

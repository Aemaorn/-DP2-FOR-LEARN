<script setup lang="ts">
import type { Entrepreneurs } from '@/models/PCM/PCM005/principleApprovalRental';
import { TitleHeader } from '@/components/cosmetic';
import { Button } from 'primevue';
import { computed, defineAsyncComponent, ref, watch } from 'vue';
import { ArrayHelper } from '@/helpers/array';
import { PP006EntrepreneurType } from '@/views/PP/enums/pp006';
import { ToDateTime } from '@/helpers/dateTime';
import { usePcm005PrinApproveRentStore } from '@/stores/PCM/PCM005/principleApprovalRental';
import { isNonEmptyString } from '@/helpers/string';
import { useMenuStore } from '@/stores/menu';
import draggable from 'vuedraggable';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';

const EntrepreneurDialog = defineAsyncComponent(
  (): Promise<typeof import('./EntrepreneurDialog.vue')> => import('./EntrepreneurDialog.vue')
);
const CheckDialog = defineAsyncComponent(
  (): Promise<typeof import('./CheckDialog.vue')> => import('./CheckDialog.vue')
);
const PriceDetailDialog = defineAsyncComponent(
  (): Promise<typeof import('./PriceDetailDialog.vue')> => import('./PriceDetailDialog.vue')
);

const { reSequence } = ArrayHelper();
const menuStore = useMenuStore();
const store = usePcm005PrinApproveRentStore();

const showModal = ref(false);
const showCheckModal = ref(false);
const showPriceDetailModal = ref(false);
const entrepreneurSelected = ref<number | null>(null);
const titleCheckModal = ref('');

const onShowModalCheck = (title: string, index: number): void => {
  titleCheckModal.value = title;
  entrepreneurSelected.value = index;

  if (!selectedVendor.value) return;

  showCheckModal.value = true;
};

const selectedVendor = computed<Entrepreneurs | undefined>(() => {
  if (entrepreneurSelected.value === null) return undefined;
  return store.body.entrepreneurs?.[entrepreneurSelected.value];
});

const onShowModalPriceDetail = (index: number): void => {
  entrepreneurSelected.value = index;

  if (!selectedVendor.value) return;

  showPriceDetailModal.value = true;
};

const onShowModal = (index?: number): void => {
  entrepreneurSelected.value = index ?? null;
  showModal.value = true;
};

const onRequence = (): void => {
  if (store.body.entrepreneurs) {
    store.body.entrepreneurs = reSequence(store.body.entrepreneurs);
  }
};

const onRemove = async (index: number): Promise<void> => {
  const confirmed = await showConfirmDialogAsync(ConfirmDialogType.Delete);
  if (!confirmed) return;

  store.body.entrepreneurs.splice(index, 1);
  await store.updateAsync();
};

watch(
  [(): boolean => showModal.value, (): boolean => showCheckModal.value],
  ([newShowModal, newShowCheckModal], [oldShowModal, oldShowCheckModal]): void => {
    if (oldShowModal && !newShowModal || oldShowCheckModal && !newShowCheckModal) {
      entrepreneurSelected.value = 0;
    }
  }
);
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ผู้ประกอบการ">
        <template #action>
          <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="() => onShowModal()"
            v-if="store.status.canEdit && menuStore.hasManage" />
        </template>
      </TitleHeader>
      <draggable v-model="store.body.entrepreneurs" group="group" handle=".drag-data" itemKey="sequence"
        @end="onRequence">
        <template #item="{ element: data, index }: { element: Entrepreneurs, index: number }">
          <div class="border-1 border-gray-300 rounded-lg px-3 py-2 mt-5" :key="data.id">
            <div class="flex items-center justify-between gap-2">
              <div class="flex items-center gap-4">
                <p class="font-bold">ข้อมูลผู้ประกอบการ</p>
                <span class="material-symbols-outlined text-orange-400 cursor-pointer"
                  @click="() => onShowModal(index)">
                  border_color
                </span>
              </div>
              <div class="flex h-full gap-4 items-start justify-end" v-if="store.status.canEdit && menuStore.hasManage">
                <Button icon="pi pi-trash" severity="danger" variant="text" @click="() => onRemove(index)" />
                <span class="material-symbols-outlined drag-data cursor-move mt-2">drag_indicator</span>
              </div>
            </div>

            <div class="grid lg:grid-cols-12 mb-5">
              <div class="lg:col-span-5">
                <InfoRow label="เลขประจำตัวผู้เสียภาษี">
                  <p>{{ data.entrepreneurTaxId }}</p>
                </InfoRow>
                <InfoRow label="ประเภทผู้ประกอบการ">
                  <p>{{ data.entrepreneurTypeLabel }}</p>
                </InfoRow>
                <InfoRow label="ชื่อสถานประกอบการ">
                  <p class="font-bold">{{ data.entrepreneurName }}</p>
                </InfoRow>

                <InfoRow label="รหัสสาขา">
                  <p>{{ data.sapBranchNumber || '-' }}</p>
                </InfoRow>

                <InfoRow label="อีเมล">
                  <p>{{ isNonEmptyString(data.entrepreneurEmail) ? data.entrepreneurEmail : '-' }}</p>
                </InfoRow>
              </div>
              <div class="lg:col-span-7 justify-center items-center flex gap-10">
                <div class="text-center">
                  <p class="font-bold">COI</p>
                  <span class="material-symbols-outlined text-green-400" v-if="data.coiResult">
                    check_circle
                  </span>
                  <span class="material-symbols-outlined text-red-400" v-if="!data.coiResult && data.coiResultAt">
                    cancel
                  </span>
                  <span class="material-symbols-outlined text-[#F9A825]" v-if="!data.coiResultAt">
                    error
                  </span>
                  <p class="text-gray-400 text-[16px] mb-3">
                    <span v-if="data.coiResultAt">ตรวจวันที่ : {{ ToDateTime(data.coiResultAt) }}</span>
                  </p>
                  <Button label="ตรวจสอบ" severity="success"
                    @click="() => onShowModalCheck(PP006EntrepreneurType.COI, index)" />
                </div>
                <div class="text-center">
                  <p class="font-bold">Watchlist</p>
                  <span class="material-symbols-outlined text-green-400" v-if="data.watchlistResult">
                    check_circle
                  </span>
                  <span class="material-symbols-outlined text-red-400"
                    v-if="!data.watchlistResult && data.watchlistResultAt">
                    cancel
                  </span>
                  <span class="material-symbols-outlined text-[#F9A825]" v-if="!data.watchlistResultAt">
                    error
                  </span>
                  <p class="text-gray-400 text-[16px] mb-3">
                    <span v-if="data.watchlistResultAt">ตรวจวันที่ : {{ ToDateTime(data.watchlistResultAt) }}</span>
                  </p>
                  <Button label="ตรวจสอบ" severity="success"
                    @click="() => onShowModalCheck(PP006EntrepreneurType.Watchlist, index)" />
                </div>
                <div class="text-center">
                  <p class="font-bold">ผู้ทิ้งงาน (e-GP)</p>
                  <span class="material-symbols-outlined text-green-400" v-if="data.egpResult">
                    check_circle
                  </span>
                  <span class="material-symbols-outlined text-red-400" v-if="!data.egpResult && data.egpResultAt">
                    cancel
                  </span>
                  <span class="material-symbols-outlined text-[#F9A825]" v-if="!data.egpResultAt">
                    error
                  </span>
                  <p class="text-gray-400 text-[16px] mb-3">
                    <span v-if="data.egpResultAt">ตรวจวันที่ : {{ ToDateTime(data.egpResultAt) }}</span>
                  </p>
                  <Button label="ตรวจสอบ" severity="success"
                    @click="() => onShowModalCheck(PP006EntrepreneurType.EGP, index)" />
                </div>
                <div class="text-center">
                  <Button :label="`${(data.details?.length ?? 0) > 0 ? 'บันทึกรายละเอียดราคาแล้ว' : 'บันทึกรายละเอียดราคา'}`"
                    :severity="`${(data.details?.length ?? 0) > 0 ? 'success' : 'warn'}`"
                    @click="() => onShowModalPriceDetail(index)" />
                </div>
              </div>
            </div>
          </div>
        </template>
      </draggable>

      <div class="px-4 mt-4 mb-2" v-if="false">
        <div class="flex justify-end">
          <Button icon="pi pi-send" label="ส่งอีเมลเชิญชวนแบบกลุ่ม" severity="primary" variant="outlined" />
        </div>
      </div>
    </template>
  </Card>
  <EntrepreneurDialog v-model:show="showModal" :entrepreneur-selected="entrepreneurSelected" :vendor="selectedVendor" />
  <CheckDialog v-model="showCheckModal" :title="titleCheckModal" :vendor="selectedVendor!" />
  <PriceDetailDialog v-model:show="showPriceDetailModal" :vendor="selectedVendor!" />
</template>

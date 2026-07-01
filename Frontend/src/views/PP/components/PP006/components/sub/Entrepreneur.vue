<script setup lang="ts">
import type { InvitedEntrepreneurs } from '@/views/PP/models/PP006/pp006Model';
import { TitleHeader } from '@/components/cosmetic';
import { Button } from 'primevue';
import { ref, watch } from 'vue';
import { ArrayHelper } from '@/helpers/array';
import { usePP006DetailStore } from '@/views/PP/stores/PP006/PP006Store';
import { PP006Status } from '@/views/PP/enums/pp006';
import { isNonEmptyString } from '@/helpers/string';
import { useMenuStore } from '@/stores/menu';
import EntrepreneurDialog from './EntrepreneurDialog.vue';
import CheckDialog from './CheckDialog.vue';
import SendEmailInviteDialog from './SendEmailInviteDialog.vue';
import draggable from 'vuedraggable';
import InfoRow from '@/components/cosmetic/InfoRow.vue';

const { reSequence } = ArrayHelper();
const menuStore = useMenuStore();
const store = usePP006DetailStore();

const showModal = ref(false);
const showCheckModal = ref(false);
const showSendEmailDialog = ref(false);
const entrepreneurSelected = ref('');
const titleCheckModal = ref('');

// TODO: UnComment When Use
// const onShowModalCheck = async (title: string, id: string): Promise<void> => {
//   showCheckModal.value = true;
//   titleCheckModal.value = title;
//   entrepreneurSelected.value = id;
// };

const onShowModal = async (id?: string): Promise<void> => {
  if (id) {
    entrepreneurSelected.value = id;
  }

  showModal.value = true;
};

const onRequence = (): void => {
  store.detail.invitedEntrepreneurs = reSequence(store.detail.invitedEntrepreneurs);
};

const onRemove = (index: number): void => {
  store.detail.invitedEntrepreneurs.splice(index, 1);
};

const onShowSendEmailDialog = (entrepreneursId: string | undefined): void => {
  if (!entrepreneursId) return;

  entrepreneurSelected.value = entrepreneursId;
  showSendEmailDialog.value = true;
};

watch(
  [(): boolean => showModal.value, (): boolean => showCheckModal.value, (): boolean => showSendEmailDialog.value],
  ([newShowModal, newShowCheckModal, newShowSendEmail], [oldShowModal, oldShowCheckModal, oldShowSendEmail]): void => {
    if (oldShowModal && !newShowModal || oldShowCheckModal && !newShowCheckModal || oldShowSendEmail && !newShowSendEmail) {
      entrepreneurSelected.value = '';
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
      <draggable v-model="store.detail.invitedEntrepreneurs" group="group" handle=".drag-data" itemKey="sequence"
        @end="onRequence">
        <template #item="{ element: data, index }: { element: InvitedEntrepreneurs, index: number }">
          <div class="border-1 border-gray-300 rounded-lg px-3 py-2 mt-5" :key="data.id">
            <div class="flex items-center justify-between gap-2">
              <div class="flex items-center gap-4">
                <p class="font-bold">ข้อมูลผู้ประกอบการ</p>
                <span class="material-symbols-outlined text-orange-400 cursor-pointer"
                  @click="() => onShowModal(data.id)">
                  border_color
                </span>
              </div>
              <div class="flex h-full gap-4 items-start justify-end">
                <Button icon="pi pi-trash" severity="danger" variant="text" @click="() => onRemove(index)"
                  v-if="store.status.canEdit && menuStore.hasManage" />
                <span class="material-symbols-outlined drag-data cursor-move mt-2"
                  v-if="store.status.canEdit && menuStore.hasManage">drag_indicator</span>
              </div>
            </div>

            <div class="grid lg:grid-cols-12 mb-5">
              <div class="lg:col-span-9">
                <InfoRow label="เลขประจำตัวผู้เสียภาษี">
                  <p>{{ data.entrepreneurTaxId }}</p>
                </InfoRow>
                <InfoRow label="ประเภทผู้ประกอบการ">
                  <p>{{ data.entrepreneurType }}</p>
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
              <div class="lg:col-span-3 justify-end items-start flex gap-10">
                <!-- #TODO: UnComment When Use -->
                <!-- <div class="text-center">
                  <p class="font-bold">COI</p>
                  <span class="material-symbols-outlined text-green-400" v-if="data.coiResult">
                    check_circle
                  </span>
                  <span class="material-symbols-outlined text-red-400" v-if="!data.coiResult && data.coiResultAt">
                    cancel
                  </span>
                  <span class="material-symbols-outlined text-[#F9A825]"
                    v-if="data.coiResult == null && !data.coiResultAt">
                    error
                  </span>
                  <p class="text-m"
                    v-if="data.coiResult && (data.coiCheckerResult?.result === QualificationResult.Fail || data.shareholders?.some(s => s.coiCheckerResult?.result === QualificationResult.Fail))">
                    ผ่านแบบมีเงื่อนไข
                  </p>
                  <p class="text-gray-400 text-[16px] mb-3">
                    <span v-if="data.coiResultAt">ตรวจวันที่ : {{ ToDateTime(data.coiResultAt) }}</span>
                  </p>
                  <Button label="ตรวจสอบ" severity="success"
                    @click="() => onShowModalCheck(PP006EntrepreneurType.COI, data.id ?? '')" />
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
                  <span class="material-symbols-outlined text-[#F9A825]"
                    v-if="data.watchlistResult == null && !data.watchlistResultAt">
                    error
                  </span>
                  <p class="text-m"
                    v-if="data.watchlistResult && (data.watchlistCheckerResult?.result === QualificationResult.Fail || data.shareholders?.some(s => s.watchlistCheckerResult?.result === QualificationResult.Fail))">
                    ผ่านแบบมีเงื่อนไข
                  </p>
                  <p class="text-gray-400 text-[16px] mb-3">
                    <span v-if="data.watchlistResultAt">ตรวจวันที่ : {{ ToDateTime(data.watchlistResultAt) }}</span>
                  </p>
                  <Button label="ตรวจสอบ" severity="success"
                    @click="() => onShowModalCheck(PP006EntrepreneurType.Watchlist, data.id ?? '')" />
                </div>
                <div class="text-center">
                  <p class="font-bold">e-GP</p>
                  <span class="material-symbols-outlined text-green-400" v-if="data.egpResult">
                    check_circle
                  </span>
                  <span class="material-symbols-outlined text-red-400" v-if="!data.egpResult && data.egpResultAt">
                    cancel
                  </span>
                  <span class="material-symbols-outlined text-[#F9A825]"
                    v-if="data.egpResult == null && !data.egpResultAt">
                    error
                  </span>
                  <p class="text-gray-400 text-[16px] mb-3">
                    <span v-if="data.egpResultAt">ตรวจวันที่ : {{ ToDateTime(data.egpResultAt) }}</span>
                  </p>
                  <Button label="ตรวจสอบ" severity="success"
                    @click="() => onShowModalCheck(PP006EntrepreneurType.EGP, data.id ?? '')" />
                </div> -->
                <div class="text-center">
                  <Button label="ส่งอีเมลเชิญชวน" icon="pi pi-send" severity="warn"
                    @click="() => onShowSendEmailDialog(data.id)"
                    :disabled="store.detail.status != PP006Status.Approved" />
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
  <EntrepreneurDialog v-model="showModal" :selected="entrepreneurSelected" />
  <CheckDialog v-model="showCheckModal" :title="titleCheckModal" :selected="entrepreneurSelected" />
  <SendEmailInviteDialog v-model="showSendEmailDialog" :entrepreneur-id="entrepreneurSelected" />
</template>

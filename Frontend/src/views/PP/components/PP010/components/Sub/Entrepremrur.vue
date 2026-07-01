<script setup lang="ts">
import { ToDateTime } from '@/helpers/dateTime';
import { isNonEmptyString } from '@/helpers/string';
import { PP006EntrepreneurType } from '@/views/PP/enums/pp006';
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';
import { ref } from 'vue';
import EntrepreneurDialog from './EntrepreneurDialog.vue';
import CheckDialog from './CheckDialog.vue';

type Props = {
  disable?: boolean;
}

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });

const isShowEgpDialog = ref<boolean>(false);
const titleCheckModal = ref('');
const showModal = ref(false);

const onShowModalCheck = async (title: string): Promise<void> => {
  isShowEgpDialog.value = true;
  titleCheckModal.value = title;
};

const onShowModal = async (): Promise<void> => {
  showModal.value = true;
};
</script>

<template>
  <Card id="entrepreneur-section" class="mb-4" v-if="body" :pt="{ root: { 'data-section-id': 'entrepreneur', 'data-section-label': 'ผู้ประกอบการเสนอราคา' } }">
    <template #content>
      <TitleHeader label="ผู้ประกอบการเสนอราคา" />
      <div class="border-1 border-gray-300 rounded-lg p-3">
        <div class="mb-2 px-1 flex gap-2 items-center">
          <p class="font-bold text-start">ข้อมูลผู้ประกอบการ</p>
          <span class="material-symbols-outlined text-orange-400 cursor-pointer" @click="() => onShowModal()">
            border_color
          </span>
        </div>
        <div class="grid lg:grid-cols-5 mb-5 gap-4">
          <div class="lg:col-span-5">
            <InfoRow label="เลขประจำตัวผู้เสียภาษี">
              <p>{{ body.detail.vendor.taxpayerIdentificationNo }}</p>
            </InfoRow>
            <InfoRow label="ประเภทผู้ประกอบการ">
              <p>{{ body.detail.vendor.entrepreneurTypeName }}</p>
            </InfoRow>
            <InfoRow label="ชื่อสถานประกอบการ">
              <p class="font-bold">{{ body.detail.vendor.name }}</p>
            </InfoRow>

            <InfoRow label="รหัสสาขา">
              <p>{{ body.detail.vendor.sapBranchNumber || '-' }}</p>
            </InfoRow>

            <InfoRow label="อีเมล">
              <p>{{ isNonEmptyString(body.email) ? body.email : '-' }}</p>
            </InfoRow>
          </div>
          <div class="lg:col-span-5 justify-around items-center flex gap-4">
            <div class="text-center">
              <p class="font-bold">COI</p>
              <span class="material-symbols-outlined text-green-400" v-if="body.coiResult">
                check_circle
              </span>
              <span class="material-symbols-outlined text-red-400" v-if="!body.coiResult && body.coiDate">
                cancel
              </span>
              <span class="material-symbols-outlined text-[#F9A825]" v-if="body.coiResult == null && !body.coiDate">
                error
              </span>
              <p class="text-gray-400 text-[16px] mb-3">
                <span v-if="body.coiDate">ตรวจวันที่ : {{ ToDateTime(body.coiDate) }}</span>
              </p>
              <Button label="ตรวจสอบ" @click="() => onShowModalCheck(PP006EntrepreneurType.COI)" severity="success" />
            </div>
            <div class="text-center">
              <p class="font-bold">Watchlist</p>
              <span class="material-symbols-outlined text-green-400" v-if="body.watchlistResult">
                check_circle
              </span>
              <span class="material-symbols-outlined text-red-400" v-if="!body.watchlistResult && body.watchlistDate">
                cancel
              </span>
              <span class="material-symbols-outlined text-[#F9A825]"
                v-if="body.watchlistResult == null && !body.watchlistDate">
                error
              </span>
              <p class="text-gray-400 text-[16px] mb-3">
                <span v-if="body.watchlistDate">ตรวจวันที่ : {{ ToDateTime(body.watchlistDate) }}</span>
              </p>
              <Button label="ตรวจสอบ" @click="() => onShowModalCheck(PP006EntrepreneurType.Watchlist)"
                severity="success" />
            </div>
            <div class="text-center">
              <p class="font-bold">ผู้ทิ้งงานครั้งที่ 2 (e-GP)</p>
              <span class="material-symbols-outlined text-green-400" v-if="body.egpResult">
                check_circle
              </span>
              <span class="material-symbols-outlined text-red-400" v-if="!body.egpResult && body.egpDate">
                cancel
              </span>
              <span class="material-symbols-outlined text-[#F9A825]" v-if="body.egpResult == null && !body.egpDate">
                error
              </span>
              <p class="text-gray-400 text-[16px] mb-3">
                <span v-if="body.egpDate">ตรวจวันที่ : {{ ToDateTime(body.egpDate) }}</span>
              </p>
              <Button label="ตรวจสอบ" @click="() => onShowModalCheck(PP006EntrepreneurType.EGP)" severity="success" />
            </div>
          </div>
        </div>
      </div>
    </template>
  </Card>

  <EntrepreneurDialog v-model="showModal" />
  <CheckDialog v-model="isShowEgpDialog" :title="titleCheckModal" v-model:vendor="body" :disable="props.disable" />
</template>
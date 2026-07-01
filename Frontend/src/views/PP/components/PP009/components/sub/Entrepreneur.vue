<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { ToDateTime } from '@/helpers/dateTime';
import { isNonEmptyString } from '@/helpers/string';
import { PP006EntrepreneurType } from '@/views/PP/enums/pp006';
import type { VendorInfo } from '@/views/PP/models/PP009/pp009Model';
import { ref, watch } from 'vue';
import CheckDialog from './CheckDialog.vue';
import { usePP009DetailStore } from '@/views/PP/stores/PP009/PP009Store';
import { PP009Status } from '@/views/PP/enums/pp009';
import EntrepreneurDialog from './EntrepreneurDialog.vue';
import SendEmailInviteDialog from './SendEmailInviteDialog.vue';

type Props = {
  isDisabled?: boolean;
  vendorIndex?: number;
}

const { isDisabled, vendorIndex } = defineProps<Props>();

const store = usePP009DetailStore();

const value = defineModel<VendorInfo>({
  required: true,
});

const isShowEgpDialog = ref<boolean>(false);
const titleCheckModal = ref('');
const showModal = ref(false);
const showSendEmailDialog = ref(false);

const onShowModalCheck = async (title: string): Promise<void> => {
  isShowEgpDialog.value = true;
  titleCheckModal.value = title;
};

const onSendEmail = async (id?: string) => {
  if (!id) return;

  showSendEmailDialog.value = true;
}

const onShowModal = async (): Promise<void> => {
  showModal.value = true;
};

watch(isShowEgpDialog, (newVal) => {
  if (!newVal) {
    store.fn.syncCheckDataByTaxId(value.value);
  }
});
</script>

<template>
  <Card class="mb-4" v-if="value.entrepreneur" id="entrepreneur-section">
    <template #content>
      <TitleHeader label="ผู้ประกอบการเสนอราคา" />
      <div class="border-1 border-gray-300 rounded-lg p-3">
        <div class="flex gap-2 items-center mb-2 px-1">
          <p class="font-bold text-start">ข้อมูลผู้ประกอบการ</p>
          <span class="material-symbols-outlined text-orange-400 cursor-pointer" @click="() => onShowModal()">
            border_color
          </span>
        </div>
        <div class="grid lg:grid-cols-5 mb-8 gap-4">
          <div class="lg:col-span-5">
            <InfoRow label="เลขประจำตัวผู้เสียภาษี">
              <p>{{ value.entrepreneur.taxpayerIdentificationNo }}</p>
            </InfoRow>
            <InfoRow label="ประเภทผู้ประกอบการ">
              <p>{{ value.entrepreneur.entrepreneurTypeName }}</p>
            </InfoRow>
            <InfoRow label="ชื่อสถานประกอบการ">
              <p class="font-bold">{{ value.entrepreneur.establishmentName }}</p>
            </InfoRow>

            <InfoRow label="รหัสสาขา">
              <p>{{ value.entrepreneur.sapBranchNumber || '-' }}</p>
            </InfoRow>

            <InfoRow label="อีเมล">
              <p>{{ isNonEmptyString(value.entrepreneur.email) ? value.entrepreneur.email : '-' }}</p>
            </InfoRow>
          </div>
          <div class="lg:col-span-7 justify-center items-center flex gap-10">
            <div class="text-center">
              <p class="font-bold">COI</p>
              <span class="material-symbols-outlined text-green-400" v-if="value.coiResult">
                check_circle
              </span>
              <span class="material-symbols-outlined text-red-400" v-if="!value.coiResult && value.coiDate">
                cancel
              </span>
              <span class="material-symbols-outlined text-[#F9A825]" v-if="value.coiResult == null && !value.coiDate">
                error
              </span>
              <p class="text-gray-400 text-[16px] mb-3">
                <span v-if="value.coiDate">ตรวจวันที่ : {{ ToDateTime(value.coiDate) }}</span>
              </p>
              <Button label="ตรวจสอบ" @click="() => onShowModalCheck(PP006EntrepreneurType.COI)" severity="success" />
            </div>
            <div class="text-center">
              <p class="font-bold">Watchlist</p>
              <span class="material-symbols-outlined text-green-400" v-if="value.watchlistResult">
                check_circle
              </span>
              <span class="material-symbols-outlined text-red-400" v-if="!value.watchlistResult && value.watchlistDate">
                cancel
              </span>
              <span class="material-symbols-outlined text-[#F9A825]"
                v-if="value.watchlistResult == null && !value.watchlistDate">
                error
              </span>
              <p class="text-gray-400 text-[16px] mb-3">
                <span v-if="value.watchlistDate">ตรวจวันที่ : {{ ToDateTime(value.watchlistDate) }}</span>
              </p>
              <Button label="ตรวจสอบ" @click="() => onShowModalCheck(PP006EntrepreneurType.Watchlist)"
                severity="success" />
            </div>
            <div class="text-center">
              <p class="font-bold">ผู้ทิ้งงาน (e-GP)</p>
              <span class="material-symbols-outlined text-green-400" v-if="value.egpResult">
                check_circle
              </span>
              <span class="material-symbols-outlined text-red-400" v-if="!value.egpResult && value.egpDate">
                cancel
              </span>
              <span class="material-symbols-outlined text-[#F9A825]" v-if="value.egpResult == null && !value.egpDate">
                error
              </span>
              <p class="text-gray-400 text-[16px] mb-3">
                <span v-if="value.egpDate">ตรวจวันที่ : {{ ToDateTime(value.egpDate) }}</span>
              </p>
              <Button label="ตรวจสอบ" @click="() => onShowModalCheck(PP006EntrepreneurType.EGP)" severity="success" />
            </div>
            <div class="text-center" v-if="store.body.status === PP009Status.Approved">
              <Button label="ส่งอีเมลเชิญชวน" icon="pi pi-send" :severity="value.emailTemplate ? 'success' : 'warn'"
                @click="() => onSendEmail(value.id)" />
            </div>
          </div>
        </div>
      </div>
    </template>
  </Card>
  <EntrepreneurDialog v-model="showModal" :vendorSelected="vendorIndex" />
  <CheckDialog v-model="isShowEgpDialog" :title="titleCheckModal" v-model:vendor="value" :disable="isDisabled" />
  <SendEmailInviteDialog v-model="showSendEmailDialog" :vendor-id="value.id" />
</template>
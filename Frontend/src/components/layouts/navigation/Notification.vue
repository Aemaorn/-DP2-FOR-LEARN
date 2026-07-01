<script setup lang="ts">
import { timeFromNowSimple } from '@/helpers/dateTime';
import { useNotificationStore } from '@/stores/notification/notification';
import { computed, onMounted, onUnmounted, ref } from 'vue';

const store = useNotificationStore();
const overlayRef = ref<HTMLElement>({} as HTMLElement);
const overlay = ref();
const timer = ref();
const showReadAllConfirm = ref(false);

const programLabelMap: Record<string, string> = {
  Plan: 'รายการจัดซื้อจัดจ้าง',
  ContractAgreement: 'ร่างสัญญาและสัญญา',
  Procurement: 'การจัดซื้อจัดจ้าง',
  ContractManagement: 'บริหารสัญญา',
  ContractAmendment: 'แก้ไขสัญญา',
  ExpenseDisbursement: 'เบิกจ่าย',
  PlanAnnouncement: 'ขออนุมัติเผยแพร่แผนจัดซื้อจัดจ้าง',
  BranchSpaceRent: 'เช่าพื้นที่ทำการสาขา',
};

const programLabel = (program?: string): string => programLabelMap[program ?? ''] ?? program ?? '';

const onConfirmReadAllAsync = async (): Promise<void> => {
  showReadAllConfirm.value = false;
  await store.onReadAllNotiAsync();
};

onMounted(() => {
  initAsync();
});

const initAsync = async () => {
  await store.getListAsync();

  countdownToFetchNotiAsync();
};

const countdownToFetchNotiAsync = () => {
  if (timer.value) {
    clearInterval(timer.value);
  }

  timer.value = setInterval(async () => {
    await store.getListAsync();
  }, 60000);
};

const toggleAsync = async (event: any) => {
  overlay.value.toggle(event);

  showReadAllConfirm.value = false;
  store.onClearData();
  await store.getListAsync();
};

const onScrollAsync = async (e: Event) => {
  const el = e.target as HTMLElement;
  if (el.scrollTop + el.clientHeight >= el.scrollHeight && (store.notiRes.notifications.totalRecords > store.criteria.pageSize)) {
    store.criteria.pageSize = store.criteria.pageSize + 10;

    await store.getListAsync();
  }
};

const onNavigateToDetailAsync = async (id: string, linkUrl: string, isRead: boolean) => {
  store.onClickNotiAsync(id, linkUrl, isRead);

  overlayRef.value.click();
};

// eslint-disable-next-line @typescript-eslint/no-unused-vars
const onReadAllNotiAsync = async () => {
  await store.onReadAllNotiAsync();
};

onUnmounted(() => {
  if (timer.value) {
    clearInterval(timer.value);
  }
});

const noOfNoti = computed(() => {
  if (store.notiRes.count < 100) {
    return store.notiRes.count;
  }

  return "99+";
});
</script>

<template>
  <div>
    <div class="relative cursor-pointer" ref="overlayRef" @click="toggleAsync">
      <span class="material-symbols-rounded text-white text-[35px]! ghb-notification-icon">
        notifications
      </span>
      <div class="notification-status text-center" v-if="store.notiRes.count > 0">
        {{ noOfNoti }}
      </div>
    </div>
    <OverlayPanel ref="overlay" appendTo="self" class="t-0"
      :pt="{ content: { class: 'p-0!' } }">
      <div class="w-96 h-[32rem] scrollable" @scroll.passive="onScrollAsync">
        <div class="flex items-center justify-between border-b-2 border-gray-300 px-3 py-2 relative">
          <p class="font-bold pb-2 mb-2">การแจ้งเตือน</p>
          <p @click.stop="showReadAllConfirm = !showReadAllConfirm"
            class="font-bold pb-2 mb-2 text-blue-500 underline cursor-pointer">
            ทำเครื่องหมายอ่านทั้งหมด</p>

          <div v-if="showReadAllConfirm"
            class="absolute right-3 top-full -mt-1 z-10 bg-white border border-gray-200 rounded-lg shadow-lg p-3 w-64">
            <div class="absolute -top-1.5 right-8 w-3 h-3 rotate-45 bg-white border-t border-l border-gray-200" />
            <div class="flex items-start gap-2 mb-3">
              <i class="pi pi-question-circle text-yellow-500 mt-0.5" />
              <p class="text-sm text-gray-700">ยืนยันทำเครื่องหมายอ่านทั้งหมดหรือไม่?</p>
            </div>
            <div class="flex justify-end gap-2">
              <button type="button" @click.stop="showReadAllConfirm = false"
                class="text-xs px-3 py-1 rounded text-gray-600 hover:bg-gray-100 cursor-pointer">
                ยกเลิก
              </button>
              <button type="button" @click.stop="onConfirmReadAllAsync"
                class="text-xs px-3 py-1 rounded bg-blue-500 text-white hover:bg-blue-600 cursor-pointer">
                ยืนยัน
              </button>
            </div>
          </div>
        </div>
        <div v-if="store.notiRes.notifications.data.length > 0">
          <div v-for="item in store.notiRes.notifications.data" :key="item.id">
            <div
              :class="['flex items-start px-3 justify-between cursor-pointer transition-colors',
                !item.isRead ? 'bg-blue-50 hover:bg-blue-100 border-l-4 border-primary' : 'hover:bg-gray-100 border-l-4 border-transparent']"
              @click="() => onNavigateToDetailAsync(item.id, item.linkUrl, item.isRead)">
              <div class="py-1.5 px-2 flex-1 min-w-0 pr-2">
                <div class="flex items-center gap-2">
                  <p class="text-primary text-sm">{{ programLabel(item.program) }}</p>
                  <span v-if="!item.isRead"
                    class="text-[10px] font-semibold text-white bg-primary px-1.5 py-0.5 rounded-full leading-none">
                    ใหม่
                  </span>
                </div>
                <p :class="['font-bold break-words leading-tight', !item.isRead ? 'text-gray-900' : 'text-gray-600']">{{ item.title }}</p>
                <small class="block break-words leading-tight">"{{ item.message }}"</small>
              </div>
              <div class="flex items-start text-nowrap py-1.5">
                <small>{{ timeFromNowSimple(item.createdAt) }}</small>
              </div>
            </div>
            <Divider class="my-0!" />
          </div>
        </div>

        <div v-else class="text-center text-gray-400">
          ไม่มีการแจ้งเตือน
        </div>
      </div>
    </OverlayPanel>
  </div>
</template>

<style scoped lang="scss">
:deep(.p-popover) {
  top: unset !important
}

.scrollable {
  overflow: auto;
  scrollbar-width: none;
}

.scrollable::-webkit-scrollbar {
  display: none;
}

.material-symbols-rounded.ghb-notification-icon {
  font-variation-settings:
    'FILL' 0,
    'wght' 300,
    'GRAD' -50,
    'opsz' 12
}

.notification-status {
  position: absolute;
  top: 0;
  right: 0;
  width: 18px;
  height: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 9999px;
  border: 1px solid #fcc800;
  background-color: #fcc800;
  color: #7c4b1d;
  font-size: 11px;
  font-weight: 600;
}
</style>

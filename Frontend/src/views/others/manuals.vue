<script setup lang="ts">
import { Card } from 'primevue';
import { onMounted, ref } from 'vue';
import { HttpStatusCode } from 'axios';
import UserManualService from '@/services/userManual';
import type { TUserManualDetail, TUserManualListItem } from '@/models/userManual';
import ChEditor from '@/components/Document/ChEditor.vue';

const manuals = ref<TUserManualListItem[]>([]);
const isLoading = ref(false);
const selectedManual = ref<TUserManualDetail | null>(null);
const isLoadingDetail = ref(false);

const loadManuals = async (): Promise<void> => {
  isLoading.value = true;
  try {
    const { data, status } = await UserManualService.onGetListAsync();

    if (status === HttpStatusCode.Ok) {
      manuals.value = data;

      const defaultManual = manuals.value.find(m => m.code === 'Plan');
      if (defaultManual) {
        await onOpenManual(defaultManual.id);
      }
    }
  } finally {
    isLoading.value = false;
  }
};

const onOpenManual = async (id: string): Promise<void> => {
  isLoadingDetail.value = true;
  try {
    const { data, status } = await UserManualService.onGetByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      selectedManual.value = data;
    }
  } finally {
    isLoadingDetail.value = false;
  }
};

onMounted(() => {
  loadManuals();
});
</script>

<template>
  <div class="min-h-dvh bg-gray-50 py-6 px-6">
    <div class="w-full">
      <div class="mb-4">
        <h1 class="text-2xl font-bold text-gray-800">คู่มือการใช้งานระบบ</h1>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-12 gap-4">
        <!-- Left: List -->
        <Card class="lg:col-span-3">
          <template #content>
            <p class="text-sm font-semibold text-gray-700 mb-3">รายการคู่มือ</p>

            <div v-if="isLoading" class="text-center py-12 text-gray-400">
              <i class="pi pi-spin pi-spinner text-2xl mb-2 block" />
              <p>กำลังโหลดข้อมูล...</p>
            </div>

            <div v-else-if="manuals.length === 0" class="text-center py-12 text-gray-500">
              <i class="pi pi-book text-3xl mb-2 block opacity-30" />
              <p>ยังไม่มีรายการคู่มือ</p>
            </div>

            <ul v-else class="divide-y divide-gray-100">
              <li v-for="manual in manuals" :key="manual.id"
                :class="['flex items-center gap-3 py-3 px-2 cursor-pointer transition-colors',
                  selectedManual?.id === manual.id
                    ? 'bg-blue-50 border-l-4 border-blue-500 -ml-2 pl-3'
                    : 'hover:bg-gray-50 border-l-4 border-transparent -ml-2 pl-3']"
                @click="onOpenManual(manual.id)">
                <i class="pi pi-file-pdf text-red-500 text-xl shrink-0" />
                <div class="min-w-0 flex-1">
                  <p class="text-sm font-medium text-gray-800 truncate">{{ manual.name }}</p>
                  <p class="text-xs text-gray-400 truncate">{{ manual.code }}</p>
                </div>
                <i v-if="isLoadingDetail && selectedManual?.id !== manual.id" class="pi pi-spin pi-spinner text-gray-400" />
              </li>
            </ul>
          </template>
        </Card>

        <!-- Right: ChEditor -->
        <Card class="lg:col-span-9">
          <template #content>
            <div v-if="isLoadingDetail && !selectedManual" class="text-center py-12 text-gray-400">
              <i class="pi pi-spin pi-spinner text-2xl mb-2 block" />
              <p>กำลังโหลดเอกสาร...</p>
            </div>

            <div v-else-if="!selectedManual" class="text-center py-20 text-gray-400">
              <i class="pi pi-book text-4xl mb-3 block opacity-30" />
              <p>เลือกคู่มือจากรายการเพื่อแสดงเอกสาร</p>
            </div>

            <template v-else>
              <p class="text-sm font-semibold text-gray-700 mb-3 truncate">{{ selectedManual.name }}</p>

              <div v-if="selectedManual.previewPdfFileId">
                <ChEditor :docId="selectedManual.previewPdfFileId"
                  :docName="selectedManual.previewPdfFileName || selectedManual.name"
                  :readonly="true" :key="selectedManual.previewPdfFileId" />
              </div>
              <div v-else class="text-center py-12 text-gray-500">
                <i class="pi pi-exclamation-circle text-3xl mb-2 block opacity-30" />
                <p>ไม่มีไฟล์ตัวอย่างสำหรับคู่มือนี้</p>
              </div>
            </template>
          </template>
        </Card>
      </div>
    </div>
  </div>
</template>

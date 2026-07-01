<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Form } from 'vee-validate';
import type { MenuItem } from 'primevue/menuitem';
import { Select, InputUploadFile } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { useAnnouncementReportDetailStore, useAnnouncementReportListStore } from '@/stores/ANN/ANN002';
import { YearOptions } from '@/constants/date';
import { showActivityDialog } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import FileHelper from '@/helpers/file';

const route = useRoute();
const router = useRouter();

const listStore = useAnnouncementReportListStore();
const detailStore = useAnnouncementReportDetailStore();

const routeItems: MenuItem[] = [
  { label: 'รายงานประกาศ', url: '/ann/ann002' },
  { label: 'ข้อมูลรายงานประกาศ' },
];

onMounted(async (): Promise<void> => {
  await listStore.onGetReportTypeOptionsAsync();

  if (route.params.id) {
    await detailStore.onGetByIdAsync(route.params.id as string);
  }
});

onUnmounted((): void => {
  detailStore.onResetBody();
});

const onSubmitAsync = async (): Promise<void> => {

  const hasDocument = !!(detailStore.body.documentInfo || detailStore.body.documentId || detailStore.body.documentUrl);
  if (!hasDocument) {
    ToastHelper.error('กรุณาแนบเอกสาร', 'กรุณาอัปโหลดไฟล์เอกสารที่เกี่ยวข้อง');
    return;
  }

  if (route.params.id) {
    const ok = await detailStore.onUpdateAsync(route.params.id as string);
    if (ok) await detailStore.onGetByIdAsync(route.params.id as string);
  } else {
    const newId = await detailStore.onCreateAsync();
    if (newId) {
      router.replace({ name: 'ann002Detail', params: { id: newId } });
    }
  }
};
</script>

<template>
  <Form class="pb-10" @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()">
    <TitleHeader
      label="ข้อมูลรายงาน"
      :route-items="routeItems"
    >
      <template #action>
        <Button
          label="ประวัติการใช้งาน"
          icon="pi pi-refresh"
          variant="outlined"
          severity="primary"
          v-if="route.params.id"
          class="bg-white! hover:bg-red-50!"
          @click="() => showActivityDialog(route.params.id as string)"
        />
      </template>
      <template #breadcrumbAction>
        <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
      </template>
    </TitleHeader>

    <Card class="my-4">
      <template #content>
        <div class="space-y-6 mt-6">
          <div class="grid grid-cols-1 md:grid-cols-4 gap-x-4 gap-y-6">
            <Select
              label="ประเภทรายงาน"
              v-model="detailStore.body.announcementReportTypeCode"
              :options="listStore.reportTypeOptions"
              :disabled="!!route.params.id"
            />
            <Select
              label="ปี"
              v-model="detailStore.body.year"
              :options="YearOptions"
              :disabled="!!route.params.id"
            />
          </div>

          <div class="grid grid-cols-1 md:grid-cols-4 gap-x-4 gap-y-6">
            <InputArea
              class="md:col-span-2"
              label="รายละเอียด"
              v-model="detailStore.body.discretion"
            />
          </div>
        </div>
      </template>
    </Card>

    <Card class="my-4">
      <template #content>
        <div class="space-y-4">
          <TitleHeader label="เอกสารแนบ" />
          <InputUploadFile
            class="mt-8"
            label="เอกสารที่เกียวข้อง"
            :model-value="detailStore.body.documentInfo ?? undefined"
            :clearable="!!(detailStore.body.documentInfo || detailStore.body.documentName)"
            @update:model-value="(v) => detailStore.body.documentInfo = v ?? null"
            @clear="() => { detailStore.body.documentInfo = null; }"
            helper-text="รองรับเฉพาะไฟล์ .pdf ขนาดไม่เกิน 10 MB"
          />
          <div
            v-if="detailStore.body.documentId || detailStore.body.documentUrl"
            class="mt-2 flex items-center gap-2 text-sm text-gray-500"
          >
            <span class="font-medium">เอกสารแนบ:</span>
            <a
              :href="detailStore.body.documentId ? FileHelper.getFileUrl(detailStore.body.documentId) : detailStore.body.documentUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="inline-flex items-center gap-1.5 text-blue-600 hover:text-blue-800 hover:underline"
            >
              <i class="pi pi-file text-sm" />
              <span>{{ detailStore.body.documentName ?? detailStore.body.documentUrl }}</span>
            </a>
            <Button
              icon="pi pi-trash"
              severity="danger"
              variant="text"
              size="small"
              @click="() => { detailStore.body.documentId = undefined; detailStore.body.documentName = undefined; detailStore.body.documentUrl = undefined; }"
            />
          </div>
        </div>
      </template>
    </Card>
  </Form>
</template>

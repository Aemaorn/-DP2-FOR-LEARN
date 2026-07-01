<script setup lang="ts">
import { onMounted, onUnmounted, nextTick, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Form } from 'vee-validate';
import type { MenuItem } from 'primevue/menuitem';
import { InputField, InputNumber, Select, Datepicker, PrimeVueDatePicker, InputUploadFile } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { useAnnouncementInfoDetailStore, useAnnouncementInfoListStore } from '@/stores/ANN/ANN001';
import { YearOptions } from '@/constants/date';
import { showActivityDialog } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import FileHelper from '@/helpers/file';

const route = useRoute();
const router = useRouter();

const listStore = useAnnouncementInfoListStore();
const detailStore = useAnnouncementInfoDetailStore();

const routeItems: MenuItem[] = [
  { label: 'ประกาศข้อมูลการจัดซื้อจัดจ้าง', url: '/ann/ann001' },
  { label: 'ข้อมูลการจัดซื้อจัดจ้าง' },
];

const initialized = ref(false);

onMounted(async (): Promise<void> => {
  await Promise.all([
    listStore.onGetAnnouncementCategoryOptionsAsync(),
    listStore.onGetSupplyMethodOptionsAsync(),
  ]);

  if (route.params.id) {
    await detailStore.onGetByIdAsync(route.params.id as string);
  }

  await nextTick();
  initialized.value = true;
});

onUnmounted((): void => {
  initialized.value = false;
  detailStore.onResetBody();
});

watch(
  (): string => detailStore.body.announcementCategoryCode,
  (): void => {
    if (!initialized.value) return;
    detailStore.body.expectedDate = undefined;
    detailStore.body.startDate = undefined;
    detailStore.body.endDate = undefined;
    detailStore.body.referencePrice = undefined;
    detailStore.body.description = undefined;
  },
);

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
      router.replace({ name: 'ann001Detail', params: { id: newId } });
    }
  }
};
</script>

<template>
  <Form class="pb-10" @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()">
    <TitleHeader
      :label="'ประกาศข้อมูลการจัดซื้อจัดจ้าง'"
      :route-items="routeItems"
    >
      <template #action>
        <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
          v-if="route.params.id" class="bg-white! hover:bg-red-50!"
          @click="() => showActivityDialog(route.params.id as string)" />
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
              label="ประเภทประกาศ"
              v-model="detailStore.body.announcementCategoryCode"
              :options="listStore.announcementCategoryOptions"
              rules="required"
            />
            <InputField
              v-if="detailStore.body.announcementCategoryCode === 'AnnOther'"
              label="รายละเอียด"
              v-model="detailStore.body.description"
              rules="required"
            />
          </div>

          <div class="grid grid-cols-2 md:grid-cols-4 gap-x-4 gap-y-6">
            <InputArea
              class="md:col-span-2"
              label="ชื่อประกาศ"
              v-model="detailStore.body.announcementName"
              rules="required"
            />
          </div>

          <div class="grid grid-cols-1 md:grid-cols-4 gap-x-4 gap-y-6">
            <Select
              label="วิธีการจัดหา"
              v-model="detailStore.body.supplyMethodCode"
              :options="listStore.supplyMethodOptions"
              rules="required"
            />
            <Datepicker
              label="วันที่ประกาศเผยแพร่"
              v-model="detailStore.body.announcementDate"
              rules="required"
            />
          </div>

          <div class="grid grid-cols-1 md:grid-cols-4 gap-x-4 gap-y-6">
            <Select
              label="ปีงบประมาณ"
              v-model="detailStore.body.budgetYear"
              :options="YearOptions"
              rules="required"
            />
            <InputNumber
              label="งบประมาณ"
              v-model="detailStore.body.budgetAmount"
              grouping :min-fraction-digits="2" :max-fraction-digits="2"
              rules="required"
            />
          </div>

          <div class="grid grid-cols-1 md:grid-cols-4 gap-x-4 gap-y-6"
            v-if="detailStore.body.announcementCategoryCode === 'AnnPlan'">
            <PrimeVueDatePicker
              label="คาดว่าจะประกาศจัดซื้อจัดจ้าง (เดือน/ปี)"
              v-model="detailStore.body.expectedDate"
              view="month"
            />
          </div>

          <div class="grid grid-cols-1 md:grid-cols-4 gap-x-4 gap-y-6"
           v-if="detailStore.body.announcementCategoryCode === 'AnnTOR'">
            <Datepicker
              label="วันที่เริ่มต้นประชาพิจารณ์"
              v-model="detailStore.body.startDate"
            />
            <Datepicker
              label="วันที่สิ้นสุดประชาพิจารณ์"
              v-model="detailStore.body.endDate"
              :minDate="detailStore.body.startDate"
            />
          </div>


          <div class="grid grid-cols-1 md:grid-cols-4 gap-x-4 gap-y-6"
            v-if="detailStore.body.announcementCategoryCode === 'AnnRefPrice'">
            <InputNumber
              label="ราคากลางอ้างอิง"
              v-model="detailStore.body.referencePrice"
              grouping :min-fraction-digits="2" :max-fraction-digits="2"
            />
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-x-4 gap-y-6">
            <InputArea
              class="md:col-span-2"
              label="หมายเหตุ"
              v-model="detailStore.body.remark"
            />
          </div>
        </div>
      </template>
    </Card>
    <Card class="my-4 mt-4">
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

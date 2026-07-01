<script setup lang="ts">
import type { OnlyFileAttachment } from '@/models/shared/uploadFile';
import ST003Service from '@/services/file';
import { useCm001DetailStore } from '@/stores/CM/cm001';
import CM001Service from '@/services/CM/cm001';
import ToastHelper from '@/helpers/toast';
import { HttpStatusCode } from 'axios';
import { Button, Dialog } from 'primevue';
import Editor from 'primevue/editor';
import { ref, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { ToTHDateFullMonthOnly } from '@/helpers/dateTime';

type Props = {
  modelValue: boolean;
};

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
}>();

const store = useCm001DetailStore();
const { body } = storeToRefs(store);
const files = ref<OnlyFileAttachment[]>([]);
const editorContent = ref('');

const onClose = (): void => {
  editorContent.value = '';
  files.value = [];
  emit('update:modelValue', false);
};

const onSendEmail = async (): Promise<void> => {
  if (!store.body.id) return;

  const { status } = await CM001Service.sendWarrantyPeriodEmailAsync(
    store.body.id,
    body.value.cm001Info?.vendorEmail ?? '',
    editorContent.value,
    files.value
  );

  if (status === HttpStatusCode.Ok || status === HttpStatusCode.Accepted) {
    ToastHelper.success('ส่งอีเมลแจ้งระยะเวลารับประกัน', 'ส่งอีเมลแจ้งระยะเวลารับประกันสำเร็จ');
    onClose();
  }
};

const onFileSelect = async (event: Event): Promise<void> => {
  const target = event.target as HTMLInputElement;
  if (!target.files) return;

  const fileList = Array.from(target.files);

  for (const file of fileList) {
    const { data, status } = await ST003Service.uploadFile(file);

    if (status === HttpStatusCode.Ok) {
      const uploadedFile: OnlyFileAttachment = {
        fileId: data.id,
        fileName: file.name,
        sequence: files.value.length + 1,
      };

      files.value = [...files.value, uploadedFile];
    }
  }

  target.value = '';
};

const onRemoveFile = (index: number): void => {
  files.value.splice(index, 1);
};

const formatWarrantyDuration = (): string => {
  const period = body.value.cm001Info?.warranty?.warrantyPeriod;
  if (!period) return '....................';

  const parts: string[] = [];
  if (period.year) parts.push(`${period.year} ปี`);
  if (period.month) parts.push(`${period.month} เดือน`);
  if (period.day) parts.push(`${period.day} วัน`);

  return parts.length > 0 ? parts.join(' ') : '....................';
};

const buildEmailTemplate = (): string => {
  const vendorName = body.value.cm001Info?.establishmentName || '';
  const contractName = body.value.cm001Info?.name || '';
  const contractNumber = body.value.cm001Info?.contractNumber || '';
  const lastAcceptanceDate = body.value.cm001Info?.warranty?.lastAcceptanceDate
    ? ToTHDateFullMonthOnly(new Date(body.value.cm001Info.warranty.lastAcceptanceDate)).replace(/^0/, '')
    : '....................';
  const warrantyStartDate = body.value.cm001Info?.warranty?.warrantyStartDate
    ? ToTHDateFullMonthOnly(new Date(body.value.cm001Info.warranty.warrantyStartDate)).replace(/^0/, '')
    : '....................';
  const warrantyEndDate = body.value.cm001Info?.warranty?.warrantyEndDate
    ? ToTHDateFullMonthOnly(new Date(body.value.cm001Info.warranty.warrantyEndDate)).replace(/^0/, '')
    : '....................';
  const warrantyDuration = formatWarrantyDuration();

  return `<p>เรียน ${vendorName}</p>
<p><br></p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ตามที่ธนาคารอาคารสงเคราะห์ (ธอส.) ได้ทำสัญญากับ${vendorName} เพื่อดำเนินการ${contractName} ตามสัญญาเลขที่ ${contractNumber} และ${vendorName} ได้ดำเนินการปฏิบัติงานเรียบร้อยแล้วในวันที่ ${lastAcceptanceDate}</p>
<p><br></p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;เพื่อเป็นการปฏิบัติตามข้อกำหนดในสัญญาและให้ความมั่นใจในคุณภาพ ธนาคารอาคารสงเคราะห์ (ธอส.) จึงขอแจ้งระยะเวลาการรับประกันผลงาน${contractName} โดยมีรายละเอียดดังต่อไปนี้</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;1. ระยะเวลารับประกัน เริ่มตั้งแต่วันที่ ${warrantyStartDate} ถึงวันที่ ${warrantyEndDate} รวมระยะเวลา ${warrantyDuration}</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2. ขอบเขตของการรับประกัน ${vendorName} รับผิดชอบในการแก้ไขปรับปรุงข้อบกพร่องที่เกิดจากการ${contractName} ตามข้อตกลงในสัญญาเลขที่ ${contractNumber}</p>
<p><br></p>
<p><br></p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ขอแสดงความนับถือ</p>
<p>ส่วนบริหารสัญญา ฝ่ายจัดหาและการพัสดุ</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ธนาคารอาคารสงเคราะห์</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;โทร.02-2022105 , 02-2022109</p>`;
};

watch(() => props.modelValue, (newValue) => {
  if (newValue) {
    files.value = [];
    editorContent.value = buildEmailTemplate();
  } else {
    editorContent.value = '';
    files.value = [];
  }
});
</script>

<template>
  <Dialog :visible="modelValue" modal header="ส่งอีเมลแจ้งระยะเวลารับประกัน" :style="{ width: '100rem' }"
    @update:visible="(val) => emit('update:modelValue', val)">
    <div class="space-y-4">
      <div>
        <p class="font-bold mb-2">คู่ค้า</p>
        <p class="mb-2">{{ body.cm001Info?.vendorName }}</p>

        <label for="warranty-email-input" class="block mb-2">
          E-mail<span class="text-red-500">*</span>
        </label>
        <input id="warranty-email-input" v-model="body.cm001Info!.vendorEmail" type="email"
          class="w-full border-1 border-gray-300 rounded-md px-3 py-2" placeholder="E-mail" />
      </div>

      <div>
        <Editor v-model="editorContent" editorStyle="height: 320px" />
      </div>

      <div class="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center">
        <input type="file" id="warrantyEmailFileUpload" class="hidden" multiple @change="onFileSelect" />
        <label for="warrantyEmailFileUpload" class="cursor-pointer">
          <div class="flex flex-col items-center gap-2">
            <span class="material-symbols-outlined text-gray-400 text-5xl">cloud_upload</span>
            <p class="text-gray-500">คลิกที่นี่เพื่อเลือกไฟล์ของคุณ</p>
            <p class="text-gray-400 text-sm">ขนาด : JPG, PNG, XLS ,XLSX (ขนาด 15 MB)</p>
          </div>
        </label>
      </div>

      <div v-if="files.length > 0" class="space-y-2">
        <div v-for="(file, index) in files" :key="index"
          class="flex items-center justify-between border-1 border-gray-300 rounded-lg p-2">
          <div class="flex items-center gap-2">
            <span class="material-symbols-outlined">description</span>
            <span>{{ file.fileName }}</span>
          </div>
          <div class="flex items-center gap-2">
            <Button icon="pi pi-trash" severity="danger" text @click="() => onRemoveFile(index)" />
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2">
        <Button label="ยกเลิก" severity="secondary" variant="outlined" @click="onClose" />
        <Button label="ยืนยันส่งเมล" icon="pi pi-send" severity="success" @click="onSendEmail" :disabled="!body.cm001Info?.vendorEmail" />
      </div>
    </template>
  </Dialog>
</template>

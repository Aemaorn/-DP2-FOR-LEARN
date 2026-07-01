<script setup lang="ts">
import type { OnlyFileAttachment } from '@/models/shared/uploadFile';
import ST003Service from '@/services/file';
import { useCm006DetailStore } from '@/stores/CM/CM006/cm006.detail';
import { HttpStatusCode } from 'axios';
import { Button, Dialog } from 'primevue';
import Editor from 'primevue/editor';
import { ref, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { ToTHDateFullMonthOnly } from '@/helpers/dateTime';
import { formatCurrency } from '@/helpers/currency';

type Props = {
  modelValue: boolean;
};

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
}>();

const store = useCm006DetailStore();
const { body } = storeToRefs(store);
const email = ref('');
const files = ref<OnlyFileAttachment[]>([]);
const editorContent = ref('');

const onClose = (): void => {
  email.value = '';
  editorContent.value = '';
  files.value = [];
  emit('update:modelValue', false);
};

const onSendEmail = async (): Promise<void> => {
  await store.onSendEmailAsync(email.value, editorContent.value, files.value);
  onClose();
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

const buildPBondType001Template = (): string => {
  const entrepreneurName = body.value.entrepreneurName || '';
  const contractName = body.value.contractName || '';
  const contractNumber = body.value.contractNumber || '';
  const contractSignedDate = ToTHDateFullMonthOnly(body.value.contractSignedDate);
  const returnAmount = formatCurrency(body.value.guaranteeReturn.returnAmount || 0);

  return `<p>เรียน ${entrepreneurName}</p>
<p><br></p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ตามที่ ${entrepreneurName} ได้ดำเนินการทำสัญญา${contractName} สัญญาเลขที่ จพ.(สบส.) ${contractNumber} ลงวันที่ ${contractSignedDate} กับธนาคารอาคารสงเคราะห์ และวางหลักประกันสัญญาเป็นเงินสด (เงินโอน) จำนวนเงิน ${returnAmount} บาท นั้น</p>
<p><br></p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;บัดนี้ สัญญาดังกล่าวได้พ้นภาระผูกพันเรียบร้อยแล้ว จึงขอแจ้งให้บริษัทฯ จัดเตรียมเอกสารดังนี้ เพื่อประกอบในการโอนหลักประกันสัญญาคืน</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;1. ใบเสร็จรับเงินตัวจริงที่ธนาคารอาคารสงเคราะห์ออกให้ (กรุณาส่งฉบับจริงที่ธนาคารอาคารสงเคราะห์ สำนักงานใหญ่)</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2. สำเนาหน้าบัญชีของธนาคารอาคารสงเคราะห์ (ไม่มีค่าธรรมเนียมในการโอนเงิน)</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;3. สำเนาหน้าบัญชีของธนาคารพาณิชย์อื่นๆ (โดยบริษัทคู่สัญญาเป็นผู้รับผิดชอบค่าใช้จ่ายในการโอนเงิน)</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;4. สำเนาบัตรประชาชนของผู้มีอำนาจลงนามผูกพันบริษัทฯ/ห้างหุ้นส่วนจำกัด</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;5. หนังสือรับรองสำนักงานทะเบียนหุ้นส่วนบริษัท กระทรวงพาณิชย์ที่ออกให้ไม่เกิน 6 เดือน</p>
<p><br></p>
<p><br></p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ขอแสดงความนับถือ</p>
<p>ส่วนบริหารสัญญา ฝ่ายจัดหาและการพัสดุ</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ธนาคารอาคารสงเคราะห์</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;โทร.02-2022105 , 02-2022109</p>`;
};

const buildOtherTypeTemplate = (): string => {
  const entrepreneurName = body.value.entrepreneurName || '';
  const contractName = body.value.contractName || '';
  const contractNumber = body.value.contractNumber || '';
  const contractSignedDate = ToTHDateFullMonthOnly(body.value.contractSignedDate);
  const returnAmount = formatCurrency(body.value.guaranteeReturn.returnAmount || 0);
  const bankName = body.value.guaranteeBankName || '';
  const bankBranch = body.value.guaranteeBankBranch || '';

  return `<p>เรียน ${entrepreneurName}</p>
<p><br></p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ตามที่ ${entrepreneurName} ได้ดำเนินการทำสัญญา${contractName} สัญญาเลขที่ จพ.(สบส.) ${contractNumber} ลงวันที่ ${contractSignedDate} กับ ธนาคารอาคารสงเคราะห์และได้วางหลักประกันสัญญาเป็นหนังสือค้ำประกันสัญญาของธนาคาร${bankName} สาขา${bankBranch} จำนวนเงิน ${returnAmount} บาท นั้น</p>
<p><br></p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;บัดนี้ สัญญาดังกล่าวได้พ้นภาระผูกพันเรียบร้อยแล้ว จึงขอแจ้งให้บริษัทฯ จัดเตรียมเอกสารดังนี้ เพื่อเข้ามารับหนังสือค้ำประกันสัญญา</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;1. หนังสือมอบอำนาจ พร้อมติดอากรแสตมป์ 10 บาท (ถ้าหากมีการมอบอำนาจ)</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2. สำเนาบัตรประชาชนของผู้มีอำนาจลงนามผูกพันบริษัทฯ/ห้างหุ้นส่วนจำกัด</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;3. สำเนาบัตรประชาชนของผู้ได้รับมอบอำนาจ</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;4. หนังสือรับรองสำนักงานทะเบียนหุ้นส่วนบริษัท กระทรวงพาณิชย์ที่ออกให้ไม่เกิน 6 เดือน</p>
<p><br></p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ทั้งนี้สามารถเข้ามารับหนังสือค้ำประกันได้ที่ธนาคารอาคารสงเคราะห์ สำนักงานใหญ่ อาคาร 2 เวลา 08.30 น. -16.30 น.</p>
<p><br></p>
<p><br></p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ขอแสดงความนับถือ</p>
<p>ส่วนบริหารสัญญา ฝ่ายจัดหาและการพัสดุ</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ธนาคารอาคารสงเคราะห์</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;โทร.02-2022105 , 02-2022109</p>`;
};

watch(() => props.modelValue, (newValue) => {
  if (newValue) {
    email.value = body.value.guaranteeReturn.emailSend || body.value.entrepreneurEmail || '';

    files.value = body.value.guaranteeReturn.emailAttachments
      ? [...body.value.guaranteeReturn.emailAttachments]
      : [];

    editorContent.value = body.value.guaranteeReturn.emailTemplate
      || (body.value.guaranteeReturn.guaranteeTypeCode === 'PBondType001'
        ? buildPBondType001Template()
        : buildOtherTypeTemplate());
  } else {
    email.value = '';
    editorContent.value = '';
    files.value = [];
  }
});
</script>

<template>
  <Dialog :visible="modelValue" modal header="ส่งอีเมลคืนหลักประกัน" :style="{ width: '100rem' }"
    @update:visible="(val) => emit('update:modelValue', val)">
    <div class="space-y-4">
      <div>
        <p class="font-bold mb-2">คู่ค้า</p>
        <p class="mb-2">{{ body.entrepreneurName }}</p>

        <label for="guarantee-email-input" class="block mb-2">
          E-mail<span class="text-red-500">*</span>
        </label>
        <input id="guarantee-email-input" v-model="email" type="email"
          class="w-full border-1 border-gray-300 rounded-md px-3 py-2" placeholder="E-mail" />
      </div>

      <div>
        <Editor v-model="editorContent" editorStyle="height: 320px" />
      </div>

      <div class="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center">
        <input type="file" id="guaranteeEmailFileUpload" class="hidden" multiple @change="onFileSelect" />
        <label for="guaranteeEmailFileUpload" class="cursor-pointer">
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
        <Button label="ยืนยันส่งเมล" icon="pi pi-send" severity="success" @click="onSendEmail" :disabled="!email" />
      </div>
    </template>
  </Dialog>
</template>

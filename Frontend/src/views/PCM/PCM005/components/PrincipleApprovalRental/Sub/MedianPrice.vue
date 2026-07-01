<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputNumber } from '@/components/forms';
import { useMenuStore } from '@/stores/menu';
import { usePcm005PrinApproveRentStore } from '@/stores/PCM/PCM005/principleApprovalRental';
import { ref, onMounted } from 'vue';
import draggable from 'vuedraggable';
import ST003Service from '@/services/file';
import { HttpStatusCode } from 'axios';
import ToastHelper from '@/helpers/toast';
import type { comparingAttachments } from '@/models/shared/uploadFile';

const menuStore = useMenuStore();
const store = usePcm005PrinApproveRentStore();

const inputFile = ref<HTMLElement>({} as HTMLElement);
const fileLists = ref<File[]>([]);

onMounted(() => {
  if (!store.body.comparingAttachments || !Array.isArray(store.body.comparingAttachments)) {
    store.body.comparingAttachments = [];
  }
});

const uploadFile = async (event: HTMLInputElement): Promise<void> => {
  const file = event.files![0];
  if (!file) return;

  fileLists.value.push(file);

  const { data, status } = await ST003Service.uploadFile(file);

  if (status === HttpStatusCode.Ok) {
    if (!store.body.comparingAttachments || !Array.isArray(store.body.comparingAttachments)) {
      store.body.comparingAttachments = [];
    }

    store.body.comparingAttachments.push({
      fileId: data.id,
      fileName: file.name,
      sequence: store.body.comparingAttachments.length + 1,
      isPublic: true,
    });

    if (store.body.id) {
      await store.updateAsync();
    } else {
      ToastHelper.success('สำเร็จ', 'อัปโหลดไฟล์สำเร็จ');
    }
  } else {
    fileLists.value.pop();
    ToastHelper.error('ไม่สำเร็จ', 'อัปโหลดไฟล์ไม่สำเร็จ');
  }
};

const removeFile = (index: number): void => {
  fileLists.value.splice(index, 1);

  if (store.body.comparingAttachments) {
    store.body.comparingAttachments.splice(index, 1);

    store.body.comparingAttachments.forEach((item, idx) => {
      item.sequence = idx + 1;
    });
  }
};

const downloadFileAsync = async (fileSelected: comparingAttachments): Promise<void> => {
  await ST003Service.downloadFile(fileSelected.fileId, fileSelected.fileName);
};
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="เอกสารแนบอ้างอิง" />
      <div class="bg-gray-100 p-2">
        รองรับไฟล์ที่มีนามสกุล .doc, .docx, .xls, .csv, .pdf, .png, .jpg, .jpeg และมีขนาดไม่เกิน 10 MB
      </div>
      <div class="mt-5 flex justify-between items-center">
        <p class="font-bold underline">เอกสารราคากลาง (ราคาอ้างอิง) แบบ บก.06 :
          รายละเอียดค่าใช้จ่ายการจัดซื้อจัดจ้างที่มิใช่งานก่อสร้าง
          <span class="text-red-500">*</span>
        </p>
        <Button severity="warn" @click="() => inputFile.click()" v-if="store.status.canEdit && menuStore.hasManage">
          <template #default>
            <i class="pi pi-upload"></i>
            อัปโหลด
          </template>
        </Button>
        <input type="file" class="hidden" ref="inputFile" @input="(e) => uploadFile(e.target as HTMLInputElement)" />
      </div>
      <draggable v-if="store.body.comparingAttachments && Array.isArray(store.body.comparingAttachments)"
        v-model="store.body.comparingAttachments" handle=".drag-handle" item-key="fileId">
        <template #item="{ element, index }">
          <div class="mt-5 flex justify-between">
            <div class="flex items-center gap-4">
              <i class="pi pi-file text-xl"></i>
              <p class="text-[#448AFF] underline cursor-pointer" @click="() => downloadFileAsync(element)">
                {{ element.fileName }}
              </p>
            </div>
            <div class="flex items-center gap-2" v-if="store.status.canEdit && menuStore.hasManage">
              <i class="pi pi-trash text-red-500 cursor-pointer" @click="() => removeFile(index)"></i>
              <span class="material-symbols-outlined drag-handle cursor-pointer">
                drag_indicator
              </span>
            </div>
          </div>
        </template>
      </draggable>

      <div class="grid grid-cols-3">
        <InputNumber label="ราคากลางหรือราคาอ้างอิง" v-model="store.body.referencePriceAmount" class="mt-8" grouping
          :min-fraction-digits="2" :disabled="!store.status.canEdit || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>

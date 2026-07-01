<script setup lang="ts">
import { Card, Button } from 'primevue';
import { useFileDialog } from '@vueuse/core'
import { TitleHeader } from '@/components/cosmetic';
import draggable from 'vuedraggable';
import { PreProcurementPP004FileType } from '@/enums/preProcurement';
import { ref, type Ref } from 'vue';
import type { UploadFileList } from '@/models/shared/uploadFile';
import { useMenuStore } from '@/stores/menu';

type Props = {
  isBelow: boolean;
}

const props = defineProps<Props>();
const uploadType = ref<PreProcurementPP004FileType | null>(null);

const prepareDocValue = defineModel<Array<UploadFileList>>("prepareDoc", {
  required: true,
  default: () => [],
});

const torDocValue = defineModel<Array<UploadFileList>>("torDoc", {
  required: true,
  default: () => [],
});

const quotationDocValue = defineModel<Array<UploadFileList>>("quotationDoc", {
  required: true,
  default: () => [],
});

const menuStore = useMenuStore();

const fileModels: Record<PreProcurementPP004FileType, Ref<Array<UploadFileList>>> = {
  [PreProcurementPP004FileType.Prepare]: prepareDocValue,
  [PreProcurementPP004FileType.Tor]: torDocValue,
  [PreProcurementPP004FileType.Quotation]: quotationDocValue,
};

const { open, onChange } = useFileDialog({
  multiple: true,
  accept:
    'application/msword, application/vnd.openxmlformats-officedocument.wordprocessingml.document, application/vnd.ms-excel, application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, text/csv, application/pdf, image/*',
});

const openDialogByType = (type: PreProcurementPP004FileType) => {
  uploadType.value = type;
  open();
};

onChange((files: FileList | null): void => {
  if (!files || !uploadType.value) {
    return;
  };
  //TODO: Implement Upload file and replacement params assignFile function.
  assignFile(files, uploadType.value);

  uploadType.value = null;
});

const assignFile = (files: FileList, type: PreProcurementPP004FileType): void => {
  const model = fileModels[type];

  const newFiles = Array.from(files).map((file, idx) => ({
    order: model.value.length + idx + 1,
    fileName: file.name,
    fileId: '',
  }));
  model.value = [...model.value, ...newFiles];
};

const deleteFileByType = (type: PreProcurementPP004FileType, index: number) => {
  const model = fileModels[type];
  model.value.splice(index, 1);
  model.value.forEach((item, idx) => {
    item.order = idx + 1;
  });
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="เอกสารแนบอ้างอิง " />
      <div class="mt-4">
        <div class="p-2 bg-gray-100">
          <small>รองรับไฟล์ที่มีนามสกุล .doc, .docx, .xls, .csv, .pdf, .png, .jpg, .jpeg และมีขนาดไม่เกิน
            10 MB</small>
        </div>
        <div class="mt-4">
          <div class="my-4">
            <div class="my-2">
              <div class="flex justify-between items-center gap-2 px-2">
                <p class="underline">
                  เอกสารขอแต่งตั้งบุคคล/คกก. จัดทำขอบเขตของงาน/ราคากลาง <span class="text-red-500">*</span>
                </p>
                <Button icon="pi pi-upload" label="อัปโหลด" severity="warn"
                  @click="() => openDialogByType(PreProcurementPP004FileType.Prepare)" v-if="menuStore.hasManage" />
              </div>
              <draggable v-model="prepareDocValue" handle=".drag-prepareDoc" group="prepareDoc" itemKey="order">
                <template
                  #item="{ element: prepareDoc, index: prepareDocIndex }: { element: UploadFileList, index: number }">
                  <div class="px-4">
                    <div class="flex items-center justify-between gap-2">
                      <div id="section-file">
                        <p class="text-blue-500 underline underline-offset-2 cursor-pointer">{{ prepareDoc.fileName }}
                        </p>
                      </div>
                      <div id="section-action" v-if="menuStore.hasManage">
                        <div class="flex items-center gap-2">
                          <Button icon="pi pi-trash" severity="danger" variant="text"
                            class="mt-2 bg-transparent transition hover:scale-110 duration-300"
                            @click="() => deleteFileByType(PreProcurementPP004FileType.Prepare, prepareDocIndex)" />
                          <span class="mt-1.75 material-symbols-outlined cursor-pointer text-gray-500 drag-prepareDoc">
                            drag_indicator
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </template>
              </draggable>
            </div>

            <div class="my-2">
              <div class="flex justify-between items-center gap-2 px-2">
                <p class="underline">
                  เอกสารขอบเขตของงาน (TOR)<span class="text-red-500">*</span>
                </p>
                <Button icon="pi pi-upload" label="อัปโหลด" severity="warn"
                  @click="() => openDialogByType(PreProcurementPP004FileType.Tor)" />
              </div>
              <draggable v-model="torDocValue" handle=".drag-torDoc" group="torDoc" itemKey="order">
                <template #item="{ element: torDoc, index: torIndex }: { element: UploadFileList, index: number }">
                  <div class="px-4">
                    <div class="flex items-center justify-between gap-2">
                      <div id="section-file">
                        <p class="text-blue-500 underline underline-offset-2 cursor-pointer">{{ torDoc.fileName }}
                        </p>
                      </div>
                      <div id="section-action">
                        <div class="flex items-center gap-2">
                          <Button icon="pi pi-trash" severity="danger" variant="text"
                            class="mt-2 bg-transparent transition hover:scale-110 duration-300"
                            @click="() => deleteFileByType(PreProcurementPP004FileType.Tor, torIndex)" />
                          <span class="mt-1.75 material-symbols-outlined cursor-pointer text-gray-500 drag-torDoc">
                            drag_indicator
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </template>
              </draggable>
            </div>

            <div class="my-2">
              <div class="flex justify-between items-center gap-2 px-2">
                <p class="underline">
                  {{ props.isBelow ? 'เอกสารกำหนดราคากลาง (ราคาอ้างอิง) ใบเสนอราคา' : `เอกสารกำหนดราคากลาง (ราคาอ้างอิง)
                  บก.01 -
                  บก.06` }}
                  <span class="text-red-500">*</span>
                </p>
                <Button icon="pi pi-upload" label="อัปโหลด" severity="warn"
                  @click="() => openDialogByType(PreProcurementPP004FileType.Quotation)" />
              </div>
              <draggable v-model="quotationDocValue" handle=".drag-quotationDoc" group="quotationDoc" itemKey="order">
                <template
                  #item="{ element: quotationDoc, index: quotationIndex }: { element: UploadFileList, index: number }">
                  <div class="px-4">
                    <div class="flex items-center justify-between gap-2">
                      <div id="section-file">
                        <p class="text-blue-500 underline underline-offset-2 cursor-pointer">{{ quotationDoc.fileName }}
                        </p>
                      </div>
                      <div id="section-action">
                        <div class="flex items-center gap-2">
                          <Button icon="pi pi-trash" severity="danger" variant="text"
                            class="mt-2 bg-transparent transition hover:scale-110 duration-300"
                            @click="() => deleteFileByType(PreProcurementPP004FileType.Quotation, quotationIndex)" />
                          <span
                            class="mt-1.75 material-symbols-outlined cursor-pointer text-gray-500 drag-quotationDoc">
                            drag_indicator
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </template>
              </draggable>
            </div>
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>
<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
  InputNumber,
  Select,
} from '@/components/forms';
import Button from 'primevue/button';
import draggable from 'vuedraggable';
import type { TAttacementFile, TAttachmentBase, TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';
import { ArrayHelper } from '@/helpers/array';
import UploadFileButton from '@/components/forms/UploadFileButton.vue';
import type { Option } from '@/models/shared/option';
import { ref, watch } from 'vue';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { TContractDraftStatus } from '@/views/PP/enums/pp010';
import contractDraftService from '@/views/PP/services/PP010/ContractDraftService';
import ST003Service from '@/services/file';
import { useMenuStore } from '@/stores/menu';
import { ConfirmDialogType } from '@/enums/dialog';
import { showConfirmDialogAsync } from '@/helpers/dialog';

type Props = {
  label: string;
  disable?: boolean;
};

const store = useContractDraftStore();
const menuStore = useMenuStore();

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });

const dropDown = defineModel<Option[]>("dropdown", { required: true })

const { addSequence, deleteItemAndReSequence, reSequence } = ArrayHelper();

watch(dropDown, (newVal) => {
  if (!newVal || newVal.length === 0) return;

  if (!body.value.detail.attachments || body.value.detail.attachments.length === 0) {
    const newAttachments = newVal
      .filter(option => option.value !== 'CAppendOther001')
      .map((option, index) =>
      ({
        typeCode: option.value as string,
        description: '',
        pageNumber: 0,
        files: [] as TAttacementFile[],
        sequence: index + 1,
      } as TAttachmentBase));

    body.value.detail.attachments = newAttachments;
  }
}, { deep: true, immediate: true });

const addAttachment = () => {
  if (!body.value.detail.attachments) {
    body.value.detail.attachments = [];
  }
  body.value.detail.attachments = addSequence(
    body.value.detail.attachments,
    { files: [] as TAttacementFile[] } as TAttachmentBase
  );
};

const deleteItem = (index: number): void => {
  body.value.detail.attachments = deleteItemAndReSequence(
    body.value.detail.attachments as TAttachmentBase[],
    index
  ) as TAttachmentBase[];
};

const reSequenceDrag = (): void => {
  if (!body.value.detail.attachments) return;
  body.value.detail.attachments = reSequence(body.value.detail.attachments);
};

const reorderFile = (docIndex: number): void => {

  if (!body.value.detail.attachments) return;

  body.value.detail.attachments[docIndex].files.map((item, index) => item.sequence = index + 1)
};

const onDeleteFile = (rowIndex: number, fileIndex: number): void => {
  const files = body.value.detail.attachments?.[rowIndex]?.files;
  if (!files) return;

  files.splice(fileIndex, 1);

  body.value.detail.attachments![rowIndex].files = files.map((f, i) => ({
    ...f,
    sequence: i + 1,
  }));

  updatePageNumber(body.value.detail.attachments![rowIndex]);
};

const updatePageNumber = (data: TAttachmentBase): void => {
  const totalPages = data.files.reduce((sum, file) => sum + (file.pageCount ?? 0), 0);
  data.pageNumber = totalPages;

  if (store.body.id) {
    store.api.onUpdateContractDraft(false);
  }
};

const isMerging = ref(false);

const mergeAndDownload = async () => {
  const attachments = body.value.detail.attachments
    .map(a => ({
      documentName: dropDown.value.find(d => d.value === a.typeCode)?.label ?? '',
      description: a.description,
      sequence: a.sequence,
      fileIds: a.files.map(f => f.fileId).filter((id): id is string => !!id)
    })) ?? [];

  if (attachments.length === 0) return;

  isMerging.value = true;
  try {
    await contractDraftService.mergeAttachmentsAsync({
      contractNumber: body.value.contractNumber ?? '',
      attachments
    });
  } finally {
    isMerging.value = false;
  }
};

const resetToDefault = async () => {
  if (!dropDown.value || dropDown.value.length === 0) return;

  const confirmed = await showConfirmDialogAsync(ConfirmDialogType.ConfirmChange);
  if (!confirmed) return;

  body.value.detail.attachments = dropDown.value
    .filter(option => option.value !== 'CAppendOther001')
    .map((option, index) => ({
      typeCode: option.value as string,
      description: '',
      pageNumber: 0,
      files: [] as TAttacementFile[],
      sequence: index + 1,
    } as TAttachmentBase));
};

const downloadFileAsync = async (fileSelected: TAttacementFile): Promise<void> => {
  await ST003Service.downloadFile(fileSelected.fileId, fileSelected.fileName);
};
</script>

<template>
  <Card v-if="body.detail.attachments"
    :pt="{ root: { 'data-section-id': 'part-of-contract', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label">
        <template #action>
          <div class="flex gap-2">
            <Button
              v-if="store.isHasPermission && menuStore.hasManage && store.body.status === TContractDraftStatus.Approved"
              label="บันทึกภาคผนวก" icon="pi pi-save" severity="success" variant="outlined"
              @click="store.api.onUpdateContractDraft(false)" />
            <Button label="พิมพ์เอกสารแนบ" icon="pi pi-print" severity="info" variant="outlined"
              @click="mergeAndDownload" :loading="isMerging" />
            <Button label="กำหนดค่าเริ่มต้น" icon="pi pi-refresh" severity="warn" variant="outlined"
              @click="resetToDefault" />
            <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
              @click="addAttachment" />
          </div>
        </template>
      </TitleHeader>

      <p v-if="body.detail.attachments.length == 0" class="text-center">ไม่พบข้อมูล</p>

      <draggable v-model="body.detail.attachments" group="rowData" class="mt-4" handle=".drag-handle"
        item-key="sequence" @end="reSequenceDrag">
        <template #item="{ element: data, index: rowIndex }">
          <div
            :class="['px-4 pt-8 pb-4 border-l-4', rowIndex % 2 === 0 ? 'bg-white border-l-primary-300' : 'bg-gray-100 border-l-gray-300']">
            <div class="flex w-full gap-3 items-end">
              <p class="mb-2 px-2 font-semibold">ผนวก {{ data.sequence }}</p>
              <Select :disabled="false" label="ชื่อเอกสาร" :options="dropDown" v-model="data.typeCode" hide-details
                rules="required" class="min-w-[280px] w-1/3" />

              <InputField v-if="data.typeCode === 'CAppendOther001'" class="w-1/6" :disabled="false"
                v-model="data.formatOtherName" label="ระบุชื่อผนวก" hide-details />

              <InputField class="flex-1" :disabled="false" v-model="data.description" label="ข้อความอ้างอิง"
                hide-details />

              <InputNumber class="w-1/6" :disabled="false" v-model="data.pageNumber" label="จำนวน(หน้า)" hide-details />

              <div class="flex items-center gap-2 shrink-0">
                <UploadFileButton v-model="data.files" class="w-auto whitespace-nowrap" accept=".pdf"
                  @update:modelValue="updatePageNumber(data)" />
                <i class="pi pi-trash cursor-pointer text-red-600 text-lg" @click="deleteItem(rowIndex)" />
                <span v-if="body.detail.attachments.length != 1"
                  class="material-symbols-outlined cursor-move drag-handle text-gray-400" title="ลากเพื่อจัดลำดับ">
                  drag_indicator
                </span>
              </div>
            </div>

            <draggable v-model="data.files" group="files" handle=".drag-handle" item-key="sequence"
              @end="reorderFile(rowIndex)" class="mt-3 pl-8 pr-16">
              <template #item="{ element: fileData, index: fileIndex }">
                <div class="flex justify-between items-center py-1.5 px-2 rounded hover:bg-gray-100 transition-colors">
                  <div class="flex items-center gap-3">
                    <p class="text-gray-500 min-w-[1.5rem] text-right">{{ fileData.sequence }}.</p>
                    <i class="pi pi-file-pdf text-red-400"></i>
                    <p class="underline text-blue-600 cursor-pointer hover:text-blue-800"
                      @click="() => downloadFileAsync(fileData)">
                      {{ fileData.fileName }}
                    </p>
                  </div>
                  <div class="flex items-center gap-2">
                    <i class="pi pi-trash cursor-pointer text-red-500"
                      @click="() => onDeleteFile(rowIndex, fileIndex)"></i>
                    <span class="material-symbols-outlined drag-handle cursor-move text-gray-400">
                      drag_indicator
                    </span>
                  </div>
                </div>
              </template>
            </draggable>
            <p v-if="data.files.length == 0" class="text-center text-gray-400 mt-2">ไม่พบข้อมูลเอกสาร</p>
          </div>
        </template>
      </draggable>
    </template>
  </Card>
</template>

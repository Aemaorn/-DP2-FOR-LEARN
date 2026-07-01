<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();

const openAttachmentFileAll = (): void => {
  const route = router.resolve({ name: 'attachment-file-all', params: { procurementId: props.id } });
  window.open(route.href, '_blank');
};
import { TitleHeader } from '../cosmetic';
import type { Attachments, FileAttachment } from '@/models/shared/uploadFile';
import type { Option } from '@/models/shared/option';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { ArrayHelper } from '@/helpers/array';
import { useMenuStore } from '@/stores/menu';
import draggable from 'vuedraggable';
import Select from './Select.vue';
import InputField from './InputField.vue';
import Checkbox from './Checkbox.vue';
import SharedService from '@/services/Shared/dropdown';
import ST003Service from '@/services/file';
import ToastHelper from '@/helpers/toast';
import FileHelper from '@/helpers/file';
import { useAuthenticationStore } from '@/stores/authentication';
import { showActivityDialog, showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { PlanDepartmentCode } from '@/enums/plan';

const props = defineProps({
  label: { type: String, default: 'เอกสารแนบ' },
  disabled: { type: Boolean, default: false },
  isShowActivityDialog: { type: Boolean, default: false },
  isShowLinkFileAll: { type: Boolean, default: false },
  id: { type: String, default: '' },
});

const authStore = useAuthenticationStore();
const { addSequence, reSequence } = ArrayHelper();
const attachments = defineModel<Attachments[]>({ default: () => [] });
const emit = defineEmits(['upload', 'removeFile', 'removeGroup', 'reorder']);
const { isFileSizeValid } = FileHelper;
const menuStore = useMenuStore();

const inputFile = ref<HTMLElement>({} as HTMLElement);
const indexSelected = ref();
const documentType = ref<Option[]>([]);

// Keep this in sync with backend FileValidationExtensions.ExtensionContentTypeMap
// and the support-text rendered above. The browser file picker filters by these,
// but uploadFileAsync still validates via the server because client filters can be bypassed.
const acceptedExtensions =
  '.doc,.docx,.xls,.xlsx,.csv,.pdf,.png,.jpg,.jpeg,' +
  'application/msword,' +
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document,' +
  'application/vnd.ms-excel,' +
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,' +
  'text/csv,application/pdf,image/png,image/jpeg';

onMounted(() => {
  getDocumentTypeAsync();
});

const getDocumentTypeAsync = async (): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.AttachmentType);

  if (status === HttpStatusCode.Ok) {
    documentType.value = data;
  }

  if (attachments.value.length === 0) {
    addDocumentType();
  }
};

const addDocumentType = (): void => {
  attachments.value = addSequence(attachments.value, {
    fileAttachments: [] as Array<FileAttachment>,
    documentTypeCode: documentType.value.length > 0
      ? String(documentType.value[0].value)
      : '',
  } as Attachments) as Attachments[];
};

const onUpload = (index: number): void => {
  if (!attachments.value[index].documentTypeCode) return;

  if (!attachments.value[index].fileAttachments) {
    attachments.value[index].fileAttachments = [];
  }

  inputFile.value.click();
  indexSelected.value = index;
};

const uploadFileAsync = async (event: HTMLInputElement): Promise<void> => {
  const index = indexSelected.value;

  if (event.files?.length === 0) return;
  const fileListType = event.files as FileList;
  const fileList = Array.from(fileListType);

  if (fileList.some(s => !isFileSizeValid(s))) {
    ToastHelper.errorDescription('มีไฟล์ที่ขนาดเกิน 10 MB');

    return;
  };

  const files = ref<Array<FileAttachment>>([]);

  for (const element of fileList) {
    const { data, status } = await ST003Service.uploadFile(element);

    if (status === HttpStatusCode.Ok) {
      const newFile = {
        fileId: data.id,
        fileName: element.name,
        isPublic: true,
        createdBy: data.createdBy,
      } as FileAttachment;

      files.value.push(newFile);

      attachments.value[index].fileAttachments = addSequence(attachments.value[index].fileAttachments, newFile);
    }
  }

  const fileEmit = files.value.length === 1 ? files.value[0] as FileAttachment : files.value as FileAttachment[];

  emit('upload', fileEmit);

  files.value = [];
  event.value = '';
};

const reorderFile = (docIndex: number, emitFn: boolean = true): void => {
  attachments.value[docIndex].fileAttachments = reSequence(attachments.value[docIndex].fileAttachments);

  if (emitFn) {
    emit('reorder');
  }
};

const removeDocument = async (docIndex: number): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  attachments.value.splice(docIndex, 1);

  emit('removeGroup');
};

const removeFileInDoc = async (docIndex: number, fileIndex: number) => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  attachments.value[docIndex].fileAttachments.splice(fileIndex, 1);

  if (attachments.value[docIndex].fileAttachments && attachments.value[docIndex].fileAttachments.length > 1) {
    reorderFile(docIndex, false);
  }

  emit('removeFile');
};

const downloadFileAsync = async (fileSelected: FileAttachment) => {
  if (!canAccessFile(fileSelected)) return;

  await ST003Service.downloadFile(fileSelected.fileId, fileSelected.fileName);
};

const canAccessFile = (file: FileAttachment): boolean => {
  return file.isPublic || file.createdBy === authStore.profile.id || authStore.profile.departmentCode === PlanDepartmentCode.JP;
};

const onSelectGroup = (index: number) => {
  if (!attachments.value[index].fileAttachments || attachments.value[index].fileAttachments.length <= 0) return;

  emit('upload');
};
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader :label="props.label">
        <template #action>
          <a
            v-if="props.isShowLinkFileAll"
            class="flex items-center gap-1 text-sm text-blue-500 underline cursor-pointer hover:text-blue-700"
            @click="openAttachmentFileAll"
          >
            <i class="pi pi-folder-open text-sm" />
            ไปยังเอกสารแนบทั้งหมด
          </a>
           <Button label="ประวัติเอกสารแนบ" v-if="props.isShowActivityDialog" icon="pi pi-refresh" variant="outlined"
            severity="primary" class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(props.id, undefined, 'ประวัติเอกสารแนบ')"/>
        </template>
      </TitleHeader>
      <div class="lg:flex items-center justify-between gap-3">
        <div class="bg-[#F5F5F5] p-2 flex-1">
          รองรับไฟล์ที่มีนามสกุล .doc, .docx, .xls, .xlsx, .csv, .pdf, .png, .jpg, .jpeg และมีขนาดไม่เกิน 10 MB
        </div>
        <Button class="lg:mt-0 mt-2 lg:w-fit w-full" variant="outlined" severity="primary" label="เพิ่มประเภทเอกสาร"
          icon="pi pi-plus" @click="addDocumentType" v-if="!disabled" />
      </div>
      <input type="file" class="hidden" ref="inputFile" @change="(e) => uploadFileAsync(e.target as HTMLInputElement)"
        :accept="acceptedExtensions" multiple />
      <div v-for="(document, docIndex) in attachments" :key="docIndex">
        <div class="mt-10">
          <div class="grid grid-cols-12 gap-5 items-center">
            <Select label="ประเภทเอกสาร" v-model="document.documentTypeCode" :options="documentType" class="col-span-11"
              rules="required" hide-details v-if="document.documentTypeCode !== 'other'"
              @on-select="() => onSelectGroup(docIndex)">
              <template #appendAction>
                <InputGroupAddon>
                  <Button label="อัปโหลด" class="rounded-none! text-white! bg-gray-500! border-none! h-full"
                    @click="() => onUpload(docIndex)" />
                </InputGroupAddon>
              </template>
            </Select>
            <div v-else class="col-span-11">
              <div class="grid grid-cols-12 gap-2">
                <Select label="ประเภทเอกสาร" v-model="document.documentTypeCode" :options="documentType"
                  class="col-span-3" rules="required" hide-details />
                <InputField label="ประเภทเอกสาร" class="col-span-9" hide-details model-value="" disabled>
                  <template #appendAction>
                    <InputGroupAddon>
                      <Button label="อัปโหลด" class="rounded-none! text-white! bg-gray-500! border-none! h-full"
                        @click="() => onUpload(docIndex)" />
                    </InputGroupAddon>
                  </template>
                </InputField>
              </div>
            </div>
            <div class="flex justify-end items-center gap-2 col-span-1">
              <Button icon="pi pi-trash" severity="danger" variant="text" size="small" @click="() => removeDocument(docIndex)" />
            </div>
            <draggable v-model="document.fileAttachments" group="files" handle=".drag-handle" class="col-span-12 mx-10"
              @end="() => reorderFile(docIndex)" itemKey="id">
              <template #item="{ element: file, index: fileIndex }">
                <div>
                  <div class="flex w-full items-center justify-between mt-4">
                    <div class="flex items-center gap-3 min-w-0">
                      <p class="shrink-0 w-5 text-right">{{ file.sequence }}.</p>
                      <Checkbox v-model="file.isPublic" :true-value="false" :false-value="true" label="เอกสารส่วนบุคคล"
                        class="shrink-0" hide-details :disabled="!menuStore.hasManage || file.createdBy !== authStore.profile.id"
                        @onChange="emit('upload')" />
                      <div class="w-px h-5 bg-gray-300 shrink-0"></div>
                      <div class="flex items-center gap-2 min-w-0">
                        <i class="pi pi-file shrink-0"></i>
                        <p class="truncate" :class="canAccessFile(file) ? 'text-blue-500 cursor-pointer underline' : ''"
                          @click="() => downloadFileAsync(file)">
                          {{ file.fileName }}
                        </p>
                      </div>
                    </div>
                    <div class="flex items-center gap-2 shrink-0" v-if="menuStore.hasManage && canAccessFile(file)">
                      <Button icon="pi pi-trash" severity="danger" variant="text" size="small"
                        @click="() => removeFileInDoc(docIndex, fileIndex)" />
                      <span class="material-symbols-outlined drag-handle cursor-move">
                        drag_indicator
                      </span>
                    </div>
                  </div>
                  <div class="h-px bg-gray-300 flex-1 mt-4" />
                </div>
              </template>
            </draggable>
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>
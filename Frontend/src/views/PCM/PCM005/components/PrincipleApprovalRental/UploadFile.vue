<script setup lang="ts">
import { onMounted, ref } from 'vue';
import type { Attachments, FileAttachment } from '@/models/shared/uploadFile';
import type { Option } from '@/models/shared/option';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { ArrayHelper } from '@/helpers/array';
import { useMenuStore } from '@/stores/menu';
import draggable from 'vuedraggable';
import SharedService from '@/services/Shared/dropdown';
import ST003Service from '@/services/file';
import ToastHelper from '@/helpers/toast';
import FileHelper from '@/helpers/file';
import { useAuthenticationStore } from '@/stores/authentication';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { PlanDepartmentCode } from '@/enums/plan';

const props = defineProps({
  label: { type: String, default: 'เอกสารแนบ' },
  disabled: { type: Boolean, default: false },
});

const authStore = useAuthenticationStore();
const { addSequence, reSequence } = ArrayHelper();
const attachment = defineModel<Attachments>({
  default: () => ({
    fileAttachments: [],
    documentTypeCode: 'comparingDoc',
    sequence: 1
  } as Attachments)
});
const emit = defineEmits(['upload', 'removeFile', 'removeGroup', 'reorder']);
const { isFileSizeValid } = FileHelper;
const menuStore = useMenuStore();

const inputFile = ref<HTMLElement>({} as HTMLElement);
const documentType = ref<Option[]>([]);

onMounted(() => {
  getDocumentTypeAsync();
});

const getDocumentTypeAsync = async (): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(
    EGroupCode.AttachmentType
  );

  if (status === HttpStatusCode.Ok) {
    documentType.value = data;

    if (attachment.value && data.length > 0) {
      attachment.value.documentTypeCode = String(data[0].value);
    }
  }
};

const onUpload = (): void => {
  if (!attachment.value.fileAttachments) {
    attachment.value.fileAttachments = [];
  }

  inputFile.value.click();
};

const uploadFileAsync = async (event: HTMLInputElement): Promise<void> => {
  if (event.files?.length === 0) return;
  const fileListType = event.files as FileList;
  const fileList = Array.from(fileListType);

  if (fileList.some((s) => !isFileSizeValid(s))) {
    ToastHelper.errorDescription('มีไฟล์ที่ขนาดเกิน 10 MB');

    return;
  }

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

      console.log('newFile => ', newFile)

      attachment.value.fileAttachments = addSequence(
        attachment.value.fileAttachments,
        newFile
      );
    }
  }

  const fileEmit =
    files.value.length === 1
      ? (files.value[0] as FileAttachment)
      : (files.value as FileAttachment[]);

  emit('upload', fileEmit);

  files.value = [];
  event.value = '';
};

const reorderFile = (emitFn: boolean = true): void => {
  attachment.value.fileAttachments = reSequence(
    attachment.value.fileAttachments
  );

  if (emitFn) {
    emit('reorder');
  }
};

const removeFileInDoc = async (fileIndex: number): Promise<void> => {
  if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;

  attachment.value.fileAttachments.splice(fileIndex, 1);

  if (
    attachment.value.fileAttachments &&
    attachment.value.fileAttachments.length > 1
  ) {
    reorderFile(false);
  }

  emit('removeFile');
};

const downloadFileAsync = async (fileSelected: FileAttachment): Promise<void> => {
  if (
    (!fileSelected.isPublic && fileSelected.createdBy !== authStore.profile.id) ||
    (!fileSelected.isPublic && authStore.profile.departmentCode !== PlanDepartmentCode.JP)
  )
    return;

  await ST003Service.downloadFile(fileSelected.fileId, fileSelected.fileName);
};
</script>

<template>
  <Card>
    <template #content>
      <div class="lg:flex items-center justify-between gap-3">
        <div class="bg-[#F5F5F5] p-2 flex-1">
          รองรับไฟล์ที่มีนามสกุล .doc, .docx, .xls, .csv, .pdf, .png, .jpg, .jpeg และมีขนาดไม่เกิน
          10 MB
        </div>
      </div>
      <input
        type="file"
        class="hidden"
        ref="inputFile"
        @change="(e) => uploadFileAsync(e.target as HTMLInputElement)"
        multiple
      />
      <div>
        <div class="mt-7">
          <div class="grid grid-cols-12 gap-5 items-center">
            <div class="col-span-11">
              <div class="flex items-center">
                <div class="flex-1 p-2 border border-gray-300 rounded-l bg-gray-50">
                  แนบตารางเปรียบเทียบสัญญาเดิม และประมาณการค่าเช่าตามสัญญาใหม่
                </div>
                <InputGroupAddon v-if="!props.disabled && menuStore.hasManage">
                  <Button
                    label="อัปโหลด"
                    class="rounded-none! text-white! bg-gray-500! border-none!"
                    @click="() => onUpload()"
                  />
                </InputGroupAddon>
              </div>
            </div>
            <draggable
              v-model="attachment.fileAttachments"
              group="files"
              handle=".drag-handle"
              class="col-span-12 mx-10"
              @end="() => reorderFile()"
              itemKey="sequence"
            >
              <template #item="{ element: file, index: fileIndex }">
                <div>
                  <div class="flex w-full items-center justify-between mt-4">
                    <div class="flex items-center gap-10">
                      <p>{{ file.sequence }}.</p>
                      <div class="flex items-center gap-2">
                        <i class="pi pi-file"></i>
                        <p
                          :class="`${file.isPublic || (!file.isPublic && file.createdBy === authStore.profile.id) || (!file.isPublic && authStore.profile.departmentCode === PlanDepartmentCode.JP) ? 'text-blue-500 cursor-pointer underline' : ''}`"
                          @click="() => downloadFileAsync(file)"
                        >
                          {{ file.fileName }}
                        </p>
                      </div>
                    </div>
                    <div
                      class="flex justify-end items-center gap-2 col-span-1"
                      v-if="
                        !props.disabled &&
                        menuStore.hasManage &&
                        (file.isPublic ||
                          (!file.isPublic && file.createdBy === authStore.profile.id) ||
                          (!file.isPublic &&
                            authStore.profile.departmentCode === PlanDepartmentCode.JP))
                      "
                    >
                      <i
                        class="pi pi-trash text-red-500 text-lg cursor-pointer"
                        @click="() => removeFileInDoc(fileIndex)"
                      ></i>
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

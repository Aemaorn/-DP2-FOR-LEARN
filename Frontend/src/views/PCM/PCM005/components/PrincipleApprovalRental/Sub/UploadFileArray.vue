<script setup lang="ts">
import { ref } from 'vue';
import { HttpStatusCode } from 'axios';
import { ArrayHelper } from '@/helpers/array';
import { useMenuStore } from '@/stores/menu';
import draggable from 'vuedraggable';
import ST003Service from '@/services/file';
import ToastHelper from '@/helpers/toast';
import FileHelper from '@/helpers/file';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import type { comparingAttachments } from '@/models/shared/uploadFile';

const props = defineProps({
  disabled: { type: Boolean, default: false },
});

const { addSequence, reSequence } = ArrayHelper();
const fileAttachments = defineModel<comparingAttachments[]>({
  default: () => []
});
const emit = defineEmits(['upload', 'removeFile', 'removeGroup', 'reorder']);
const { isFileSizeValid } = FileHelper;
const menuStore = useMenuStore();

const inputFile = ref<HTMLElement>({} as HTMLElement);

const onUpload = (): void => {
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

  const files = ref<Array<comparingAttachments>>([]);

  for (const element of fileList) {
    const { data, status } = await ST003Service.uploadFile(element);

    if (status === HttpStatusCode.Ok) {
      const newFile = {
        fileId: data.id,
        fileName: element.name,
        isPublic: true,
      } as comparingAttachments;

      files.value.push(newFile);

      fileAttachments.value = addSequence(
        fileAttachments.value,
        newFile
      );
    }
  }

  const fileEmit =
    files.value.length === 1
      ? (files.value[0] as comparingAttachments)
      : (files.value as comparingAttachments[]);

  emit('upload', fileEmit);

  files.value = [];
  event.value = '';
};

const reorderFile = (emitFn: boolean = true): void => {
  fileAttachments.value = reSequence(fileAttachments.value);

  if (emitFn) {
    emit('reorder');
  }
};

const removeFileInDoc = async (fileIndex: number): Promise<void> => {
  if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;

  fileAttachments.value.splice(fileIndex, 1);

  if (fileAttachments.value && fileAttachments.value.length > 1) {
    reorderFile(false);
  }

  emit('removeFile');
};

const downloadFileAsync = async (fileSelected: comparingAttachments): Promise<void> => {
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
      <input type="file" class="hidden" ref="inputFile" @change="(e) => uploadFileAsync(e.target as HTMLInputElement)"
        multiple />
      <div>
        <div class="mt-7">
          <div class="grid grid-cols-12 gap-5 items-center">
            <div class="col-span-11">
              <div class="flex items-center">
                <div class="flex-1 p-2 border border-gray-300 rounded-l bg-gray-50">
                  แนบตารางเปรียบเทียบสัญญาเดิม และประมาณการค่าเช่าตามสัญญาใหม่
                </div>
                <InputGroupAddon v-if="!props.disabled && menuStore.hasManage">
                  <Button label="อัปโหลด" class="rounded-none! text-white! bg-gray-500! border-none!"
                    @click="() => onUpload()" />
                </InputGroupAddon>
              </div>
            </div>
            <draggable v-model="fileAttachments" group="files" handle=".drag-handle" class="col-span-12 mx-10"
              @end="() => reorderFile()" itemKey="sequence">
              <template #item="{ element: file, index: fileIndex }">
                <div>
                  <div class="flex w-full items-center justify-between mt-4">
                    <div class="flex items-center gap-10">
                      <p>{{ file.sequence }}.</p>
                      <div class="flex items-center gap-2">
                        <i class="pi pi-file"></i>
                        <p class="text-blue-500 cursor-pointer underline" @click="() => downloadFileAsync(file)">
                          {{ file.fileName }}
                        </p>
                      </div>
                    </div>
                    <div class="flex justify-end items-center gap-2 col-span-1"
                      v-if="!props.disabled && menuStore.hasManage">
                      <i class="pi pi-trash text-red-500 text-lg cursor-pointer"
                        @click="() => removeFileInDoc(fileIndex)"></i>
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

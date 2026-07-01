<script setup lang="ts" generic="T">
import { useDropZone, useFileDialog } from '@vueuse/core';
import { Field } from 'vee-validate';
import { v4 as uuidv4 } from 'uuid';
import { ref } from 'vue';
import { TitleHeader } from '../cosmetic';
import ToastHelper from '@/helpers/toast';
import FileHelper from '@/helpers/file';

// Default attachment whitelist — must mirror backend FileValidationExtensions.ExtensionContentTypeMap.
// Avoid 'image/*' here since that lets .webp / .svg / .gif through the picker; list the
// allowed image MIME types explicitly.
const listAcceptFile = [
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    'application/vnd.ms-excel',
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    'text/csv',
    'application/pdf',
    'image/png',
    'image/jpeg',
  ];

type Props = {
  name?: string;
  id?: string;
  rules?: string;
  label?: string;
  disabled?: boolean;
  multiple?: boolean;
  fileKey?: string;
  accept?: string[];
  supportText?: string;
};

const { isFileSizeValid, isFileTypeValid, getFileExtensions } = FileHelper;
const dropZoneRef = ref<HTMLDivElement>();
const props = defineProps<Props>();
const value = defineModel<T | T[] | undefined>({
  required: true,
});
const emit = defineEmits(['onChange']);

const key = uuidv4();
const name = ref(props.name ?? key);
const fileKey = props.fileKey ?? 'file';

const { open, onChange } = useFileDialog({
  multiple: props.multiple ?? false,
  accept: props.accept ? props.accept.join(', ') : listAcceptFile.join(', '),
});

useDropZone(dropZoneRef, {
  onDrop,
  dataTypes: props.accept ?? listAcceptFile,
  multiple: props.multiple ?? false,
  preventDefaultForUnhandled: false,
});

onChange((files) => {
  if (!files) {
    return;
  }

  const filesArray: File[] = [...files];
  const acceptedTypes = props.accept ?? listAcceptFile;

  const invalidSizeFiles = filesArray.filter((file): boolean => !isFileSizeValid(file));
  const invalidTypeFiles = filesArray.filter((file): boolean => !isFileTypeValid(file, acceptedTypes));
  const validFiles = filesArray.filter((file): boolean =>
    isFileSizeValid(file) && isFileTypeValid(file, acceptedTypes)
  );

  if (invalidSizeFiles.length > 0) {
    ToastHelper.errorDescription('ขนาดไฟล์เกิน 10 MB');
  }

  if (invalidTypeFiles.length > 0) {
    const acceptError = getFileExtensions(acceptedTypes);

    ToastHelper.errorDescription(`รองรับเฉพาะไฟล์ที่มีนามสกุล ${acceptError} เท่านั้น`);
  }

  if (validFiles.length === 0) {
    return;
  }

  const mapped = validFiles.map((file): unknown => {
    return {
      [fileKey]: file,
    };
  }) as T[];

  if (props.multiple) {
    const current = (value.value as T[]) ?? [];
    value.value = [...current, ...mapped] as T[];
    emit('onChange', value.value);

    return;
  }

  value.value = mapped[0] as T;
  emit('onChange', value.value);
});

function onDrop(files: File[] | null) {
  if (!files) {
    return;
  }

  const filesArray: File[] = [...files];
  const acceptedTypes = props.accept ?? listAcceptFile;

  const invalidSizeFiles = filesArray.filter((file): boolean => !isFileSizeValid(file));
  const invalidTypeFiles = filesArray.filter((file): boolean => !isFileTypeValid(file, acceptedTypes));
  const validFiles = filesArray.filter((file): boolean =>
    isFileSizeValid(file) && isFileTypeValid(file, acceptedTypes)
  );

  if (invalidSizeFiles.length > 0) {
    ToastHelper.errorDescription('ขนาดไฟล์เกิน 10 MB');
  }

  if (invalidTypeFiles.length > 0) {
    const acceptError = getFileExtensions(acceptedTypes);

    ToastHelper.errorDescription(`รองรับเฉพาะไฟล์ที่มีนามสกุล ${acceptError} เท่านั้น`);
  }

  if (validFiles.length === 0) {
    return;
  }

  const mapped = validFiles.map((file): unknown => {
    return {
      [fileKey]: file,
    };
  }) as T[];

  if (props.multiple) {
    const current = (value.value as T[]) ?? [];
    value.value = [...current, ...mapped] as T[];

    emit('onChange', value.value);
    return;
  }

  value.value = mapped[0] as T;
  emit('onChange', value.value);
};
</script>

<template>
  <Field v-model="value" :name="name" :rules="rules" v-slot="{ errorMessage }">
    <Card>
      <template #content>
        <div class="my-2">
          <TitleHeader :label="props.label ?? 'เอกสารแนบ'" />
        </div>
        <div @click="() => open()" ref="dropZoneRef"
          class="border-2 border-dashed min-h-72 mx-10 lg:mx-40 relative"
          :class="`${errorMessage ? 'border-red-500' : 'border-gray-300'}`">
          <div class="absolute inset-0 flex items-center justify-center">
            <div class="flex flex-col items-center justify-center">
              <span class="material-symbols-outlined text-7xl! text-gray-400"> cloud_upload </span>
              <p class="font-bold">Drag and drop or browse.</p>
              <small>
                {{ props.supportText ??
                  `รองรับไฟล์ที่มีนามสกุล .doc, .docx, .xls, xlsx, .csv, .pdf, .png, .jpg, .jpeg
                และมีขนาดไม่เกิน 10 MB` }}
              </small>
              <small class="text-red-500">
                {{ errorMessage }}
              </small>
            </div>
          </div>
        </div>
      </template>

      <template #footer>
        <div class="mx-10 lg:mx-40">
          <template v-if="props.multiple">
            <slot name="fileList" v-for="(item, index) in value as T[]" :item="item" :index="index" :key="index">
            </slot>
          </template>
          <template v-else>
            <slot name="file" :item="value as T"></slot>
          </template>
        </div>
        <slot name="extraRender"></slot>
      </template>
    </Card>
  </Field>
</template>

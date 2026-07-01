<script setup lang="ts">
import ST003Service from '@/services/file';
import type { TAttacementFile } from '@/views/PP/models/PP0010/ContractDraft';
import { HttpStatusCode } from 'axios';
import { ref } from 'vue';

type Prop = {
  disabled?: boolean;
  accept?: string;
};

const props = defineProps<Prop>();
const value = defineModel<TAttacementFile[]>({ default: () => [] });

const dropFileRef = ref<HTMLInputElement | null>(null);

const onUploadImage = (): void => {
  if (props.disabled) return;
  dropFileRef.value?.click();
};

const onChange = async (event: Event): Promise<void> => {
  const input = event.target as HTMLInputElement;
  const files = input.files;
  if (!files?.length) return;

  const { data, status } = await ST003Service.uploadFile(files[0]);

  if (status === HttpStatusCode.Ok) {
    const fileRaw = files[0];

    const file: TAttacementFile = {
      fileId: data.id,
      fileName: fileRaw.name,
      fileType: fileRaw.type,
      sequence: value.value.length + 1,
      pageCount: data.pageCount,
    };

    value.value = [...value.value, file];
  }

  input.value = '';
};
</script>

<template>
  <div class="grid grid-cols-1 gap-2 relative">
    <Button label="แนบเอกสาร" icon="pi pi-paperclip" severity="primary" variant="outlined" class="w-full"
      @click="onUploadImage" />

    <input type="file" class="hidden" ref="dropFileRef" :accept="props.accept" @change="onChange" />
  </div>
</template>

<script setup lang="ts" generic="T">
import { InputField } from '@/components/forms';
import { useFileDialog } from '@vueuse/core'
import { ref, watch } from 'vue'
import FileHelper from '@/helpers/file';
import ToastHelper from '@/helpers/toast';

type Props = {
  name?: string;
  fileName?: string;
  id?: string;
  rules?: string;
  label?: string;
  disabled?: boolean;
  multiple?: boolean;
  fileKey?: string;
  required?: boolean;
  helperText?: string;
  clearable?: boolean;
  showRequired?: boolean;
}

const props = defineProps<Props>()
const value = defineModel<File | undefined>({
  required: true,
})
const emit = defineEmits(['onChange', 'clear'])
const { isFileSizeValid } = FileHelper;
const fileName = ref(props.fileName ?? 'x')
const fileKey = props.fileKey ?? 'file'
const defaultFileName = props.fileName ?? 'x'

function onClear() {
  value.value = undefined
  fileName.value = defaultFileName
  emit('clear')
}

const { open, onChange } = useFileDialog({
  multiple: props.multiple ?? false,
  accept: 'application/pdf',
})

watch(() => props.fileName, (newValue) => {
  fileName.value = newValue ?? ''
})

onChange((files) => {
  if (!files) {
    return
  }
  const filesArray: File[] = [...files]

  if (filesArray.some(s => !isFileSizeValid(s))) {
    return ToastHelper.errorDescription('ขนาดไฟล์เกิน 10 MB');
  }
  const mapped = filesArray.map((file) => {
    return {
      [fileKey]: file,
    }
  })

  emit('onChange', mapped[0].file)
  value.value = mapped[0].file
  fileName.value = mapped[0].file.name
})
</script>

<template>
  <InputField v-model="fileName" :label="props.label" @click.stop="(e: any) => open()" :rules="props.rules" readonly
    :helperText :showRequired="props.showRequired">
    <template #appendAction>
      <InputGroupAddon v-if="props.clearable">
        <Button icon="pi pi-times" class="h-full rounded-none! border-none! text-gray-500! bg-white!"
          @click.stop="onClear" />
      </InputGroupAddon>
      <InputGroupAddon>
        <Button label="อัปโหลด" class="h-full rounded-none! text-white! bg-gray-500! border-none!"
          @click.stop="() => open()" />
      </InputGroupAddon>
    </template>
  </InputField>
</template>

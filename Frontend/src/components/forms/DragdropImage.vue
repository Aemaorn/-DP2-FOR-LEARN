<script setup lang="ts">
import { Field } from 'vee-validate';
import { v4 as uuidv4 } from 'uuid';
import { ref, watch } from 'vue';
import { UseImage } from '@vueuse/components';
import FileHelper from '@/helpers/file';
import ST003Service from '@/services/file';
import { HttpStatusCode } from 'axios';

type Prop = {
  label?: string;
  name?: string;
  id?: string;
  rules?: string;
  disabled?: boolean;
  helperText?: string;
};

const props = defineProps<Prop>();
const value = defineModel<string>();

const dropFileRef = ref<HTMLDivElement>({} as HTMLInputElement);

const key = uuidv4();
const name = ref(props.name ?? key);
const previewImage = ref();

watch(
  () => value.value,
  (imgValue?: string) => {
    if (imgValue) {
      previewImage.value = FileHelper.getFileUrl(imgValue);
    }
  },
);

const onChange = async (event: Event): Promise<void> => {
  const input = event.target as HTMLInputElement;
  const files = input.files;

  if (!files?.length) return;

  const { data, status } = await ST003Service.uploadFile(files[0]);

  if (status === HttpStatusCode.Ok) {
    value.value = data.id;
  }

  input.value = '';
}

const onUploadImage = (): void => {
  if (!props.disabled) {
    dropFileRef.value.click();
  }
};

const onRemoveItem = (): void => {
  previewImage.value = null;
  value.value = undefined;
};
</script>

<template>
  <Field v-model="value" :name="name" :rules="rules" v-slot="{ errorMessage }" as="div">
    <p>
      {{ props.label }}
      <span v-if="rules && rules.includes('required')" class="text-red-500">*</span>
    </p>
    <div>
      <div v-if="!previewImage" class="border-1 border-dashed min-h-72 relative"
        :class="`${errorMessage ? 'border-red-500' : 'border-gray-500'}`" ref="dropFileRef" @click="onUploadImage">
        <div class="absolute inset-0 flex items-center justify-center">
          <div class="flex flex-col items-center">
            <i class="pi pi-image text-4xl!" :class="`${errorMessage ? 'text-red-500' : 'text-black'}`"></i>
          </div>
        </div>
      </div>
      <div>
        <UseImage :src="previewImage" :alt="name" class="" v-if="previewImage">
          <template #loading> Loading.. </template>

          <template #default>
            <Image :src="previewImage" :alt="name" class="max-h-72 rounded-lg bg-gray-100"
              image-class="border-1 w-full rounded-lg border-gray-300 object-contain">
              <template #image>
                <div class="relative inline-flex bg-gray-100 max-h-72 rounded-lg">
                  <img :src="previewImage" :alt="name"
                    class="border-1 w-full rounded-lg border-gray-300 object-contain" />
                  <span @click="() => onRemoveItem()"
                    class="absolute right-1.5 top-1.5 material-symbols-outlined text-white bg-red-500 rounded-2xl p-1 cursor-pointer hover:scale-110 duration-25 text-center">delete</span>
                </div>
              </template>
            </Image>
          </template>
        </UseImage>
      </div>
      <input type="file" accept="image/*" class="hidden" ref="dropFileRef" @change="onChange" />
    </div>
    <small class="pl-1 text-gray-500!" v-if="props.helperText">
      {{ props.helperText }}
    </small>
    <small class="pl-1 text-red-500!">
      {{ errorMessage }}
    </small>
  </Field>
</template>

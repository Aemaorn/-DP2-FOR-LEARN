<script setup lang="ts">
import { ButtonSave } from '@/components/Button';
import { TitleHeader } from '@/components/cosmetic';
import { InputArea, Select } from '@/components/forms';
import { ConfirmDialogType } from '@/enums/dialog';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import { usePcm005DetailStore } from '@/stores/PCM/PCM005/pcm005';
import { Button, Dialog } from 'primevue';
import { Form } from 'vee-validate';
import { ref, watch } from 'vue';

const show = defineModel('show', { type: Boolean, required: true, default: false });

const store = usePcm005DetailStore();

type EditForm = {
  departmentCode?: string;
  planName?: string;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
};

const form = ref<EditForm>({});

const syncFormFromStore = (): void => {
  form.value = {
    departmentCode: store.body.departmentCode,
    planName: store.body.planName,
    supplyMethodCode: store.body.supplyMethodCode,
    supplyMethodTypeCode: store.body.supplyMethodTypeCode,
    supplyMethodSpecialTypeCode: store.body.supplyMethodSpecialTypeCode,
  };
};

const onChangeSMCodeAsync = async (code: string): Promise<void> => {
  store.smSpTypeCodeDDL = [];

  await store.getsmSpTypeCodeDDLAsync(code);

  form.value.supplyMethodTypeCode = undefined;
  form.value.supplyMethodSpecialTypeCode = undefined;
};

const onClose = (): void => {
  show.value = false;
};

const onSubmitAsync = async (): Promise<void> => {
  const id = store.body.id;

  if (!id) return;

  if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData)) return;

  store.body = {
    ...store.body,
    departmentCode: form.value.departmentCode ?? store.body.departmentCode,
    planName: form.value.planName,
    supplyMethodCode: form.value.supplyMethodCode,
    supplyMethodTypeCode: form.value.supplyMethodTypeCode,
    supplyMethodSpecialTypeCode: form.value.supplyMethodSpecialTypeCode,
  };

  await store.updateAsync(id);

  show.value = false;
};

watch(() => show.value, async (newValue) => {
  if (!newValue) return;

  syncFormFromStore();

  if (store.body.supplyMethodCode) {
    await store.getsmSpTypeCodeDDLAsync(store.body.supplyMethodCode);
  }
});
</script>

<template>
  <Dialog v-model:visible="show" modal :style="{ width: '60vw' }" :draggable="false" :breakpoints="{ '575px': '90vw' }">
    <template #container>
      <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" class="p-5 overflow-auto">
        <TitleHeader label="แก้ไขข้อมูล Procurement">
          <template #action>
            <i class="pi pi-times cursor-pointer" @click="onClose"></i>
          </template>
        </TitleHeader>
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-2 gap-y-8 mt-8">
          <Select label="ฝ่าย/ภาคเขต" :options="store.departmentDDL" rules="required"
            v-model="form.departmentCode" />
          <InputArea label="เรื่อง" rules="required" class="lg:col-span-2 lg:col-start-1 col-start-auto"
            v-model="form.planName" />
          <Select label="วิธีจัดหา" :options="store.smCodeDDL" class="col-start-auto lg:col-start-1"
            v-model="form.supplyMethodCode" @on-select="onChangeSMCodeAsync" rules="required" />
          <Select label="ประเภทวิธีจัดหา" :options="store.smTypeCodeDDL"
            v-model="form.supplyMethodTypeCode" />
          <Select label="วิธีจัดหาแบบพิเศษ" :options="store.smSpTypeCodeDDL"
            v-model="form.supplyMethodSpecialTypeCode" />
        </div>
        <div class="mt-8 flex gap-2 justify-end items-center">
          <Button variant="outlined" severity="contrast" label="ยกเลิก" @click="onClose" />
          <ButtonSave type="submit" text="บันทึก" />
        </div>
      </Form>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue';
import { useRoute } from 'vue-router';
import { Form } from 'vee-validate';
import type { MenuItem } from 'primevue/menuitem';
import { InputField, Select } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { useSt013DetailStore } from '@/stores/ST/st013';
import ToastHelper from '@/helpers/toast';

const route = useRoute();
const detailStore = useSt013DetailStore();

const isEditMode = computed((): boolean => Boolean(route.params.id));

const routeItems = computed((): MenuItem[] => [
  { label: 'ตำบล/แขวง', url: '/st/st013' },
  { label: isEditMode.value ? 'ตำบล/แขวง (แก้ไข)' : 'ตำบล/แขวง (เพิ่ม)' },
]);

onMounted(async (): Promise<void> => {
  await detailStore.onFetchProvinceOptions();

  if (isEditMode.value) {
    await detailStore.onGetByIdAsync(route.params.id as string);
    return;
  }

  await detailStore.onInitCreate();
});

onUnmounted((): void => {
  detailStore.onResetBody();
});

const onSubmitAsync = async (): Promise<void> => {
  await detailStore.onSubmitAsync(route.params.id as string | undefined);
};
</script>

<template>
  <Form @submit="onSubmitAsync" @invalid-submit="() => ToastHelper.invalidMessageToast()">
    <TitleHeader :label="isEditMode ? 'แก้ไขตำบล/แขวง' : 'เพิ่มตำบล/แขวง'" :route-items="routeItems">
      <template #breadcrumbAction>
        <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit"
          :loading="detailStore.isSubmitting" :disabled="detailStore.isSubmitting" />
      </template>
    </TitleHeader>
    <Card class="my-4">
      <template #content>
        <div class="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-6 gap-2 mt-8">
          <Select label="จังหวัด" v-model="detailStore.body.provinceId" :options="detailStore.provinceOptions"
            rules="required" :disabled="isEditMode" @update:model-value="detailStore.onChangeProvince" />
          <Select label="อำเภอ/เขต" v-model="detailStore.body.districtId" :options="detailStore.districtOptions"
            rules="required" :disabled="isEditMode || !detailStore.body.provinceId" />
          <InputField label="รหัส" v-model="detailStore.body.code" rules="required" disabled />
          <InputField label="ตำบล/แขวง" v-model="detailStore.body.nameTh" rules="required" />
          <InputField label="ตำบล/แขวง (EN)" v-model="detailStore.body.nameEn" rules="required" />
          <InputField label="รหัสไปรษณีย์" v-model="detailStore.body.postalCode" rules="required" />
        </div>
      </template>
    </Card>
  </Form>
</template>

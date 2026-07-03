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
        <div class="mt-8 space-y-4">
          <div class="grid grid-cols-1 md:grid-cols-3 gap-2">
            <Select label="จังหวัด" v-model="detailStore.body.provinceCode" :options="detailStore.provinceOptions"
              rules="required" :disabled="isEditMode" @update:model-value="detailStore.onChangeProvince" />
          </div>
          <div class="grid grid-cols-1 md:grid-cols-3 gap-2">
            <Select label="อำเภอ/เขต" v-model="detailStore.body.districtCode" :options="detailStore.districtOptions"
              rules="required" :disabled="isEditMode || !detailStore.body.provinceCode"
              @update:model-value="detailStore.onChangeDistrict" />
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-8">
            <InputField label="ตำบล/แขวง" v-model="detailStore.body.nameTh" rules="required" />
            <InputField label="ตำบล/แขวง (EN)" v-model="detailStore.body.nameEn" rules="required" />
          </div>
          <div class="grid grid-cols-1 md:grid-cols-3 gap-2">
            <InputField label="รหัสไปรษณีย์" v-model="detailStore.body.postalCode" rules="required" />
          </div>
        </div>
      </template>
    </Card>
  </Form>
</template>

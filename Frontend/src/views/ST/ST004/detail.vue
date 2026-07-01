<script setup lang="ts">
import { useRoute } from 'vue-router';
import type { MenuItem } from 'primevue/menuitem';
import { Checkbox as PrimeCheckBox } from 'primevue';
import { InputField, Checkbox } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { Form as VeeForm } from 'vee-validate';
import { computed, onBeforeMount, onMounted, ref } from 'vue';
import { useSt004DetailStore } from '@/stores/ST/st004';
import ToastHelper from '@/helpers/toast';
import { useAuthenticationStore } from '@/stores/authentication';
import { useMenuStore } from '@/stores/menu';
import type { Role } from '@/models/authentication';

const route = useRoute();
const detailStore = useSt004DetailStore();
const authStore = useAuthenticationStore();
const menuStore = useMenuStore();

const routeItems = ref(
  [
    { label: 'กำหนดสิทธิ์', url: '/st/st004' },
    { label: 'รายละเอียดสิทธิ์', },
  ] as MenuItem[]);

onBeforeMount((): void => {
  detailStore.onClearBody();
});

onMounted(async (): Promise<void> => {
  await detailStore.onGetByCode(<string>route.params.code);
});

const onChangeManage = (value: boolean, index: number): void => {
  if (value) {
    detailStore.body.programPermissions[index].isView = value;
  }
};

const disabled = computed((): boolean => {
  if (detailStore.body.code) {
    return authStore.profile.role.filter((value: Role): boolean => value.roleCode.includes(detailStore.body.code!)).length > 0;
  }

  return !menuStore.hasManage;
});
</script>

<template>
  <VeeForm @submit="() => detailStore.onSubmitAsync(<string>route.params.code)"
    @invalid-submit="() => ToastHelper.invalidMessageToast()">
    <TitleHeader :label="route.params.id ? 'แก้ไขสิทธิ์' : 'เพิ่มสิทธิ์'" :route-items="routeItems">
      <template #breadcrumbAction>
        <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
      </template>
    </TitleHeader>
    <Card class="my-4">
      <template #content>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-2 mt-8">
          <InputField label="รหัสสิทธิ์" v-model="detailStore.body.code" rules="required"
            :disabled="!!(<string>route.params.code)" />
          <InputField label="ชื่อสิทธิ์" v-model="detailStore.body.name" rules="required" :disabled />
          <Checkbox label="ใช้งาน" v-model="detailStore.body.isActive" :disabled class="mt-2" />
        </div>
      </template>
    </Card>

    <div class="pb-4">
      <DataTable :value="detailStore.body.programPermissions" groupRowsBy="groupName" rowGroupMode="subheader">
        <Column field="code">
          <template #header>
            <p class="w-full font-bold text-center">รหัสโปรแกรม</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">{{ data.code }}</p>
          </template>
        </Column>
        <Column field="name">
          <template #header>
            <p class="w-full font-bold text-center">โปรแกรม</p>
          </template>
          <template #body="{ data }">
            <p>{{ data.name }}</p>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">ดูเท่านั้น</p>
          </template>
          <template #body="{ data }">
            <div class="w-full flex items-center justify-center">
              <PrimeCheckBox v-model="data.isView" binary :disabled />
            </div>
          </template>
        </Column>
        <Column>
          <template #header>
            <p class="w-full font-bold text-center">จัดการ</p>
          </template>
          <template #body="{ data, index }">
            <div class="w-full flex items-center justify-center">
              <PrimeCheckBox v-model="data.isManage" binary @update:model-value="(e) => onChangeManage(e, index)"
                :disabled />
            </div>
          </template>
        </Column>
        <Column field="groupName" />
        <template #groupheader="slotProps">
          <div class="flex items-center justify-center gap-2">
            <p class="text-primary font-bold">{{ slotProps.data.groupName }}</p>
          </div>
        </template>
      </DataTable>
    </div>
  </VeeForm>
</template>
<script setup lang="ts">
import { Form } from 'vee-validate';
import { onMounted, onUnmounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import { Column, DataTable } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import { TitleHeader } from '@/components/cosmetic';
import { DragdropImage, InputField, Select, Switch } from '@/components/forms';
import StatusChip from '@/components/StatusChip.vue';
import { useSt005DetailStore } from '@/stores/ST/st005';
import MemberListDialog from './memberListDialog.vue';
import ToastHelper from '@/helpers/toast';
import { useMenuStore } from '@/stores/menu';
import cookie from '@/configs/cookie';
import { computed } from 'vue';

const route = useRoute();
const router = useRouter();
const menuStore = useMenuStore();

const id = ref(route.params?.id);
const isSelfEdit = computed(() =>
  !!id.value &&
  String(id.value).toLowerCase() === String(cookie.get('userLogin') ?? '').toLowerCase()
);

const detailStore = useSt005DetailStore();
const visible = ref(false);

const routeItems = ref([
  { label: 'จัดการผู้ใช้งาน', url: '/st/st005' },
  { label: 'รายละเอียดผู้ใช้งาน' },
] as MenuItem[]);

const onSubmit = async (): Promise<void> => {
  if (id.value) {
    await detailStore.onUpdateUser(id.value.toString());
    return;
  }

  const newId = await detailStore.onCreateUser();

  if (newId) {
    router.push(`detail/${newId}`);
  }
};

onMounted(async (): Promise<void> => {
  await detailStore.getDropDownRole();

  if (id.value) {
    await detailStore.onGetUserById(id.value.toString());
  }
});

onUnmounted((): void => {
  detailStore.onClearBody();
})
</script>

<template>
  <Form @submit="onSubmit()" @invalidSubmit="ToastHelper.invalidMessageToast()">
    <TitleHeader label="จัดการผู้ใช้งาน" :route-items="routeItems" class="mb-4">
      <template #breadcrumbAction>
        <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" v-if="menuStore.hasManage" />
      </template>
    </TitleHeader>

    <Card class="mb-4">
      <template #content>
        <TitleHeader label="ข้อมูลผู้ใช้งาน" />
        <div class="flex flex-col ca mt-10">
          <InputField class="w-full md:w-1/4" v-model="detailStore.body.name" label="ชื่อ-นามสกุล/อีเมล"
            rules="required" readonly>
            <template #appendAction>
              <InputGroupAddon>
                <Button label="ค้นหา" :disabled="!!id" class="h-full rounded-none! text-white! bg-gray-400 border-none!"
                  @click="() => (visible = true)" />
              </InputGroupAddon>
            </template>
          </InputField>
          <div class="grid grid-cols-5">
            <div>
              <p class="text-gray-400">อีเมล</p>
              <span class="text-xl">
                {{ detailStore.body.email }}
              </span>
            </div>

            <div>
              <p class="text-gray-400">ฝ่าย</p>
              <span class="text-xl">
                {{ detailStore.body.departmentName }}
              </span>
            </div>

            <div>
              <p class="text-gray-400">ตำแหน่ง</p>
              <span class="text-xl">
                {{ detailStore.body.positionName }}
              </span>
            </div>
          </div>

          <div class="mt-4 flex items-center gap-3">
            <Switch label="ใช้งาน" v-model="detailStore.body.isActive" :disabled="!menuStore.hasManage" />
            <StatusChip v-if="detailStore.body.isLockedOut" label="ถูกล็อค" color="Error" class="h-fit text-nowrap" />
            <Button v-if="detailStore.body.isLockedOut && menuStore.hasManage && id" label="ปลดล็อคบัญชี"
              icon="pi pi-unlock" severity="warn" variant="outlined" size="small"
              @click="detailStore.onUnlockUser(id.toString())" />
          </div>

          <div class="w-1/4">
            <DragdropImage label="ลายเซ็น" v-model="detailStore.body.signatureImageId"
              :disabled="!menuStore.hasManage" />
          </div>
        </div>
      </template>
    </Card>

    <Card class="mb-4">
      <template #content>
        <TitleHeader label="สิทธิ์การเข้าใช้งาน">
          <template #action>
            <Button icon="pi pi-plus" variant="outlined" severity="primary" label="เพิ่มกลุ่มสิทธิ์"
              @click="() => detailStore.addRole()" v-if="menuStore.hasManage && !isSelfEdit" />
          </template>
        </TitleHeader>

        <div>
          <DataTable :value="detailStore.body.role">
            <Column>
              <template #header>
                <span class="text-center w-[10vw] text-2xl font-bold"> ลำดับ </span>
              </template>
              <template #body="{ index }">
                <p class="text-end w-[10vw]">
                  {{ index + 1 }}
                </p>
              </template>
            </Column>
            <Column>
              <template #header>
                <span class="text-center w-[50vw] text-2xl font-bold"> กลุ่มสิทธิ์ </span>
              </template>
              <template #body="{ data }">
                <p class="w-[50vw]">
                  <Select hide-details v-model="data.roleCode" :options="detailStore.dropdowns.role"
                    :disabled="!menuStore.hasManage || isSelfEdit" />
                </p>
              </template>
            </Column>
            <Column>
              <template #header> </template>
              <template #body="{ index }">
                <Button icon="pi pi-trash" class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text" @click="() => detailStore.removeRole(index)" v-if="menuStore.hasManage && !isSelfEdit" />
              </template>
            </Column>
            <template #empty>
              <div class="text-center text-2xl">ไม่พบข้อมูล</div>
            </template>
          </DataTable>
        </div>
      </template>
    </Card>
  </Form>

  <MemberListDialog v-model:visible="visible" />
</template>

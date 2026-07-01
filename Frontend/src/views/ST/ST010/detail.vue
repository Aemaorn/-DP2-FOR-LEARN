<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { Form } from 'vee-validate';
import { Button, Card, Divider } from 'primevue';
import InputGroupAddon from 'primevue/inputgroupaddon';
import type { MenuItem } from 'primevue/menuitem';

import { showUserDialogAsync, showConfirmDialogAsync, showActivityDialog } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { TSt010Secretary } from '@/models/ST/st010';

import { useSt010DetailStore } from '@/stores/ST/st010';

import { InputField, Checkbox, Datepicker, UploadFileGroup, Select, Radio } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { storeToRefs } from 'pinia';
import { ConfirmDialogType } from '@/enums/dialog';
import { useMenuStore } from '@/stores/menu';

const routeItems = ref<MenuItem[]>([
  { label: 'กำหนดเลขา', url: '/st/st010' },
  { label: 'กำหนดเลขา (เพิ่ม / แก้ไข)' },
]);

const route = useRoute();
const detailStore = useSt010DetailStore();
const menuStore = useMenuStore();
const { body } = storeToRefs(detailStore);

const isEditMode = computed((): boolean => Boolean(route.params.id));

onMounted(async (): Promise<void> => {
  await Promise.all([
    detailStore.onFetchBusinessUnitDropdowns(),
    detailStore.onFetchAllPositions(),
  ]);
  if (isEditMode.value) {
    await detailStore.onGetByIdAsync(route.params.id as string);
  }
});

onUnmounted((): void => {
  detailStore.onResetBody();
});

const onSubmitAsync = async (): Promise<void> => {
  if (!body.value.secretaries.length) {
    ToastHelper.warning('กรุณาระบุข้อมูล', 'กรุณาระบุเลขาอย่างน้อย 1 ราย');
    return;
  }

  if (route.params.id) {
    await detailStore.onUpdateAsync(route.params.id!.toString());
    return;
  }
  await detailStore.onCreateAsync();
};

const onToggleOwnerType = (): void => {
  const isPositionType = body.value.suSecretaryOwner.isPositionType;
  body.value.suSecretaryOwner = { isPositionType };
  body.value.secretaries = [];
};

const onSelectBusinessUnit = (value: string): void => {
  const option = detailStore.businessUnitDropdown.find((o): boolean => o.value === value);
  body.value.suSecretaryOwner.businessUnitName = option?.label;
  body.value.suSecretaryOwner.positionId = undefined;
  body.value.suSecretaryOwner.fullPositionName = undefined;
};

const onSelectPosition = (value: string): void => {
  const option = detailStore.positionDropdown.find((o): boolean => o.value === value);
  body.value.suSecretaryOwner.fullPositionName = option?.label;
};

const selectPrincipalUser = async (): Promise<void> => {
  try {
    const selectedData = await showUserDialogAsync();
    if (!selectedData) return;

    body.value.suSecretaryOwner = {
      isPositionType: false,
      suUserId: selectedData.id,
      userFullName: selectedData.name,
      employeeCode: selectedData.employeeCode,
      positionId: selectedData.positionCode,
      fullPositionName: selectedData.positionName,
      email: selectedData.email,
    };

    body.value.secretaries = [];
  } catch {
    ToastHelper.error('Error', 'ไม่สามารถเลือกผู้ใช้งานได้');
  }
};

const addSecretary = async (): Promise<void> => {
  try {
    const selectedData = await showUserDialogAsync();
    if (!selectedData) return;

    if (selectedData.id === body.value.suSecretaryOwner.suUserId) {
      ToastHelper.error('ไม่สำเร็จ', 'ไม่สามารถกำหนดตัวเองเป็นเลขาได้');
      return;
    }

    const isDuplicate = body.value.secretaries.some((s) => s.suUserId === selectedData.id);
    if (isDuplicate) {
      ToastHelper.error('ไม่สำเร็จ', 'ผู้ใช้งานนี้เป็นเลขาอยู่แล้ว');
      return;
    }

    const sequence = body.value.secretaries.length + 1;

    body.value.secretaries.push({
      suUserId: selectedData.id,
      userFullName: selectedData.name,
      positionId: selectedData.positionCode,
      fullPositionName: selectedData.positionName,
      email: selectedData.email,
      active: true,
      sequence,
    } as TSt010Secretary);
  } catch {
    ToastHelper.error('Error', 'ไม่สามารถเพิ่มเลขาได้');
  }
};

const removeSecretary = async (secretary: TSt010Secretary): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  if (secretary.id && route.params.id) {
    await detailStore.onDeleteSecretaryAsync(route.params.id.toString(), secretary.id);
    return;
  }

  body.value.secretaries = body.value.secretaries.filter((s) => s.suUserId !== secretary.suUserId);
  body.value.secretaries.forEach((s, i) => {
    s.sequence = i + 1;
  });
};
</script>

<template>
  <Form class="pb-10" @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()">
    <TitleHeader label="กำหนดเลขา" :route-items="routeItems">
      <template #breadcrumbAction>
        <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
      </template>
    </TitleHeader>

    <!-- Card 1: ผู้ใช้งานหลัก -->
    <Card class="my-4">
      <template #content>
        <TitleHeader label="ผู้ใช้งานหลัก">
          <template #action>
            <Button v-if="isEditMode" label="ประวัติการใช้งาน" icon="pi pi-refresh" severity="primary" variant="outlined"
              @click="showActivityDialog(route.params.id as string)" />
          </template>
        </TitleHeader>

        <!-- Toggle ประเภท (เฉพาะ create mode) -->
        <Radio v-if="!isEditMode" v-model="body.suSecretaryOwner.isPositionType" class="mt-6"
          :options="[{ label: 'กำหนดที่คน', value: false }, { label: 'กำหนดที่ตำแหน่ง', value: true }]"
          @change="onToggleOwnerType" hide-details />

        <!-- Person mode -->
        <div v-if="body.suSecretaryOwner.isPositionType === false" class="grid lg:grid-cols-4 gap-2 gap-y-8 mt-6">
          <InputField label="ชื่อ-นามสกุล" :model-value="body.suSecretaryOwner.userFullName" rules="required" disabled>
            <template #appendAction>
              <InputGroupAddon v-if="!isEditMode">
                <Button label="ค้นหา" class="rounded-none h-full text-white! bg-gray-500! border-none!"
                  @click="selectPrincipalUser" />
              </InputGroupAddon>
            </template>
          </InputField>
          <InputField class="lg:col-start-1" label="ตำแหน่ง" :model-value="body.suSecretaryOwner.fullPositionName" disabled />
          <InputField label="อีเมล" :model-value="body.suSecretaryOwner.email" disabled />
        </div>

        <!-- Position mode -->
        <div v-if="body.suSecretaryOwner.isPositionType === true" class="grid lg:grid-cols-4 gap-2 gap-y-8 mt-10">
          <Select label="กลุ่มงาน/สายงาน/ฝ่าย" v-model="body.suSecretaryOwner.businessUnitId" rules="required"
            :options="detailStore.businessUnitDropdown" :disabled="isEditMode"
            @onSelect="onSelectBusinessUnit" />
          <Select label="ตำแหน่ง" v-model="body.suSecretaryOwner.positionId" rules="required"
            :options="detailStore.positionDropdown"
            :disabled="isEditMode"
            @onSelect="onSelectPosition" />
        </div>
      </template>
    </Card>

    <!-- Card 2: รายชื่อเลขา -->
    <Card class="my-4">
      <template #content>
        <TitleHeader label="รายชื่อเลขา">
          <template #action>
            <Button v-if="body.suSecretaryOwner.isPositionType ? body.suSecretaryOwner.businessUnitId && body.suSecretaryOwner.positionId : body.suSecretaryOwner.suUserId"
              label="เพิ่มเลขา" icon="pi pi-plus" severity="primary" variant="outlined"
              @click="addSecretary" />
          </template>
        </TitleHeader>

        <div v-if="body.secretaries.length === 0" class="text-center text-gray-400 py-8">
          ยังไม่มีเลขา
        </div>

        <div v-for="(secretary, index) in body.secretaries" :key="`secretary-${index}`">
          <div class="grid lg:grid-cols-12 gap-2 py-4">
            <div class="lg:col-span-1 flex items-center justify-center text-gray-500 font-semibold">
              {{ secretary.sequence }}
            </div>
            <div class="lg:col-span-3">
              <p class="font-semibold">{{ secretary.userFullName }}</p>
              <p class="text-sm text-gray-500">{{ secretary.fullPositionName }}</p>
              <p class="text-sm text-gray-400">{{ secretary.email }}</p>
            </div>
            <div class="lg:col-span-5 grid grid-cols-2 gap-2">
              <Datepicker label="วันที่เริ่มทำหน้าที่" v-model="secretary.effectiveStartDate"
                :max-date="secretary.effectiveEndDate" />
              <Datepicker label="วันที่สิ้นสุด" v-model="secretary.effectiveEndDate"
                :min-date="secretary.effectiveStartDate" />
            </div>
            <div class="lg:col-span-3 flex items-center justify-end gap-4">
              <Checkbox label="ใช้งาน" v-model="secretary.active" />
              <Button icon="pi pi-trash" severity="danger" variant="text" class="-mt-4"
                @click="removeSecretary(secretary)" />
            </div>
          </div>
          <Divider v-if="index < body.secretaries.length - 1" />
        </div>
      </template>
    </Card>

    <!-- Card 3: เอกสารแนบ -->
    <Card class="my-4">
      <template #content>
        <TitleHeader label="เอกสารแนบ" />
        <UploadFileGroup v-if="!body.suSecretaryOwner.id" v-model="body.attachments"
          :disabled="!menuStore.hasPermission" />
        <UploadFileGroup v-if="body.suSecretaryOwner.id" v-model="body.attachments"
          @upload="detailStore.onUpsertAttachments"
          @remove-file="detailStore.onUpsertAttachments"
          @remove-group="detailStore.onUpsertAttachments"
          @reorder="detailStore.onUpsertAttachments"
          :disabled="!menuStore.hasPermission" />
      </template>
    </Card>
  </Form>
</template>

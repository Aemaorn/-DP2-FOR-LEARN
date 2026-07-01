<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, type ComputedRef } from 'vue';
import { useRoute } from 'vue-router';
import { Form } from 'vee-validate';
import { Button, Card, Divider } from 'primevue';
import InputGroupAddon from 'primevue/inputgroupaddon';
import type { MenuItem } from 'primevue/menuitem';

import { showUserDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { Option } from '@/models/shared/option';
import type {
  TSuDelegateCreateUserRequestType,
  TSuDelegateeCreateUserRequestType,
  TGetListRawEmpPosition,
} from '@/models/ST/st001';

import { useSt001DetailStore } from '@/stores/ST/st001';

import { InputField, InputArea, Datepicker, Select, Checkbox } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { storeToRefs } from 'pinia';
import { ArrayHelper } from '@/helpers/array';

const routeItems = ref<MenuItem[]>([
  { label: 'การมอบหมายให้ปฏิบัติหน้าที่แทน', url: '/st/st001' },
  { label: 'การมอบหมายให้ปฏิบัติหน้าที่แทน(เพิ่ม / แก้ไข)' },
]);

type bool = boolean | "" | undefined;

const route = useRoute();
const detailStore = useSt001DetailStore();
const { body } = storeToRefs(detailStore);

const { reSequence } = ArrayHelper();

const isEditMode = computed((): boolean => Boolean(route.params.id));

const delegateesOrEmpty = computed(
  (): TSuDelegateeCreateUserRequestType[] => detailStore.body.delegatees || []
);

onMounted(async (): Promise<void> => {
  if (isEditMode.value) {
    await detailStore.onGetByIdAsync(route.params.id as string);
  }
});

onUnmounted((): void => {
  detailStore.onResetBody();
});

const onSubmitAsync = async (): Promise<void> => {
  if (route.params.id) {
    await detailStore.onUpdateAsync(route.params.id!.toString());
    return;
  }
  await detailStore.onCreateAsync();
};

const ensureDelegateesArray = (): void => {
  if (!detailStore.body.delegatees) {
    detailStore.body.delegatees = [];
  }
};

const addSuUser = async (): Promise<void> => {
  try {
    const selectedData = await showUserDialogAsync();
    if (!selectedData) return;
    ensureDelegateesArray();

    await detailStore.onGetSuUserByIdAsync(selectedData.id);
    await detailStore.onGetBuPositionByEmpCodeAsync(selectedData.employeeCode);

    detailStore.body.delegator = {
      ...detailStore.body.delegator,
      suUserId: selectedData.id,
      userFullName: selectedData.name,
      fullPositionName: selectedData.positionName,
      positionId: selectedData.positionCode,
      email: selectedData.email,
    } as TSuDelegateCreateUserRequestType;

    detailStore.body.delegatees = [];
  } catch {
    ToastHelper.error('Error', 'ไม่สามารถเพิ่มผู้ใช้งานได้');
  }
};

const addDelegateeUser = async (positionsId: string, businessUnitId: string): Promise<void> => {
  try {
    const selectedData = await showUserDialogAsync();
    if (!selectedData) return;
    ensureDelegateesArray();

    if (selectedData.id === detailStore.body.delegator.suUserId) {
      ToastHelper.error('ไม่สำเร็จ', 'ไม่สามารถมอบหมายให้ปฏิบัติหน้าที่แทนตัวเองได้');
      return;
    }

    const usersForSlot = getDelegateesForPositionAndBusinessUnit(positionsId, businessUnitId);
    const userCount = usersForSlot.length + 1;

    detailStore.body.delegatees!.push({
      suUserId: selectedData.id,
      userFullName: selectedData.name,
      delegatorPositionId: positionsId,
      delegatorBusinessUnitId: businessUnitId,
      fullPositionName: selectedData.positionName,
      positionId: selectedData.positionCode,
      email: selectedData.email,
      levelOneBusinessUnitId: '',
      sequence: userCount,
      active: true,
    } as TSuDelegateeCreateUserRequestType);
  } catch {
    ToastHelper.error('Error', 'ไม่สามารถเพิ่มผู้ใช้งานได้');
  }
};

const isOddIndex = (index: number): boolean => index % 2 !== 0;

/* -------------------------------------------------------------------------- */
/*                            Consolidated level helpers                      */
/* -------------------------------------------------------------------------- */

const getLevelOptions = (
  level: 1 | 2 | 3,
  positions: TGetListRawEmpPosition[],
  delegatorBusinessUnitId: string,
  selectedLevelOneId?: string,
  selectedLevelTwoId?: string
): Option[] => {
  const position = positions.find((p): boolean => p.businessUnitId === delegatorBusinessUnitId);
  if (!position) return [];

  if (level === 1) {
    return position.levelOnes.map((item): Option => ({ value: item.businessUnitId, label: item.label }));
  }

  const levelOne = position.levelOnes.find((l): boolean => l.businessUnitId === selectedLevelOneId);
  if (!levelOne) return [];

  if (level === 2) {
    return levelOne.levelTwos.map((item): Option => ({ value: item.businessUnitId, label: item.label }));
  }

  const levelTwo = levelOne.levelTwos.find((l): boolean => l.businessUnitId === selectedLevelTwoId);
  if (!levelTwo) return [];

  return levelTwo.levelThrees.map((item): Option => ({ value: item.businessUnitId, label: item.label }));
};

const lv = (
  level: 1 | 2 | 3,
  positionId: string,
  businessId: string,
  selectedLevelOneId?: string,
  selectedLevelTwoId?: string,
  currentId?: string
): ComputedRef<Option[]> => {
  return computed((): Array<Option> => {
    const delegatees = getDelegateesForPositionAndBusinessUnit(positionId, businessId);

    const options = getLevelOptions(
      level,
      detailStore.delegateeListSelection,
      businessId,
      selectedLevelOneId,
      selectedLevelTwoId
    );

    if (level === 1) {
      if (delegatees.some((x): string | undefined => x.businessUnitId)) return options;

      const otherParentIds = delegatees
        .filter((x): bool => x.parentBusinessUnitId && x.parentBusinessUnitId !== currentId)
        .map((x): string | undefined => x.parentBusinessUnitId);

      if (!otherParentIds.length) return options;

      return options.filter((x): boolean => !otherParentIds.includes(x.value as string));
    }

    if (level === 2) {
      if (delegatees.some((x): string | undefined => x.subBusinessUnitId)) return options;

      const otherBusinessUnitIds = delegatees
        .filter((x): bool => x.businessUnitId && x.businessUnitId !== currentId)
        .map((x): string | undefined => x.businessUnitId);

      if (!otherBusinessUnitIds.length) return options;

      return options.filter((x): boolean => !otherBusinessUnitIds.includes(x.value as string));
    }

    const subBusinessIds = delegatees
      .filter((x): boolean | "" | undefined => x.subBusinessUnitId && x.subBusinessUnitId !== currentId)
      .map((x): string | undefined => x.subBusinessUnitId);

    if (!subBusinessIds.length) return options;

    return options.filter((x): boolean => !subBusinessIds.includes(x.value as string));
  });
};

/* -------------------------------------------------------------------------- */
/*                             small utility functions                        */
/* -------------------------------------------------------------------------- */

const removeUser = (positionId: string, businessUnitId: string, sequence: number): void => {
  if (!detailStore.body.delegatees) return;

  detailStore.body.delegatees = detailStore.body.delegatees.filter(
    (user): boolean =>
      !(
        user.delegatorPositionId === positionId &&
        user.delegatorBusinessUnitId === businessUnitId &&
        user.sequence === sequence
      )
  );

  reSequence(detailStore.body.delegatees.filter((x): boolean => x.delegatorPositionId === positionId));
};

const getDelegateesForPositionAndBusinessUnit = (
  positionId: string,
  businessUnitId: string
): TSuDelegateeCreateUserRequestType[] => {
  return delegateesOrEmpty.value.filter(
    (user): boolean => user.delegatorBusinessUnitId === businessUnitId && user.delegatorPositionId === positionId
  );
};

const canAddUserFromSelection = (delegator: TGetListRawEmpPosition): boolean => {
  const users = getDelegateesForPositionAndBusinessUnit(delegator.positionId, delegator.businessUnitId);

  if (users.length === 0) return true;

  const lastUser = users[users.length - 1];

  if (!lastUser.parentBusinessUnitId) return true;

  if (!lastUser.businessUnitId && !lastUser.subBusinessUnitId) return false;

  return true;
};

const canShowAddButton = (delegator: TGetListRawEmpPosition): boolean => {
  const hasLevel1 = getLevelOptions(
    1,
    detailStore.delegateeListSelection,
    delegator.businessUnitId
  ).length > 0;

  return canAddUserFromSelection(delegator) && hasLevel1;
};
</script>

<template>
  <Form class="pb-10" @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()">
    <TitleHeader label="การมอบหมายให้ปฏิบัติหน้าที่แทน" :route-items="routeItems">
      <template #breadcrumbAction>
        <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
      </template>
    </TitleHeader>

    <Card class="my-4">
      <template #content>
        <TitleHeader label="ผู้ปฎิบัติหน้าที่" />
        <div class="grid lg:grid-cols-4 gap-2 gap-y-8 mt-10">
          <InputField label="ชื่อ-นามสกุล" :model-value="body.delegator.userFullName" rules="required" disabled>
            <template #appendAction>
              <InputGroupAddon v-if="!isEditMode">
                <Button label="ค้นหา" class="rounded-none h-full text-white! bg-gray-500! border-none!"
                  @click="addSuUser" />
              </InputGroupAddon>
            </template>
          </InputField>

          <InputField class="lg:col-start-1" label="ตำแหน่ง" v-model="detailStore.body.delegator.fullPositionName"
            disabled />
          <InputField label="อีเมล" v-model="detailStore.body.delegator.email" disabled />

          <Datepicker class="lg:col-start-1" label="มอบหมายให้ปฏิบัติหน้าที่แทนตั้งแต่วันที่" :min-date="new Date()"
            :max-data="detailStore.body.delegator.delegationEndDate"
            v-model="detailStore.body.delegator.delegationStartDate" rules="required" />
          <Datepicker label="มอบหมายให้ปฏิบัติหน้าที่แทนถึงวันที่"
            v-model="detailStore.body.delegator.delegationEndDate" rules="required"
            :min-date="detailStore.body.delegator.delegationStartDate" />

          <InputArea class="lg:col-start-1" label="เหตุผล" v-model="detailStore.body.delegator.annotation"
            rules="required" />
        </div>
      </template>
    </Card>

    <Card class="my-4">
      <template #content>
        <TitleHeader label="ผู้ปฏิบัติหน้าที่แทน" />
        <Card v-for="(delegator, index) in detailStore.delegateeListSelection" :key="`delegator-${index}`"
          :class="`mb-4 ${isOddIndex(index) ? 'bg-gray-200' : 'bg-white'}`">
          <template #content>
            <TitleHeader :label="delegator.label" :hidden-icon="true" class="mb-10">
              <template #action>
                <Button label="เพิ่มรายชื่อ" icon="pi pi-plus" severity="primary" variant="outlined"
                  @click="addDelegateeUser(delegator.positionId, delegator.businessUnitId)"
                  v-if="canShowAddButton(delegator)" />
              </template>
            </TitleHeader>


            <div
              v-for="(user, userIndex) in getDelegateesForPositionAndBusinessUnit(delegator.positionId, delegator.businessUnitId)"
              :key="`user-${userIndex}`">
              <div class="grid lg:grid-cols-6 gap-2">
                <div class="lg:col-span-2 items-center order-2 lg:order-1 pb-4 lg:pb-0">
                  <p>{{ user.userFullName }}</p>
                  <p>{{ user.fullPositionName }}</p>
                  <p>{{ user.email }}</p>
                </div>

                <div class="lg:col-span-3 order-3 lg:order-2">
                  <div class="lg:flex gap-4 items-center">
                    <div class="w-full">
                      <Select v-model="user.parentBusinessUnitId" :options="lv(1,
                        delegator.positionId,
                        delegator.businessUnitId,
                        user.parentBusinessUnitId,
                        user.businessUnitId,
                        user.parentBusinessUnitId
                      ).value"
                        @update:model-value="() => { user.subBusinessUnitId = undefined; user.businessUnitId = undefined }"
                        optionLabel="label" optionValue="value" rules="required"
                        label="ปฏิบัติหน้าที่แทนหน่วยงานระดับที่ 1" />
                    </div>

                    <div class="w-full" v-if="user.parentBusinessUnitId">
                      <Select v-if="user.parentBusinessUnitId && lv(2,
                        delegator.positionId,
                        delegator.businessUnitId,
                        user.parentBusinessUnitId,
                        user.businessUnitId,
                        user.businessUnitId
                      ).value.length > 0" v-model="user.businessUnitId" :options="lv(2,
                        delegator.positionId,
                        delegator.businessUnitId,
                        user.parentBusinessUnitId,
                        user.businessUnitId,
                        user.businessUnitId
                      ).value" @update:model-value="() => user.subBusinessUnitId = undefined" optionLabel="label"
                        optionValue="value" label="ปฏิบัติหน้าที่แทนหน่วยงานระดับที่ 2" />
                    </div>

                    <div class="w-full" v-if="user.subBusinessUnitId">
                      <Select v-if="user.parentBusinessUnitId && user.businessUnitId && lv(3,
                        delegator.positionId,
                        delegator.businessUnitId,
                        user.parentBusinessUnitId,
                        user.businessUnitId,
                        user.subBusinessUnitId
                      ).value.length > 0" v-model="user.subBusinessUnitId" :options="lv(3,
                        delegator.positionId,
                        delegator.businessUnitId,
                        user.parentBusinessUnitId,
                        user.businessUnitId,
                        user.subBusinessUnitId
                      ).value" optionLabel="label" optionValue="value" label="ปฏิบัติหน้าที่แทนหน่วยงานระดับที่ 3" />
                    </div>
                  </div>
                </div>

                <div class="items-center order-1 lg:order-3">
                  <div class="flex gap-4 justify-end">
                    <Checkbox class="mt-1" label="ใช้งาน" v-model="user.active" />
                    <Button icon="pi pi-trash" severity="danger" variant="text"
                      @click="removeUser(delegator.positionId, delegator.businessUnitId, user.sequence)" />
                  </div>
                </div>
              </div>
              <Divider class="mb-10" />
            </div>
          </template>
        </Card>
      </template>
    </Card>
  </Form>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { Form as VeeForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import type { MenuItem } from 'primevue/menuitem';
import { TitleHeader } from '@/components/cosmetic';
import { AutoCompleteField, InputField, InputNumber, Select } from '@/components/forms';
import { useSt009DetailStore } from '@/stores/ST/st009';
import { getSectionProcessTypeOptions } from '@/helpers/sectionProcessType';
import ToastHelper from '@/helpers/toast';
import type { St009UpdateApproverBody } from '@/models/ST/st009';
import { useMenuStore } from '@/stores/menu';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';

const route = useRoute();
const router = useRouter();
const store = useSt009DetailStore();
const menuStore = useMenuStore();
const { detail, editableApprovers, editableSection, supplyMethodOptions, supplyMethodSpecialTypeOptions } = storeToRefs(store);

const isCreateMode = computed((): boolean => !route.params.id);
const newId = ref<number | undefined>(undefined);

const routeItems: MenuItem[] = [
  { label: 'กำหนดอำนาจอนุมัติ', url: '/st/st009' },
  { label: 'รายละเอียดคำสั่งธนาคาร' },
];

const processTypeOptions = getSectionProcessTypeOptions();

const getProcessTypeLabel = (value: string): string =>
  processTypeOptions.find(o => o.value === value)?.label ?? value;

const tableApprovers = computed((): { index: number; approver: St009UpdateApproverBody; processType: string }[] =>
  editableApprovers.value
    .map((approver: St009UpdateApproverBody, index: number): { index: number; approver: St009UpdateApproverBody; processType: string } => ({
      index,
      approver,
      processType: approver.processType,
    }))
    .filter((item): boolean => !filterProcessType.value || item.processType === filterProcessType.value)
    .filter((item): boolean => {
      const keyword = filterPositionName.value?.trim().toLowerCase();
      if (!keyword) return true;
      return (item.approver.positionName ?? '').toLowerCase().includes(keyword);
    })
    .filter((item): boolean => {
      const keyword = filterCommandText.value?.trim().toLowerCase();
      if (!keyword) return true;
      return (item.approver.commandText ?? '').toLowerCase().includes(keyword);
    })
    .sort((a, b): number => {
      const groupCmp = a.processType.localeCompare(b.processType);
      if (groupCmp !== 0) return groupCmp;
      const commandTextCmp = (a.approver.commandText ?? '').localeCompare(b.approver.commandText ?? '');
      if (commandTextCmp !== 0) return commandTextCmp;
      return (b.approver.budget ?? 0) - (a.approver.budget ?? 0);
    })
);

const filterProcessType = ref<string | undefined>(undefined);
const filterPositionName = ref<string>('');
const filterCommandText = ref<string>('');

const editingRows = ref<Set<number>>(new Set());
const editingSnapshot = ref<Map<number, St009UpdateApproverBody>>(new Map());
const invalidRows = ref<Set<number>>(new Set());

const isRowInvalid = (index: number): boolean => {
  const a = editableApprovers.value[index];
  return !a.inRefCode || !a.positionName || a.budget == null || a.commandBudget == null;
};

const startEditRow = (index: number): void => {
  const next = new Set(editingRows.value);
  next.add(index);
  editingRows.value = next;
  const snap = new Map(editingSnapshot.value);
  snap.set(index, { ...editableApprovers.value[index] });
  editingSnapshot.value = snap;
};

const closeEditRow = (index: number): void => {
  const next = new Set(editingRows.value);
  next.delete(index);
  editingRows.value = next;
  const snap = new Map(editingSnapshot.value);
  snap.delete(index);
  editingSnapshot.value = snap;
};

const confirmEditRow = async (index: number): Promise<void> => {
  if (isRowInvalid(index)) {
    invalidRows.value = new Set([...invalidRows.value, index]);
    ToastHelper.invalidMessageToast();
    return;
  }
  invalidRows.value = new Set([...invalidRows.value].filter((i): boolean => i !== index));
  await store.onUpdateSingleApproverAsync(route.params.id as string, editableApprovers.value[index]);
  closeEditRow(index);
};

const cancelEditRow = (index: number): void => {
  const original = editingSnapshot.value.get(index);
  if (original) {
    editableApprovers.value[index] = { ...original };
  }
  closeEditRow(index);
};

const dialogVisible = ref(false);
const editingIndex = ref<number | null>(null);
const editingForm = ref<St009UpdateApproverBody>({
  inRefCode: '',
  positionName: '',
  shortPosition: '',
  budget: 0,
  processType: '',
  commandText: '',
  commandBudget: 0,
});

const openAddDialog = (processType: string): void => {
  editingIndex.value = null;
  editingForm.value = {
    inRefCode: '',
    positionName: '',
    shortPosition: '',
    budget: 0,
    processType,
    commandText: '',
    commandBudget: 0,
  };
  dialogVisible.value = true;
};


const onSaveDialog = async (): Promise<void> => {
  const newId = await store.onCreateApproverAsync(route.params.id as string, editingForm.value);
  if (newId) {
    editableApprovers.value.push({ ...editingForm.value, id: newId });
    dialogVisible.value = false;
  }
};

const onCreateSectionSubmitAsync = async (): Promise<void> => {
  const createdId = await store.onCreateSectionAsync(String(newId.value));
  if (createdId) {
    await router.replace({ name: 'st009Detail', params: { id: createdId } });
    await store.onGetBySuSectionIdAsync(createdId);
  }
};

const onDeleteApproverAsync = async (approverId: string): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;
  await store.onDeleteApproverAsync(route.params.id as string, approverId);
};

watch(
  () => editableSection.value.supplyMethodCode,
  async (code: string | undefined): Promise<void> => {
    editableSection.value.supplyMethodSpecialTypeCode = undefined;
    await store.onGetSupplyMethodSpecialTypeOptionsAsync(code);
  }
);

onMounted(async (): Promise<void> => {
  await store.onGetDropdownOptionsAsync();
  if (!isCreateMode.value) {
    await store.onGetBySuSectionIdAsync(route.params.id as string);
  }
});

onUnmounted((): void => {
  store.onClear();
});
</script>

<template>
  <TitleHeader label="รายละเอียดอำนาจอนุมัติ" :route-items="routeItems">
  </TitleHeader>

  <Card class="my-4" v-if="isCreateMode || detail">
    <template #content>
      <VeeForm @submit="onCreateSectionSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()">
        <TitleHeader label="ข้อมูลคำสั่งธนาคาร" class="mb-4">
          <template #action>
            <template v-if="isCreateMode">
              <Button label="บันทึก" icon="pi pi-save" size="small" severity="success" type="submit" />
            </template>
            <Button
              v-else-if="menuStore.hasPermission"
              label="บันทึก"
              icon="pi pi-save"
              size="small"
              severity="success"
              @click="store.onUpdateSectionAsync(route.params.id as string)"
            />
          </template>
        </TitleHeader>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 gap-y-6 mt-10">
          <InputNumber v-if="isCreateMode" label="รหัส" v-model="newId" rules="required" />
          <InputField label="รหัสอ้างอิงคำสั่งธนาคาร" v-model.trim="editableSection.refBankOrder" rules="required" />
          <InputNumber label="วงเงินสูงสุด" v-model="editableSection.maximumBudget" :min-fraction-digits="2" grouping rules="required" />
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 gap-y-6 mt-10">
          <AutoCompleteField label="วิธีการจัดหา" v-model="editableSection.supplyMethodCode" :options="supplyMethodOptions" />
          <AutoCompleteField v-model="editableSection.supplyMethodSpecialTypeCode" :options="supplyMethodSpecialTypeOptions" />
          <InputField label="หมายเหตุ" v-model.trim="editableSection.remark" class="md:col-span-2" />
        </div>
      </VeeForm>
    </template>
  </Card>

  <Card v-if="detail" class="mb-4">
    <template #content>
      <TitleHeader label="รายละเอียดอำนาจอนุมัติตามวงเงิน">
        <template #action>
          <Button
            label="เพิ่มรายการ"
            icon="pi pi-plus"
            size="small"
            severity="primary"
            variant="outlined"
            @click="openAddDialog('')"
            v-if="menuStore.hasPermission"
          />
        </template>
      </TitleHeader>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4 mt-10">
        <Select
          label="กรองตามประเภทกระบวนการ"
          v-model="filterProcessType"
          :options="processTypeOptions"
          show-clear
          hide-details
          @clear="filterProcessType = undefined"
        />
        <InputField
          label="กรองตามชื่อตำแหน่ง"
          v-model.trim="filterPositionName"
          hide-details
        />
        <InputField
          label="กรองตามข้อความคำสั่ง"
          v-model.trim="filterCommandText"
          hide-details
        />
      </div>

      <DataTable
        :value="tableApprovers"
        groupRowsBy="processType"
        rowGroupMode="subheader"
        showGridlines
        class="text-sm"
        :pt="{ rowgroupheadercell: { style: 'padding: 0' }, bodycell: { style: 'padding: 4px 8px; height: 48px; vertical-align: middle' } }"
      >
        <Column field="processType" style="width: 200px">
          <template #header><p class="font-bold w-full">ประเภทกระบวนการ</p></template>
          <template #body="{ data }">
            <Select v-if="editingRows.has(data.index)" v-model="editableApprovers[data.index].processType" :options="processTypeOptions" hide-details size="small" />
            <p v-else>{{ getProcessTypeLabel(editableApprovers[data.index].processType) }}</p>
          </template>
        </Column>
        <Column field="inRefCode">
          <template #header><p class="font-bold text-center w-full">รหัสอ้างอิง</p></template>
          <template #body="{ data }">
            <InputField v-if="editingRows.has(data.index)" v-model.trim="editableApprovers[data.index].inRefCode" hide-details :invalid="invalidRows.has(data.index) && !editableApprovers[data.index].inRefCode" />
            <p v-else class="text-center">{{ editableApprovers[data.index].inRefCode }}</p>
          </template>
        </Column>
        <Column field="positionName">
          <template #header><p class="font-bold w-full">ชื่อตำแหน่ง</p></template>
          <template #body="{ data }">
            <InputField v-if="editingRows.has(data.index)" v-model.trim="editableApprovers[data.index].positionName" hide-details :invalid="invalidRows.has(data.index) && !editableApprovers[data.index].positionName" />
            <p v-else>{{ editableApprovers[data.index].positionName }}</p>
          </template>
        </Column>
        <Column field="shortPosition">
          <template #header><p class="font-bold text-center w-full">ชื่อย่อตำแหน่ง</p></template>
          <template #body="{ data }">
            <InputField v-if="editingRows.has(data.index)" v-model.trim="editableApprovers[data.index].shortPosition" hide-details />
            <p v-else class="text-center">{{ editableApprovers[data.index].shortPosition }}</p>
          </template>
        </Column>
        <Column field="budget">
          <template #header><p class="font-bold text-right w-full">วงเงิน</p></template>
          <template #body="{ data }">
            <InputNumber v-if="editingRows.has(data.index)" v-model="editableApprovers[data.index].budget" :min-fraction-digits="2" grouping hide-details :invalid="invalidRows.has(data.index) && editableApprovers[data.index].budget == null" />
            <p v-else class="text-right">{{ editableApprovers[data.index].budget.toLocaleString('th-TH', { minimumFractionDigits: 2 }) }}</p>
          </template>
        </Column>
        <Column field="commandText">
          <template #header><p class="font-bold w-full">ข้อความคำสั่ง</p></template>
          <template #body="{ data }">
            <InputField v-if="editingRows.has(data.index)" v-model.trim="editableApprovers[data.index].commandText" hide-details />
            <p v-else>{{ editableApprovers[data.index].commandText }}</p>
          </template>
        </Column>
        <Column field="commandBudget">
          <template #header><p class="font-bold text-right w-full">วงเงินคำสั่ง</p></template>
          <template #body="{ data }">
            <InputNumber v-if="editingRows.has(data.index)" v-model="editableApprovers[data.index].commandBudget" :min-fraction-digits="2" grouping hide-details :invalid="invalidRows.has(data.index) && editableApprovers[data.index].commandBudget == null" />
            <p v-else class="text-right">{{ editableApprovers[data.index].commandBudget?.toLocaleString('th-TH', { minimumFractionDigits: 2 }) ?? '-' }}</p>
          </template>
        </Column>
        <Column v-if="menuStore.hasPermission" style="width: 100px">
          <template #body="{ data }">
            <div class="flex items-center justify-center gap-1">
              <template v-if="editingRows.has(data.index)">
                <Button
                  icon="pi pi-check"
                  size="small"
                  variant="text"
                  class="text-green-600! hover:bg-green-300/20!"
                  @click="confirmEditRow(data.index)"
                />
                <Button
                  icon="pi pi-times"
                  size="small"
                  variant="text"
                  class="text-red-600! hover:bg-red-300/20!"
                  @click="cancelEditRow(data.index)"
                />
              </template>
              <template v-else>
                <Button
                  icon="pi pi-pen-to-square"
                  size="small"
                  variant="text"
                  class="text-blue-600! hover:bg-blue-300/20!"
                  @click="startEditRow(data.index)"
                />
                <Button
                  icon="pi pi-trash"
                  size="small"
                  variant="text"
                  class="text-red-500! hover:bg-red-300/20!"
                  @click="onDeleteApproverAsync(data.approver.id!)"
                />
              </template>
            </div>
          </template>
        </Column>
        <template #groupheader="{ data }">
          <div class="flex items-center justify-between px-3 py-1">
            <p class="text-primary font-bold">{{ getProcessTypeLabel(data.processType) }}</p>
            <Button
              v-if="menuStore.hasPermission"
              icon="pi pi-plus"
              size="small"
              variant="text"
              class="text-primary!"
              @click="openAddDialog(data.processType)"
            />
          </div>
        </template>
        <template #empty>
          <p class="text-center py-2">ไม่พบข้อมูลผู้อนุมัติ</p>
        </template>
      </DataTable>
    </template>
  </Card>

  <Dialog
    v-model:visible="dialogVisible"
    :header="editingIndex !== null ? 'แก้ไขผู้อนุมัติ' : 'เพิ่มผู้อนุมัติ'"
    modal
    class="w-full max-w-5xl"
  >
    <VeeForm @submit="onSaveDialog" @invalidSubmit="ToastHelper.invalidMessageToast()">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 gap-y-6 mt-6">
        <Select
          label="ประเภท"
          v-model="editingForm.processType"
          :options="processTypeOptions"
          rules="required"
          class="md:col-span-2"
        />
        <div class="md:col-span-2">
          <InputField label="รหัสอ้างอิง" v-model.trim="editingForm.inRefCode" rules="required" class="md:w-1/2" />
        </div>
        <InputField label="ชื่อตำแหน่ง" v-model.trim="editingForm.positionName" rules="required" />
        <InputField label="ชื่อย่อตำแหน่ง" v-model.trim="editingForm.shortPosition" />
        <div class="md:col-span-2">
          <InputNumber
            label="วงเงิน"
            v-model="editingForm.budget"
            :min-fraction-digits="2"
            grouping
            rules="required"
            class="md:w-1/2"
          />
        </div>
        <InputField
          label="ข้อความคำสั่ง"
          v-model.trim="editingForm.commandText"
        />
        <InputNumber
          label="วงเงินคำสั่ง"
          v-model="editingForm.commandBudget"
          :min-fraction-digits="2"
          grouping
          rules="required"
        />
      </div>
      <div class="flex justify-end gap-2 mt-6">
        <Button label="ยกเลิก" severity="secondary" variant="outlined" @click="dialogVisible = false" type="button" />
        <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
      </div>
    </VeeForm>
  </Dialog>
</template>
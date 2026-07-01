<script setup lang="ts">
import { Card, Button, DataTable, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Select, Radio } from '@/components/forms';
import SharedConstants from '@/constants/shared';
import { ArrayHelper } from '@/helpers/array';
import { computed, onMounted, ref } from 'vue';
import type { Option } from '@/models/shared/option';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { showUserDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import { DatatableHelper } from '@/helpers/datable';
import { useMenuStore } from '@/stores/menu';
import type { pp008CommitteeType } from '@/views/PP/enums/pp008';
import type { PurchaseOrderApprovalCommittee } from '@/models/PP/ppModel';

type Props = {
  showOption: boolean;
  label: string;
  person: string;
  groupType: pp008CommitteeType;
  disable: boolean;
}
const { addSequence, deleteItemAndReSequence, reSequence } = ArrayHelper();
const { onRowReorder } = DatatableHelper();

const props = withDefaults(defineProps<Props>(), { showOption: false, label: '', person: 'ผู้จัดซื้อ/ผู้จัดจ้าง', disable: false });

const committee = defineModel<PurchaseOrderApprovalCommittee[]>('committee', {
  required: true,
});

const spacialDropdown = defineModel<Option[]>('spacialOption', {
  required: true,
});

const isHas = defineModel<boolean>('isHas', {
  default: true,
});

const isCommittee = defineModel<boolean>('isCommittee', {
  default: true,
  required: true,
});

const menuStore = useMenuStore();

const hasPosBoard001 = computed(() =>
  committee.value.some(item => item.committeePositionsCode === 'PosBoard001')
);

const dropdownCondition = (index: number) => computed(() => {
  if (isCommittee.value) {
    const currentValue = committee.value[index]?.committeePositionsCode;
    if (hasPosBoard001.value && currentValue !== 'PosBoard001') {
      return posBoardDropdown.value.filter(f => f.value !== 'PosBoard001');
    }
    return posBoardDropdown.value;
  }

  return spacialDropdown.value;
});

const posBoardDropdown = ref<Option[]>([]);

const getPosBoardDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoard, undefined, true);

  if (status === HttpStatusCode.Ok) {
    posBoardDropdown.value = data.slice(0, -1);
  }
};

const optionTypeData = [
  { value: true, label: "คณะกรรมการ" },
  { value: false, label: props.person },
] as Option[];

const addCommittee = async (): Promise<void> => {
  const selectedData = await showUserDialogAsync();

  if (!selectedData) return;

  if (committee?.value?.some(item => item.suUserId === selectedData.id)) {
    return ToastHelper.warning("ไม่สามารถเพิ่มรายชื่อได้", "มีรายชื่อคณะกรรมการนี้อยู่แล้ว");
  }

  if (!isCommittee?.value && committee?.value?.length > 1) {
    return;
  }
  const committeeCode = !isCommittee?.value ? spacialDropdown?.value[0]?.value : undefined;

  committee.value = addSequence(committee?.value, {
    suUserId: selectedData.id,
    fullName: selectedData.name,
    committeePositionsCode: committeeCode,
    fullPositionName: selectedData.positionName?.trim(),
    departmentCode: selectedData.departmentCode,
  } as PurchaseOrderApprovalCommittee);
};

const deleteItem = (index: number): void => {
  committee.value = deleteItemAndReSequence(committee.value as PurchaseOrderApprovalCommittee[], index) as PurchaseOrderApprovalCommittee[];
};

const reOrderDataTable = (event: DataTableRowReorderEvent): void => {
  committee.value = onRowReorder(event) as PurchaseOrderApprovalCommittee[];

  reOrderByPositionOnBoard();
};

const reOrderByPositionOnBoard = () => {
  if (isCommittee.value) {
    const positionData = [...committee.value
      .filter(f => f.committeePositionsCode)
      .sort((a, b) => {
        const codeCompare = a.committeePositionsCode!.localeCompare(b.committeePositionsCode!);
        if (codeCompare !== 0) return codeCompare;
        return a.sequence - b.sequence;
      })];
    const emptyPositionData = [...committee.value.filter(f => !f.committeePositionsCode)];

    committee.value = reSequence([
      ...positionData,
      ...emptyPositionData,
    ]);
  }
};

onMounted(() => {
  getPosBoardDropdownAsync();
});
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader :label="props.label">
        <template #action>
          <Button v-if="isHas && props.disable && menuStore.hasManage" icon="pi pi-plus" label="เพิ่มรายชื่อ"
            severity="primary" variant="outlined" @click="addCommittee" />
        </template>
      </TitleHeader>
      <Radio class="px-4" v-if="props.showOption" v-model="isHas" :options="SharedConstants.HasOptions"
        :disabled="!props.disable || !menuStore.hasManage" @change="committee = []" />
      <div v-if="isHas">
        <Radio class="px-4" v-model="isCommittee" :options="optionTypeData" rules="required"
          @change="() => committee = []" :disabled="!props.disable || !menuStore.hasManage" />
        <div class="mt-4" v-if="isHas">
          <DataTable :value="committee" @row-reorder="(e) => reOrderDataTable(e)">
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">ชื่อ-นามสกุล/ตำแหน่ง</p>
              </template>
              <template #body="{ data }">
                <div>
                  <p>{{ data.fullName }}</p>
                  <small class="text-gray-400">{{ data.positionName }}</small>
                </div>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">ตำแหน่งในคณะกรรมการ</p>
              </template>
              <template #body="{ data, index }">
                <Select v-model="data.committeePositionsCode" :options="dropdownCondition(index).value"
                  @update:modelValue="reOrderByPositionOnBoard" :disabled="!props.disable || !menuStore.hasManage"
                  rules="required" :key="data.suUserId" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
              <template #body="{ index }">
                <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text" @click="deleteItem(index)"
                  v-if="props.disable && menuStore.hasManage" />
              </template>
            </Column>
            <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
              bodyStyle="vertical-align: top;padding-top: 25px">
              <template #body="{ data }">
                <span v-if="data.committeePositionsCode != 'POB001' && props.disable && menuStore.hasManage"
                  class="material-symbols-outlined cursor-pointer" :draggable="true">
                  drag_indicator
                </span>
              </template>
            </Column>
            <template #empty>
              <p class="text-center text-gray-500">ไม่มีข้อมูล</p>
            </template>
          </DataTable>
        </div>
      </div>
    </template>
  </Card>
</template>
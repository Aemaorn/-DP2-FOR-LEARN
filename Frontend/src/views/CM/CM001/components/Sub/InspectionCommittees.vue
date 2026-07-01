<script setup lang="ts">
import { Card, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { ButtonAddUserOutlined } from '@/components/Button';
import { ArrayHelper } from '@/helpers/array';
import { DatatableHelper } from '@/helpers/datable';
import { showUserDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { CommitteeInfo } from '@/views/PP/models/PP005/pp005Model';
import type { CommitteeSection } from '@/models/CM/cm001';
import type { Option } from '@/models/shared/option';
import { computed, onMounted, ref } from 'vue';
import { HttpStatusCode } from 'axios';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import { useMenuStore } from '@/stores/menu';

type Props = {
  label: string;
  isDisabled?: boolean;
  person?: string;
};

const { addSequence, deleteItemAndReSequence, reSequence } = ArrayHelper();
const { onRowReorder } = DatatableHelper();
const props = defineProps<Props>();
const value = defineModel<CommitteeSection>({
  required: true,
  default: () => ({ committees: [], isCommittee: true }),
});
const menuStore = useMenuStore();

const committeeDropdown = ref<Array<Option>>([]);
const personDropdown = ref<Array<Option>>([]);

const spacialDropdown = defineModel<Option[]>('spacialOption');


const committeeTypeData = [
  { value: true, label: "คณะกรรมการ" },
  { value: false, label: props.person ?? "ผู้จัดซื้อจัดจ้าง" },
] as Option[];


const onGetPositionOnBoardDropdownByTypeAsync = async (): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoard, undefined, true);

  if (status === HttpStatusCode.Ok) {
    if (spacialDropdown.value) {
      personDropdown.value = spacialDropdown.value;
      committeeDropdown.value = data.slice(0, -1);
    } else {
      personDropdown.value = data.splice(data.length - 1, 1);
      committeeDropdown.value = data.slice(0, -1);
    }
  }
};

const addItemAsync = async () => {
  if (!value.value) return;
  if (!value.value.isCommittee && value.value.committees.length > 0) return;
  const selectedData = await showUserDialogAsync();

  if (!selectedData) return;

  if (value.value.committees.some(item => item.userId === selectedData.id)) {
    return ToastHelper.warning("ไม่สามารถเพิ่มรายชื่อได้", "รายชื่อซ้ำ");
  }

  const comCode = defaultComCode(value.value.isCommittee);

  const newCommittees = addSequence(value.value.committees, {
    userId: selectedData.id,
    fullName: selectedData.name,
    fullPositionName: selectedData.positionName?.trim(),
    committeePositionsCode: comCode,
    sequence: value.value.committees.length + 1,
  } as CommitteeInfo);

  console.log(newCommittees);


  value.value = { ...value.value, committees: newCommittees };
  reOrderByPositionOnBoard();
};

const defaultComCode = (isCommittee: boolean) => {
  if (isCommittee) return undefined;

  return personDropdown.value[0].value;
}

const deleteItem = (index: number) => {
  if (!value.value) return;
  const newCommittees = deleteItemAndReSequence(value.value.committees, index);
  value.value = { ...value.value, committees: newCommittees };
  reOrderByPositionOnBoard();
};

const reOrderDatatable = (event: DataTableRowReorderEvent) => {
  if (!value.value) return;
  const newCommittees = onRowReorder(event) as CommitteeInfo[];
  value.value = { ...value.value, committees: newCommittees };
  reOrderByPositionOnBoard();
}

const reOrderByPositionOnBoard = () => {
  if (value.value?.isCommittee) {
    const positionData = [...value.value.committees
      .filter(f => f.committeePositionsCode)
      .sort((a, b) => {
        const codeCompare = a.committeePositionsCode!.localeCompare(b.committeePositionsCode!);
        if (codeCompare !== 0) return codeCompare;
        return a.sequence - b.sequence;
      })];
    const emptyPositionData = [...value.value.committees.filter(f => !f.committeePositionsCode)];

    value.value.committees = reSequence([
      ...positionData,
      ...emptyPositionData,
    ]);
  }
};

const hasPosBoard001 = computed(() =>
  value.value?.committees?.some(item => item.committeePositionsCode === 'PosBoard001') ?? false
);

const dropdownCondition = (index: number) => computed(() => {
  if (value.value?.isCommittee) {
    const currentValue = value.value.committees[index]?.committeePositionsCode;
    if (hasPosBoard001.value && currentValue !== 'PosBoard001') {
      return committeeDropdown.value.filter(f => f.value !== 'PosBoard001');
    }
    return committeeDropdown.value;
  }

  return personDropdown.value;
});

const onGetParameterByGroupCodeAsync = async (groupCode: EGroupCode): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(groupCode);

  if (status === HttpStatusCode.Ok) {
    personDropdown.value = data;
  }
};


onMounted(async () => {
  await onGetPositionOnBoardDropdownByTypeAsync();

  if (!personDropdown.value || personDropdown.value.length === 0) {
    const groupCode = props.person === 'ผู้จัดซื้อจัดจ้าง' ? EGroupCode.PosBoardProc : EGroupCode.PosBoardInsp;
    await onGetParameterByGroupCodeAsync(groupCode);
  }
});
</script>

<template>
  <Card v-if="value" class="mb-4">
    <template #content>
      <TitleHeader :label="props.label">
        <template #action>
          <ButtonAddUserOutlined
            v-if="!props.isDisabled && !(!value.isCommittee && value.committees?.length >= 1) && menuStore.hasManage"
            type="button" @click="addItemAsync" />
        </template>
      </TitleHeader>
      <Radio v-model="value.isCommittee" :options="committeeTypeData" @change="() => value.committees = []"
        :disabled="props.isDisabled || !menuStore.hasManage" />
      <div class="px-4 my-4">
        <DataTable :value="value.committees" @row-reorder="reOrderDatatable">
          <Column>
            <template #header>
              <p class="w-full font-bold text-center">ชื่อ-นามสกุล/ตำแหน่ง</p>
            </template>
            <template #body="{ data }">
              <div>
                <p>{{ data.fullName }}</p>
                <small class="text-gray-400">{{ data.fullPositionName }}</small>
              </div>
            </template>
          </Column>
          <Column>
            <template #header>
              <p class="w-full font-bold text-center">ตำแหน่งในคณะกรรมการ</p>
            </template>
            <template #body="{ data, index }">
              <Select v-model="data.committeePositionsCode" hide-details rules="required"
                :disabled="props.isDisabled || !menuStore.hasManage" :options="dropdownCondition(index).value"
                :key="data.userId" @update:modelValue="reOrderByPositionOnBoard" />
            </template>
          </Column>
          <Column class="max-w-[10px]" v-if="!props.isDisabled && menuStore.hasManage">
            <template #body="{ index }">
              <Button icon="pi pi-trash" severity="danger" variant="text" @click="() => deleteItem(index)" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" bodyStyle="padding-top: 20px"
            v-if="!props.isDisabled && value.isCommittee && menuStore.hasManage">
            <template #body>
              <span class="material-symbols-outlined cursor-pointer" :draggable="!props.isDisabled">
                drag_indicator
              </span>
            </template>
          </Column>
          <template #empty>
            <p class="text-center font-bold">ไม่มีข้อมูลรายชื่อ</p>
          </template>
        </DataTable>
      </div>
    </template>
  </Card>
</template>
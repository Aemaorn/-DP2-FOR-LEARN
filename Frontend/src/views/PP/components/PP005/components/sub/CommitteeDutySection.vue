<script setup lang="ts">
import { Card, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { ButtonAddUserOutlined } from '@/components/Button';
import { ArrayHelper } from '@/helpers/array';
import { DatatableHelper } from '@/helpers/datable';
import { showUserDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { CommitteeDuty, CommitteeInfo, DutyInfo } from '@/views/PP/models/PP005/pp005Model';
import type { SectionType } from '@/models/shared/participants';
import type { Option } from '@/models/shared/option';
import { computed, onMounted, ref, watch } from 'vue';
import { HttpStatusCode } from 'axios';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import { useMenuStore } from '@/stores/menu';
import { SharedConstants } from '@/constants';

type Props = {
  label: string;
  isDisabled?: boolean;
  person?: string;
  onShowOption?: boolean;
};

const { addSequence, deleteItemAndReSequence, reSequence } = ArrayHelper();
const { onRowReorder } = DatatableHelper();
const props = defineProps<Props>();
const value = defineModel<CommitteeDuty>({
  required: true,
});

const isHas = defineModel<boolean>('isHas', {
  default: true,
});

const menuStore = useMenuStore();

const committeeDropdown = ref<Array<Option>>([]);
const personDropdown = ref<Array<Option>>([]);

const spacialDropdown = defineModel<Option[]>('spacialOption');


const committeeTypeData = [
  { value: true, label: "คณะกรรมการ" },
  { value: false, label: props.person ?? "ผู้จัดซื้อจัดจ้าง" },
] as Option[];

const keyMapData: Record<SectionType, keyof CommitteeDuty> = {
  Committee: 'committees',
  Duty: 'duties'
};

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

const addItemAsync = async (type: SectionType) => {
  switch (type) {
    case 'Committee': {
      if (!value.value.isCommittee && value.value.committees.length > 0) return;
      const selectedData = await showUserDialogAsync();

      if (!selectedData) return;

      if (value.value.committees.some(item => item.userId === selectedData.id)) {
        return ToastHelper.warning("ไม่สามารถเพิ่มรายชื่อได้", "รายชื่อซ้ำ");
      }

      const comCode = defaultComCode(value.value.isCommittee);

      value.value.committees = addSequence(value.value.committees, {
        userId: selectedData.id,
        fullName: selectedData.name,
        fullPositionName: selectedData.positionName?.trim(),
        committeePositionsCode: comCode,
      } as CommitteeInfo);

      reOrderByPositionOnBoard();
      break;
    }
    case 'Duty': {
      value.value.duties = addSequence(value.value.duties, {} as DutyInfo);
      break;
    }
  };
};

const defaultComCode = (isCommittee: boolean) => {
  if (isCommittee) return undefined;

  return personDropdown.value[0].value;
}

const deleteItem = (type: SectionType, index: number) => {
  value.value[keyMapData[type]] = deleteItemAndReSequence(value.value[keyMapData[type]] as any, index) as any;

  reOrderByPositionOnBoard();
};

const reOrderDatatable = (type: SectionType, event: DataTableRowReorderEvent) => {
  if (type == 'Committee') {
    value.value.committees = onRowReorder(event) as CommitteeInfo[];

    reOrderByPositionOnBoard();
  }

  if (type == 'Duty') {
    value.value.duties = onRowReorder(event);
  }
}

const reOrderByPositionOnBoard = () => {
  checkAndUpdateIsCommittee();

  if (value.value.isCommittee) {
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
  value.value.committees.some(item => item.committeePositionsCode === 'PosBoard001')
);

const dropdownCondition = (index: number) => computed(() => {
  if (value.value.isCommittee) {
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

const checkAndUpdateIsCommittee = () => {
  if (value.value.committees.length > 0) {
    const firstCommittee = value.value.committees[0];
    const code = firstCommittee.committeePositionsCode;

    if (code && (
      code === 'PosBoardSup001' ||
      code === 'PosBoardMA001' ||
      code === 'PosBoardInsp001' ||
      code === 'PosBoardProc001' ||
      code.startsWith('PosBoardMA') ||
      code.startsWith('PosBoardSup') ||
      code.startsWith('PosBoardProc') ||
      code.startsWith('PosBoardInsp')
    )) {
      value.value.isCommittee = false;
    }
    else if (value.value.committees.length > 1) {
      value.value.isCommittee = true;
    }
    else if (code && !code.match(/^PosBoard(MA|Sup|Proc|Insp)/)) {
      value.value.isCommittee = true;
    }
  }
};

watch(
  () => value.value.committees.map(c => c.committeePositionsCode).join(','),
  () => {
    checkAndUpdateIsCommittee();
  },
  { immediate: true, deep: true }
);

watch(
  () => value.value.committees.length,
  () => {
    checkAndUpdateIsCommittee();
  },
  { immediate: true }
);


onMounted(async () => {
  await onGetPositionOnBoardDropdownByTypeAsync();

  if (!personDropdown.value || personDropdown.value.length === 0) {
    let groupCode: EGroupCode;

    switch (props.person) {
      case 'ผู้จัดซื้อจัดจ้าง':
        groupCode = EGroupCode.PosBoardProc;
        break;
      case 'ผู้ตรวจรับพัสดุงานจ้างบริการบำรุงรักษา':
        groupCode = EGroupCode.PosBoardMA;
        break;
      case 'ผู้ควบคุมงาน':
        groupCode = EGroupCode.PosBoardSup;
        break;
      default:
        groupCode = EGroupCode.PosBoardInsp;
        break;
    }

    await onGetParameterByGroupCodeAsync(groupCode);
  }

  checkAndUpdateIsCommittee();

  if (value.value.isCommittee === undefined) {
    value.value.isCommittee = false;
  }
});
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader :label="props.label">
        <template #action>
          <ButtonAddUserOutlined
            v-if="isHas && !props.isDisabled && !(!value.isCommittee && value.committees.length >= 1) && menuStore.hasManage"
            type="button" @click="() => addItemAsync('Committee')" />
        </template>
      </TitleHeader>
      <Radio class="px-4" v-if="props.onShowOption" v-model="isHas" :options="SharedConstants.HasOptions"
        :disabled="props.isDisabled || !menuStore.hasManage"
        @change="() => { value.committees = []; value.duties = []; }" />
      <div v-if="isHas">
        <Radio v-model="value.isCommittee" :options="committeeTypeData" @change="() => value.committees = []"
          :disabled="props.isDisabled || !menuStore.hasManage" />
        <div class="px-4 my-4">
          <DataTable :value="value.committees" @row-reorder="(e) => reOrderDatatable('Committee', e)">
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
                <Button icon="pi pi-trash" severity="danger" variant="text"
                  @click="() => deleteItem('Committee', index)" />
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
        <div class="px-4 mt-10">
          <TitleHeader label="อำนาจหน้าที่" hiddenIcon>
            <template #action>
              <Button v-if="!props.isDisabled && menuStore.hasManage" label="เพิ่มอำนาจหน้าที่" icon="pi pi-plus"
                severity="primary" variant="outlined" class="bg-white! hover:bg-red-50!"
                @click="() => addItemAsync('Duty')" />
            </template>
          </TitleHeader>
          <div class="mt-4">
            <DataTable :value="value.duties" @row-reorder="(e) => reOrderDatatable('Duty', e)">
              <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
                <template #header>
                  <p class="w-full font-bold text-center">ลำดับ</p>
                </template>
                <template #body="{ data }">
                  <div>
                    <p class="text-center">{{ data.sequence }}</p>
                  </div>
                </template>
              </Column>
              <Column bodyStyle="vertical-align: top">
                <template #header>
                  <p class="w-full font-bold text-center">รายละเอียด</p>
                </template>
                <template #body="{ data }">
                  <InputField v-model="data.description" :disabled="props.isDisabled || !menuStore.hasManage"
                    rules="required" />
                </template>
              </Column>
              <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top"
                v-if="!props.isDisabled && menuStore.hasManage">
                <template #body="{ index }">
                  <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                    @click="() => deleteItem('Duty', index)" />
                </template>
              </Column>
              <Column rowReorder headerStyle="width: 3rem" bodyStyle="vertical-align: top;padding-top: 25px"
                v-if="!props.isDisabled && value.isCommittee && menuStore.hasManage">
                <template #rowreordericon>
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
        </div>
      </div>
    </template>
  </Card>
</template>
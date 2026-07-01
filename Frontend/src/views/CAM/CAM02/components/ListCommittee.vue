<script setup lang="ts">
import { Card, Button, DataTable, Column, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Select, Radio } from '@/components/forms';
import { CommitteeType, type TListCam02Committee } from '@/models/CAM/CAM02/cam02';
import { computed, onMounted, ref, watch } from 'vue';
import type { Option } from '@/models/shared/option';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { DatatableHelper } from '@/helpers/datable';
import { ArrayHelper } from '@/helpers/array';
import { showUserDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import { useCam02DetailStore } from '@/stores/CAM/CAM02/cam02Store';

type Props = {
  label: string;
  readonly?: boolean;
  committeeType?: CommitteeType;
  person?: string;
};

const { addSequence, deleteItemAndReSequence, reSequence } = ArrayHelper();
const { onRowReorder } = DatatableHelper();

const props = withDefaults(defineProps<Props>(), { label: '', person: 'บุคคล', readonly: false });

const value = defineModel<TListCam02Committee[]>({ default: (): TListCam02Committee[] => [] });

const isCommittee = defineModel<boolean>('isCommittee', { default: true });

const spacialOption = defineModel<Option[]>('spacialOption', { default: (): Option[] => [] });

const store = useCam02DetailStore();

const isInspectionType = computed<boolean>((): boolean =>
  props.committeeType === CommitteeType.InspectionCommittee ||
  props.committeeType === CommitteeType.AcceptanceCommittee
);

const hasPosBoard001 = computed((): boolean =>
  value.value.some((item): boolean => item.committeePositionsCode === 'PosBoard001')
);

const dropdownCondition = (index: number) => computed((): Option[] => {
  if (isCommittee.value) {
    const currentValue = value.value[index]?.committeePositionsCode;

    let options = posBoardDropdown.value.filter((f): boolean => f.value !== 'PosBoard006');
    if (hasPosBoard001.value && currentValue !== 'PosBoard001') {
      options = options.filter((f): boolean => f.value !== 'PosBoard001');
    }
    return options;
  }

  if (isInspectionType.value) {
    return posBoardInspDropdown.value.filter((f): boolean => f.value === 'PosBoardInsp001');
  }

  return spacialOption.value.length > 0
    ? spacialOption.value
    : posBoardDropdown.value.filter((f): boolean => f.value === 'PosBoard006');
});

const posBoardDropdown = ref<Option[]>([]);
const posBoardInspDropdown = ref<Option[]>([]);
const isDropdownLoaded = ref<boolean>(false);

const getPosBoardDropdownAsync = async (): Promise<void> => {
  const [boardResult, inspResult] = await Promise.all([
    SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoard, undefined, true),
    isInspectionType.value
      ? SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoardInsp, undefined, true)
      : Promise.resolve({ data: [] as Option[], status: HttpStatusCode.Ok }),
  ]);

  if (boardResult.status === HttpStatusCode.Ok) {
    posBoardDropdown.value = boardResult.data;
  }

  if (inspResult.status === HttpStatusCode.Ok) {
    posBoardInspDropdown.value = inspResult.data;
  }

  isDropdownLoaded.value = true;
};

const optionTypeData = [
  { value: true, label: 'คณะกรรมการ' },
  { value: false, label: props.person },
] as Option[];

const addCommittee = async (): Promise<void> => {
  const selectedData = await showUserDialogAsync();

  if (!selectedData) return;

  if (value.value.some((item): boolean => item.suUserId === selectedData.id)) {
    return ToastHelper.warning('ไม่สามารถเพิ่มรายชื่อได้', 'มีรายชื่อคณะกรรมการนี้อยู่แล้ว');
  }

  if (!isCommittee.value && value.value.length >= 1) {
    return;
  }

  const currentDropdown = isInspectionType.value ? posBoardInspDropdown.value : posBoardDropdown.value;
  const committeeCode = !isCommittee.value ? currentDropdown.find((f): boolean => f.value === (isInspectionType.value ? 'PosBoardInsp001' : 'PosBoard001'))?.value : undefined;

  value.value = addSequence(value.value, {
    suUserId: selectedData.id,
    fullName: selectedData.name,
    fullPositionName: selectedData.positionName,
    committeePositionsCode: committeeCode,
  } as TListCam02Committee);
};

const deleteItem = (index: number): void => {
  value.value = deleteItemAndReSequence(value.value as TListCam02Committee[], index) as TListCam02Committee[];
};

const reOrderByPositionOnBoard = (): void => {
  if (isCommittee.value) {
    const positionData = [...value.value
      .filter((f): boolean => !!f.committeePositionsCode)
      .sort((a, b): number => {
        const codeCompare = a.committeePositionsCode!.localeCompare(b.committeePositionsCode!);
        if (codeCompare !== 0) return codeCompare;
        return a.sequence - b.sequence;
      })];
    const emptyPositionData = [...value.value.filter((f): boolean => !f.committeePositionsCode)];

    value.value = reSequence([
      ...positionData,
      ...emptyPositionData,
    ]) as TListCam02Committee[];
  }
};

const reOrderDataTable = (event: DataTableRowReorderEvent): void => {
  value.value = onRowReorder(event) as TListCam02Committee[];
  reOrderByPositionOnBoard();
};

const onChangePosition = (userId: string, positionCode: string): void => {
  const findIndex = store.procurementDetail.newCommittees.findIndex((c): boolean => c.suUserId === userId);
  const allOptions = [...posBoardDropdown.value, ...posBoardInspDropdown.value];
  const findPositionName = allOptions.find((p): boolean => p.value === positionCode);

  if (findIndex < 0 || !findPositionName) return;

  store.procurementDetail.newCommittees[findIndex].committeePositionsName = findPositionName.label;
};

const deriveIsCommittee = (): void => {
  if (value.value.length > 0 && value.value[0].committeePositionsCode) {
    const personCodes = ['PosBoard006', 'PosBoardInsp001'];
    isCommittee.value = !personCodes.includes(value.value[0].committeePositionsCode);
  }
};

watch(
  (): CommitteeType | undefined => props.committeeType,
  async (): Promise<void> => {
    isDropdownLoaded.value = false;
    await getPosBoardDropdownAsync();
  }
);

watch(value, (): void => {
  deriveIsCommittee();
});

onMounted((): void => {
  deriveIsCommittee();
  getPosBoardDropdownAsync();
});
</script>

<template>
  <Card class="mt-4 mb-4">
    <template #content>
      <TitleHeader :label="props.label">
        <template #action>
          <Button v-if="!props.readonly && !(isCommittee === false && value.length >= 1)" icon="pi pi-plus" label="เพิ่มรายชื่อ" severity="warn" variant="outlined"
            @click="addCommittee" />
        </template>
      </TitleHeader>
      <Radio v-model="isCommittee" :options="optionTypeData" :disabled="props.readonly"
        @change="() => value = []" />
      <div class="mt-4" v-if="isDropdownLoaded">
        <DataTable :value="value" @row-reorder="(e) => reOrderDataTable(e)">
          <Column bodyStyle="vertical-align: top">
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
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">ตำแหน่งในคณะกรรมการ</p>
            </template>
            <template #body="{ data, index }">
              <Select v-model="data.committeePositionsCode" :options="dropdownCondition(index).value"
                @update:modelValue="reOrderByPositionOnBoard"
                @on-select="(e) => onChangePosition(data.suUserId, e)" rules="required" :key="data.suUserId"
                :disabled="props.readonly" />
            </template>
          </Column>
          <Column v-if="!props.readonly" bodyStyle="vertical-align: top" class="max-w-[10px]">
            <template #body="{ index }">
              <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                @click="deleteItem(index)" />
            </template>
          </Column>
          <Column v-if="!props.readonly" rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
            bodyStyle="vertical-align: top;padding-top: 25px">
            <template #body="{ data }">
              <span v-if="data.committeePositionsCode !== 'PosBoard001' && data.committeePositionsCode !== 'PosBoardInsp001' && !props.readonly"
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
    </template>
  </Card>
</template>

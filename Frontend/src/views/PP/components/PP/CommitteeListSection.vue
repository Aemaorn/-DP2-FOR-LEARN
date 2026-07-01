<script setup lang="ts">
import { Card, DataTable, type DataTableRowReorderEvent } from 'primevue';
import { InputArea, Radio, Select } from '@/components/forms';
import { ButtonAddUserOutlined } from '@/components/Button';
import { TitleHeader } from '@/components/cosmetic';
import { showConfirmDialogAsync, showUserDialogAsync } from '@/helpers/dialog';
import type { TCommitteeDutyList, TCommitteeSection, TDutySection } from '../../../../models/PP/ppModel';
import { ArrayHelper } from '@/helpers/array';
import { DatatableHelper } from '@/helpers/datable';
import { usePP001DetailStore } from '../../stores/PP001/pp001Store';
import type { Option } from '@/models/shared/option';
import ToastHelper from '@/helpers/toast';
import type { SectionType } from '@/models/shared/participants';
import { computed } from 'vue';

type Props = {
  label: string;
  isEdit?: boolean;
  copyType?: boolean;
  copyList?: TCommitteeSection[];
  readonly?: boolean;
};

const preProcurement001DetailStore = usePP001DetailStore();

const { addSequence, deleteItemAndReSequence, } = ArrayHelper();
const { onRowReorder } = DatatableHelper();

const props = withDefaults(defineProps<Props>(), {
  isEdit: false,
  readonly: false,
});

const committeeSection = defineModel<TCommitteeSection[]>('committeeSection', { required: true });
const dutySection = defineModel<TDutySection[]>('dutySection', { required: true });
const isCommittee = defineModel<boolean>('isCommittee', { default: true, required: true });

const sectionKeyMap: Record<SectionType, keyof TCommitteeDutyList> = {
  Committee: 'committeeSection',
  Duty: 'dutySection',
};

const addCommittee = async (): Promise<void> => {
  const selectedData = await showUserDialogAsync();

  if (!selectedData) return;

  if (committeeSection.value.some(item => item.userId === selectedData.id)) {
    return ToastHelper.warning("ไม่สามารถเพิ่มรายชื่อได้", "มีรายชื่อคณะกรรมการนี้อยู่แล้ว");
  }

  if (!isCommittee.value && committeeSection.value.length === 1) {
    return;
  }

  committeeSection.value = addSequence(committeeSection.value, {
    userId: selectedData.id,
    fullName: selectedData.name,
    fullPositionName: selectedData.positionName,
    departmentCode: selectedData.departmentCode,
    departmentName: selectedData.departmentName,
    committeePositionsCode: isCommittee.value ? undefined : preProcurement001DetailStore.pob1Dropdown[0].value,
  } as TCommitteeSection);
};

const addDuty = (): void => {
  dutySection.value.push({
    sequence: dutySection.value.length + 1,
  } as TDutySection);
};

const committeeTypeData = [
  { value: true, label: "คณะกรรมการ" },
  { value: false, label: "ผู้จัดทำ" },
] as Option[];


const deleteItem = (type: SectionType, index: number): void => {
  const key = sectionKeyMap[type];

  if (key == 'committeeSection') {
    committeeSection.value = deleteItemAndReSequence(committeeSection.value as TCommitteeSection[], index) as TCommitteeSection[];
  }

  if (key == 'dutySection') {
    dutySection.value = deleteItemAndReSequence(dutySection.value as TDutySection[], index) as TDutySection[];
  }
};

const reOrderDataTable = (type: SectionType, event: DataTableRowReorderEvent): void => {
  const key = sectionKeyMap[type];

  if (key == 'committeeSection') {
    committeeSection.value = onRowReorder(event) as TCommitteeSection[];

    reOrderPositionCommitteeSection()
  }

  if (key == 'dutySection') {
    dutySection.value = onRowReorder(event) as TDutySection[];
  }
};

const reOrderPositionCommitteeSection = () => {
  const current = committeeSection.value;

  const nonNullCodes = Array.from(
    new Set(
      current
        .map(x => x.committeePositionsCode)
        .filter((c): c is string => c != null)
    )
  ).sort((a, b) => a.localeCompare(b));

  const sorted: TCommitteeSection[] = [];
  nonNullCodes.forEach(code => {
    const group = current
      .filter(x => x.committeePositionsCode === code)
      .sort((a, b) => (a.sequence ?? 0) - (b.sequence ?? 0));
    sorted.push(...group);
  });

  const nullGroup = current
    .filter(x => x.committeePositionsCode == null)
    .sort((a, b) => (a.sequence ?? 0) - (b.sequence ?? 0));
  sorted.push(...nullGroup);

  sorted.forEach((item, idx) => {
    item.sequence = idx + 1;
  });

  console.log(sorted);

  committeeSection.value = sorted;
};

const hasPosBoard001 = computed(() =>
  committeeSection.value.some(item => item.committeePositionsCode === 'PosBoard001')
);

const dropdownCondition = (index: number) => computed(() => {
  if (isCommittee.value) {
    const currentValue = committeeSection.value[index]?.committeePositionsCode;

    if (hasPosBoard001.value && currentValue !== 'PosBoard001') {
      return preProcurement001DetailStore.pobDropdown.filter(f => f.value !== 'PosBoard001');
    }

    return preProcurement001DetailStore.pobDropdown;
  }

  return preProcurement001DetailStore.pob1Dropdown;
});

const onConfirmCopy = async () => {
  if (!props.copyList) return;

  if (committeeSection.value.length > 0) {
    if (!await showConfirmDialogAsync(undefined, "ต้องการคัดลอกบุคคล/คณะกรรมการ  หาก \"ยืนยัน\" รายชื่อทั้งหมดจะถูกแทนที่")) return;
  }

  isCommittee.value = props.copyType;
  committeeSection.value = [...props.copyList.map((s): TCommitteeSection => ({
    ...s,
    id: undefined,
  }))];
}
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader :label="props.label">
        <template #action v-if="isEdit">
          <Button v-if="copyList && copyList.length > 0" severity="success" icon="pi pi-clone"
            label="คัดลอกบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงาน" type="button" @click="onConfirmCopy" />

          <ButtonAddUserOutlined v-if="isEdit && ((!isCommittee && committeeSection.length < 1) || isCommittee)"
            type="button" @click="addCommittee" />
        </template>
      </TitleHeader>
      <Radio v-model="isCommittee" :options="committeeTypeData" :disabled="!isEdit || props.readonly" rules="required"
        @change="() => committeeSection = []" />
      <div class="px-4 my-4">
        <DataTable :value="committeeSection" @row-reorder="(e) => reOrderDataTable('Committee', e)">
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
              <Select v-model="data.committeePositionsCode" hide-details rules="required" :disabled="!isEdit || props.readonly"
                :options="dropdownCondition(index).value" @update:modelValue="reOrderPositionCommitteeSection"
                :key="data.userId" />
            </template>
          </Column>
          <Column class="max-w-[10px]">
            <template #body="{ index }">
              <Button v-if="isEdit" icon="pi pi-trash" severity="danger" variant="text"
                @click="() => deleteItem('Committee', index)" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false" bodyStyle="padding-top: 20px">
            <template #body="{ data }">
              <span v-if="data.committeePositionsCode != 'POB001' && isEdit"
                class="material-symbols-outlined cursor-pointer" :draggable="true">
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
            <Button v-if="isEdit" label="เพิ่มอำนาจหน้าที่" icon="pi pi-plus" severity="primary" variant="outlined"
              class="bg-white! hover:bg-red-50!" @click="addDuty" />
          </template>
        </TitleHeader>
        <div class="mt-4">
          <DataTable :value="dutySection" @row-reorder="(e) => reOrderDataTable('Duty', e)">
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
                <InputArea :row="1" rules="required" :disabled="!isEdit || props.readonly" v-model="data.description" />
              </template>
            </Column>
            <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top">
              <template #body="{ index }">
                <Button v-if="isEdit" icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                  @click="() => deleteItem('Duty', index)" />
              </template>
            </Column>
            <Column rowReorder headerStyle="width: 3rem" bodyStyle="vertical-align: top;padding-top: 25px">
              <template #rowreordericon>
                <span v-if="isEdit" class="material-symbols-outlined cursor-pointer" :draggable="true">
                  drag_indicator
                </span>
              </template>
            </Column>
            <template #empty>
              <p class="text-center font-bold">ไม่พบข้อมูล</p>
            </template>
          </DataTable>
        </div>
      </div>
    </template>
  </Card>
</template>
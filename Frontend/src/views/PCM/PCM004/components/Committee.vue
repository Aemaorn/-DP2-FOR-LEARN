<script setup lang="ts">
import { Card, Button, DataTable, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Select, Radio } from '@/components/forms';
import SharedConstants from '@/constants/shared';
import { ArrayHelper } from '@/helpers/array';
import type { Option } from '@/models/shared/option';
import { showUserDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import { DatatableHelper } from '@/helpers/datable';
import type { Pcm004CommitteeType } from '@/enums/pcm004';
import type { Pcm004Committee } from '@/models/PCM/pcm004';

type Props = {
  showOption: boolean;
  label: string;
  groupType: Pcm004CommitteeType;
  disable: boolean;
}
const { addSequence, deleteItemAndReSequence } = ArrayHelper();
const { onRowReorder } = DatatableHelper();

const props = withDefaults(defineProps<Props>(), { showOption: false, label: '', person: 'ผู้จัดซื้อ', disable: false });

const committee = defineModel<Pcm004Committee[]>('committee', {
  required: true,
});

const spacialDropdown = defineModel<Option[]>('spacialOption', {
  required: true,
  default: () => [],
});

const addCommittee = async (): Promise<void> => {
  const selectedData = await showUserDialogAsync();

  if (!selectedData) return;

  if (committee.value.some(item => item.userId === selectedData.id)) {
    return ToastHelper.warning("ไม่สามารถเพิ่มรายชื่อได้", "มีรายชื่อคณะกรรมการนี้อยู่แล้ว");
  }

  const committeeCode = !spacialDropdown.value[0].value ? undefined : spacialDropdown.value[0].value;

  committee.value = addSequence(committee.value, {
    groupType: props.groupType,
    userId: selectedData.id,
    fullName: selectedData.name,
    committeePositionCode: committeeCode,
    positionName: selectedData.positionName,
  } as Pcm004Committee);
};

const deleteItem = (index: number): void => {
  committee.value = deleteItemAndReSequence(committee.value as Pcm004Committee[], index) as Pcm004Committee[];
};

const reOrderDataTable = (event: DataTableRowReorderEvent): void => {
  committee.value = onRowReorder(event) as Pcm004Committee[];

  reOrderPositionCommitteeSection();
};

const reOrderPositionCommitteeSection = () => {
  const current = committee.value;

  const nonNullCodes = Array.from(
    new Set(
      current
        .map(x => x.committeePositionCode)
        .filter((c): c is string => c != null)
    )
  ).sort((a, b) => a.localeCompare(b));

  const sorted: Pcm004Committee[] = [];
  nonNullCodes.forEach(code => {
    const group = current
      .filter(x => x.committeePositionCode === code)
      .sort((a, b) => (a.sequence ?? 0) - (b.sequence ?? 0));
    sorted.push(...group);
  });

  const nullGroup = current
    .filter(x => x.committeePositionCode == null)
    .sort((a, b) => (a.sequence ?? 0) - (b.sequence ?? 0));
  sorted.push(...nullGroup);

  sorted.forEach((item, idx) => {
    item.sequence = idx + 1;
  });

  committee.value = sorted;
};

</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader :label="props.label">
        <template #action>
          <Button v-if="!props.disable && !(committee.length > 0)" icon="pi pi-plus" label="เพิ่มรายชื่อ"
            severity="primary" variant="outlined" @click="addCommittee" />
        </template>
      </TitleHeader>
      <Radio class="px-4" v-if="props.showOption" :options="SharedConstants.HasOptions" :disabled="!props.disable"
        @change="committee = []" />
      <div>
        <div class="mt-4">
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
              <template #body="{ data }">
                <Select v-model="data.committeePositionCode" :options="spacialDropdown"
                  @update:modelValue="reOrderPositionCommitteeSection" :disabled="props.disable" rules="required"
                  :key="data.suUserId" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
              <template #body="{ index }">
                <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text" @click="deleteItem(index)"
                  v-if="!props.disable" />
              </template>
            </Column>
            <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
              bodyStyle="vertical-align: top;padding-top: 25px">
              <template #body="{ data }">
                <span v-if="data.committeePositionCode != 'POB001' && props.disable"
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
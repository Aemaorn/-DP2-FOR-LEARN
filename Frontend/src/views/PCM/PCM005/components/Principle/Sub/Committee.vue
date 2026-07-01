<script setup lang="ts">
import { CommitteeGroupType } from '@/enums/PCM005/principle';
import type { Committee } from '@/models/PCM/PCM005/principle';
import type { Option } from '@/models/shared/option';
import { Select } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { ref, watch } from 'vue';
import { usePcm005PrincipleStore } from '@/stores/PCM/PCM005/principle';
import { showUserDialogAsync } from '@/helpers/dialog';
import { ArrayHelper } from '@/helpers/array';
import ToastHelper from '@/helpers/toast';
import { useMenuStore } from '@/stores/menu';

type Props = {
  title: string;
  type: CommitteeGroupType,
}

const props = defineProps<Props>();

const { reSequence } = ArrayHelper();
const menuStore = useMenuStore();
const store = usePcm005PrincipleStore();

const data = ref<Committee[]>([]);
const positionOnBoards = ref<Option[]>([]);

const committeeTypeData = [
  { value: true, label: "คณะกรรมการ" },
  { value: false, label: "ผู้จัดทำ" },
] as Option[];

const addUserAsync = async (): Promise<void> => {
  const res = await showUserDialogAsync();

  if (!res) return;

  const findDupUser = store.body.committees.find(c => c.groupType === props.type && c.userId === res.id);

  if (findDupUser) {
    return ToastHelper.warning('เพิ่มรายการ', 'ไม่สามารถเพิ่มข้อมูลซ้ำได้');
  }

  if (!res) return;

  const isNonCommittee = props.type === CommitteeGroupType.RentCommittee ? store.body.isRentCommittee : store.body.isAcceptanceCommittee;

  const data: Committee = {
    userId: res.id,
    fullName: res.name,
    groupType: props.type,
    sequence: store.body.committees?.filter(c => c.groupType === props.type)?.length + 1,
    fullPositionName: res.positionName,
    committeePositionsCode: !isNonCommittee ? store.posOnBardNonCommitteeDDL[0].value as unknown as string : undefined,
    committeePositionsName: !isNonCommittee ? store.posOnBardNonCommitteeDDL[0].label : undefined,
  };

  store.body.committees.push(data);
};

const removeUser = (userId: string): void => {
  const findIndex = store.body.committees.findIndex(c => c.groupType === props.type && c.userId === userId);

  if (findIndex > -1) {
    store.body.committees.splice(findIndex, 1);

    const filterData = store.body.committees.filter(c => c.groupType === props.type);

    if (filterData && filterData.length > 0) {
      filterData.forEach((f, i) => {
        f.sequence = i + 1;
      });
    }
  }
};

const sortData = (dataSort: Committee[]) => {
  const filterNullPosition = dataSort.filter(d => !d.committeePositionsCode);
  const filterHasPosition = dataSort.filter(d => d.committeePositionsCode);
  filterHasPosition.sort((a, b) => (a.committeePositionsCode ?? '').localeCompare(b.committeePositionsCode ?? ''));

  const filterData = store.body.committees.filter(c => c.groupType !== props.type);

  let mergeCommittee = [...filterHasPosition, ...filterNullPosition];
  mergeCommittee = reSequence(mergeCommittee);

  return [...filterData, ...mergeCommittee];
};


const reorderData = (dataList: Committee[]): void => {
  store.body.committees = store.body.committees.filter(c => c.groupType !== props.type);

  dataList.forEach((d, i) => {
    d.sequence = i + 1;

    store.body.committees.push(d);
  });

  store.body.committees = sortData(dataList);
};

const onChangePosition = (userId: string, positionCode: string): void => {
  const findIndex = store.body.committees.findIndex(c => c.groupType === props.type && c.userId === userId);
  const findPostionName = store.positionOnBoardDDL.find(p => p.value === positionCode);

  if (findIndex < 0 || !findPostionName) return;

  store.body.committees[findIndex].committeePositionsName = findPostionName.label;
  store.body.committees = sortData(data.value);
};

const filterPositionOnBoardDDL = (): void => {
  if (!data.value || data.value.length === 0) return;

  if (data.value.some(s => s.committeePositionsCode === 'PosBoard001')) {
    const findIndex = positionOnBoards.value.findIndex(v => v.value === 'PosBoard001');

    if (findIndex > -1) {
      positionOnBoards.value.splice(findIndex, 1);
    }
  } else {
    positionOnBoards.value = [...store.positionOnBoardDDL];
  }
};

const onChangeIsCommittee = () => {
  store.body.committees = store.body.committees.filter(f => f.groupType !== props.type);
}

watch(() => store.body.committees, (newValue) => {
  if (props.type) {
    data.value = newValue.filter(c => c.groupType === props.type);
  }

  filterPositionOnBoardDDL();
}, { deep: true });

watch(() => store.positionOnBoardDDL, (newValue) => {
  positionOnBoards.value = [...newValue];
});
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader :label="props.title">
        <template #action>
          <Button label="เพิ่มรายชื่อ" icon="pi pi-plus" severity="primary" variant="outlined" @click="addUserAsync"
            v-if="store.status.canEdit && menuStore.hasManage" />
        </template>
      </TitleHeader>
      <Radio v-if="props.type === CommitteeGroupType.RentCommittee" v-model="store.body.isRentCommittee"
        :options="committeeTypeData" :disabled="!store.status.canEdit || !menuStore.hasManage"
        @change="() => onChangeIsCommittee()" rules="required" />
      <Radio v-if="props.type === CommitteeGroupType.AcceptanceCommittee" v-model="store.body.isAcceptanceCommittee"
        :options="committeeTypeData" :disabled="!store.status.canEdit || !menuStore.hasManage" rules="required"
        @change="() => onChangeIsCommittee()" />
      <DataTable :value="data" @row-reorder="(e) => reorderData(e.value)">
        <Column field="order" headerStyle="width: 30rem; min-width: 30rem; max-width: 30rem">
          <template #header>
            <p class="w-full font-bold text-center">ชื่อ-นามสกุล/ตำแหน่ง</p>
          </template>
          <template #body="{ data }">
            <p>{{ data.fullName }}</p>
            <small class="opacity-50">{{ data.fullPositionName }}</small>
          </template>
        </Column>
        <Column field="list">
          <template #header>
            <p class="w-full font-bold text-center">ตำแหน่งในคณะกรรมการ</p>
          </template>
          <template #body="{ data }">
            <Select class="mt-8" v-model="data.committeePositionsCode"
              v-if="props.type === CommitteeGroupType.RentCommittee"
              :options="!store.body.isRentCommittee ? store.posOnBardNonCommitteeDDL : data.committeePositionsCode === 'PosBoard001' ? store.positionOnBoardDDL : positionOnBoards"
              rules="required" @on-select="(e) => onChangePosition(data.userId, e)" :key="data.userId"
              :disabled="!store.status.canEdit || !menuStore.hasManage" />
            <Select class="mt-8" v-model="data.committeePositionsCode"
              v-if="props.type === CommitteeGroupType.AcceptanceCommittee"
              :options="!store.body.isAcceptanceCommittee ? store.posOnBardNonCommitteeDDL : data.committeePositionsCode === 'PosBoard001' ? store.positionOnBoardDDL : positionOnBoards"
              rules="required" @on-select="(e) => onChangePosition(data.userId, e)" :key="data.userId"
              :disabled="!store.status.canEdit || !menuStore.hasManage" />
          </template>
        </Column>
        <Column field="control" class="text-end" v-if="store.status.canEdit && menuStore.hasManage">
          <template #body="{ data }">
            <i class="pi pi-trash mt-4 text-red-600 cursor-pointer" @click="() => removeUser(data.userId)" />
          </template>
        </Column>
        <Column rowReorder headerStyle="width: 3rem" bodyStyle="padding-top: 25px"
          v-if="store.status.canEdit && menuStore.hasManage">
          <template #rowreordericon>
            <span class="material-symbols-outlined cursor-pointer" :draggable="true">
              drag_indicator
            </span>
          </template>
        </Column>
        <template #empty>
          <p class="text-center my-4">ไม่มีข้อมูล</p>
        </template>
      </DataTable>
    </template>
  </Card>
</template>

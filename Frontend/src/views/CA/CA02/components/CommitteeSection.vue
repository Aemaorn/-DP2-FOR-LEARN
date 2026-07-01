<script setup lang="ts">
import { Card, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { ButtonAddUserOutlined } from '@/components/Button';
import { ArrayHelper } from '@/helpers/array';
import { DatatableHelper } from '@/helpers/datable';
import { showUserDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { ParticipantsCommitteeAcceptor } from '@/models/shared/participants';
import type { Option } from '@/models/shared/option';
import { computed, onMounted, ref } from 'vue';
import { HttpStatusCode } from 'axios';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import { AcceptorType, AcceptorStatus } from '@/enums/participants';


type Props = {
  isDisabled?: boolean;
};

const { addSequence, deleteItemAndReSequence, reSequence } = ArrayHelper();
const { onRowReorder } = DatatableHelper();
const props = defineProps<Props>();
const acceptors = defineModel<ParticipantsCommitteeAcceptor[]>({ required: true });

const isCommittee = defineModel<boolean>('isCommittee', { default: true });
const committeeDropdown = ref<Option[]>([]);
const personDropdown = ref<Option[]>([]);

const committeeTypeData: Option[] = [
  { value: true, label: 'คณะกรรมการ' },
  { value: false, label: 'ผู้ตรวจรับพัสดุ' },
];

const hasPosBoard001 = computed(() =>
  acceptors.value.some(a => a.committeePositionsCode === 'PosBoard001')
);

const dropdownCondition = (index: number) => computed(() => {
  if (isCommittee.value) {
    const current = acceptors.value[index]?.committeePositionsCode;
    if (hasPosBoard001.value && current !== 'PosBoard001') {
      return committeeDropdown.value.filter(f => f.value !== 'PosBoard001');
    }
    return committeeDropdown.value;
  }
  return personDropdown.value;
});

const onGetDropdownAsync = async (): Promise<void> => {
  const { data: items, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PosBoard, undefined, true);
  if (status !== HttpStatusCode.Ok) return;
  personDropdown.value = items.splice(items.length - 1, 1);
  committeeDropdown.value = items.slice(0, -1);
};

const defaultPositionCode = () => isCommittee.value ? undefined : personDropdown.value[0]?.value as string | undefined;

const reOrderArray = (arr: ParticipantsCommitteeAcceptor[]): ParticipantsCommitteeAcceptor[] => {
  if (!isCommittee.value) return arr;
  const withPosition = [...arr.filter(a => a.committeePositionsCode)]
    .sort((a, b) => {
      const codeCompare = a.committeePositionsCode!.localeCompare(b.committeePositionsCode!);
      return codeCompare !== 0 ? codeCompare : a.sequence - b.sequence;
    });
  const withoutPosition = arr.filter(a => !a.committeePositionsCode);
  return reSequence([...withPosition, ...withoutPosition]) as ParticipantsCommitteeAcceptor[];
};

const addItemAsync = async () => {
  if (!isCommittee.value && acceptors.value.length > 0) return;
  const res = await showUserDialogAsync();
  if (!res) return;

  if (acceptors.value.some(a => a.userId === res.id)) {
    return ToastHelper.warning('เพิ่มรายชื่อ', 'ไม่สามารถเพิ่มได้เนื่องจากผู้ใช้งานซ้ำ');
  }

  const newItem: ParticipantsCommitteeAcceptor = {
    userId: res.id,
    fullName: res.name,
    positionName: res.positionName ?? '',
    departmentName: res.departmentName ?? '',
    departmentCode: res.departmentCode,
    sequence: acceptors.value.length + 1,
    acceptorType: AcceptorType.AcceptanceCommittee,
    status: AcceptorStatus.Draft,
    isUnableToPerformDuties: false,
    committeePositionsCode: defaultPositionCode(),
    organizationLevel: typeof res.organizationLevel === 'string' ? parseInt(res.organizationLevel) : res.organizationLevel,
  };

  const added = addSequence(acceptors.value, newItem) as ParticipantsCommitteeAcceptor[];
  acceptors.value = reOrderArray(added);
};

const deleteItem = (index: number) => {
  const deleted = deleteItemAndReSequence(acceptors.value, index) as ParticipantsCommitteeAcceptor[];
  acceptors.value = reOrderArray(deleted);
};

const reOrderDatatable = (event: DataTableRowReorderEvent) => {
  acceptors.value = reOrderArray(onRowReorder(event) as ParticipantsCommitteeAcceptor[]);
};

const onToggleType = () => {
  acceptors.value = [];
};

onMounted(onGetDropdownAsync);
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ผู้ตรวจรับ/คณะกรรมการตรวจรับพัสดุ">
        <template #action>
          <ButtonAddUserOutlined
            v-if="!props.isDisabled && !(!isCommittee && acceptors.length >= 1)"
            type="button"
            @click="addItemAsync"
          />
        </template>
      </TitleHeader>
      <Radio v-model="isCommittee" :options="committeeTypeData" :disabled="props.isDisabled"
        @change="onToggleType" />
      <div class="px-4 my-4">
        <DataTable :value="acceptors" @row-reorder="reOrderDatatable">
          <Column>
            <template #header>
              <p class="w-full font-bold text-center">ชื่อ-นามสกุล/ตำแหน่ง</p>
            </template>
            <template #body="{ data: row }">
              <div>
                <p>{{ row.fullName }}</p>
                <small class="text-gray-400">{{ row.positionName }}</small>
              </div>
            </template>
          </Column>
          <Column>
            <template #header>
              <p class="w-full font-bold text-center">ตำแหน่งในคณะกรรมการ</p>
            </template>
            <template #body="{ data: row, index }">
              <Select
                v-model="row.committeePositionsCode"
                :options="dropdownCondition(index).value"
                :disabled="props.isDisabled"
                :key="row.userId"
                rules="required"
                hide-details
                @update:modelValue="() => { acceptors = reOrderArray([...acceptors]) }"
              />
            </template>
          </Column>
          <Column class="max-w-[10px]" v-if="!props.isDisabled">
            <template #body="{ index }">
              <Button icon="pi pi-trash" severity="danger" variant="text" @click="() => deleteItem(index)" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" bodyStyle="padding-top: 20px"
            v-if="!props.isDisabled && isCommittee">
            <template #body>
              <span class="material-symbols-outlined cursor-pointer">drag_indicator</span>
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

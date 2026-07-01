<script setup lang="ts">
import type { ParticipantsAcceptor, ParticipantsCommitteeAcceptor } from '@/models/shared/participants';
import type { AcceptorTorDraft } from '@/views/PP/models/PP002/pp002Model';
import { ref, watch, type PropType, type Ref } from 'vue';
import { AcceptorStatus, AcceptorType } from '@/enums/participants';
import { AccordHeader } from '../cosmetic';
import { ToDateTime } from '@/helpers/dateTime';
import { ArrayHelper } from '@/helpers/array';
import { showConfirmDialogAsync, showReasonDialogAsync, showUserDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import { isNonEmptyString } from '@/helpers/string';
import draggable from 'vuedraggable';
import ToastHelper from '@/helpers/toast';
import { useAuthenticationStore } from '@/stores/authentication';
import { CommitteePositions } from '@/enums/PCM005/principle';

const props = defineProps({
  title: { type: String, required: true },
  acceptorType: { type: String as PropType<AcceptorType>, required: true },
  isManage: { type: Boolean, default: false },
  isDisable: { type: Boolean, default: false },
  isShowCheckBoxAll: { type: Boolean, default: false },
  isApprove: { type: Boolean, default: false },
  isSetDefault: { type: Boolean },
  defaultStatus: { type: String as PropType<AcceptorStatus>, default: AcceptorStatus.Draft },
  noSelfDelete: { type: Boolean, default: false },
});

const emit = defineEmits(['setDefault', 'setIsUnableToPerformDuties', 'add', 'remove']);
const authStore = useAuthenticationStore();

const modelValue = defineModel({
  type: Array as PropType<ParticipantsCommitteeAcceptor[] | ParticipantsAcceptor[]>,
  required: true,
});

const data = ref(modelValue.value?.filter((m): boolean => m.acceptorType === props.acceptorType));
const { deleteItemAndReSequence, reSequence } = ArrayHelper();

const onFilterValueOtherType = (): (ParticipantsCommitteeAcceptor[] | ParticipantsAcceptor[]) =>
  modelValue.value.filter((f): boolean => f.acceptorType !== props.acceptorType);

const modelValueFilterData = (): (ParticipantsCommitteeAcceptor[] | ParticipantsAcceptor[]) =>
  modelValue.value.filter((f): boolean => f.acceptorType === props.acceptorType);

const countKey = ref(0);

const isCommitteeAcceptor = (
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  item: ParticipantsAcceptor | ParticipantsCommitteeAcceptor
): item is ParticipantsCommitteeAcceptor => {
  return [
    AcceptorType.TorDraftCommittee,
    AcceptorType.MedianPriceCommittee,
    AcceptorType.ProcurementCommittee,
    AcceptorType.Jp005Committee,
    AcceptorType.AcceptanceCommittee,
    AcceptorType.RentCommittee,
    AcceptorType.InspectionCommittee,
    AcceptorType.AccountingApprover].includes(props.acceptorType);
};

const removeItem = async (index: number): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  data.value = deleteItemAndReSequence(data.value, index);
  const otherTypeData = onFilterValueOtherType();
  AssignToModelValue(data.value, otherTypeData);
  emit('remove');
};

const reSequenceDrag = (): void => {
  data.value = reSequence(data.value);
  const otherTypeData = onFilterValueOtherType();
  AssignToModelValue(data.value, otherTypeData);
};

const AssignToModelValue = (
  dataValue: ParticipantsCommitteeAcceptor[] | ParticipantsAcceptor[],
  otherTypeData: ParticipantsCommitteeAcceptor[] | ParticipantsAcceptor[]
): void => {
  modelValue.value = [
    ...otherTypeData.sort((a, b): number => a.sequence - b.sequence),
    ...dataValue.sort((a, b): number => a.sequence - b.sequence),
  ];
};

const addUserAcceptorAsync = async (): Promise<void> => {
  const res = await showUserDialogAsync();
  if (!res || !data.value) return;

  const isDuplicate = data.value.some(
    (a): boolean => a.userId === res.id && a.acceptorType === props.acceptorType
  );

  if (isDuplicate) {
    return ToastHelper.warning(
      'เพิ่มรายชื่อ',
      'ไม่สามารถเพิ่มได้เนื่องจากผู้ใช้งานซ้ำ'
    );
  }

  const isDelegateDuplicate = data.value.some((s): boolean => s.userId === res.delegateeId && s.acceptorType === props.acceptorType);

  if (isDelegateDuplicate) {
    return ToastHelper.warning(
      'เพิ่มรายชื่อ',
      'คุณเลือกผู้ปฏิบัติหน้าที่แทนตำแหน่งนี้แล้ว'
    );
  }

  modelValue.value.push({
    userId: res.id,
    acceptorType: props.acceptorType,
    departmentName: res.departmentName ?? undefined,
    fullName: res.name,
    positionName: res.positionName ?? undefined,
    sequence: data.value.length + 1,
    status: props.defaultStatus,
    businessUnitName: res.departmentName ?? undefined,
    employeeCode: res.employeeCode,
    isUnableToPerformDuties: false,
    departmentCode: res.departmentCode,
    organizationLevel: res.organizationLevel,
  } as AcceptorTorDraft);

  data.value = modelValueFilterData();
  emit('add');
};

watch(
  (): (ParticipantsCommitteeAcceptor[] | ParticipantsAcceptor[]) => modelValue.value,
  (): void => {
    data.value = modelValueFilterData();
  },
  { deep: true }
);

const onSetIsUnableToPerformDuties = async (value: boolean, idx: number, acceptorId?: string): Promise<void> => {
  if (!acceptorId) {
    return;
  }
  const temp = data as Ref<Array<ParticipantsCommitteeAcceptor>>;

  if (value && [AcceptorType.TorDraftCommittee, AcceptorType.MedianPriceCommittee, AcceptorType.ProcurementCommittee, AcceptorType.AcceptanceCommittee, AcceptorType.RentCommittee, AcceptorType.InspectionCommittee].includes(props.acceptorType)) {
    const resp = await showReasonDialogAsync(ReasonDialogType.UnableToPerformDuties);

    if (!resp.isConfirm) {
      countKey.value++;

      return;
    };

    temp.value[idx].isUnableToPerformDuties = value;

    data.value = [...temp.value];
    countKey.value = 0;

    emit('setIsUnableToPerformDuties', value, acceptorId, resp.reason);

    return;
  }

  temp.value[idx].isUnableToPerformDuties = value;

  data.value = [...temp.value];
  countKey.value = 0;

  emit('setIsUnableToPerformDuties', value, acceptorId);
};
</script>

<template>
  <div>
    <AccordHeader :label="props.title" />
    <AccordionContent>
      <Card class="rounded-none">
        <template #content>
          <div v-if="!props.isDisable && props.isManage" class="flex justify-between mb-5">
            <Button label="กำหนดค่าเริ่มต้น" severity="primary" variant="outlined" icon="pi pi-undo"
              @click="emit('setDefault')" v-if="isSetDefault" />
            <div v-else />
            <Button label="เพิ่มรายชื่อ" severity="primary" variant="outlined" icon="pi pi-plus"
              @click="addUserAcceptorAsync" />
          </div>
          <draggable v-model="data" group="group" handle=".drag-data" itemKey="userId" @end="reSequenceDrag">
            <template
              #item="{ element: data, index }: { element: ParticipantsAcceptor | ParticipantsCommitteeAcceptor, index: number }">
              <div>
                <div v-if="!isManage">
                  <div class="flex items-start mb-3"
                    v-if="isCommitteeAcceptor(data) && data.isUnableToPerformDuties != null">
                    <div class="text-xs"
                      v-if="(data.userId !== authStore.profile.id && data.committeePositionsCode !== CommitteePositions.PosBoard001)">
                      <Checkbox label="ไม่สามารถปฏิบัติงานได้" :modelValue="data.isUnableToPerformDuties"
                        :disabled="props.isDisable || ![AcceptorStatus.Pending, AcceptorStatus.Draft, AcceptorStatus.UnableToPerformDuties].includes(data.status)"
                        @update:modelValue="(e) => onSetIsUnableToPerformDuties(e, index, data.id)"
                        :key="`${countKey}-${index}`" />
                    </div>
                    <p class="ml-auto">{{ data.committeePositionName }}</p>
                  </div>
                </div>
                <div class="flex items-center justify-between">
                  <div class="flex items-center">
                    <span class="material-symbols-outlined text-green-500 text-[20px]"
                      v-if="data.status === AcceptorStatus.Approved">
                      check_circle
                    </span>
                    <span class="material-symbols-outlined text-red-500 text-[20px]"
                      v-if="data.status === AcceptorStatus.Rejected">
                      cancel
                    </span>
                    <span class="material-symbols-outlined text-yellow-400 text-[20px]"
                      v-if="data.status === AcceptorStatus.Pending">
                      schedule
                    </span>
                    <span class="material-symbols-outlined text-gray-500 text-[20px]"
                      v-if="data.status === AcceptorStatus.UnableToPerformDuties">
                      do_not_disturb_on
                    </span>
                    <Divider layout="vertical" class="mx-1"
                      v-if="data.status && data.status !== AcceptorStatus.Draft" />
                    <div>
                      <p>{{ data.fullName }}</p>
                      <small class="text-gray-400 text-base">{{ data.positionName }}</small>
                    </div>
                  </div>
                  <div class="flex gap-1 items-center" v-if="!props.isDisable && props.isManage && !(props.noSelfDelete && (data.userId === authStore.profile.id || data.delegateeUserId === authStore.profile.id))">
                    <Button icon="pi pi-trash" severity="danger" variant="text" size="small"
                      @click="() => removeItem(index)" />
                    <span
                      class="material-symbols-outlined drag-data cursor-move text-gray-400 text-[20px]">drag_indicator</span>
                  </div>
                </div>
                <div class="ml-8"
                  v-if="data.status == AcceptorStatus.Approved && !isNonEmptyString(data.remark) && data.actionAt">
                  <ul class="list-disc pl-4 text-sm text-gray-700">
                    <li>
                      <p class="text-sm">[{{ ToDateTime(data.actionAt) }}]</p>
                    </li>
                  </ul>
                </div>
                <div
                  v-if="([AcceptorStatus.Approved, AcceptorStatus.Rejected, AcceptorStatus.UnableToPerformDuties].includes(data.status)) && isNonEmptyString(data.remark)"
                  class="ml-8">
                  <ul class="list-disc pl-4 text-sm text-gray-700">
                    <li>
                      <p class="text-sm">[{{ ToDateTime(data.actionAt) }}]</p>
                      <p class="text-sm break-all">{{ data.remark }}</p>
                    </li>
                  </ul>
                </div>
                <Divider class="my-2" />
              </div>
            </template>
          </draggable>
        </template>
      </Card>
    </AccordionContent>
  </div>
</template>

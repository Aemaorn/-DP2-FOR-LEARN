<script setup lang="ts">
import type { ParticipantsAssignee } from '@/models/shared/participants';
import { computed, type PropType } from 'vue';
import { AssigneeGroup, AssigneeStatus, AssigneeType } from '@/enums/participants';
import { showConfirmDialogAsync, showReasonDialogAsync, showUserDialogAsync } from '@/helpers/dialog';
import { ArrayHelper } from '@/helpers/array';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import { useAuthenticationStore } from '@/stores/authentication';
import { ToDateTime } from '@/helpers/dateTime';
import AccordHeader from '../cosmetic/AccordHeader.vue';
import draggable from 'vuedraggable';
import ToastHelper from '@/helpers/toast';
import type { Option } from '@/models/shared/option';
import Select from '../forms/Select.vue';

interface AccordAssigneeEmit {
  reason: string;
  userId: string;
}

const props = defineProps({
  title: { type: String, required: true },
  disabled: { type: Boolean, default: false },
  isComment: { type: Boolean, default: false },
  group: { type: String as PropType<AssigneeGroup>, default: AssigneeGroup.JorPor },
  isDropdown: { type: Boolean, default: false },
  dropdown: { type: Array as PropType<Option[]>, default: () => [] },
  dropdownRules: { type: [String, Object, Function] as PropType<any>, default: '' },
  disabledDropdown: { type: Boolean, default: false },
  onChange: { type: Function as PropType<(value: any) => void> },
});

const data = defineModel({ type: Array as PropType<ParticipantsAssignee[]>, required: true });

const selectData = defineModel("selectData", { type: String, required: false });

const emit = defineEmits<{
  (event: 'onComment', data: AccordAssigneeEmit): void;
  (event: 'onChange'): void;
}>();

const store = useAuthenticationStore();
const filterGroup = computed({
  get: () => data.value?.filter(v => v.assigneeGroup === props.group),
  set: (val: ParticipantsAssignee[]) => {
    const others = data.value.filter(v => v.assigneeGroup !== props.group);
    data.value = [...others, ...val];
  },
});
const filterJorpor = computed(() => filterGroup.value?.find(v => [AssigneeType.Director].includes(v.assigneeType)));
const { reSequence } = ArrayHelper();

const addUserAssigneeAsync = async (): Promise<void> => {
  const res = await showUserDialogAsync();

  if (!data.value || !res) return;

  if (data.value.some(a => a.userId === res.id && a.assigneeGroup === props.group)) {
    return ToastHelper.warning('เพิ่มรายชื่อ', 'ไม่สามารถเพิ่มรายชื่อซ้ำ');
  }

  data.value.push({
    assigneeType: AssigneeType.Assignee,
    departmentName: res.departmentName,
    fullName: res.name,
    positionName: res.positionName,
    sequence: data.value.length + 1,
    status: AssigneeStatus.Draft,
    userId: res.id,
    assigneeGroup: props.group,
  } as ParticipantsAssignee);

  emit('onChange');
};

const reSequenceDrag = (): void => {
  const filterData = data.value.filter(v => v.assigneeGroup !== props.group);
  const reDataSequence = reSequence(filterGroup.value);

  const mergeData = [...filterData, ...reDataSequence];

  data.value = mergeData;

  emit('onChange');
};

const removeItem = async (userId: string): Promise<void> => {
  if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;

  const findIndex = data.value.findIndex(d => d.userId === userId && d.assigneeGroup === props.group);

  if (findIndex > -1) {
    data.value.splice(findIndex, 1);
    emit('onChange');
  }
};

const onCommentAsync = async (): Promise<void> => {
  const commentAssignee = data.value.find(f => f.userId === store.profile.id);

  const res = await showReasonDialogAsync(ReasonDialogType.RemarkOfficer, true, undefined, undefined, undefined, commentAssignee?.remark);

  if (res && res.isConfirm && res.reason) {
    emit('onComment', { reason: res.reason, userId: store.profile.id });
  }
};
</script>

<template>
  <div>
    <AccordHeader :label="props.title" />
    <AccordionContent>
      <Card class="rounded-none!">
        <template #content>
          <div>
            <Card v-if="filterJorpor">
              <template #content>
                <div class="flex items-center justify-between">
                  <div class="flex items-center min-w-0">
                    <span class="material-symbols-outlined text-red-500 text-[20px]"
                      v-if="filterJorpor?.status === AssigneeStatus.Rejected">
                      cancel
                    </span>
                    <Divider layout="vertical" class="mx-1" v-if="filterJorpor?.status === AssigneeStatus.Rejected" />
                    <div class="min-w-0">
                      <p>{{ filterJorpor?.fullName }}</p>
                      <small class="text-gray-400 text-base">{{ filterJorpor?.positionName }}</small>
                    </div>
                  </div>
                </div>
                <div v-if="filterJorpor?.remark" class="ml-8">
                  <small class="text-red-500">
                    {{ `${filterJorpor.status === AssigneeStatus.Rejected ?
                      'หมายเหตุส่งกลับแก้ไข' :
                      'ความคิดเห็น'}` }}
                  </small>
                  <ul class="list-disc pl-4 text-sm text-gray-700">
                    <li>
                      <p class="text-sm">
                        [{{ ToDateTime(filterJorpor?.actionAt) }}]{{ filterJorpor?.fullName }}
                      </p>
                      <p class="text-sm break-all">{{ filterJorpor?.remark }}</p>
                    </li>
                  </ul>
                </div>
              </template>
            </Card>
            <div class="flex justify-between items-center my-5">
              <p class="font-bold" v-if="data && data.length > 1 || !props.disabled">ผู้รับผิดชอบ</p>
              <Button icon="pi pi-plus" label="เพิ่มรายชื่อมอบหมาย" severity="primary" variant="outlined"
                class="bg-white! hover:bg-red-50!" @click="addUserAssigneeAsync" v-if="!props.disabled" />
            </div>
            <Select class="mt-8" label="มอบหมายส่วนงาน" :rules="props.dropdownRules" v-model="selectData"
              :options="dropdown" v-if="isDropdown" :disabled="disabledDropdown" @onSelect="props.onChange" />
            <slot name="additional" />
            <draggable v-model="filterGroup" handle=".drag-handle" item-key="id" @end="reSequenceDrag">
              <template #item="{ element }">
                <div v-if="element.assigneeType == AssigneeType.Assignee">
                  <div class="flex items-center justify-between">
                    <div class="flex items-center min-w-0 overflow-hidden">
                      <span class="material-symbols-outlined text-red-500 text-[20px]"
                        v-if="element.status === AssigneeStatus.Rejected">
                        cancel
                      </span>
                      <Divider layout="vertical" class="mx-1"
                        v-if="[AssigneeStatus.Rejected].includes(element.status)" />
                      <div class="min-w-0 overflow-hidden">
                        <p>{{ element.fullName }}</p>
                        <small class="text-gray-400 text-base">{{ element.positionName }}</small>
                        <div v-if="element.remark" class="ml-8">
                          <small class="text-red-500">
                            {{ `${element.status === AssigneeStatus.Rejected ?
                              'หมายเหตุส่งกลับแก้ไข' :
                              'ความคิดเห็น'}` }}
                          </small>
                          <ul class="list-disc pl-4 text-sm text-gray-700">
                            <li>
                              <p class="text-sm">
                                [{{ ToDateTime(element.actionAt) }}]{{ element.fullName }}
                              </p>
                              <p class="text-sm break-all">{{ element.remark }}</p>
                            </li>
                          </ul>
                        </div>
                      </div>
                    </div>

                    <div class="flex items-center gap-1">
                      <i class="pi pi-trash text-red-500 cursor-pointer text-sm"
                        @click="() => removeItem(element.userId)"
                        v-if="!props.disabled && element.userId != store.profile.id"></i>
                      <span class="material-symbols-outlined drag-handle cursor-pointer text-gray-400 text-[20px]"
                        v-if="!props.disabled">
                        drag_indicator
                      </span>
                    </div>
                  </div>
                  <Divider class="my-2" />
                </div>
              </template>
            </draggable>
            <div class="flex justify-end">
              <Button label="บันทึกให้ความเห็น" severity="success" icon="pi pi-file-edit" @click="onCommentAsync"
                v-if="props.isComment" :disabled="data.some(s => !s.id)" />
            </div>
          </div>
        </template>
      </Card>
    </AccordionContent>
  </div>
</template>

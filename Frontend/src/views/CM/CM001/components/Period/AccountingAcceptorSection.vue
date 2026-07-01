<script setup lang="ts">
import { AcceptorStatus, type AcceptorType } from '@/enums/participants';
import { ToDateTime } from '@/helpers/dateTime';
import { isNonEmptyString } from '@/helpers/string';
import type { ParticipantsAcceptor } from '@/models/shared/participants';
import { Button, Divider } from 'primevue';
import draggable from 'vuedraggable';

type Props = {
  title: string;
  acceptorType: AcceptorType;
  readonly: boolean;
  dragHandleClass: string;
  groupName: string;
  currentUserId?: string;
  canRemoveSelf?: boolean;
  inlineAddButton?: boolean;
};

const props = defineProps<Props>();

const modelValue = defineModel<ParticipantsAcceptor[]>({ required: true });

const emit = defineEmits<{
  (e: 'add'): void;
  (e: 'remove', index: number): void;
  (e: 'dragEnd'): void;
}>();
</script>

<template>
  <div>
    <div class="flex items-center gap-3 mb-3">
      <p class="font-bold whitespace-nowrap">{{ props.title }}</p>
      <div class="flex-grow border-t border-gray-300"></div>
      <Button v-if="props.inlineAddButton && !props.readonly" label="เพิ่มรายชื่อ" severity="primary"
        variant="outlined" icon="pi pi-plus" size="small" @click="emit('add')" />
    </div>
    <div class="flex justify-between gap-2 mb-4" v-if="!props.inlineAddButton && !props.readonly">
      <slot name="leadingButtons" />
      <Button label="เพิ่มรายชื่อ" severity="primary" variant="outlined" icon="pi pi-plus"
        class="ml-auto" @click="emit('add')" />
    </div>
    <draggable v-model="modelValue" :group="props.groupName" :handle="`.${props.dragHandleClass}`"
      itemKey="sequence" @end="emit('dragEnd')">
      <template #item="{ element: item, index }: { element: ParticipantsAcceptor, index: number }">
        <div>
          <div class="flex items-center justify-between">
            <div class="flex items-center">
              <span class="material-symbols-outlined text-green-500 text-[20px]"
                v-if="item.status === AcceptorStatus.Approved">check_circle</span>
              <span class="material-symbols-outlined text-red-500 text-[20px]"
                v-if="item.status === AcceptorStatus.Rejected">cancel</span>
              <span class="material-symbols-outlined text-yellow-400 text-[20px]"
                v-if="item.status === AcceptorStatus.Pending">schedule</span>
              <Divider layout="vertical" class="mx-1"
                v-if="item.status && item.status !== AcceptorStatus.Draft" />
              <div>
                <p>{{ item.fullName }}</p>
                <small class="text-gray-400 text-base">{{ item.positionName }}</small>
              </div>
            </div>
            <div class="flex gap-1 items-center" v-if="!props.readonly">
              <Button icon="pi pi-trash" severity="danger" variant="text" size="small"
                v-if="(props.canRemoveSelf || (item.delegateeUserId ?? item.userId) !== props.currentUserId) && item.status !== AcceptorStatus.Approved"
                @click="() => emit('remove', index)" />
              <span :class="`material-symbols-outlined ${props.dragHandleClass} cursor-move text-gray-400 text-[20px]`">drag_indicator</span>
            </div>
          </div>
          <div class="ml-8"
            v-if="item.status === AcceptorStatus.Approved && !isNonEmptyString(item.remark) && item.actionAt">
            <ul class="list-disc pl-4 text-sm text-gray-700">
              <li>
                <p class="text-sm">[{{ ToDateTime(item.actionAt) }}]</p>
              </li>
            </ul>
          </div>
          <div
            v-if="[AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(item.status) && isNonEmptyString(item.remark)"
            class="ml-8">
            <ul class="list-disc pl-4 text-sm text-gray-700">
              <li>
                <p class="text-sm">[{{ ToDateTime(item.actionAt) }}]</p>
                <p class="text-sm break-all">{{ item.remark }}</p>
              </li>
            </ul>
          </div>
          <Divider class="my-2" v-if="index < modelValue.length - 1" />
        </div>
      </template>
    </draggable>
  </div>
</template>

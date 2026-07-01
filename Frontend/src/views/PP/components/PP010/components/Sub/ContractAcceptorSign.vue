<script setup lang="ts">
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { Button, Card, type DataTableRowReorderEvent } from 'primevue';
import { DatatableHelper } from '@/helpers/datable';
import { showUserDialogAsync } from '@/helpers/dialog';
import type { ParticipantsAcceptor } from '@/models/shared/participants';
import { AcceptorType } from '@/enums/participants';
import { computed } from 'vue';

type Props = {
  label: string;
  isDisabled?: boolean;
};

defineEmits<{
  setDefaultSigner: []
}>();

const { onRowReorder } = DatatableHelper();

const props = defineProps<Props>();
const value = defineModel<ParticipantsAcceptor[]>({
  required: true,
});

const filteredValue = computed(() =>
  value.value?.filter(x => x.acceptorType === AcceptorType.AcceptorSign) ?? []
);

const reOrderDatatable = (event: DataTableRowReorderEvent) => {
  value.value = onRowReorder(event) as ParticipantsAcceptor[];
}

const changeSignerAsync = async (filteredIndex: number) => {
  const selectedData = await showUserDialogAsync();

  if (!selectedData || !value.value) {
    return;
  }

  const targetItem = filteredValue.value[filteredIndex];
  const realIndex = value.value.findIndex(
    x => x.acceptorType === AcceptorType.AcceptorSign && x.userId === targetItem.userId && x.sequence === targetItem.sequence
  );

  if (realIndex === -1) return;

  value.value[realIndex] = {
    ...value.value[realIndex],
    userId: selectedData.id,
    fullName: selectedData.name,
    positionName: selectedData.positionName?.trim() ?? '',
    departmentCode: selectedData.departmentCode,
    departmentName: selectedData.departmentName ?? '',
  };
}

</script>

<template>
  <Card :pt="{ root: { 'data-section-id': 'contract-acceptor-sign', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label">
      </TitleHeader>
      <div class="px-4 my-4">
        <DataTable :value="filteredValue" @row-reorder="(e) => reOrderDatatable(e)">
          <Column>
            <template #body="{ data }">
              <div>
                <p>{{ data.fullName }}</p>
                <small class="text-gray-400">{{ data.positionName }}</small>
              </div>
            </template>
          </Column>
          <Column headerStyle="width: 10rem" v-if="!props.isDisabled">
            <template #header>
              <p class="w-full font-bold text-center"></p>
            </template>
            <template #body="{ index }">
              <Button label="เปลี่ยนผู้ลงนาม" icon="pi pi-user-edit" severity="primary" size="small" @click="changeSignerAsync(index)" />
            </template>
          </Column>
          <template #empty>
            <p class="text-center font-bold">ไม่พบข้อมูล</p>
          </template>
        </DataTable>
      </div>
    </template>
  </Card>
</template>
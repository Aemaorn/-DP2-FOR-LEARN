<script setup lang="ts">
import type { TPP004UserList, TPP004UserListCondition } from '@/views/PP/models/PP004/pp004Model';
import { Card, Button, DataTable } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Select, Radio } from '@/components/forms';
import SharedConstants from '@/constants/shared';

type Props = {
  label: string;
}

const props = defineProps<Props>();

const value = defineModel<TPP004UserList | TPP004UserListCondition>({
  required: true,
});
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader :label="props.label">
        <template #action>
          <Button v-if="('isHas' in value && value.isHas) || !('isHas' in value)" icon="pi pi-plus" label="เพิ่มรายชื่อ"
            severity="primary" variant="outlined" />
        </template>
      </TitleHeader>
      <Radio v-if="'isHas' in value" class="px-4" v-model="value.isHas" :options="SharedConstants.HasOptions" />
      <div class="mt-4" v-if="('isHas' in value && value.isHas) || !('isHas' in value)">
        <DataTable :value="value.detail">
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">ชื่อ-นามสกุล/ตำแหน่ง</p>
            </template>
            <template #body="{ data }">
              <div>
                <p class="text-center">{{ data.name }}</p>
                <p>{{ data.sequence }}</p>
                <small class="text-gray-400">{{ data.position }}</small>
              </div>
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">ตำแหน่งในคณะกรรมการ</p>
            </template>
            <template #body="{ data }">
              <Select v-model="data.positionProcurement" :options="[]" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
            <template>
              <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
            bodyStyle="vertical-align: top;padding-top: 25px">
            <template #rowreordericon>
              <span class="material-symbols-outlined cursor-pointer" :draggable="true">
                drag_indicator
              </span>
            </template>
          </Column>
        </DataTable>
      </div>
    </template>
  </Card>
</template>
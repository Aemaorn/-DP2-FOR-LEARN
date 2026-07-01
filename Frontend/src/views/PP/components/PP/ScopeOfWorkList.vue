<script setup lang="ts">
import { Card, Button } from 'primevue';
import { Select, InputField, InputNumber, InputArea } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import type { TechnicalSpecifications } from '../../models/PP002/pp002Model';
import { ArrayHelper } from '@/helpers/array';
import type { Option } from '@/models/shared/option';
import { onMounted, ref } from 'vue';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { useMenuStore } from '@/stores/menu';
import draggable from 'vuedraggable';

const { addSequence, deleteItemAndReSequence, reSequence } = ArrayHelper();

type Props = {
  isDisabled?: boolean;
}

const props = defineProps<Props>();
const value = defineModel<Array<TechnicalSpecifications>>({
  required: true,
  default: () => [],
});

const menuStore = useMenuStore();

const unitDropdownData = ref<Array<Option>>([]);
onMounted(async (): Promise<void> => {
  await onGetunitDropdownAsync();

  if(value.value && value.value.length === 0)
  {
    addItem();
  }
});

const onGetunitDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.UnitOfMea);

  if (status === HttpStatusCode.Ok) {
    unitDropdownData.value = data;
  }
}

const addItem = (): void => {
  if (!value.value) {
    value.value = [];
  }

  value.value = addSequence(value.value, {} as TechnicalSpecifications);
};

const deleteItem = (index: number): void => {
  value.value = deleteItemAndReSequence(value.value, index);
};

const onRowReorder = (): void => {
  value.value = reSequence(value.value);
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ขอบเขตของงาน">
        <template #action>
          <Button icon="pi pi-plus" label="เพิ่มข้อมูล" severity="primary" variant="outlined" @click="addItem"
            v-if="!props.isDisabled && menuStore.hasManage" />
        </template>
      </TitleHeader>
      <div class="mt-4">
        <draggable v-model="value" group="rowData" class="mt-4" handle=".drag-handle" item-key="sequence"
          @end="onRowReorder">
          <template #item="{ element: data, index: rowIndex }">
            <div class="flex p-4 bg-gray-100 rounded-2xl flex-col gap-2 mt-4 pt-8">
              <div class="font-bold text-xl mb-6">ลำดับที่ {{ rowIndex + 1 }}</div>
              <div class="grid grid-cols-4 gap-2 w-full">
                <InputField v-model="data.name" label="รายการ" rules="required"
                  :disabled="props.isDisabled || !menuStore.hasManage" />
                <InputNumber v-model="data.quantity" label="จำนวน" :rules="`required|min_value:1`" grouping
                  :disabled="props.isDisabled || !menuStore.hasManage" />
                <Select v-model="data.unitCode" label="หน่วย" :options="unitDropdownData" rules="required"
                  :disabled="props.isDisabled || !menuStore.hasManage" />
                <div class="flex items-start justify-end" v-if="!props.isDisabled && menuStore.hasManage">
                  <span class="material-symbols-outlined drag-handle cursor-move mt-2">
                    drag_indicator
                  </span>
                  <Button icon="pi pi-trash" severity="danger" variant="text" @click="() => deleteItem(rowIndex)" />
                </div>
              </div>
              <InputArea class="mt-6" label="รายละเอียด" v-model="data.description" rules="required"
                :disabled="props.isDisabled || !menuStore.hasManage" />
            </div>
          </template>
        </draggable>
        <p v-if="!value || value.length === 0" class="text-center text-gray-500 mt-4">ไม่มีข้อมูล</p>
      </div>
    </template>
  </Card>
</template>
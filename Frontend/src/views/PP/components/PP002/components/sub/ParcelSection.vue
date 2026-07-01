<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import type { TechnicalSpecifications } from '@/views/PP/models/PP002/pp002Model';
import { ArrayHelper } from '@/helpers/array';
import { InputArea, InputField, InputNumber, Select } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { Button, Card, type DataTableRowReorderEvent } from 'primevue';
import { computed, onMounted, ref } from 'vue';
import { HttpStatusCode } from 'axios';
import { EGroupCode } from '@/enums/shared';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';
import defaultProps from '@/helpers/defaultProps';
import SharedService from '@/services/Shared/dropdown';
import Draggable from 'vuedraggable';

const props = defineProps({
  titleName: defaultProps(''),
});

const defaultItem: TechnicalSpecifications = {
  sequence: 1,
  name: '',
  description: '',
  quantity: 0,
  unitCode: '',
};

const value = defineModel<TechnicalSpecifications[]>({
  default: () => [],
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();
const { addSequence, reSequence, deleteItemAndReSequence } = ArrayHelper();

const isHireTemplate = computed(() => {
  const templateCode = store.PP002Detail.torDocumentTemplateCode;
  return ['TorBuyWithHire60', 'TorBuyWithHire80', 'TorHireWithHire60', 'TorHireWithHire80'].includes(templateCode);
});

const canDeleteItem = computed(() => {
  return value.value.length > 1;
});

if (!value.value || value.value.length === 0) {
  if (isHireTemplate.value) {
    value.value = [
      { ...defaultItem, sequence: 1 },
      { ...defaultItem, sequence: 2 }
    ];
  } else {
    value.value = [{ ...defaultItem }];
  }
}

const dropdown = ref<Option[]>([]);

onMounted(() => {
  getDropdownAsync();
});

const getDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.UnitOfMea, undefined, true);
  if (status === HttpStatusCode.Ok) {
    dropdown.value = data;
  }
};

const addItem = (): void => {
  value.value = addSequence(value.value, {} as TechnicalSpecifications);
};

const deleteItem = (index: number): void => {
  value.value = deleteItemAndReSequence(value.value, index);
};

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  value.value = reSequence(event.value);
};
</script>

<template>
  <Card class="mb-4" data-section-id="parcel" :data-section-label="props.titleName">
    <template #content>
      <TitleHeader :label="props.titleName" />
      <div class="px-4 mt-2 text-end">
        <Button label="เพิ่มคุณลักษณะเฉพาะของพัสดุ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="bg-white! hover:bg-red-50!" @click="addItem" v-if="store.status.canEditTor && menuStore.hasManage" />
      </div>
      <div v-if="value && value.length > 0">
        <div class="mt-4">
          <draggable v-model="value" group="rowData" class="mt-4" handle=".drag-handle" item-key="sequence"
            @end="onRowReorder">
            <template #item="{ element: data, index: rowIndex }">
              <div class="flex p-4 bg-gray-100 rounded-2xl flex-col gap-2 mt-4">
                <div class="font-bold text-xl mb-6">ลำดับที่ {{ rowIndex + 1 }}</div>
                <div class="grid grid-cols-4 gap-2 w-full">
                  <InputField v-model="data.name" label="รายการ" rules="required"
                    :disabled="!store.status.canEditTor || !menuStore.hasManage" />
                  <InputNumber v-model="data.quantity" label="จำนวน" :rules="`required|min_value:1`" grouping
                    :disabled="!store.status.canEditTor || !menuStore.hasManage" />
                  <Select v-model="data.unitCode" label="หน่วย" :options="dropdown" rules="required"
                    :disabled="!store.status.canEditTor || !menuStore.hasManage" />
                  <div class="flex items-start justify-end"
                    v-if="store.status.canEditTor && menuStore.hasManage && canDeleteItem">
                    <span class="material-symbols-outlined drag-handle cursor-move mt-2">
                      drag_indicator
                    </span>
                    <Button icon="pi pi-trash" severity="danger" variant="text" @click="() => deleteItem(rowIndex)" />
                  </div>
                </div>
                <InputArea class="mt-6" label="รายละเอียด" v-model="data.description" rules="required"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" />
              </div>
            </template>
          </draggable>
          <p v-if="!value || value.length === 0" class="text-center text-gray-500 mt-4">ไม่มีข้อมูล</p>
        </div>
      </div>
    </template>
  </Card>
</template>
<script setup lang="ts">
import { Radio, InputArea, InputNumber, Select } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { Card, DataTable, type DataTableRowReorderEvent } from 'primevue';
import type { JorPor04FineRate } from '@/views/PP/models/PP004/pp004Model';
import SharedConstants from '@/constants/shared';
import { onMounted, ref } from 'vue';
import { usePP004Store } from '@/views/PP/stores/PP004/pp004Store';
import { EGroupCode } from '@/enums/shared';
import SharedService from '@/services/Shared/dropdown';
import { HttpStatusCode } from 'axios';
import type { Option } from '@/models/shared/option';
import { useMenuStore } from '@/stores/menu';
import { SupplyMethodTypeConstant } from '@/enums/preProcurement';
import { usePPDetailStore } from '@/stores/PP/ppStore';

const addItem = (): void => {
  pp004Store.body.fineRates.push({
    sequence: pp004Store.body.fineRates.length + 1,
    percentage: procurement.procurementDetail.supplyMethodTypeCode === SupplyMethodTypeConstant.Hire ? 0.1 : 0.2,
  } as JorPor04FineRate);
};

const deleteItem = (index: number): void => {
  pp004Store.body.fineRates.splice(index, 1);
};

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  pp004Store.body.fineRates = event.value.map((item, index): JorPor04FineRate => {
    item.sequence = index + 1;

    return item as JorPor04FineRate;
  });
};

const pMFineTypeDropdown = ref<Option[]>([]);

const getPMFineTypeDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.PMFineType, undefined, true);


  if (status === HttpStatusCode.Ok) {
    pMFineTypeDropdown.value = data;
  }
};

const menuStore = useMenuStore();
const pp004Store = usePP004Store();
const procurement = usePPDetailStore();

onMounted((): void => {
  pp004Store.fetchDateTypeOptions();
  getPMFineTypeDropdownAsync();
});

const fineRateChange = () => {
  if (pp004Store.body.requisition.hasFineRate) {
    addItem();
  } else {
    pp004Store.body.fineRates = [];
  }
}

</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="อัตราค่าปรับ">
        <template #action>
          <Button icon="pi pi-plus" label="เพิ่มข้อมูล" severity="primary" variant="outlined" @click="addItem"
            v-if="pp004Store.body.requisition.hasFineRate && pp004Store.IsEdit && menuStore.hasManage" />
        </template>
      </TitleHeader>
      <div class="px-4">
        <Radio v-model="pp004Store.body.requisition.hasFineRate" :options="SharedConstants.HasOptions"
          @change="fineRateChange" :disabled="!pp004Store.IsEdit || !menuStore.hasManage" />
        <DataTable :value="pp004Store.body.fineRates" @row-reorder="onRowReorder"
          v-if="pp004Store.body.requisition.hasFineRate">
          <Column bodyStyle="vertical-align: top">
            <template #header>
              <p class="w-full font-bold text-center">ลำดับ</p>
            </template>
            <template #body="{ index }">
              <p class="text-center">{{ index + 1 }}</p>
            </template>
          </Column>
          <Column class="min-w-[200px]" bodyStyle="vertical-align: top;">
            <template #header>
              <p class="w-full font-bold text-center">รายละเอียด</p>
            </template>
            <template #body="{ data }">
              <div>
                <p class="text-start">{{ data.exampleDescription }}</p>
                <InputArea v-model="data.conditionOther" :disabled="!pp004Store.IsEdit || !menuStore.hasManage"
                  helper-text="ตัวอย่าง: กำหนดค่าปรับเป็นรายวันในอัตราร้อยละ 0.10 ของค่าจ้างที่เสนอมา"
                  rules="required" />
              </div>
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top;" class="min-w-[200px]">
            <template #header>
              <p class="w-full font-bold text-center">อัตราร้อยละ</p>
            </template>
            <template #body="{ data }">
              <InputNumber v-model="data.percentage" :disabled="!pp004Store.IsEdit || !menuStore.hasManage"
                :min-fraction-digits="2" :max-fraction-digits="3" rules="required" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top;" class="min-w-[200px]">
            <template #header>
              <p class="w-full font-bold text-center">คิดเป็น</p>
            </template>
            <template #body="{ data }">
              <Select v-model="data.periodTypeCode" :options="pp004Store.dateTypeOptions"
                :disabled="!pp004Store.IsEdit || !menuStore.hasManage" rules="required" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top;" class="min-w-[200px]">
            <template #header>
              <p class="w-full font-bold text-center">เงื่่อนไข</p>
            </template>
            <template #body="{ data }">
              <Select v-model="data.conditionCode" :options="pMFineTypeDropdown"
                :disabled="!pp004Store.IsEdit || !menuStore.hasManage" rules="required" />
            </template>
          </Column>
          <Column bodyStyle="vertical-align: top" v-if="pp004Store.IsEdit && menuStore.hasManage">
            <template #body="{ index }">
              <Button icon="pi pi-trash" severity="danger" variant="text" @click="() => deleteItem(index)" />
            </template>
          </Column>
          <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
            bodyStyle="vertical-align: top;padding-top: 20px" v-if="pp004Store.IsEdit && menuStore.hasManage">
            <template #rowreordericon>
              <span class="material-symbols-outlined cursor-pointer" :draggable="true">
                drag_indicator
              </span>
            </template>
          </Column>
          <template #empty>
            <p class="text-center text-gray-500">ไม่มีข้อมูล</p>
          </template>
        </DataTable>
      </div>
    </template>
  </Card>
</template>
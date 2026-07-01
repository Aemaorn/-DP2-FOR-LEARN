<script setup lang="ts">
import type { FineRates } from '@/views/PP/models/PP002/pp002Model';
import type { Option } from '@/models/shared/option';
import { ArrayHelper } from '@/helpers/array';
import { InputNumber, InputArea, Select } from '@/components/forms';
import { Card, Button, DataTable, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { onMounted, ref, type Ref } from 'vue';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';
import defaultProps from '@/helpers/defaultProps';
import SharedService from '@/services/Shared/dropdown';
import { SupplyMethodTypeConstant } from '@/enums/preProcurement';
import { PP002DocumentTemplate } from '@/views/PP/enums/pp002';

const props = defineProps({
  titleName: defaultProps(''),
  supplyMethodTypeCode: {
    type: String,
    required: false
  }
});

const value = defineModel<FineRates[]>({
  default: () => [],
  required: true,
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();
const { addSequence, reSequence, deleteItemAndReSequence } = ArrayHelper();

const periodDropdown = ref<Array<Option>>([]);
const fineDropdown = ref<Option[]>([]);

onMounted(async () => {
  await Promise.all([getDropdownAsync(periodDropdown, EGroupCode.PeriodType), getDropdownAsync(fineDropdown, EGroupCode.FineType)]);
});

const getDropdownAsync = async (target: Ref<Array<Option>>, group: EGroupCode) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(group, undefined, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const templateCode = store.PP002Detail.torDocumentTemplateCode;

const isBuyWithHireTemplate = ([
  PP002DocumentTemplate.TorBuyWithHire60,
  PP002DocumentTemplate.TorBuyWithHire80,
] as string[]).includes(templateCode);

const isHireTemplate = ([
  PP002DocumentTemplate.TorHireWithHire60,
  PP002DocumentTemplate.TorHireWithHire80,
] as string[]).includes(templateCode);

const addItem = (): void => {
  let items = [...value.value];

  if (isBuyWithHireTemplate) {
    if (items.length === 0) {
      items = addSequence(items, { rate: 0.2 } as FineRates);
      items = addSequence(items, { rate: 0.1 } as FineRates);
    } else {
      items = addSequence(items, { rate: 0.1 } as FineRates);
    }
  } else if (isHireTemplate) {
    if (items.length === 0) {
      items = addSequence(items, { rate: 0.1 } as FineRates);
      items = addSequence(items, { rate: 0.1 } as FineRates);
    } else {
      items = addSequence(items, { rate: 0.1 } as FineRates);
    }
  } else {
    const defaultRate = props.supplyMethodTypeCode === SupplyMethodTypeConstant.Hire ? 0.1 : 0.2;
    items = addSequence(items, { rate: defaultRate } as FineRates);
  }

  value.value = items;
};

const deleteItem = (index: number): void => {
  value.value = deleteItemAndReSequence(value.value, index);
};

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  value.value = reSequence(event.value);
};
</script>

<template>
  <Card class="mb-4" data-section-id="fine" :data-section-label="props.titleName">
    <template #content>
      <TitleHeader :label="props.titleName">
        <template #action>
          <Button label="เพิ่มรายละเอียดค่าปรับ" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="addItem"
            v-if="store.status.canEditTor && menuStore.hasManage && (((isBuyWithHireTemplate || isHireTemplate) && value.length < 2) || (!(isBuyWithHireTemplate || isHireTemplate) && value.length === 0))" />
        </template>
      </TitleHeader>
      <div v-if="value && value.length > 0">
        <div class="mt-4">
          <DataTable :value="value" @row-reorder="onRowReorder" striped-rows table-class="min-w-[50rem]">
            <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
              <template #header>
                <p class="w-full font-bold text-center">ลำดับ</p>
              </template>
              <template #body="{ data }">
                <div class="mt-8">
                  <p class="text-center">{{ data.sequence }}</p>
                </div>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" body-class="min-w-[500px]">
              <template #header>
                <p class="w-full font-bold text-center">รายละเอียด</p>
              </template>
              <template #body="{ data }">
                <InputArea class="mt-8" label="รายละเอียด" v-model="data.description"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage"
                  :helper-text="`ตัวอย่าง: กำหนดค่าปรับเป็นรายวันในอัตราร้อยละ ${data.rate?.toFixed(2) ?? '0.10'} ของค่าจ้างที่เสนอมา`" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" header-class="min-w-[200px]">
              <template #header>
                <p class="w-full font-bold text-center">อัตราร้อยละ</p>
              </template>
              <template #body="{ data }">
                <InputNumber class="mt-8" v-model="data.rate" rules="required|min_value:0.001"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" grouping :min-fraction-digits="2" :max-fraction-digits="3"
                  :max-number="100" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" body-class="min-w-[300px]">
              <template #header>
                <p class="w-full font-bold text-center">ต่อ</p>
              </template>
              <template #body="{ data }">
                <Select class="mt-8" v-model="data.periodTypeCode" :options="periodDropdown" rules="required"
                  :disabled="!store.status.canEditTor || !menuStore.hasManage" />
              </template>
            </Column>
            <Column header-class="min-w-[300px]" body-class="align-top min-w-[300px]">
              <template #header>
                <p class="w-full font-bold text-center">เงื่อนไข</p>
              </template>
              <template #body="{ data }">
                <div class="mt-8 flex flex-col gap-8">
                  <Select v-model="data.conditionCode" :options="fineDropdown"
                    :disabled="!store.status.canEditTor || !menuStore.hasManage" />
                  <InputArea v-if="data.conditionCode === 'FineType004'" label="หมายเหตุ" v-model="data.conditionOther"
                    :disabled="!store.status.canEditTor || !menuStore.hasManage" />
                </div>
              </template>
            </Column>
            <Column headerStyle="width: 1rem" body-class="align-top"
              v-if="store.status.canEditTor && menuStore.hasManage">
              <template #body="{ index }">
                <Button icon="pi pi-trash" class="mt-9" severity="danger" variant="text"
                  @click="() => deleteItem(index)" />
              </template>
            </Column>
          </DataTable>
        </div>
      </div>
    </template>
  </Card>
</template>

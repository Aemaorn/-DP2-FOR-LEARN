<script setup lang="ts">
import type { SequenceDescription } from '@/views/PP/models/PP002/pp002Model';
import { ArrayHelper } from '@/helpers/array';
import { InputArea } from '@/components/forms';
import { Card, Button, DataTable, Column, type DataTableRowReorderEvent } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { usePPDetailStore } from '@/stores/PP/ppStore';
import { useMenuStore } from '@/stores/menu';

type Props = {
  label: string;
};

// Default Data
const defaultData: SequenceDescription[] = [
  { sequence: 1, description: "มีความสามารถตามกฎหมาย", isDefault: true },
  { sequence: 2, description: "ไม่เป็นบุคคลล้มละลาย", isDefault: true },
  { sequence: 3, description: "ไม่อยู่ระหว่างเลิกกิจการ", isDefault: true },
  { sequence: 4, description: "ไม่เป็นบุคคลซึ่งอยู่ระหว่างถูกระงับการยื่นข้อเสนอหรือทำสัญญากับหน่วยงานของรัฐไว้ชั่วคราวเนื่องจากเป็นผู้ไม่ผ่านเกณฑ์การประเมินผลการปฏิบัติงานของผู้ประกอบการตามระเบียบที่รัฐมนตรีว่าการกระทรวงการคลังกำหนดตามที่ประกาศเผยแพร่ในระบบเครือข่ายสารสนเทศของกรมบัญชีกลาง", isDefault: true },
  { sequence: 5, description: "ไม่เป็นบุคคลซึ่งถูกระบุชื่อไว้ในบัญชีรายชื่อผู้ทิ้งงานและได้แจ้งเวียนชื่อให้เป็นผู้ทิ้งงาน ของหน่วยงานของรัฐในระบบเครือข่ายสารสนเทศของกรมบัญชีกลาง ซึ่งรวมถึงนิติบุคคลที่ผู้ทิ้งงานเป็นหุ้นส่วน ผู้จัดการ กรรมการผู้จัดการ ผู้บริหาร ผู้มีอำนาจในการดำเนินงานในกิจการของนิติบุคคลนั้นด้วย", isDefault: true },
  { sequence: 6, description: "มีคุณสมบัติและไม่มีลักษณะต้องห้ามตามที่คณะกรรมการนโยบายการจัดซื้อจัดจ้าง และการบริหารพัสดุภาครัฐกำหนดในราชกิจจานุเบกษา", isDefault: true },
  { sequence: 7, description: "เป็นบุคคลธรรมดาหรือนิติบุคคลผู้มีอาชีพตามที่จัด{supplyMethodTypeName}ดังกล่าว", isDefault: true },
  { sequence: 8, description: "ไม่เป็นผู้มีผลประโยชน์ร่วมกันกับผู้ยื่นข้อเสนอรายอื่นที่เข้ายื่นข้อเสนอให้แก่ธนาคาร ณ วันประกาศประกวดราคา หรือไม่เป็นผู้กระทำการอันเป็นการขัดขวางการแข่งขันอย่างเป็นธรรมในการประกวดราคาครั้งนี้", isDefault: true },
  { sequence: 9, description: "ไม่เป็นผู้ได้รับเอกสิทธิ์หรือความคุ้มกัน ซึ่งอาจปฏิเสธไม่ยอมขึ้นศาลไทย เว้นแต่รัฐบาลของผู้ยื่นข้อเสนอได้มีคำสั่งให้สละเอกสิทธิ์และความคุ้มกันเช่นว่านั้น", isDefault: true },
  { sequence: 10, description: "ผู้ยื่นข้อเสนอต้องลงทะเบียนในระบบจัดซื้อจัดจ้างภาครัฐด้วยอิเล็กทรอนิกส์ (Electronic Government Procurement : e – GP) ของกรมบัญชีกลาง", isDefault: true },
];

const { label } = defineProps<Props>();

const menuStore = useMenuStore();
const store = usePP002DetailStore();
const procurementStore = usePPDetailStore();
const { addSequence, reSequence, deleteItemAndReSequence } = ArrayHelper();

const value = defineModel<SequenceDescription[]>({
  default: () => []
});

const buildDefaultData = (): SequenceDescription[] =>
  defaultData.map(item => ({
    ...item,
    description: item.description.replace(
      '{supplyMethodTypeName}',
      procurementStore.procurementDetail.supplyMethodType ?? ''),
  }));

if (!value.value || value.value.length === 0) {
  value.value = buildDefaultData();
}

const addItem = (): void => {
  value.value = addSequence(value.value, {} as SequenceDescription);
};

const deleteItem = (index: number): void => {
  value.value = deleteItemAndReSequence(value.value, index);
};

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  value.value = reSequence(event.value);
};
</script>

<template>
  <Card class="mb-4" data-section-id="qualification" :data-section-label="label">
    <template #content>
      <TitleHeader :label="label">
        <template #action>
          <Button label="เพิ่มคุณสมบัติ" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="addItem" v-if="store.status.canEditTor && menuStore.hasManage" />
        </template>
      </TitleHeader>

      <div v-if="value && value.length > 0">
        <DataTable :value="value" @row-reorder="onRowReorder">
          <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
            <template #body="{ data }">
              <p class="text-center">{{ data.sequence }}</p>
            </template>
          </Column>

          <Column bodyStyle="vertical-align: top">
            <template #body="{ data }">
              <InputArea v-model="data.description" :disabled="!store.status.canEditTor || !menuStore.hasManage"
                rules="required" :autoHeight="true" />
            </template>
          </Column>

          <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top">
            <template #body="{ index }">
              <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text" @click="() => deleteItem(index)"
                v-if="store.status.canEditTor && menuStore.hasManage" />
            </template>
          </Column>

          <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
            bodyStyle="vertical-align: top;padding-top: 25px">
            <template #rowreordericon>
              <span class="material-symbols-outlined cursor-pointer" :draggable="true"
                v-if="store.status.canEditTor && menuStore.hasManage">
                drag_indicator
              </span>
            </template>
          </Column>
        </DataTable>
      </div>
    </template>
  </Card>
</template>
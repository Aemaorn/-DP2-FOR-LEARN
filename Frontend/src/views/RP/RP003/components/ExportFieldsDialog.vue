<script setup lang="ts">
import { Button, Checkbox, Dialog, InputText } from 'primevue';
import { computed, ref, watch } from 'vue';

const value = defineModel<boolean>({ default: false });

const emit = defineEmits<{
  confirm: [columns: number[]];
}>();

type Field = { index: number; label: string };

const ALL_FIELDS: Field[] = [
  { index: 1, label: 'วันที่รับเรื่องเข้าส่วนบริหารสัญญา' },
  { index: 2, label: 'เลขที่ใบสั่งซื้อ-สั่งจ้าง (PO)' },
  { index: 3, label: 'วันที่ (PO)' },
  { index: 4, label: 'วิธีการจัดซื้อจัดจ้าง' },
  { index: 5, label: 'ฝ่ายงาน-ผู้รับผิดชอบ' },
  { index: 6, label: 'ส่วนงาน-ผู้รับผิดชอบ' },
  { index: 7, label: 'เลขที่หนังสือแจ้งผู้รับจ้างลงนามสัญญา' },
  { index: 8, label: 'วันที่แจ้งผู้รับจ้างมาลงนาม' },
  { index: 9, label: 'เลขที่สัญญา จพ.(สบส.)' },
  { index: 10, label: 'วันที่ทำสัญญา' },
  { index: 11, label: 'ประเภทสัญญา' },
  { index: 12, label: 'ชื่อโครงการ (สัญญา)' },
  { index: 13, label: 'ค่าจ้าง (รวมภาษี)' },
  { index: 14, label: 'บริษัทคู่ค้า/ผู้รับจ้าง' },
  { index: 15, label: 'กรรมการบริษัท' },
  { index: 16, label: 'ผู้รับมอบอำนาจ/ผู้ลงนามสัญญา' },
  { index: 17, label: 'ผู้มอบอำนาจลงนามสัญญา' },
  { index: 18, label: 'ประเภทหลักประกันสัญญา' },
  { index: 19, label: 'วงเงินหลักประกันสัญญา' },
  { index: 20, label: 'วันที่ส่ง LG ตรวจสอบกับธนาคารผู้ออก LG' },
  { index: 21, label: 'วันที่ธนาคารตอบกลับ LG' },
  { index: 22, label: 'วันที่เริ่มหนังสือค้ำหลักประกันสัญญา (LG)' },
  { index: 23, label: 'วันที่สิ้นสุดหนังสือค้ำหลักประกันสัญญา (LG)' },
  { index: 24, label: 'ระยะเวลาแล้วเสร็จ/ส่งมอบงาน/ระยะเวลาของสัญญา' },
  { index: 25, label: 'วันที่เริ่มต้นของสัญญา' },
  { index: 26, label: 'วันที่สิ้นสุดของสัญญา' },
  { index: 27, label: 'ระยะเวลารับประกันความชำรุดบกพร่อง' },
  { index: 28, label: 'วันที่เริ่มต้นรับประกันสัญญา' },
  { index: 29, label: 'วันที่สิ้นสุดรับประกันสัญญา' },
  { index: 30, label: 'วันที่อนุมัติคืนหลักประกันสัญญา' },
  { index: 31, label: 'วันที่รับคืนหลักประกันสัญญา' },
  { index: 32, label: 'วันที่ส่งต้นฉบับให้ผู้รับจ้างลงนามในสัญญา' },
  { index: 33, label: 'วันที่รับต้นฉบับสัญญาคืนจากผู้รับจ้าง' },
  { index: 34, label: 'จำนวนวันที่คู่สัญญาลงนาม (7 วัน)' },
  { index: 35, label: 'วันที่ผู้มีอำนาจลงนามในสัญญา (ธอส.)' },
  { index: 36, label: 'วันที่ได้รับเรื่องคืนจากผู้มีอำนาจลงนาม (ธอส.)' },
  { index: 37, label: 'ผู้มีอำนาจลงนามในสัญญา' },
  { index: 38, label: 'วันที่รายงาน สตง./สรรพากร' },
  { index: 39, label: 'วันที่อนุมัติจ้าง' },
  { index: 40, label: 'วันที่เอกสารลงนามครบถ้วนพร้อมลงนาม' },
  { index: 41, label: 'สบส.-วันที่ตรวจสอบ COI' },
  { index: 42, label: 'สบส.-วันที่ตรวจสอบ CDD/KYC Watchlist' },
  { index: 43, label: 'สบส.-วันที่ตรวจสอบผู้ทิ้งงาน' },
  { index: 44, label: 'สบส.-วันที่คู่สัญญาลงนาม' },
  { index: 45, label: 'วันที่ธนาคารลงนาม' },
  { index: 46, label: 'รวมวันทำสัญญาลงนามแล้วเสร็จทั้ง 2 ฝ่าย' },
  { index: 47, label: 'ผู้รับผิดชอบ' },
  { index: 48, label: 'วันที่บันทึกข้อมูล' },
  { index: 49, label: 'ทะเบียนกล่องส่งเก็บบริษัทจัดเก็บเอกสาร' },
  { index: 50, label: 'หมายเหตุ' },
];

const mode = ref<'checkbox' | 'listbox'>('checkbox');

// --- Checkbox mode ---
const selectedIndices = ref<number[]>([]);

const isAllSelected = computed(
  () => selectedIndices.value.length === ALL_FIELDS.length
);

const isPartiallySelected = computed(
  () => selectedIndices.value.length > 0 && selectedIndices.value.length < ALL_FIELDS.length
);

const toggleSelectAll = (): void => {
  if (isAllSelected.value) selectedIndices.value = [];
  else selectedIndices.value = ALL_FIELDS.map((f): number => f.index);
};

// --- Dual Listbox mode ---
const listboxAvailable = ref<Field[]>([...ALL_FIELDS]);
const listboxSelected = ref<Field[]>([]);
const leftHighlighted = ref<number[]>([]);
const rightHighlighted = ref<number[]>([]);
const searchQuery = ref('');

const filteredAvailable = computed((): Field[] => {
  const q = searchQuery.value.trim();
  if (!q) return listboxAvailable.value;
  return listboxAvailable.value.filter((f): boolean => f.label.toLowerCase().includes(q.toLowerCase()));
});

const toggleLeft = (index: number): void => {
  const i = leftHighlighted.value.indexOf(index);
  if (i >= 0) leftHighlighted.value.splice(i, 1);
  else leftHighlighted.value.push(index);
};

const toggleRight = (index: number): void => {
  const i = rightHighlighted.value.indexOf(index);
  if (i >= 0) rightHighlighted.value.splice(i, 1);
  else rightHighlighted.value.push(index);
};

const moveToRight = (): void => {
  if (!leftHighlighted.value.length) return;
  const map = new Map(listboxAvailable.value.map((f): [number, Field] => [f.index, f]));
  const toMove = leftHighlighted.value.map((idx): Field | undefined => map.get(idx)).filter((f): f is Field => !!f);
  const set = new Set(leftHighlighted.value);
  listboxSelected.value.push(...toMove);
  listboxAvailable.value = listboxAvailable.value.filter((f): boolean => !set.has(f.index));
  leftHighlighted.value = [];
};

const moveAllToRight = (): void => {
  listboxSelected.value.push(...listboxAvailable.value);
  listboxAvailable.value = [];
  leftHighlighted.value = [];
};

const moveToLeft = (): void => {
  if (!rightHighlighted.value.length) return;
  const set = new Set(rightHighlighted.value);
  listboxAvailable.value.push(...listboxSelected.value.filter((f): boolean => set.has(f.index)));
  listboxAvailable.value.sort((a, b): number => a.index - b.index);
  listboxSelected.value = listboxSelected.value.filter((f): boolean => !set.has(f.index));
  rightHighlighted.value = [];
};

const moveAllToLeft = (): void => {
  listboxAvailable.value.push(...listboxSelected.value);
  listboxAvailable.value.sort((a, b): number => a.index - b.index);
  listboxSelected.value = [];
  rightHighlighted.value = [];
};

const dragFromIndex = ref<number | null>(null);
const dragOverIndex = ref<number | null>(null);

const onDragStart = (i: number): void => {
  dragFromIndex.value = i;
};

const onDragOver = (e: DragEvent, i: number): void => {
  e.preventDefault();
  dragOverIndex.value = i;
};

const onDrop = (i: number): void => {
  if (dragFromIndex.value === null || dragFromIndex.value === i) {
    dragFromIndex.value = null;
    dragOverIndex.value = null;
    return;
  }
  const arr = [...listboxSelected.value];
  const [removed] = arr.splice(dragFromIndex.value, 1);
  arr.splice(i, 0, removed);
  listboxSelected.value = arr;
  dragFromIndex.value = null;
  dragOverIndex.value = null;
};

const onDragEnd = (): void => {
  dragFromIndex.value = null;
  dragOverIndex.value = null;
};

watch(value, (val): void => {
  if (val) {
    selectedIndices.value = [];
    listboxAvailable.value = [...ALL_FIELDS];
    listboxSelected.value = [];
    leftHighlighted.value = [];
    rightHighlighted.value = [];
    searchQuery.value = '';
  }
});

const canConfirm = computed((): boolean => {
  if (mode.value === 'checkbox') return selectedIndices.value.length > 0;
  return listboxSelected.value.length > 0;
});

const onConfirm = (): void => {
  if (mode.value === 'checkbox') {
    const sorted = [0, ...selectedIndices.value].sort((a, b): number => a - b);
    emit('confirm', [...new Set(sorted)]);
  } else {
    emit('confirm', [0, ...listboxSelected.value.map((f): number => f.index)]);
  }
  value.value = false;
};

const onCancel = () => {
  value.value = false;
};
</script>

<template>
  <Dialog v-model:visible="value" modal :draggable="false" :style="{ width: '1280px' }"
    :breakpoints="{ '575px': '90vw' }" @hide="() => (value = false)">
    <template #header>
      <span class="font-bold text-lg">เลือกรายการที่ต้องการพิมพ์</span>
    </template>
    <template #default>
      <!-- Mode Toggle -->
      <div class="flex mb-3 justify-end gap-1 text-xs">
        <button @click="mode = 'checkbox'"
          :class="['px-3 py-1.5 font-medium rounded-lg transition-colors flex items-center gap-1.5',
            mode === 'checkbox' ? 'bg-gray-200 text-gray-700' : 'text-gray-500 hover:bg-gray-100']">
          <i class="pi pi-check-square text-xs" />
          เลือกแบบ Checkbox
        </button>
        <button @click="mode = 'listbox'"
          :class="['px-3 py-1.5 font-medium rounded-lg transition-colors flex items-center gap-1.5',
            mode === 'listbox' ? 'bg-gray-200 text-gray-700' : 'text-gray-500 hover:bg-gray-100']">
          <i class="pi pi-list text-xs" />
          เลือกและจัดลำดับ
        </button>
      </div>

      <!-- Checkbox Mode -->
      <template v-if="mode === 'checkbox'">
        <div class="bg-white rounded-xl shadow-sm ring-1 ring-gray-100 overflow-hidden flex flex-col" style="height: 440px;">
          <div class="flex items-center gap-2 px-4 py-3 border-b border-gray-100 shrink-0 hover:bg-gray-50">
            <Checkbox :modelValue="isAllSelected" @update:modelValue="toggleSelectAll" binary inputId="selectAll"
              :indeterminate="isPartiallySelected" />
            <label for="selectAll" class="cursor-pointer font-semibold">เลือกทั้งหมด</label>
            <span class="text-gray-400 text-sm ml-2">({{ selectedIndices.length }}/{{ ALL_FIELDS.length }})</span>
          </div>
          <div class="overflow-y-auto flex-1 p-2">
            <div class="grid grid-cols-3 gap-1">
              <div v-for="field in ALL_FIELDS" :key="field.index"
                class="flex items-center gap-2 p-2 rounded-lg hover:bg-gray-50">
                <Checkbox v-model="selectedIndices" :value="field.index" :inputId="`field-${field.index}`" />
                <label :for="`field-${field.index}`" class="cursor-pointer text-sm leading-tight">{{ field.label }}</label>
              </div>
            </div>
          </div>
        </div>
      </template>

      <!-- Dual Listbox Mode -->
      <template v-else>
        <div class="flex gap-3">
          <!-- Left: Available Fields -->
          <div class="flex-1 bg-white rounded-xl shadow-sm ring-1 ring-gray-100 overflow-hidden flex flex-col" style="min-width: 0; height: 440px;">
            <div class="px-4 py-3 border-b border-gray-100 flex items-center gap-1 shrink-0">
              <span class="font-semibold text-sm text-gray-700">รายการทั้งหมด</span>
              <span class="text-gray-400 text-sm">({{ listboxAvailable.length }})</span>
            </div>
            <div class="px-3 py-2 border-b border-gray-100">
              <InputText v-model="searchQuery" placeholder="ค้นหา" class="w-full" size="small" />
            </div>
            <div class="overflow-y-auto flex-1">
              <div v-for="field in filteredAvailable" :key="field.index"
                @click="toggleLeft(field.index)"
                :class="['px-4 py-2 cursor-pointer text-sm select-none transition-colors flex items-center gap-2',
                  leftHighlighted.includes(field.index) ? 'bg-gray-100 text-gray-700' : 'hover:bg-gray-50']">
                <span class="w-5 h-5 shrink-0 flex items-center justify-center">
                  <span v-if="leftHighlighted.includes(field.index)"
                    class="text-xs font-bold w-5 h-5 rounded-full bg-gray-500 text-white flex items-center justify-center leading-none">
                    {{ leftHighlighted.indexOf(field.index) + 1 }}
                  </span>
                </span>
                {{ field.label }}
              </div>
              <div v-if="filteredAvailable.length === 0"
                class="px-4 py-6 text-sm text-gray-400 text-center">
                ไม่พบรายการ
              </div>
            </div>
          </div>

          <!-- Center Buttons -->
          <div class="flex flex-col justify-center gap-2 shrink-0">
            <Button icon="pi pi-angle-right" size="small" @click="moveToRight"
              :disabled="leftHighlighted.length === 0" />
            <Button icon="pi pi-angle-left" size="small" variant="outlined" @click="moveToLeft"
              :disabled="rightHighlighted.length === 0" />
            <div class="border-t border-gray-200 my-1" />
            <Button icon="pi pi-angle-double-right" size="small" severity="secondary" @click="moveAllToRight"
              :disabled="listboxAvailable.length === 0" />
            <Button icon="pi pi-angle-double-left" size="small" severity="secondary" variant="outlined" @click="moveAllToLeft"
              :disabled="listboxSelected.length === 0" />
          </div>

          <!-- Right: Selected Fields -->
          <div class="flex-1 bg-white rounded-xl shadow-sm ring-1 ring-gray-100 overflow-hidden flex flex-col" style="min-width: 0; height: 440px;">
            <div class="px-4 py-3 border-b border-gray-100 flex items-center gap-1 shrink-0">
              <span class="font-semibold text-sm text-gray-700">รายการที่เลือก</span>
              <span class="text-gray-400 text-sm">({{ listboxSelected.length }})</span>
            </div>
            <div class="overflow-y-auto flex-1">
              <div
                v-for="(field, i) in listboxSelected"
                :key="field.index"
                draggable="true"
                @click="toggleRight(field.index)"
                @dragstart="onDragStart(i)"
                @dragover="onDragOver($event, i)"
                @drop="onDrop(i)"
                @dragend="onDragEnd"
                :class="['px-4 py-2 text-sm select-none flex items-center gap-2 transition-colors border-t-2',
                  dragFromIndex === i ? 'opacity-40 cursor-grabbing' : 'cursor-grab',
                  dragOverIndex === i && dragFromIndex !== i ? 'border-gray-400' : 'border-transparent',
                  rightHighlighted.includes(field.index) ? 'bg-gray-100 text-gray-700' : 'hover:bg-gray-50']">
                <i :class="['pi pi-bars text-xs shrink-0',
                  rightHighlighted.includes(field.index) ? 'text-gray-500' : 'text-gray-300']" />
                {{ field.label }}
              </div>
              <div v-if="listboxSelected.length === 0"
                class="px-4 py-6 text-sm text-gray-400 text-center">
                ยังไม่มีรายการที่เลือก
              </div>
            </div>
          </div>
        </div>
      </template>
    </template>
    <template #footer>
      <div class="flex justify-end gap-2">
        <Button label="ยกเลิก" variant="outlined" @click="onCancel" />
        <Button label="ออกรายงาน" icon="pi pi-file-excel" :disabled="!canConfirm" @click="onConfirm" />
      </div>
    </template>
  </Dialog>
</template>

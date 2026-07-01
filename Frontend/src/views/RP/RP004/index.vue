<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Radio } from '@/components/forms';
import { checkType } from '@/enums/RP/rp004';
import { ToDateTime } from '@/helpers/dateTime';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import type { Option } from '@/models/shared/option';
import { useRp004Store } from '@/stores/RP/rp004';
import { computed, onUnmounted, ref, watch } from 'vue';
import { InputGroupAddon } from 'primevue';
import * as XLSX from 'xlsx-js-style';
import type { TVendorItem } from '@/stores/RP/rp004';
import { useForm } from 'vee-validate';

const { validate } = useForm();

const type = [
  { value: checkType.COI, label: 'COI' },
  { value: checkType.WatchList, label: 'Watchlist' },
] as Option[];

const personTypeOptions = [
  { value: false, label: 'บุคคลธรรมดา' },
  { value: true, label: 'นิติบุคคล' },
] as Option[];

const store = useRp004Store();
const exporting = ref(false);

const normalizeFullName = (value: string): string => value.trim().replace(/\s+/g, ' ');

const isRowEmpty = (item: TVendorItem): boolean => {
  if (store.body.checkType === checkType.COI) return !item.taxpayerIdentificationNo && !item.firstName;
  if (item.searchBy === 'name') return !item.firstName;
  return !item.taxpayerIdentificationNo && !item.firstName;
};

const hasEmptyRow = computed((): boolean => store.vendorItems.some(isRowEmpty));

const onCheck = async (): Promise<void> => {
  const { valid } = await validate();
  if (!valid) return;

  // ไม่มี vendor → ตรวจสอบเลย ไม่บันทึก
  if (!store.body.vendorId) {
    await store.onCreateVendorCheckHistory(false);
    return;
  }

  // มี vendor → ถามก่อนว่าจะบันทึกกรรมการ/ผู้ถือหุ้นด้วยหรือไม่
  const result = await showConfirmDialogAsync(
    undefined,
    `ต้องการบันทึกข้อมูลกรรมการ / ผู้ถือหุ้น กับ ${store.body.vendorName} หรือไม่?`,
    'ไม่บันทึก',
    'บันทึก',
    'เมื่อบันทึกข้อมูลกรรมการ/ผู้ถือหุ้นของคู่ค้ารายการนี้แล้ว การตรวจสอบในครั้งถัดไปจะแสดงข้อมูลที่เคยบันทึกไว้ให้โดยอัตโนมัติ',
    false,
    'success'
  );

  // ปิด dialog (กากบาท) → ยกเลิก ไม่ตรวจสอบ
  if (result === undefined) return;

  // true = บันทึก, false = ไม่บันทึก (ทั้งสองกรณีตรวจสอบ)
  await store.onCreateVendorCheckHistory(result);
};

watch((): typeof store.body.checkType => store.body.checkType, (): void => {
  store.checkResults.splice(0);
  store.body.resultDate = undefined;
});

const exportToExcel = (): void => {
  exporting.value = true;

  try {
    const isCOI = store.body.checkType === checkType.COI;
    const reportTitle = isCOI
      ? 'รายงานผลการตรวจสอบความสัมพันธ์กับพนักงานธนาคารอาคารสงเคราะห์ (ตรวจสอบ COI กลุ่มจัดซื้อจัดจ้าง)'
      : 'รายงานผลการตรวจสอบ Watchlist';
    const results = store.checkResults;

    const remarkText = (res: (typeof results)[0]): string => {
      if (res.result === 'UnKnow') {
        return isCOI
          ? 'ไม่สามารถเชื่อมต่อระบบ COI ได้ กรุณาลองใหม่อีกครั้ง'
          : 'ไม่สามารถเชื่อมต่อระบบ Watchlist ได้ กรุณาลองใหม่อีกครั้ง';
      }
      return res.remark || '-';
    };

    const headers = isCOI
      ? ['เลขที่บัตรประชาชน', 'ชื่อ – นามสกุล', 'ผลการตรวจสอบ', 'ชื่อ พนักงาน', 'ตำแหน่ง', 'เวลาตรวจ', 'รหัสพนักงานผู้ตรวจ']
      : ['เลขที่บัตรประชาชน', 'ชื่อ – นามสกุล', 'ผลการตรวจสอบ', 'เวลาตรวจ', 'รหัสพนักงานผู้ตรวจ'];

    const rows = results.map((res): string[] => isCOI
      ? [res.taxpayerIdentificationNo || '', res.name || '', remarkText(res), res.employeeName || '', res.position || '', res.checkTime || '', res.checkerEmployeeCode || '']
      : [res.taxpayerIdentificationNo || '', res.name || '', remarkText(res), res.checkTime || '', res.checkerEmployeeCode || '']);

    const now = new Date();
    const pad = (n: number): string => String(n).padStart(2, '0');
    const printDateStr = `วันที่พิมพ์ : ${pad(now.getDate())}/${pad(now.getMonth() + 1)}/${now.getFullYear() + 543} ${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`;

    const colCount = headers.length;
    const titleRow = [reportTitle, ...Array(colCount - 1).fill('')];
    const printRow = [printDateStr, ...Array(colCount - 1).fill('')];
    const wsData = [titleRow, printRow, headers, ...rows];

    const wb = XLSX.utils.book_new();
    const ws = XLSX.utils.aoa_to_sheet(wsData);

    const border = {
      top: { style: 'thin', color: { rgb: '000000' } },
      bottom: { style: 'thin', color: { rgb: '000000' } },
      left: { style: 'thin', color: { rgb: '000000' } },
      right: { style: 'thin', color: { rgb: '000000' } },
    };
    const baseFont = { name: 'Cordia New', sz: 16 };
    const centerWrap = { horizontal: 'center', vertical: 'center', wrapText: true };
    const leftWrap = { horizontal: 'left', vertical: 'center', wrapText: true };
    const rightWrap = { horizontal: 'right', vertical: 'center', wrapText: false };

    for (let r = 0; r < wsData.length; r++) {
      for (let c = 0; c < colCount; c++) {
        const addr = XLSX.utils.encode_cell({ r, c });
        if (!ws[addr]) ws[addr] = { t: 's', v: '' };
        const isTitle = r === 0;
        const isPrintDate = r === 1;
        const isHeader = r === 2;
        const isOddDataRow = !isTitle && !isPrintDate && !isHeader && (r - 3) % 2 === 1;
        let fill: { patternType: string; fgColor?: { rgb: string } };
        if (isHeader) fill = { patternType: 'solid', fgColor: { rgb: 'DBDBDB' } };
        else if (isOddDataRow) fill = { patternType: 'solid', fgColor: { rgb: 'F3F4F6' } };
        else fill = { patternType: 'none' };
        if (isPrintDate) {
          ws[addr].s = {
            font: { ...baseFont, sz: 14 },
            alignment: rightWrap,
            fill: { patternType: 'none' },
          };
        } else {
          ws[addr].s = {
            font: { ...baseFont, sz: isTitle ? 18 : 16, bold: isTitle || isHeader },
            alignment: isTitle || isHeader ? centerWrap : (c === 0 || c >= colCount - 2 ? centerWrap : leftWrap),
            fill,
            border,
          };
        }
      }
    }

    ws['!merges'] = [
      { s: { r: 0, c: 0 }, e: { r: 0, c: colCount - 1 } },
      { s: { r: 1, c: 0 }, e: { r: 1, c: colCount - 1 } },
    ];

    ws['!cols'] = isCOI
      ? [{ wch: 20 }, { wch: 22 }, { wch: 30 }, { wch: 22 }, { wch: 22 }, { wch: 20 }, { wch: 20 }]
      : [{ wch: 20 }, { wch: 28 }, { wch: 35 }, { wch: 22 }, { wch: 20 }];

    ws['!rows'] = [{ hpt: 40 }, { hpt: 22 }, { hpt: 32 }, ...rows.map((): { hpt: number } => ({ hpt: 28 }))];

    const typeLabel = isCOI ? 'COI' : 'Watchlist';
    XLSX.utils.book_append_sheet(wb, ws, typeLabel);

    const dateStr = new Date().toLocaleDateString('th-TH').replace(/\//g, '-');
    XLSX.writeFile(wb, `ข้อมูล${typeLabel}_${dateStr}.xlsx`);
  } finally {
    exporting.value = false;
  }
};

onUnmounted((): void => {
  store.onClearBody();
});
</script>

<template>
  <TitleHeader label="ข้อมูล COI และ Watchlist" />

  <Card>
    <template #content>
      <TitleHeader label="รายการตรวจสอบ" />
      <div class="mb-4 mt-4">
        <Radio :options="type" v-model="store.body.checkType" />
      </div>
      <div class="mt-4 lg:w-1/3">
        <InputField v-model="store.body.vendorName" :disabled="true">
          <template #appendAction>
            <InputGroupAddon>
              <Button label="ค้นหา" class="rounded-none! text-white! bg-gray-500! border-none! w-full h-full"
                @click="store.selectVendor" />
            </InputGroupAddon>
            <InputGroupAddon>
              <Button label="ล้าง" icon="pi pi-eraser" class="rounded-none! border-none! w-full h-full"
                variant="outlined" severity="secondary" @click="store.onClearBody" />
            </InputGroupAddon>
          </template>
        </InputField>
        <p class="mt-1 text-lg text-gray-400">
          <span class="font-semibold text-gray-500">หมายเหตุ:</span> ค้นหาเพื่อดึงข้อมูลกรรมการ/ผู้ถือหุ้น
        </p>
      </div>

      <div class="flex justify-end gap-2 mb-2">
        <Button label="ตรวจสอบ" icon="pi pi-search" severity="success" :disabled="hasEmptyRow" @click="onCheck" />
        <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined" @click="store.addVendorItem" />
      </div>
      <table class="w-full border-collapse text-sm mt-4">
        <thead>
          <tr class="bg-gray-200 text-gray-900 font-bold text-base">
            <th class="border border-gray-300 px-3 py-2 text-center w-12">ลำดับที่</th>
            <th class="border border-gray-300 px-3 py-2 text-center">กรรมการ / ผู้ถือหุ้น</th>
            <template v-if="store.body.checkType === checkType.WatchList">
              <th class="border border-gray-300 px-3 py-2 text-center">ประเภท <span class="text-red-500">*</span></th>
              <th class="border border-gray-300 px-3 py-2 text-center">เลขที่บัตรประชาชน / เลขประจำตัวผู้เสียภาษี</th>
              <th class="border border-gray-300 px-3 py-2 text-center">ชื่อ-นามสกุล / บริษัท<br><span class="text-red-500 font-normal text-xs"><b>หมายเหตุ</b> ชื่อ-นามสกุล (ไม่ต้องกรอกคำนำหน้าชื่อ) เช่น สมชาย ใจดี</span></th>
            </template>
            <template v-else-if="store.body.checkType === checkType.COI">
              <th class="border border-gray-300 px-3 py-2 text-center">เลขที่บัตรประชาชน</th>
              <th class="border border-gray-300 px-3 py-2 text-center">ชื่อ-นามสกุล<br><span class="text-red-500 font-normal text-xs"><b>หมายเหตุ</b> ชื่อ-นามสกุล (ไม่ต้องกรอกคำนำหน้าชื่อ) เช่น สมชาย ใจดี</span></th>
            </template>
            <th class="border border-gray-300 px-3 py-2 w-12"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(item, index) in store.vendorItems" :key="index" :class="isRowEmpty(item) ? 'bg-red-50' : 'bg-white hover:bg-gray-50'">
            <td class="border border-gray-300 px-3 py-2 text-center text-gray-500">{{ index + 1 }}</td>
            <td class="border border-gray-300 px-3 py-2">
              <div class="flex flex-wrap gap-4">
                <div class="flex items-center gap-2">
                  <Checkbox v-model="item.isDirector" :binary="true" :inputId="`isDirector-${index}`" />
                  <label :for="`isDirector-${index}`" class="cursor-pointer text-lg md:text-xl! -mt-4">กรรมการ</label>
                </div>
                <div class="flex items-center gap-2">
                  <Checkbox v-model="item.isShareholder" :binary="true" :inputId="`isShareholder-${index}`" />
                  <label :for="`isShareholder-${index}`" class="cursor-pointer text-lg md:text-xl! -mt-4">ผู้ถือหุ้น</label>
                </div>
              </div>
            </td>
            <td v-if="store.body.checkType === checkType.WatchList" class="border border-gray-300 px-3 py-2">
              <Radio :options="personTypeOptions" v-model="item.isJuristic" rules="required" />
            </td>
            <td class="border border-gray-300 px-3 py-2">
              <InputField v-model.trim="item.taxpayerIdentificationNo" rules="digits13" eager />
            </td>
            <td class="border border-gray-300 px-3 py-2">
              <InputField v-model.trim="item.firstName" @blur="item.firstName = (store.body.checkType === checkType.WatchList && item.isJuristic) ? (item.firstName ?? '').trim() : normalizeFullName(item.firstName ?? '')" />
            </td>
            <td class="border border-gray-300 px-3 py-2 text-center">
              <Button
                icon="pi pi-trash"
                severity="danger"
                text
                rounded
                @click="async () => { if (store.body.vendorId) { const ok = await showConfirmDialogAsync(undefined, 'ยืนยันการลบข้อมูลหรือไม่?', 'ยกเลิก', 'ยืนยัน', 'ต้องการลบกรรมการ/ผู้ถือหุ้นของ ' + store.body.vendorName + ' คุณต้องการดำเนินการต่อหรือไม่?'); if (!ok) return; } store.removeVendorItem(index); }"
              />
            </td>
          </tr>
        </tbody>
      </table>
    </template>
  </Card>

  <Card class="mt-4" v-if="store.body.resultDate">
    <template #content>
      <TitleHeader :label="store.body.checkType === checkType.COI ? 'ผลการตรวจสอบ COI' : 'ผลการตรวจสอบ Watchlist'">
        <template #action>
          <Button
            v-if="!store.isUnKnow"
            label="Export Excel"
            icon="pi pi-file-excel"
            :loading="exporting"
            @click="exportToExcel"
          />
        </template>
      </TitleHeader>
      <div class="my-4 text-sm text-gray-500">
        ตรวจสอบ ณ วันที่ : <span class="text-gray-700 font-medium">{{ ToDateTime(store.body.resultDate) }}</span>
      </div>

      <div v-if="store.isUnKnow" class="flex items-center gap-2 text-gray-500">
        <span class="material-symbols-outlined">help</span>
        <span>{{ store.body.remark || (store.body.checkType === checkType.COI ? 'ไม่สามารถเชื่อมต่อระบบ COI ได้ กรุณาลองใหม่อีกครั้ง' : 'ไม่สามารถเชื่อมต่อระบบ Watchlist ได้ กรุณาลองใหม่อีกครั้ง') }}</span>
      </div>

      <table v-else class="w-full border-collapse text-sm">
        <thead>
          <tr class="bg-gray-200 text-gray-900 font-bold text-base">
            <th class="border border-gray-300 px-3 py-2 text-center">เลขที่บัตรประชาชน</th>
            <th class="border border-gray-300 px-3 py-2 text-center">ชื่อ – นามสกุล</th>
            <th class="border border-gray-300 px-3 py-2 text-center">ผลการตรวจสอบ</th>
            <template v-if="store.body.checkType === checkType.COI">
              <th class="border border-gray-300 px-3 py-2 text-center">ชื่อพนักงาน</th>
              <th class="border border-gray-300 px-3 py-2 text-center">ตำแหน่ง</th>
            </template>
            <th class="border border-gray-300 px-3 py-2 text-center">เวลาตรวจ</th>
            <th class="border border-gray-300 px-3 py-2 text-center">รหัสพนักงานผู้ตรวจ</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(item, index) in store.checkResults" :key="index" class="odd:bg-white even:bg-gray-50">
            <td class="border border-gray-300 px-3 py-2 text-center">{{ item.taxpayerIdentificationNo || '-' }}</td>
            <td class="border border-gray-300 px-3 py-2">{{ item.name || '-' }}</td>
            <td class="border border-gray-300 px-3 py-2">{{ item.remark || '-' }}</td>
            <template v-if="store.body.checkType === checkType.COI">
              <td class="border border-gray-300 px-3 py-2">{{ item.employeeName || '-' }}</td>
              <td class="border border-gray-300 px-3 py-2">{{ item.position || '-' }}</td>
            </template>
            <td class="border border-gray-300 px-3 py-2 text-center">{{ item.checkTime || '-' }}</td>
            <td class="border border-gray-300 px-3 py-2 text-center">{{ item.checkerEmployeeCode || '-' }}</td>
          </tr>
        </tbody>
      </table>
    </template>
  </Card>
</template>

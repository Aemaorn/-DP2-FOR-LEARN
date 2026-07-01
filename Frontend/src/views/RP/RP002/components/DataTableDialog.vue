<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { useRP002DetailStore } from '@/stores/RP/RP002/detail';
import { Dialog } from 'primevue';
import { computed } from 'vue';

const isShow = defineModel<boolean>({ required: true });

const store = useRP002DetailStore();

const CONTRACT_TYPE_ORDER: Record<string, number> = {
  'CMType001': 1,
  'CMType002': 2,
  'CMType003': 3,
};

// dataTableDialog มาจาก getContractSummaryAsync ซึ่งรวมข้อมูลจากทุก Quarter ที่สร้างไปแล้ว
const sourceData = computed(() =>
  store.dataTableDialog.filter(d => d.contractTypeCode !== 'ALL')
);

const quarters = computed((): number[] => {
  const max = store.body.quarter ?? 1;
  return Array.from({ length: max }, (_, i): number => i + 1);
});

const contractTypes = computed((): { code: string; name: string }[] => {
  const seen = new Set<string>();
  const result: { code: string; name: string }[] = [];

  for (const item of sourceData.value) {
    if (!seen.has(item.contractTypeCode)) {
      seen.add(item.contractTypeCode);
      result.push({ code: item.contractTypeCode, name: item.contractTypeName });
    }
  }

  result.sort((a, b): number => {
    const orderA = CONTRACT_TYPE_ORDER[a.code] ?? 99;
    const orderB = CONTRACT_TYPE_ORDER[b.code] ?? 99;
    return orderA - orderB;
  });

  result.push({ code: 'ALL', name: 'รวมสัญญาทั้งสิ้น' });
  return result;
});

const getCell = (quarter: number, contractTypeCode: string) => {
  if (contractTypeCode === 'ALL') {
    const items = sourceData.value.filter(d => d.quarter === quarter);
    const total = items.reduce((sum, d) => sum + d.contractCount, 0);
    return total > 0 ? { contractCount: total, percentComplete: 100 } : undefined;
  }
  return sourceData.value.find(d => d.quarter === quarter && d.contractTypeCode === contractTypeCode);
};

const formatPercent = (value: number): string => `${value}%`;

const getTotalCount = (contractTypeCode: string): number => {
  if (contractTypeCode === 'ALL') {
    return quarters.value.reduce((sum, q) => sum + (getCell(q, 'ALL')?.contractCount ?? 0), 0);
  }
  return sourceData.value
    .filter(d => d.contractTypeCode === contractTypeCode)
    .reduce((sum, d) => sum + d.contractCount, 0);
};

const getTotalPercent = (contractTypeCode: string): string => {
  if (contractTypeCode === 'ALL') return '100%';
  const typeTotal = getTotalCount(contractTypeCode);
  const allTotal = getTotalCount('ALL');
  if (allTotal === 0) return '0%';
  return formatPercent(Math.round(typeTotal * 10000 / allTotal) / 100);
};
</script>

<template>
  <Dialog v-model:visible="isShow" modal :draggable="false" :style="{ width: '80vw' }"
    :breakpoints="{ '1199px': '90vw', '575px': '95vw' }" :close-on-escape="false">
    <template #header>
      <TitleHeader label="ตารางสรุปการทำสัญญาแล้วเสร็จไตรมาส" />
    </template>

    <div class="overflow-x-auto">
      <table class="min-w-full border border-gray-300 text-sm text-center">
        <thead>
          <tr>
            <th rowspan="2" class="border border-gray-300 px-4 py-2 text-red-600 font-semibold">
              <p>รายการสัญญาเรียงลำดับ<br />ตามประเภทสัญญา</p>
            </th>
            <th v-for="q in quarters" :key="q" colspan="2"
              class="border border-gray-300 px-4 py-2 text-red-600 font-semibold">
              <p>สัญญาแล้วเสร็จ<br />ไตรมาส {{ q }}/{{ store.body.year }}</p>
            </th>
            <th colspan="2" class="border border-gray-300 px-4 py-2 text-red-600 font-semibold">
              <p>สัญญาแล้วเสร็จทั้งหมด<br />{{ store.body.year }}</p>
            </th>
          </tr>
          <tr>
            <template v-for="q in quarters" :key="q">
              <th class="border border-gray-300 px-2 py-1">
                <p>จำนวน (ฉบับ)</p>
              </th>
              <th class="border border-gray-300 px-2 py-1">
                <p>เปอร์เซ็นต์</p>
              </th>
            </template>
            <th class="border border-gray-300 px-2 py-1">
              <p>จำนวน (ฉบับ)</p>
            </th>
            <th class="border border-gray-300 px-2 py-1">
              <p>เปอร์เซ็นต์</p>
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="ct in contractTypes" :key="ct.code">
            <td class="border border-gray-300 px-4 py-2 text-left">
              <p :class="ct.code === 'ALL' ? 'text-red-500' : ''">{{ ct.name }}</p>
            </td>
            <template v-for="q in quarters" :key="q">
              <td class="border border-gray-300 px-2 py-1">
                <p>{{ getCell(q, ct.code)?.contractCount ?? 0 }}</p>
              </td>
              <td class="border border-gray-300 px-2 py-1">
                <p>{{ formatPercent(getCell(q, ct.code)?.percentComplete ?? 0) }}</p>
              </td>
            </template>
            <td class="border border-gray-300 px-2 py-1">
              <p>{{ getTotalCount(ct.code) }}</p>
            </td>
            <td class="border border-gray-300 px-2 py-1">
              <p>{{ getTotalPercent(ct.code) }}</p>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </Dialog>
</template>

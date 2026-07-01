<script setup lang="ts">
import type { TaskItem } from '@/models/DA/da';
import { TitleHeader } from '@/components/cosmetic';
import { Card } from 'primevue';
import { ToTHDateFullMonthOnly } from '@/helpers/dateTime';

type Props = {
  title: string;
  dataList: TaskItem[];
  monthList: string[];
}

const props = defineProps<Props>();

const getTooltip = (data: TaskItem): string => {
  if (!data.startDate || !data.endDate) return data.title;
  return `${data.title}\n${ToTHDateFullMonthOnly(data.startDate)} ถึง ${ToTHDateFullMonthOnly(data.endDate)}`;
};

const calculateDate = (startDate: Date, endDate: Date) => {
  const msPerDay = 1000 * 60 * 60 * 24;
  const diffInMs = endDate.getTime() - startDate.getTime();
  const diffInDays = diffInMs / msPerDay;

  return diffInDays === 0 ? 1 : diffInDays;
};

const calPercen = (startDate: Date, endDate: Date) => {
  const diffDay = calculateDate(startDate, endDate);
  const percent = Math.max((diffDay * 100) / 30, 30);

  return `${percent.toFixed(0)}%`;
};

const getBarColor = (status?: string) => {
  if (status === 'completed') return 'bg-green-500';
  if (status === 'inProgress') return 'bg-blue-500';
  return 'bg-green-500';
};

const toThaiMonthShort = (date: Date): string => {
  return new Intl.DateTimeFormat('th', {
    month: 'short',
    year: '2-digit',
  }).format(date);
};

const getStartAtIndex = (data: TaskItem): number => {
  if (!data.startDate) return -1;

  const monthStr = toThaiMonthShort(data.startDate);

  const index = props.monthList.findIndex((m) => m.includes(monthStr) || monthStr.includes(m));

  return index;
};

const getDayOffset = (startDate: Date): string => {
  const day = startDate.getDate();
  const daysInMonth = new Date(startDate.getFullYear(), startDate.getMonth() + 1, 0).getDate();
  const offsetPercent = ((day - 1) / daysInMonth) * 100;

  return `${offsetPercent.toFixed(0)}%`;
};
</script>

<template>
  <Card class="mt-5">
    <template #content>
      <TitleHeader :label="props.title" />
      <div class="grid grid-cols-[320px_1fr] w-full border border-gray-200 overflow-hidden">
        <!-- Data rows -->
        <template v-for="(data, dataIndex) in dataList" :key="data.title">
          <div class="px-4 py-3 border-b border-r border-gray-200 flex items-center"
            :class="dataIndex % 2 === 0 ? 'bg-white' : 'bg-gray-50/50'">
            <p class="text-gray-800 truncate" :title="data.title">{{ data.title }}</p>
          </div>

          <div class="flex overflow-x-auto border-b border-gray-200"
            :class="dataIndex % 2 === 0 ? 'bg-white' : 'bg-gray-50/50'">
            <div v-for="(month, index) in monthList" :key="month"
              class="min-w-[120px] flex-1 py-3 px-1 relative border-r border-gray-200 last:border-r-0">
              <div v-if="data.startDate && data.endDate && index === getStartAtIndex(data)"
                class="absolute top-1/2 -translate-y-1/2 cursor-default shadow-sm flex items-center justify-center h-7 z-2"
                :class="getBarColor(data.status)"
                :style="{ width: calPercen(data.startDate, data.endDate), left: getDayOffset(data.startDate) }"
                v-tooltip.top="{ value: getTooltip(data), class: 'gantt-tooltip' }">
                <span class="text-xs text-white font-medium whitespace-nowrap px-2">
                  {{ calculateDate(data.startDate, data.endDate) }} วัน
                </span>
              </div>
              <p v-if="!data.startDate || !data.endDate || index !== getStartAtIndex(data)" class="h-7">&nbsp;</p>
            </div>
          </div>
        </template>

        <!-- Footer -->
        <div class="px-4 py-2.5 bg-gray-50 border-t border-r border-gray-200 font-semibold text-gray-700">
          ระยะเวลาดำเนินการ
        </div>
        <div class="flex bg-gray-50 border-t border-gray-200 overflow-x-auto">
          <div v-for="month in monthList" :key="month"
            class="min-w-[120px] flex-1 text-center py-2.5 font-semibold text-gray-700 border-r border-gray-200 last:border-r-0">
            {{ month }}
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>

<style>
.gantt-tooltip.p-tooltip .p-tooltip-text {
  background-color: white !important;
  color: #1f2937 !important;
  border: 1px solid #e5e7eb !important;
  box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1) !important;
  white-space: pre-line;
}

.gantt-tooltip.p-tooltip .p-tooltip-arrow {
  border-top-color: white !important;
}
</style>

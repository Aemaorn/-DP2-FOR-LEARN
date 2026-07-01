<script setup lang="ts">
import type { DashboardValue } from '@/models/DA/dashboardMd';
import { Card } from 'primevue';
import Chart from 'primevue/chart';
import { ref, onMounted, watch, computed } from 'vue';

type Props = {
  label?: string;
  data: DashboardValue[];
  width?: number;
  height?: number;
  colors?: string[];
  selectedLabel?: string;
  dimUnselected?: boolean;
  activeFilterLabel?: string;
}

const props = defineProps<Props>();
const emit = defineEmits<{
  (e: 'select', label: string | undefined): void;
  (e: 'clearFilter'): void;
}>();

const chartOptions = ref<any>(null);
const chartKey = ref(0);
const chartData = computed(() => setChartData());

// สีเดียวกับ DA004
const DEFAULT_COLORS = [
  'rgb(59,130,246)',   // blue
  'rgb(168,85,247)',   // purple
  'rgb(249,115,22)',   // orange
  'rgb(34,197,94)',    // green
  'rgb(239,68,68)',    // red
  'rgb(20,184,166)',   // teal
  'rgb(234,179,8)',    // yellow
  'rgb(99,102,241)',   // indigo
];

const total = computed(() =>
  props.data.reduce((sum, d) => sum + d.value, 0)
);

const formatBaht = (value: number) =>
  value.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

onMounted(() => {
  chartOptions.value = setChartOptions();
});

watch(
  () => [props.data, props.colors, props.selectedLabel],
  () => {
    chartOptions.value = setChartOptions();
    chartKey.value++;
  },
  { deep: true }
);

function setChartData() {
  const colors = props.colors?.length ? props.colors : DEFAULT_COLORS;
  const shouldDim = props.dimUnselected !== false;

  const bg = props.data.map((d, i) => {
    const base = colors[i % colors.length];
    if (shouldDim && props.selectedLabel !== undefined && d.label !== props.selectedLabel) {
      return base.replace('rgb(', 'rgba(').replace(')', ', 0.3)');
    }
    return base;
  });
  const bgHover = bg.map(c => c.replace('rgb(', 'rgba(').replace(')', ', 0.7)'));
  const isSelected = (d: { label: string }) =>
    props.selectedLabel !== undefined && d.label === props.selectedLabel;

  const offset = props.data.map(d => isSelected(d) ? 25 : 0);
  const borderWidth = props.data.map(d => isSelected(d) ? 16 : 2);
  const borderColor = props.data.map((d, i) => isSelected(d) ? colors[i % colors.length] : '#ffffff');

  return {
    labels: props.data.map(d => d.label),
    datasets: [{
      data: props.data.map(d => d.value),
      backgroundColor: bg,
      hoverBackgroundColor: bgHover,
      borderWidth,
      borderColor,
      hoverBorderWidth: 0,
      offset,
    }]
  };
}

function onChartClick(_event: any, elements: any[]) {
  if (!elements.length) return;
  const index = elements[0].index;
  const label = props.data[index]?.label;
  if (props.selectedLabel === label) {
    emit('select', undefined);
  } else {
    emit('select', label);
  }
}

function setChartOptions() {
  return {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '65%',
    onClick: onChartClick,
    plugins: {
      legend: { display: false },
      tooltip: {
        backgroundColor: '#1f2937',
        titleColor: '#f9fafb',
        bodyColor: '#d1d5db',
        padding: 10,
        cornerRadius: 8,
        callbacks: {
          label: (ctx: any) => {
            const value = ctx.dataset.data[ctx.dataIndex] ?? 0;
            const pct = total.value > 0 ? ((value / total.value) * 100).toFixed(1) : '0.0';
            return `  ${formatBaht(value)} บาท (${pct}%)`;
          }
        }
      }
    }
  };
}
</script>

<template>
  <Card class="h-full">
    <template #content>
      <div class="mb-4">
        <div class="flex items-center gap-2 flex-wrap">
          <p class="text-lg font-bold text-gray-800">{{ props.label }}</p>
          <span v-if="props.activeFilterLabel"
            class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-orange-100 text-orange-700 border border-orange-200">
            <i class="pi pi-filter text-[10px]" />
            {{ props.activeFilterLabel }}
            <button @click.stop="emit('clearFilter')" class="ml-0.5 hover:text-orange-900 leading-none">
              <i class="pi pi-times text-[10px]" />
            </button>
          </span>
        </div>
        <p class="text-sm text-gray-500 mt-0.5">รวมทั้งสิ้น
          <span class="font-semibold text-gray-700">{{ formatBaht(total) }}</span> บาท
        </p>
      </div>
      <div class="flex flex-wrap gap-4 items-center">
        <!-- Chart -->
        <div class="flex-shrink-0 cursor-pointer" :style="{ width: `${props.width ?? 220}px`, height: `${props.height ?? 260}px` }">
          <Chart :key="chartKey" type="doughnut" :data="chartData" :options="chartOptions" class="w-full h-full" />
        </div>
        <!-- Legend -->
        <div class="flex-1 min-w-[200px] space-y-1 self-center">
          <div v-for="(item, i) in props.data" :key="i"
            class="flex flex-wrap items-center gap-x-2 gap-y-0.5 px-2 py-1.5 rounded-lg transition-all cursor-pointer select-none"
            :class="props.selectedLabel === item.label
              ? 'bg-blue-50 ring-1 ring-blue-300'
              : props.selectedLabel === undefined ? 'hover:bg-gray-50' : 'opacity-40 hover:opacity-70'"
            @click="emit('select', props.selectedLabel === item.label ? undefined : item.label)">
            <!-- สี + label + check -->
            <div class="flex items-center gap-2 min-w-0 flex-1">
              <span class="w-3 h-3 rounded-sm flex-shrink-0"
                :style="{ background: (props.colors ?? DEFAULT_COLORS)[i % (props.colors ?? DEFAULT_COLORS).length] }" />
              <span class="text-sm truncate" :class="props.selectedLabel === item.label ? 'text-blue-700 font-semibold' : 'text-gray-700'">{{ item.label }}</span>
              <i v-if="props.selectedLabel === item.label" class="pi pi-check-circle text-blue-500 text-sm flex-shrink-0" />
            </div>
            <!-- value + % (wrap ลงบรรทัดใหม่เมื่อพื้นที่แคบ) -->
            <div class="flex items-center gap-1 flex-shrink-0">
              <span class="text-sm font-semibold" :class="props.selectedLabel === item.label ? 'text-blue-700' : 'text-gray-800'">{{ formatBaht(item.value) }} บาท</span>
              <span class="text-xs text-gray-400">({{ total > 0 ? ((item.value / total) * 100).toFixed(1) : '0.0' }}%)</span>
            </div>
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js'
import { ProcurementStatus, type ProcurementItem } from '@/types/procurement'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend, Filler)

const props = defineProps<{
  items: ProcurementItem[]
  dateRange?: [Date, Date] | null
}>()

const monthLabels = ['ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.', 'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.']

type GroupMode = 'month' | 'week' | 'day'

// normalize to YYYY-MM-DD number for safe comparison
function toDateNum(d: Date): number {
  return d.getFullYear() * 10000 + (d.getMonth() + 1) * 100 + d.getDate()
}

function parseDateNum(s: string): number {
  const [y, m, d] = s.split('-').map(Number)
  return y * 10000 + m * 100 + d
}

const groupMode = computed<GroupMode>(() => {
  if (!props.dateRange) return 'month'
  const diffMs = props.dateRange[1].getTime() - props.dateRange[0].getTime()
  const diffDays = diffMs / (1000 * 60 * 60 * 24)
  if (diffDays <= 45) return 'day'
  if (diffDays <= 120) return 'week'
  return 'month'
})

interface Period {
  label: string
  match: (dateStr: string) => boolean
}

const periods = computed<Period[]>(() => {
  if (groupMode.value === 'month') {
    let startY: number, startM: number, endY: number, endM: number
    if (props.dateRange) {
      startY = props.dateRange[0].getFullYear()
      startM = props.dateRange[0].getMonth()
      endY = props.dateRange[1].getFullYear()
      endM = props.dateRange[1].getMonth()
    } else {
      const now = new Date()
      endY = now.getFullYear()
      endM = now.getMonth()
      const start = new Date(endY, endM - 11, 1)
      startY = start.getFullYear()
      startM = start.getMonth()
    }
    const result: Period[] = []
    let y = startY, m = startM
    while (y < endY || (y === endY && m <= endM)) {
      const cy = y, cm = m
      const minN = cy * 10000 + (cm + 1) * 100 + 1
      const maxN = cm === 11
        ? (cy + 1) * 10000 + 1 * 100 + 0
        : cy * 10000 + (cm + 2) * 100 + 0
      result.push({
        label: `${monthLabels[cm]} ${String(cy).slice(2)}`,
        match: (s) => { const n = parseDateNum(s); return n >= minN && n < maxN },
      })
      m++
      if (m > 11) { m = 0; y++ }
    }
    return result
  }

  if (groupMode.value === 'week') {
    const start = new Date(props.dateRange![0])
    const end = new Date(props.dateRange![1])
    const result: Period[] = []
    const cursor = new Date(start)
    cursor.setDate(cursor.getDate() - ((cursor.getDay() + 6) % 7))

    while (toDateNum(cursor) <= toDateNum(end)) {
      const wsN = toDateNum(cursor)
      const weDate = new Date(cursor)
      weDate.setDate(weDate.getDate() + 6)
      const weN = toDateNum(weDate)

      const d1 = cursor.getDate()
      const m1 = monthLabels[cursor.getMonth()]
      result.push({
        label: `${d1} ${m1}`,
        match: (s) => { const n = parseDateNum(s); return n >= wsN && n <= weN },
      })

      cursor.setDate(cursor.getDate() + 7)
    }
    return result
  }

  // group by day
  const start = new Date(props.dateRange![0])
  const end = new Date(props.dateRange![1])
  const result: Period[] = []
  const cursor = new Date(start)
  while (toDateNum(cursor) <= toDateNum(end)) {
    const dayN = toDateNum(cursor)
    result.push({
      label: `${cursor.getDate()}/${cursor.getMonth() + 1}`,
      match: (s) => parseDateNum(s) === dayN,
    })
    cursor.setDate(cursor.getDate() + 1)
  }
  return result
})

const chartData = computed(() => {
  const len = periods.value.length
  const total = new Array(len).fill(0)
  const onTrack = new Array(len).fill(0)
  const behind = new Array(len).fill(0)

  for (const item of props.items) {
    if (!item.approvalDate) continue
    const idx = periods.value.findIndex(p => p.match(item.approvalDate))
    if (idx === -1) continue
    total[idx]++
    if (item.status === ProcurementStatus.OnTrack) onTrack[idx]++
    if (item.status === ProcurementStatus.BehindSchedule) behind[idx]++
  }

  return {
    labels: periods.value.map(p => p.label),
    datasets: [
      {
        label: 'สร้างทั้งหมด',
        data: total,
        borderColor: '#a1a1aa',
        backgroundColor: 'rgba(161, 161, 170, 0.05)',
        tension: 0.4,
        fill: true,
        pointRadius: 3,
        pointBackgroundColor: '#a1a1aa',
        borderWidth: 2,
      },
      {
        label: 'ตามแผน',
        data: onTrack,
        borderColor: '#6ee7b7',
        backgroundColor: 'transparent',
        tension: 0.4,
        fill: false,
        pointRadius: 3,
        pointBackgroundColor: '#6ee7b7',
        borderWidth: 2,
      },
      {
        label: 'ช้ากว่าแผน',
        data: behind,
        borderColor: '#fca5a5',
        backgroundColor: 'transparent',
        tension: 0.4,
        fill: false,
        pointRadius: 3,
        pointBackgroundColor: '#fca5a5',
        borderWidth: 2,
      },
    ],
  }
})

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'top' as const,
      align: 'end' as const,
      labels: {
        usePointStyle: true,
        pointStyle: 'circle',
        padding: 20,
        font: { size: 11, family: 'system-ui' },
        color: '#78716c',
      },
    },
    title: {
      display: false,
    },
  },
  scales: {
    x: {
      grid: { display: false },
      ticks: { color: '#a8a29e', font: { size: 11 } },
    },
    y: {
      beginAtZero: true,
      border: { display: false },
      grid: { color: '#f5f5f4' },
      ticks: { color: '#a8a29e', font: { size: 11 } },
    },
  },
}
</script>

<template>
  <div class="bg-white border border-stone-200 rounded-xl p-5">
    <p class="text-xs text-stone-400 font-medium mb-4">Performance Trend</p>
    <div class="h-[300px]">
      <Line :data="chartData" :options="chartOptions" />
    </div>
  </div>
</template>

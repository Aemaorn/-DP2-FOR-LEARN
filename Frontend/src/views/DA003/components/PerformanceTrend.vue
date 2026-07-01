<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import Chart from 'primevue/chart'
import { ProcurementStatus, type ProcurementItem } from '@/models/DA/da003'

const props = defineProps<{
  items: ProcurementItem[]
  dateRange?: [Date, Date] | null
}>()

const chartKey = ref(0)
const monthLabels = ['ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.', 'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.']

type GroupMode = 'month' | 'week' | 'day'

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
      startY = props.dateRange[0].getFullYear(); startM = props.dateRange[0].getMonth()
      endY = props.dateRange[1].getFullYear(); endM = props.dateRange[1].getMonth()
    } else {
      const now = new Date()
      const dates = props.items.map(i => i.approvalDate).filter(Boolean) as string[]
      if (dates.length > 0) {
        const oldest = dates.reduce((a, b) => a < b ? a : b)
        const [oy, om] = oldest.split('-').map(Number)
        startY = oy; startM = om - 1
        const newest = dates.reduce((a, b) => a > b ? a : b)
        const [ny, nm] = newest.split('-').map(Number)
        // ใช้เดือนล่าสุดจากข้อมูล หรือเดือนปัจจุบัน แล้วแต่ว่าอันไหนมากกว่า
        const dataEndNum = ny * 100 + nm
        const nowEndNum = now.getFullYear() * 100 + (now.getMonth() + 1)
        if (dataEndNum > nowEndNum) { endY = ny; endM = nm - 1 }
        else { endY = now.getFullYear(); endM = now.getMonth() }
      } else {
        endY = now.getFullYear(); endM = now.getMonth()
        const start = new Date(endY, endM - 11, 1); startY = start.getFullYear(); startM = start.getMonth()
      }
    }
    const result: Period[] = []
    let y = startY, m = startM
    while (y < endY || (y === endY && m <= endM)) {
      const cy = y, cm = m
      const minN = cy * 10000 + (cm + 1) * 100 + 1
      const maxN = cm === 11 ? (cy + 1) * 10000 + 100 : cy * 10000 + (cm + 2) * 100
      result.push({ label: `${monthLabels[cm]} ${cy + 543}`, match: (s) => { const n = parseDateNum(s); return n >= minN && n < maxN } })
      m++; if (m > 11) { m = 0; y++ }
    }
    return result
  }

  if (groupMode.value === 'week') {
    const end = new Date(props.dateRange![1])
    const result: Period[] = []
    const cursor = new Date(props.dateRange![0])
    cursor.setDate(cursor.getDate() - ((cursor.getDay() + 6) % 7))
    while (toDateNum(cursor) <= toDateNum(end)) {
      const wsN = toDateNum(cursor)
      const weDate = new Date(cursor); weDate.setDate(weDate.getDate() + 6)
      const weN = toDateNum(weDate)
      result.push({ label: `${cursor.getDate()} ${monthLabels[cursor.getMonth()]}`, match: (s) => { const n = parseDateNum(s); return n >= wsN && n <= weN } })
      cursor.setDate(cursor.getDate() + 7)
    }
    return result
  }

  const end = new Date(props.dateRange![1])
  const result: Period[] = []
  const cursor = new Date(props.dateRange![0])
  while (toDateNum(cursor) <= toDateNum(end)) {
    const dayN = toDateNum(cursor)
    result.push({ label: `${cursor.getDate()}/${cursor.getMonth() + 1}`, match: (s) => parseDateNum(s) === dayN })
    cursor.setDate(cursor.getDate() + 1)
  }
  return result
})

function calcWorkingDaysInclusive(from: string | null, to: string | null): number | null {
  if (!from || !to) return null
  const start = new Date(from + 'T00:00:00')
  const end = new Date(to + 'T00:00:00')
  let count = 0
  const cur = new Date(start)
  while (cur <= end) {
    const day = cur.getDay()
    if (day !== 0 && day !== 6) count++
    cur.setDate(cur.getDate() + 1)
  }
  return count || null
}

function getLocalStatus(item: ProcurementItem): ProcurementStatus | null {
  const days = calcWorkingDaysInclusive(item.approvalDate, item.contractSignDate)
  if (days === null) return null
  if (days <= 10) return ProcurementStatus.OnPlan
  if (days <= 15) return ProcurementStatus.Risk
  return ProcurementStatus.Delay
}

const chartData = computed(() => {
  const len = periods.value.length
  const total   = new Array(len).fill(0)
  const onPlan  = new Array(len).fill(0)
  const risk    = new Array(len).fill(0)
  const delay   = new Array(len).fill(0)

  for (const item of props.items) {
    if (!item.approvalDate) continue
    const idx = periods.value.findIndex(p => p.match(item.approvalDate!))
    if (idx === -1) continue
    total[idx]++
    const st = getLocalStatus(item)
    if (st === ProcurementStatus.OnPlan) onPlan[idx]++
    else if (st === ProcurementStatus.Risk) risk[idx]++
    else if (st === ProcurementStatus.Delay) delay[idx]++
  }

  return {
    labels: periods.value.map(p => p.label),
    datasets: [
      { label: 'เป็นไปตามแผน',   data: onPlan, borderColor: '#10b981', backgroundColor: 'rgba(16,185,129,0.08)', tension: 0.4, fill: true,  pointRadius: 4, pointHoverRadius: 6, pointBackgroundColor: '#10b981', pointBorderColor: '#fff', pointBorderWidth: 2, borderWidth: 2.5 },
      { label: 'มีแนวโน้มล่าช้า', data: risk,   borderColor: '#f59e0b', backgroundColor: 'rgba(245,158,11,0.08)', tension: 0.4, fill: true,  pointRadius: 4, pointHoverRadius: 6, pointBackgroundColor: '#f59e0b', pointBorderColor: '#fff', pointBorderWidth: 2, borderWidth: 2.5 },
      { label: 'ช้ากว่าแผน',      data: delay,  borderColor: '#ef4444', backgroundColor: 'rgba(239,68,68,0.08)',  tension: 0.4, fill: true,  pointRadius: 4, pointHoverRadius: 6, pointBackgroundColor: '#ef4444', pointBorderColor: '#fff', pointBorderWidth: 2, borderWidth: 2.5 },
    ],
  }
})

const dataLabelPlugin = {
  id: 'dataLabelPlugin',
  afterDatasetsDraw(chart: any) {
    const ctx = chart.ctx
    chart.data.datasets.forEach((dataset: any, i: number) => {
      const meta = chart.getDatasetMeta(i)
      if (meta.hidden) return
      meta.data.forEach((point: any, j: number) => {
        const value = dataset.data[j]
        if (!value) return
        ctx.save()
        ctx.font = 'bold 11px system-ui'
        ctx.fillStyle = dataset.borderColor
        ctx.textAlign = 'center'
        ctx.textBaseline = 'bottom'
        ctx.fillText(value, point.x, point.y - 8)
        ctx.restore()
      })
    })
  },
}

const summaryStats = computed(() => {
  let onPlanTotal = 0, riskTotal = 0, delayTotal = 0
  for (const item of props.items) {
    if (!item.approvalDate) continue
    const st = getLocalStatus(item)
    if (st === ProcurementStatus.OnPlan) onPlanTotal++
    else if (st === ProcurementStatus.Risk) riskTotal++
    else if (st === ProcurementStatus.Delay) delayTotal++
  }
  return { onPlanTotal, riskTotal, delayTotal, total: onPlanTotal + riskTotal + delayTotal }
})

const groupModeLabel = computed(() => {
  if (groupMode.value === 'day') return 'แสดงแบบรายวัน'
  if (groupMode.value === 'week') return 'แสดงแบบรายสัปดาห์'
  return 'แสดงแบบรายเดือน'
})

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index', intersect: false },
  plugins: {
    legend: { display: false },
    title: { display: false },
    tooltip: {
      backgroundColor: 'rgba(28,25,23,0.88)',
      titleColor: '#e7e5e4',
      bodyColor: '#d6d3d1',
      padding: 12,
      cornerRadius: 8,
      titleFont: { size: 12, weight: 'bold' },
      bodyFont: { size: 12 },
      displayColors: true,
      boxWidth: 10,
      boxHeight: 10,
    },
  },
  scales: {
    x: {
      grid: { display: false },
      border: { display: false },
      ticks: { color: '#78716c', font: { size: 11 } },
    },
    y: {
      beginAtZero: true,
      min: 0,
      suggestedMax: 10,
      border: { display: false },
      grid: { color: '#e7e5e4', lineWidth: 1 },
      title: { display: true, text: 'จำนวนโครงการ', color: '#78716c', font: { size: 11 } },
      ticks: { color: '#78716c', font: { size: 11 }, stepSize: 1, callback: (v: number) => Number.isInteger(v) ? v : null },
    },
  },
}

watch(() => props.items, () => { chartKey.value++ }, { deep: true })
</script>

<template>
  <div class="bg-white shadow-sm p-5">
    <!-- Header -->
    <div class="flex flex-wrap items-start justify-between gap-3 mb-4">
      <div>
        <p class="text-sm font-semibold text-gray-700">แนวโน้มการดำเนินการจัดซื้อจัดจ้าง</p>
        <p class="text-xs text-gray-400 mt-0.5">{{ groupModeLabel }}</p>
      </div>
      <div class="flex flex-wrap gap-2">
        <div class="flex items-center gap-1.5 px-3 py-1.5 bg-emerald-50 border border-emerald-200 rounded-lg">
          <span class="w-2 h-2 rounded-full bg-emerald-500 shrink-0" />
          <span class="text-xs text-emerald-700 font-medium">เป็นไปตามแผน</span>
          <span class="text-sm font-bold text-emerald-700">{{ summaryStats.onPlanTotal }}</span>
          <span class="text-xs text-emerald-600">โครงการ</span>
        </div>
        <div class="flex items-center gap-1.5 px-3 py-1.5 bg-amber-50 border border-amber-200 rounded-lg">
          <span class="w-2 h-2 rounded-full bg-amber-400 shrink-0" />
          <span class="text-xs text-amber-700 font-medium">มีแนวโน้มล่าช้า</span>
          <span class="text-sm font-bold text-amber-700">{{ summaryStats.riskTotal }}</span>
          <span class="text-xs text-amber-600">โครงการ</span>
        </div>
        <div class="flex items-center gap-1.5 px-3 py-1.5 bg-red-50 border border-red-200 rounded-lg">
          <span class="w-2 h-2 rounded-full bg-red-500 shrink-0" />
          <span class="text-xs text-red-700 font-medium">ช้ากว่าแผน</span>
          <span class="text-sm font-bold text-red-700">{{ summaryStats.delayTotal }}</span>
          <span class="text-xs text-red-600">โครงการ</span>
        </div>
      </div>
    </div>
    <div style="height: 300px;">
      <Chart :key="chartKey" type="line" :data="chartData" :options="chartOptions" :plugins="[dataLabelPlugin]" class="w-full h-full" />
    </div>
  </div>
</template>

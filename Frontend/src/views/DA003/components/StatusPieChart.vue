<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import Chart from 'primevue/chart'
import { ProcurementStatus, type ProcurementItem } from '@/models/DA/da003'

const props = defineProps<{
  items: ProcurementItem[]
}>()

const chartKey = ref(0)

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

const STATUS_COLORS      = ['#059669', '#d97706', '#e11d48']
const STATUS_COLORS_SOFT = ['#6ee7b7', '#fde68a', '#fda4af']

const counts = computed(() => ({
  onPlan: props.items.filter(i => getLocalStatus(i) === ProcurementStatus.OnPlan).length,
  risk:   props.items.filter(i => getLocalStatus(i) === ProcurementStatus.Risk).length,
  delay:  props.items.filter(i => getLocalStatus(i) === ProcurementStatus.Delay).length,
}))

const chartData = computed(() => ({
  labels: ['เป็นไปตามแผน', 'มีแนวโน้มล่าช้า', 'ช้ากว่าแผน'],
  datasets: [{
    data: [counts.value.onPlan, counts.value.risk, counts.value.delay],
    backgroundColor: STATUS_COLORS_SOFT,
    borderColor: '#ffffff',
    borderWidth: 2,
    hoverOffset: 8,
  }],
}))

const doughnutOuterLabelPlugin = {
  id: 'statusOuterLabel',
  afterDraw(chart: any) {
    const { ctx, chartArea: { width, height, left, top } } = chart
    const cx = left + width / 2
    const cy = top + height / 2
    const dataset = chart.data.datasets[0]
    const meta = chart.getDatasetMeta(0)
    const total = (dataset.data as number[]).reduce((s: number, v: number) => s + v, 0)
    if (!total) return

    meta.data.forEach((arc: any, i: number) => {
      const count = dataset.data[i] as number
      if (!count) return

      const angle = (arc.startAngle + arc.endAngle) / 2
      const outerR = arc.outerRadius
      const lineStart = outerR + 6
      const lineEnd   = outerR + 20
      const labelR    = outerR + 28

      const cos = Math.cos(angle)
      const sin = Math.sin(angle)
      const x1 = cx + cos * lineStart
      const y1 = cy + sin * lineStart
      const x2 = cx + cos * lineEnd
      const y2 = cy + sin * lineEnd
      const lx = cx + cos * labelR
      const ly = cy + sin * labelR

      const color = STATUS_COLORS[i % STATUS_COLORS.length]

      // Leader line
      ctx.save()
      ctx.beginPath()
      ctx.moveTo(x1, y1)
      ctx.lineTo(x2, y2)
      ctx.strokeStyle = color
      ctx.lineWidth = 1.5
      ctx.stroke()
      ctx.restore()

      const isRight = cos >= 0
      const textAlign = isRight ? 'left' : 'right'
      const textX = lx + (isRight ? 4 : -4)

      const label = chart.data.labels[i] as string
      const lineH = 18
      const startY = ly - lineH

      // Label name
      ctx.save()
      ctx.font = 'bold 15px ThaiSansNeue, system-ui'
      ctx.fillStyle = color
      ctx.textAlign = textAlign as CanvasTextAlign
      ctx.textBaseline = 'top'
      ctx.fillText(label, textX, startY)
      ctx.restore()

      // Count
      ctx.save()
      ctx.font = 'bold 13px ThaiSansNeue, system-ui'
      ctx.fillStyle = '#1f2937'
      ctx.textAlign = textAlign as CanvasTextAlign
      ctx.textBaseline = 'top'
      ctx.fillText(`${count} โครงการ`, textX, startY + lineH)
      ctx.restore()
    })
  },
}

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: '55%',
  layout: { padding: 70 },
  plugins: {
    legend: { display: false },
    tooltip: {
      backgroundColor: 'rgba(28,25,23,0.88)',
      titleColor: '#e7e5e4',
      bodyColor: '#d6d3d1',
      padding: 12,
      cornerRadius: 8,
      callbacks: {
        label: (ctx: any) => ` ${ctx.parsed} โครงการ`,
      },
    },
  },
}))

watch(() => props.items, () => { chartKey.value++ }, { deep: true })
</script>

<template>
  <div class="bg-white shadow-sm p-5">
    <p class="text-xs text-stone-400 font-medium mb-3">สถานะรวมทั้งหมด</p>
    <div style="height: 400px;">
      <Chart :key="chartKey" type="doughnut" :data="chartData" :options="chartOptions" :plugins="[doughnutOuterLabelPlugin]" class="w-full h-full" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS,
  ArcElement,
  Tooltip,
  Legend,
} from 'chart.js'
import { ProcurementStatus, type ProcurementItem } from '@/types/procurement'

ChartJS.register(ArcElement, Tooltip, Legend)

const props = defineProps<{
  items: ProcurementItem[]
}>()

const allStatusData = computed(() => {
  const onTrack = props.items.filter(i => i.status === ProcurementStatus.OnTrack).length
  const trending = props.items.filter(i => i.status === ProcurementStatus.TrendingLate).length
  const behind = props.items.filter(i => i.status === ProcurementStatus.BehindSchedule).length
  return {
    labels: ['ตามแผน', 'มีแนวโน้มล่าช้า', 'ช้ากว่าแผน'],
    datasets: [{
      data: [onTrack, trending, behind],
      backgroundColor: ['#6ee7b7', '#fcd34d', '#fca5a5'],
      borderWidth: 0,
    }],
  }
})

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '65%',
  plugins: {
    legend: {
      position: 'bottom' as const,
      labels: {
        usePointStyle: true,
        padding: 16,
        font: { size: 11, family: 'system-ui' },
        color: '#78716c',
      },
    },
  },
}
</script>

<template>
  <div class="bg-white border border-stone-200 rounded-xl p-5">
    <p class="text-xs text-stone-400 font-medium mb-4">สถานะรวมทั้งหมด</p>
    <div class="h-[220px]">
      <Doughnut :data="allStatusData" :options="chartOptions" />
    </div>
  </div>
</template>

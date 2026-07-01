<script setup lang="ts">
import { computed } from 'vue'
import { ProcurementStatus, type ProcurementItem } from '@/models/DA/da003'
import { useCountUp } from '../composables/useCountUp'

const props = defineProps<{
  items: ProcurementItem[]
}>()

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

const totalItems = computed(() => props.items.length)
const onPlanItems = computed(() => props.items.filter(i => getLocalStatus(i) === ProcurementStatus.OnPlan).length)
const riskItems = computed(() => props.items.filter(i => getLocalStatus(i) === ProcurementStatus.Risk).length)
const overdueItems = computed(() => props.items.filter(i => getLocalStatus(i) === ProcurementStatus.Delay).length)
const inProgressItems = computed(() => totalItems.value - (onPlanItems.value + riskItems.value + overdueItems.value))
const successCount = computed(() => onPlanItems.value + riskItems.value + overdueItems.value)
const feedbackPct = computed(() => {
  if (totalItems.value === 0) return 0
  return Math.round((successCount.value / totalItems.value) * 100)
})

const animTotal = useCountUp(totalItems)
const animInProgress = useCountUp(inProgressItems)
const animOnPlan = useCountUp(onPlanItems)
const animRisk = useCountUp(riskItems)
const animOverdue = useCountUp(overdueItems)
const animPct = useCountUp(feedbackPct)

const cards = computed(() => [
  { title: 'ทั้งหมด', value: animTotal.value, icon: 'pi pi-list', color: 'text-stone-700', dot: 'bg-stone-400', bg: 'bg-stone-50' },
  { title: 'กำลังดำเนินการ', value: animInProgress.value, icon: 'pi pi-spinner', color: 'text-blue-600', dot: 'bg-blue-400', bg: 'bg-blue-50' },
  { title: 'เป็นไปตามแผน', value: animOnPlan.value, icon: 'pi pi-check', color: 'text-emerald-600', dot: 'bg-emerald-400', bg: 'bg-emerald-50' },
  { title: 'มีแนวโน้มล่าช้า', value: animRisk.value, icon: 'pi pi-clock', color: 'text-amber-600', dot: 'bg-amber-400', bg: 'bg-amber-50' },
  { title: 'ช้ากว่าแผน', value: animOverdue.value, icon: 'pi pi-exclamation-circle', color: 'text-rose-600', dot: 'bg-rose-400', bg: 'bg-rose-50' },
  { title: 'อัตราสำเร็จ', value: `${animPct.value}%`, icon: 'pi pi-percentage', color: 'text-violet-600', dot: 'bg-violet-400', bg: 'bg-violet-50' },
])
</script>

<template>
  <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3 mb-5">
    <div
      v-for="card in cards"
      :key="card.title"
      :class="[card.bg, 'p-2 shadow-sm hover:shadow-md transition-all cursor-default']"
    >
      <div class="flex items-center gap-2 mb-3">
        <span :class="[card.dot, 'w-2 h-2 rounded-full']"></span>
        <span class="text-xs text-stone-400 font-medium">{{ card.title }}</span>
      </div>
      <div class="flex items-end justify-between">
        <p :class="[card.color, 'text-2xl font-semibold tracking-tight']">{{ card.value }}</p>
        <i :class="[card.icon, 'text-stone-300 text-lg']"></i>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { ProcurementStatus, type ProcurementItem } from '@/models/DA/da003'
import { useCountUp } from '../composables/useCountUp'

const props = defineProps<{
  items: ProcurementItem[]
  activeStatus?: ProcurementStatus | null
}>()

const emit = defineEmits<{
  filter: [status: ProcurementStatus | null]
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

const onPlanCount = computed(() => props.items.filter(i => getLocalStatus(i) === ProcurementStatus.OnPlan).length)
const riskCount   = computed(() => props.items.filter(i => getLocalStatus(i) === ProcurementStatus.Risk).length)
const delayCount  = computed(() => props.items.filter(i => getLocalStatus(i) === ProcurementStatus.Delay).length)

const animOnPlan = useCountUp(onPlanCount)
const animRisk   = useCountUp(riskCount)
const animDelay  = useCountUp(delayCount)

const cards = [
  { label: 'เป็นไปตามแผน', sub: 'ภายใน 10 วันทำการ',   count: animOnPlan, dot: 'bg-emerald-400', bg: 'bg-emerald-50', color: 'text-emerald-700', icon: 'pi pi-check-circle',       status: ProcurementStatus.OnPlan, ring: 'ring-emerald-400' },
  { label: 'มีแนวโน้มล่าช้า',  sub: '11-15 วันทำการ',       count: animRisk,   dot: 'bg-amber-400',   bg: 'bg-amber-50',   color: 'text-amber-700',   icon: 'pi pi-clock',              status: ProcurementStatus.Risk,   ring: 'ring-amber-400'   },
  { label: 'ช้ากว่าแผน',  sub: 'มากกว่า 15 วันทำการ', count: animDelay,  dot: 'bg-rose-400',    bg: 'bg-rose-50',    color: 'text-rose-700',    icon: 'pi pi-exclamation-circle', status: ProcurementStatus.Delay,  ring: 'ring-rose-400'    },
]

function toggle(status: ProcurementStatus) {
  emit('filter', props.activeStatus === status ? null : status)
}
</script>

<template>
  <div class="grid grid-cols-1 md:grid-cols-3 gap-3 mb-5">
    <div
      v-for="card in cards"
      :key="card.label"
      :class="[
        'p-2 cursor-pointer transition-all shadow-sm hover:shadow-md',
        props.activeStatus === card.status ? '' : card.bg,
        props.activeStatus === card.status ? `ring-2 ${card.ring} border-transparent` : 'border-stone-200'
      ]"
      @click="toggle(card.status)"
    >
      <div class="flex items-center gap-2 mb-3">
        <span :class="[card.dot, 'w-2 h-2 rounded-full']"></span>
        <span class="text-xs text-stone-400 font-medium">{{ card.label }}</span>
      </div>
      <div class="flex items-end justify-between">
        <div>
          <p :class="[card.color, 'text-2xl font-semibold tracking-tight']">{{ card.count.value }}</p>
          <p class="text-[11px] text-stone-300 mt-1">{{ card.sub }}</p>
        </div>
        <i :class="[card.icon, card.color, 'text-xl opacity-30']"></i>
      </div>
    </div>
  </div>
</template>

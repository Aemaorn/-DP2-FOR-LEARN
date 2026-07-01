<script setup lang="ts">
import { computed } from 'vue'
import { ProcurementStatus, type ProcurementItem } from '@/types/procurement'
import { useCountUp } from '@/composables/useCountUp'

const props = defineProps<{
  items: ProcurementItem[]
  activeStatus?: ProcurementStatus | null
}>()

const emit = defineEmits<{
  filter: [status: ProcurementStatus | null]
}>()

const onTrackCount = computed(() => props.items.filter(i => i.status === ProcurementStatus.OnTrack).length)
const trendingLateCount = computed(() => props.items.filter(i => i.status === ProcurementStatus.TrendingLate).length)
const behindCount = computed(() => props.items.filter(i => i.status === ProcurementStatus.BehindSchedule).length)

const animOnTrack = useCountUp(onTrackCount)
const animTrending = useCountUp(trendingLateCount)
const animBehind = useCountUp(behindCount)

const cards = [
  { label: 'ตามแผน', sub: 'ภายใน 10 วันทำการ', count: animOnTrack, dot: 'bg-emerald-400', color: 'text-emerald-700', status: ProcurementStatus.OnTrack, ring: 'ring-emerald-400' },
  { label: 'มีแนวโน้มล่าช้า', sub: '11-15 วันทำการ', count: animTrending, dot: 'bg-amber-400', color: 'text-amber-700', status: ProcurementStatus.TrendingLate, ring: 'ring-amber-400' },
  { label: 'ช้ากว่าแผน', sub: 'มากกว่า 15 วันทำการ', count: animBehind, dot: 'bg-rose-400', color: 'text-rose-700', status: ProcurementStatus.BehindSchedule, ring: 'ring-rose-400' },
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
        'bg-white border rounded-xl p-4 cursor-pointer transition-all hover:shadow-sm',
        props.activeStatus === card.status ? `ring-2 ${card.ring} border-transparent` : 'border-stone-200'
      ]"
      @click="toggle(card.status)"
    >
      <div class="flex items-center gap-2 mb-2">
        <span :class="[card.dot, 'w-2 h-2 rounded-full']"></span>
        <span class="text-xs text-stone-400 font-medium">{{ card.label }}</span>
      </div>
      <p :class="[card.color, 'text-2xl font-semibold']">{{ card.count.value }}</p>
      <p class="text-[11px] text-stone-300 mt-1">{{ card.sub }}</p>
    </div>
  </div>
</template>

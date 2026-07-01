<script setup lang="ts">
import { computed } from 'vue'
import { ProcurementStatus, type ProcurementItem } from '@/types/procurement'
import { useCountUp } from '@/composables/useCountUp'

const props = defineProps<{
  items: ProcurementItem[]
}>()

const totalItems = computed(() => props.items.length)
const inProgressItems = computed(() => props.items.filter(i => !i.contractSignDate && i.poDate).length)
const completedItems = computed(() => props.items.filter(i => i.contractSignDate !== null).length)
const inDueItems = computed(() => props.items.filter(i => i.status === ProcurementStatus.OnTrack).length)
const overdueItems = computed(() => props.items.filter(i => i.status === ProcurementStatus.BehindSchedule).length)
const feedbackPct = computed(() => {
  if (totalItems.value === 0) return 0
  return Math.round((completedItems.value / totalItems.value) * 1000) / 10
})

const animTotal = useCountUp(totalItems)
const animInProgress = useCountUp(inProgressItems)
const animCompleted = useCountUp(completedItems)
const animInDue = useCountUp(inDueItems)
const animOverdue = useCountUp(overdueItems)
const animPct = useCountUp(computed(() => Math.round(feedbackPct.value)))

const cards = computed(() => [
  { title: 'ทั้งหมด', value: animTotal.value, icon: 'pi pi-list', color: 'text-stone-700', dot: 'bg-stone-400' },
  { title: 'กำลังดำเนินการ', value: animInProgress.value, icon: 'pi pi-spinner', color: 'text-amber-600', dot: 'bg-amber-400' },
  { title: 'เสร็จสมบูรณ์', value: animCompleted.value, icon: 'pi pi-check', color: 'text-emerald-600', dot: 'bg-emerald-400' },
  { title: 'ตามแผน', value: animInDue.value, icon: 'pi pi-clock', color: 'text-sky-600', dot: 'bg-sky-400' },
  { title: 'ช้ากว่าแผน', value: animOverdue.value, icon: 'pi pi-exclamation-circle', color: 'text-rose-600', dot: 'bg-rose-400' },
  { title: 'อัตราสำเร็จ', value: `${animPct.value}%`, icon: 'pi pi-percentage', color: 'text-violet-600', dot: 'bg-violet-400' },
])
</script>

<template>
  <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3 mb-5">
    <div
      v-for="card in cards"
      :key="card.title"
      class="bg-white border border-stone-200 rounded-xl p-4"
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

<script setup lang="ts">
import { ref, computed } from 'vue'
import FilterBar, { type FilterCriteria } from '@/components/FilterBar.vue'
import OverviewCards from '@/components/OverviewCards.vue'
import PerformanceTrend from '@/components/PerformanceTrend.vue'
import StatusPieCharts from '@/components/StatusPieCharts.vue'
import { useProcurementStore } from '@/stores/procurement'
import { ProcurementStatus, type ProcurementItem } from '@/types/procurement'

const store = useProcurementStore()
const allItems = computed(() => [...store.bankItems, ...store.otherItems])

const filters = ref<FilterCriteria | null>(null)

const statusMap: Record<string, ProcurementStatus> = {
  'ตามแผน': ProcurementStatus.OnTrack,
  'มีแนวโน้มล่าช้า': ProcurementStatus.TrendingLate,
  'ช้ากว่าแผน': ProcurementStatus.BehindSchedule,
}

function getDateField(item: ProcurementItem, periodType: string): string | null {
  switch (periodType) {
    case 'ออกใบสั่งซื้อเมื่อ': return item.poDate
    case 'ลงนามสัญญาเมื่อ': return item.contractSignDate
    default: return item.approvalDate
  }
}

function toDateNum(d: Date): number {
  return d.getFullYear() * 10000 + (d.getMonth() + 1) * 100 + d.getDate()
}

function parseDateNum(s: string): number {
  const [y, m, d] = s.split('-').map(Number)
  return y * 10000 + m * 100 + d
}

function isInDateRange(dateStr: string | null, start: Date, end: Date): boolean {
  if (!dateStr) return false
  const n = parseDateNum(dateStr)
  return n >= toDateNum(start) && n <= toDateNum(end)
}

// กราฟ depend on date filter only
const dateFilteredItems = computed(() => {
  if (!filters.value) return allItems.value
  const f = filters.value
  const range = f.dateRange
  if (!range || range.length !== 2 || !range[0] || !range[1]) return allItems.value
  return allItems.value.filter(item => {
    return isInDateRange(getDateField(item, f.periodType), range[0]!, range[1]!)
  })
})

// cards depend on all filters
const filteredItems = computed(() => {
  if (!filters.value) return allItems.value

  const f = filters.value
  return allItems.value.filter(item => {
    if (f.dateRange && f.dateRange.length === 2 && f.dateRange[0] && f.dateRange[1]) {
      if (!isInDateRange(getDateField(item, f.periodType), f.dateRange[0], f.dateRange[1])) return false
    }
    if (f.method && item.method !== f.method) return false
    if (f.status && item.status !== statusMap[f.status]) return false
    if (f.search && !item.projectName.toLowerCase().includes(f.search.toLowerCase())) return false
    return true
  })
})

const activeDateRange = computed<[Date, Date] | null>(() => {
  if (!filters.value) return null
  const r = filters.value.dateRange
  if (!r || r.length !== 2 || !r[0] || !r[1]) return null
  return [r[0], r[1]]
})

function onQuery(criteria: FilterCriteria) {
  filters.value = criteria
}

function onReset() {
  filters.value = null
}

</script>

<template>
  <div class="p-4 sm:p-6 max-w-[1400px] mx-auto">
    <FilterBar @query="onQuery" @reset="onReset" />
    <OverviewCards :items="filteredItems" />
    <div class="grid grid-cols-1 lg:grid-cols-[1fr_280px] gap-5 mb-5">
      <PerformanceTrend :items="dateFilteredItems" :dateRange="activeDateRange" />
      <StatusPieCharts :items="dateFilteredItems" />
    </div>
  </div>
</template>

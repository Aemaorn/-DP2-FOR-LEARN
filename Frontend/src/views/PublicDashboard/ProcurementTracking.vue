<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import ProcurementTable from '@/views/DA003/components/ProcurementTable.vue'
import type { ProcurementItem } from '@/models/DA/da003'
import publicDashboardService from '@/services/DA/publicDashboard'

const items = ref<ProcurementItem[]>([])
const loading = ref(false)

function mapItem(raw: any): ProcurementItem {
  return {
    planId: raw.planId,
    planNumber: raw.planNumber,
    budgetYear: raw.budgetYear,
    departmentName: raw.departmentName,
    projectName: raw.projectName,
    method: raw.supplyMethod,
    supplyMethodSpecialType: raw.supplyMethodSpecialType ?? null,
    summaryId: raw.summaryId ?? null,
    approvalDate: raw.planDate ?? null,
    poDate: raw.purchaseOrderDate ?? null,
    docPrepNoticeDate: raw.docPrepareNotifyDate ?? null,
    contractSignDate: raw.contractDate ?? null,
    totalDurationDays: null,
    status: raw.status ?? null,
  }
}

const fetching = ref(false)

async function fetchData() {
  if (fetching.value) return
  fetching.value = true
  loading.value = items.value.length === 0
  try {
    const res = await publicDashboardService.getPublicListAsync({ pageNumber: 1, pageSize: 9999 })
    const payload = res.data as any
    const rawItems: any[] = Array.isArray(payload) ? payload : Array.isArray(payload?.data) ? payload.data : (payload.items ?? [])
    items.value = rawItems.map(mapItem)
  } finally {
    fetching.value = false
    loading.value = false
  }
}

let timer: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  fetchData()
  timer = setInterval(fetchData, 10_000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <div class="bg-white border-b border-gray-200 px-6 py-4">
      <h1 class="text-lg font-semibold text-gray-800">ติดตามสถานะการออกใบสั่งจ้างและการลงนามสัญญา</h1>
    </div>

    <div class="px-6 py-4">
      <div v-if="loading" class="flex justify-center items-center py-20 text-gray-400">
        <i class="pi pi-spin pi-spinner text-2xl mr-2" />
        <span>กำลังโหลดข้อมูล...</span>
      </div>
      <div v-else class="bg-white shadow-sm rounded-lg px-4">
        <ProcurementTable
          :items="items"
          :showBudget="false"
          :readonly="true"
          @update="() => {}"
          @remove="() => {}"
        />
      </div>
    </div>
  </div>
</template>

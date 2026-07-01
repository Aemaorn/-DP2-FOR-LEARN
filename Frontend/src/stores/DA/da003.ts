import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ProcurementItem, GetProcurementProgressListParams } from '@/models/DA/da003'
import da003Service from '@/services/DA/da003'

function calcStatus(approvalDate: string | null, contractDate: string | null): string | null {
  if (!approvalDate || !contractDate) return null
  const start = new Date(approvalDate + 'T00:00:00')
  const end = new Date(contractDate + 'T00:00:00')
  let count = 0
  const cur = new Date(start)
  while (cur <= end) {
    const day = cur.getDay()
    if (day !== 0 && day !== 6) count++
    cur.setDate(cur.getDate() + 1)
  }
  if (count <= 10) return 'OnPlan'
  if (count <= 15) return 'Risk'
  return 'Delay'
}

function toISODate(d: Date | null | undefined): string | null {
  if (!d) return null
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

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

export const useDa003Store = defineStore('da003Store', () => {
  const items1 = ref<ProcurementItem[]>([])
  const items2 = ref<ProcurementItem[]>([])
  const loading = ref(false)
  const totalCount = ref(0)

  async function fetchItems1(params: Omit<GetProcurementProgressListParams, 'pageNumber' | 'pageSize'> & { pageNumber?: number; pageSize?: number }) {
    loading.value = true
    try {
      const res = await da003Service.getListAsync({ pageNumber: 1, pageSize: 9999, ...params })
      const payload = res.data as any
      const rawItems: any[] = Array.isArray(payload) ? payload : Array.isArray(payload?.data) ? payload.data : (payload.items ?? [])
      items1.value = rawItems.map(mapItem)
      totalCount.value = payload?.totalCount ?? rawItems.length
    } finally {
      loading.value = false
    }
  }

  async function fetchItems2(params: Omit<GetProcurementProgressListParams, 'pageNumber' | 'pageSize'> & { pageNumber?: number; pageSize?: number }) {
    loading.value = true
    try {
      const res = await da003Service.getListAsync({ pageNumber: 1, pageSize: 9999, ...params })
      const payload = res.data as any
      const rawItems: any[] = Array.isArray(payload) ? payload : Array.isArray(payload?.data) ? payload.data : (payload.items ?? [])
      items2.value = rawItems.map(mapItem)
      totalCount.value = payload?.totalCount ?? rawItems.length
    } finally {
      loading.value = false
    }
  }

  async function updateItem1(updated: ProcurementItem) {
    await da003Service.upsertAsync({
      planId: updated.planId,
      planDate: updated.approvalDate,
      purchaseOrderDate: updated.poDate,
      docPrepareNotifyDate: updated.docPrepNoticeDate,
      contractDate: updated.contractSignDate,
      status: calcStatus(updated.approvalDate, updated.contractSignDate),
    })
    const idx = items1.value.findIndex(i => i.planId === updated.planId)
    if (idx !== -1) items1.value[idx] = updated
  }

  async function updateItem2(updated: ProcurementItem) {
    await da003Service.upsertAsync({
      planId: updated.planId,
      planDate: updated.approvalDate,
      purchaseOrderDate: updated.poDate,
      docPrepareNotifyDate: updated.docPrepNoticeDate,
      contractDate: updated.contractSignDate,
      status: calcStatus(updated.approvalDate, updated.contractSignDate),
    })
    const idx = items2.value.findIndex(i => i.planId === updated.planId)
    if (idx !== -1) items2.value[idx] = updated
  }

  function removeItem1(planId: string) {
    items1.value = items1.value.filter(i => i.planId !== planId)
  }

  function removeItem2(planId: string) {
    items2.value = items2.value.filter(i => i.planId !== planId)
  }

  return {
    items1,
    items2,
    loading,
    totalCount,
    fetchItems1,
    fetchItems2,
    updateItem1,
    updateItem2,
    removeItem1,
    removeItem2,
    toISODate,
  }
})

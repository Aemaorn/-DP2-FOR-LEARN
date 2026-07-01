import http from '@/configs/axios'
import type { GetProcurementProgressListParams, ProcurementItem } from '@/models/DA/da003'

export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface UpsertProcurementProgressBody {
  planId: string
  planDate: string | null
  purchaseOrderDate: string | null
  docPrepareNotifyDate: string | null
  contractDate: string | null
  status: string | null
}

const getListAsync = async (params: GetProcurementProgressListParams) => {
  return http.get<PaginatedResult<ProcurementItem>>('/api/dashboard/procurement-progress', { params })
}

const upsertAsync = async (body: UpsertProcurementProgressBody) => {
  return http.post<{ summaryId: string }>('/api/dashboard/procurement-progress/upsert', body)
}

const exportExcelAsync = async (params: Omit<GetProcurementProgressListParams, 'pageNumber' | 'pageSize'>) => {
  return http.get<Blob>('/api/dashboard/procurement-progress/export-excel', {
    params,
    responseType: 'blob',
  })
}

const importExcelAsync = async (file: File) => {
  const formData = new FormData()
  formData.append('file', file)
  return http.post<{ imported: number; skipped: number; skippedPlanNumbers: string[] }>(
    '/api/dashboard/procurement-progress/import-excel',
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  )
}

const da003Service = {
  getListAsync,
  upsertAsync,
  exportExcelAsync,
  importExcelAsync,
}

export default da003Service

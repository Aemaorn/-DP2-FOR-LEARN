import http from '@/configs/axios'
import type { ProcurementItem } from '@/models/DA/da003'

export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface GetPublicProcurementProgressParams {
  pageNumber: number
  pageSize: number
  keyword?: string
}

const getPublicListAsync = async (params: GetPublicProcurementProgressParams) => {
  return http.get<PaginatedResult<ProcurementItem>>('/api/dashboard/public/procurement-progress', { params })
}

const publicDashboardService = {
  getPublicListAsync,
}

export default publicDashboardService

export enum ProcurementStatus {
  OnPlan = 'OnPlan',
  Risk = 'Risk',
  Delay = 'Delay',
}

export interface ProcurementItem {
  planId: string
  planNumber: string
  budgetYear: number
  departmentName: string
  projectName: string
  method: string
  supplyMethodSpecialType: string | null
  summaryId: string | null
  approvalDate: string | null
  poDate: string | null
  docPrepNoticeDate: string | null
  contractSignDate: string | null
  totalDurationDays: number | null
  status: ProcurementStatus | null
}

export type DateFilterType = 'PlanDate' | 'PurchaseOrderDate' | 'DocPrepareNotifyDate' | 'ContractDate'

export interface GetProcurementProgressListParams {
  pageNumber: number
  pageSize: number
  keyword?: string
  supplyMethodSpecialTypeLabel?: string
  status?: ProcurementStatus
  dateType?: DateFilterType
  dateFrom?: string
  dateTo?: string
}

export interface FilterCriteria {
  periodType: string
  dateFrom: Date | null
  dateTo: Date | null
  method: string | null
  status: string | null
  search: string
}

export enum ProcurementStatus {
  OnTrack = 'ON_TRACK',
  TrendingLate = 'TRENDING_LATE',
  BehindSchedule = 'BEHIND_SCHEDULE',
}

export interface ProcurementItem {
  id: number
  projectName: string
  method: string
  approvalDate: string
  poDate: string | null
  docPrepNoticeDate: string | null
  contractSignDate: string | null
  totalDurationDays: number | null
  status: ProcurementStatus
  budgetAmount?: number
}

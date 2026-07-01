import { differenceInBusinessDays } from 'date-fns'
import { ProcurementStatus } from '@/types/procurement'

export function calculateStatus(approvalDate: string, referenceDate?: string): ProcurementStatus {
  const endDate = referenceDate ? new Date(referenceDate) : new Date()
  const days = differenceInBusinessDays(endDate, new Date(approvalDate))

  if (days <= 10) return ProcurementStatus.OnTrack
  if (days <= 15) return ProcurementStatus.TrendingLate
  return ProcurementStatus.BehindSchedule
}

export function calculateTotalDuration(
  approvalDate: string,
  contractSignDate: string | null,
): number | null {
  if (!contractSignDate) return null
  return differenceInBusinessDays(new Date(contractSignDate), new Date(approvalDate))
}

export function getStatusLabel(status: ProcurementStatus): string {
  switch (status) {
    case ProcurementStatus.OnTrack:
      return 'ตามแผน'
    case ProcurementStatus.TrendingLate:
      return 'มีแนวโน้มล่าช้า'
    case ProcurementStatus.BehindSchedule:
      return 'ช้ากว่าแผน'
  }
}

export function getStatusSeverity(status: ProcurementStatus): 'success' | 'warn' | 'danger' {
  switch (status) {
    case ProcurementStatus.OnTrack:
      return 'success'
    case ProcurementStatus.TrendingLate:
      return 'warn'
    case ProcurementStatus.BehindSchedule:
      return 'danger'
  }
}

import { differenceInBusinessDays } from 'date-fns'
import { ProcurementStatus } from '@/models/DA/da003'

export function calculateStatus(approvalDate: string, referenceDate?: string): ProcurementStatus {
  const endDate = referenceDate ? new Date(referenceDate) : new Date()
  const days = differenceInBusinessDays(endDate, new Date(approvalDate))

  if (days <= 10) return ProcurementStatus.OnPlan
  if (days <= 15) return ProcurementStatus.Risk
  return ProcurementStatus.Delay
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
    case ProcurementStatus.OnPlan: return 'เป็นไปตามแผน'
    case ProcurementStatus.Risk: return 'มีแนวโน้มล่าช้า'
    case ProcurementStatus.Delay: return 'ช้ากว่าแผน'
  }
}

export function getStatusSeverity(status: ProcurementStatus): 'success' | 'warn' | 'danger' {
  switch (status) {
    case ProcurementStatus.OnPlan: return 'success'
    case ProcurementStatus.Risk: return 'warn'
    case ProcurementStatus.Delay: return 'danger'
  }
}

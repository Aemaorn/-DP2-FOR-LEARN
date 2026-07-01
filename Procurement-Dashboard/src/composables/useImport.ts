import * as XLSX from 'xlsx'
import type { ProcurementItem } from '@/types/procurement'
import { calculateStatus, calculateTotalDuration } from '@/composables/useStatusCalculation'

function parseThaiDate(val: unknown): string | null {
  if (!val) return null
  if (typeof val === 'number') {
    // Excel serial date
    const date = XLSX.SSF.parse_date_code(val)
    const y = date.y
    const m = String(date.m).padStart(2, '0')
    const d = String(date.d).padStart(2, '0')
    return `${y}-${m}-${d}`
  }
  const s = String(val).trim()
  // dd/mm/yyyy or dd/mm/yy
  const match = s.match(/^(\d{1,2})\/(\d{1,2})\/(\d{2,4})$/)
  if (match) {
    let y = parseInt(match[3])
    if (y > 2400) y -= 543 // Buddhist year
    else if (y < 100) y += 2000
    const m = match[2].padStart(2, '0')
    const d = match[1].padStart(2, '0')
    return `${y}-${m}-${d}`
  }
  // ISO yyyy-mm-dd
  if (/^\d{4}-\d{2}-\d{2}$/.test(s)) return s
  return null
}

export function parseExcelFile(file: File): Promise<Omit<ProcurementItem, 'id'>[]> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (e) => {
      try {
        const data = new Uint8Array(e.target!.result as ArrayBuffer)
        const wb = XLSX.read(data, { type: 'array', cellDates: false })
        const ws = wb.Sheets[wb.SheetNames[0]]
        const rows = XLSX.utils.sheet_to_json(ws) as Record<string, unknown>[]

        const items: Omit<ProcurementItem, 'id'>[] = rows.map((row) => {
          const approvalDate = parseThaiDate(row['วันที่อนุมัติ']) ?? ''
          const contractSignDate = parseThaiDate(row['วันที่ลงนามสัญญา'])
          return {
            projectName: String(row['ชื่อโครงการ/แผนงาน'] ?? ''),
            method: String(row['วิธีการซื้อจ้าง'] ?? ''),
            approvalDate,
            poDate: parseThaiDate(row['วันที่ออกใบสั่งซื้อ']),
            docPrepNoticeDate: parseThaiDate(row['วันที่แจ้งเตรียมเอกสาร']),
            contractSignDate,
            budgetAmount: row['วงเงินซื้อจ้าง'] ? Number(row['วงเงินซื้อจ้าง']) : undefined,
            status: calculateStatus(approvalDate || new Date().toISOString().slice(0, 10), contractSignDate ?? undefined),
            totalDurationDays: approvalDate ? calculateTotalDuration(approvalDate, contractSignDate) : null,
          }
        }).filter(item => item.projectName && item.approvalDate)

        resolve(items)
      } catch (err) {
        reject(err)
      }
    }
    reader.onerror = reject
    reader.readAsArrayBuffer(file)
  })
}

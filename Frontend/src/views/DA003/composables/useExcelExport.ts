import * as XLSX from 'xlsx'
import { type ProcurementItem } from '@/models/DA/da003'

function formatDateTh(dateStr: string | null): string {
  if (!dateStr) return '-'
  const d = new Date(dateStr + 'T00:00:00')
  return d.toLocaleDateString('th-TH', {
    calendar: 'buddhist',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function countWorkingDays(from: Date, to: Date, includeStart: boolean): number {
  const [a, b] = from <= to ? [from, to] : [to, from]
  let count = 0
  const cur = new Date(a)
  if (!includeStart) cur.setDate(cur.getDate() + 1)
  while (cur <= b) {
    const day = cur.getDay()
    if (day !== 0 && day !== 6) count++
    cur.setDate(cur.getDate() + 1)
  }
  return count
}

function calcDaysExclusive(from: string | null, to: string | null): string {
  if (!from || !to) return '-'
  const start = new Date(from + 'T00:00:00')
  const end = new Date(to + 'T00:00:00')
  const sign = end >= start ? 1 : -1
  const count = countWorkingDays(start, end, false)
  if (count === 0) return '-'
  return `(${sign * count} วันทำการ)`
}

function calcDaysInclusive(from: string | null, to: string | null): string {
  if (!from || !to) return '-'
  const start = new Date(from + 'T00:00:00')
  const end = new Date(to + 'T00:00:00')
  const count = countWorkingDays(start, end, true)
  return count === 0 ? '-' : `${count} วันทำการ`
}

function calcTotalDays(from: string | null, to: string | null): number | null {
  if (!from || !to) return null
  const start = new Date(from + 'T00:00:00')
  const end = new Date(to + 'T00:00:00')
  const count = countWorkingDays(start, end, true)
  return count === 0 ? null : count
}

function getStatus(item: ProcurementItem): string {
  const days = calcTotalDays(item.approvalDate, item.contractSignDate)
  if (days === null) return '-'
  if (days <= 10) return 'เป็นไปตามแผน'
  if (days <= 15) return 'มีแนวโน้มล่าช้า'
  return 'ช้ากว่าแผน'
}

export function useExcelExport() {
  function exportToExcel(items: ProcurementItem[], groupLabel: string) {
    const headers = [
      'ลำดับ',
      'ชื่อโครงการ/แผนงาน\n(Project Name)',
      'วิธีการจัดจ้าง\n(Procurement Method)',
      'วันที่อนุมัติจัดจ้าง\n(Approval Date)',
      'วันที่ออกใบสั่งซื้อ/สั่งจ้าง\n(SLA 2 วันทำการนับจากวันอนุมัติ)',
      'วันที่ออกหนังสือแจ้งเตรียมเอกสาร\n(SLA 2 วันทำการนับจากออกใบสั่งจ้าง)',
      'วันที่ลงนามสัญญา\n(SLA 5 วันทำการนับจากวันที่ได้รับเอกสารครบถ้วน)',
      'รวมระยะเวลาดำเนินการ',
      'สถานะ',
    ]

    const rows = items.map((item, idx) => {
      const poText = item.poDate
        ? `${formatDateTh(item.poDate)} ${calcDaysExclusive(item.approvalDate, item.poDate)}`
        : '-'
      const docText = item.docPrepNoticeDate
        ? `${formatDateTh(item.docPrepNoticeDate)} ${calcDaysExclusive(item.poDate, item.docPrepNoticeDate)}`
        : '-'
      const contractText = item.contractSignDate
        ? `${formatDateTh(item.contractSignDate)} ${calcDaysInclusive(item.docPrepNoticeDate, item.contractSignDate)}`
        : '-'

      return [
        idx + 1,
        item.projectName,
        item.supplyMethodSpecialType ?? item.method,
        formatDateTh(item.approvalDate),
        poText,
        docText,
        contractText,
        calcDaysInclusive(item.approvalDate, item.contractSignDate),
        getStatus(item),
      ]
    })

    // Summary row
    const summaryRow = [
      'รวม',
      `${items.length} โครงการ`,
      '', '', '', '', '', '', '',
    ]

    const wsData = [headers, ...rows, summaryRow]

    const wb = XLSX.utils.book_new()
    const ws = XLSX.utils.aoa_to_sheet(wsData)

    // Column widths
    ws['!cols'] = [
      { wch: 6 },   // ลำดับ
      { wch: 45 },  // ชื่อโครงการ
      { wch: 18 },  // วิธีการจัดจ้าง
      { wch: 22 },  // วันที่อนุมัติ
      { wch: 32 },  // PO
      { wch: 32 },  // แจ้งเตรียมเอกสาร
      { wch: 38 },  // ลงนาม
      { wch: 20 },  // รวม
      { wch: 14 },  // สถานะ
    ]

    // Row heights: header row taller
    ws['!rows'] = [{ hpt: 50 }]

    XLSX.utils.book_append_sheet(wb, ws, groupLabel.slice(0, 31))

    const now = new Date()
    const dateStr = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}`
    const fileName = `ติดตามสถานะการออกใบสั่งจ้างและการลงนามสัญญา_${dateStr}.xlsx`

    XLSX.writeFile(wb, fileName)
  }

  return { exportToExcel }
}

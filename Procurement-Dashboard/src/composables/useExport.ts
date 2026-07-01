import * as XLSX from 'xlsx'
import type { ProcurementItem } from '@/types/procurement'
import { getStatusLabel } from '@/composables/useStatusCalculation'

function formatDateDisplay(date: string | null): string {
  if (!date) return ''
  return new Date(date).toLocaleDateString('th-TH', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function toRows(items: ProcurementItem[], showBudget: boolean) {
  return items.map((item, i) => ({
    ลำดับ: i + 1,
    'ชื่อโครงการ/แผนงาน': item.projectName,
    'วิธีการซื้อจ้าง': item.method,
    ...(showBudget ? { 'วงเงินซื้อจ้าง': item.budgetAmount ?? '' } : {}),
    'วันที่อนุมัติ': item.approvalDate || '',
    'วันที่ออกใบสั่งซื้อ': item.poDate || '',
    'วันที่แจ้งเตรียมเอกสาร': item.docPrepNoticeDate || '',
    'วันที่ลงนามสัญญา': item.contractSignDate || '',
    'ระยะเวลา (วัน)': item.totalDurationDays ?? '',
    สถานะ: getStatusLabel(item.status),
  }))
}

function toDisplayRows(items: ProcurementItem[], showBudget: boolean) {
  return items.map((item, i) => ({
    ลำดับ: i + 1,
    'ชื่อโครงการ/แผนงาน': item.projectName,
    'วิธีการซื้อจ้าง': item.method,
    ...(showBudget ? { 'วงเงินซื้อจ้าง': item.budgetAmount ?? '' } : {}),
    'วันที่อนุมัติ': formatDateDisplay(item.approvalDate),
    'วันที่ออกใบสั่งซื้อ': formatDateDisplay(item.poDate),
    'วันที่แจ้งเตรียมเอกสาร': formatDateDisplay(item.docPrepNoticeDate),
    'วันที่ลงนามสัญญา': formatDateDisplay(item.contractSignDate),
    'ระยะเวลา (วัน)': item.totalDurationDays ?? '-',
    สถานะ: getStatusLabel(item.status),
  }))
}

export function exportToExcel(items: ProcurementItem[], showBudget: boolean, filename: string) {
  const rows = toRows(items, showBudget)
  const ws = XLSX.utils.json_to_sheet(rows)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'ข้อมูล')
  XLSX.writeFile(wb, `${filename}.xlsx`)
}

export function exportToPDF(items: ProcurementItem[], showBudget: boolean, title: string) {
  const rows = toDisplayRows(items, showBudget)
  if (rows.length === 0) return

  const headers = Object.keys(rows[0])
  const headerRow = `<tr>${headers.map(h => `<th>${h}</th>`).join('')}</tr>`
  const bodyRows = rows.map(r =>
    `<tr>${Object.values(r).map(v => `<td>${v}</td>`).join('')}</tr>`
  ).join('')

  const html = `
    <html><head><meta charset="utf-8">
    <style>
      body { font-family: sans-serif; font-size: 11px; }
      h2 { margin-bottom: 8px; }
      table { border-collapse: collapse; width: 100%; }
      th, td { border: 1px solid #ccc; padding: 4px 8px; text-align: left; }
      th { background: #f5f5f4; }
    </style></head>
    <body>
      <h2>${title}</h2>
      <table><thead>${headerRow}</thead><tbody>${bodyRows}</tbody></table>
      <script>window.onload=()=>{ window.print(); window.onafterprint=()=>window.close() }<\/script>
    </body></html>`

  const win = window.open('', '_blank')
  win?.document.write(html)
  win?.document.close()
}

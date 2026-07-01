import type { ProcurementItem } from '@/types/procurement'
import { calculateStatus, calculateTotalDuration } from '@/composables/useStatusCalculation'

const bankProjectNames = [
  'จัดซื้อเครื่องคอมพิวเตอร์สำนักงาน', 'จ้างพัฒนาระบบบริหารจัดการสินเชื่อ', 'เช่าเครื่องพิมพ์เอกสาร',
  'จ้างบำรุงรักษาระบบ Core Banking', 'จัดซื้อซอฟต์แวร์ลิขสิทธิ์', 'จ้างที่ปรึกษาโครงการ Digital Transformation',
  'จัดซื้ออุปกรณ์เครือข่าย Firewall', 'เช่ารถยนต์ประจำตำแหน่ง', 'จ้างบริการ Cloud Infrastructure',
  'จัดซื้อเฟอร์นิเจอร์สำนักงาน', 'จ้างพัฒนา Mobile Banking App', 'จ้างบำรุงรักษาระบบ ATM',
  'จัดซื้อเครื่อง Server', 'จ้างติดตั้งระบบ CCTV', 'เช่าพื้นที่สำนักงานสาขา',
  'จัดซื้อระบบ Anti-Virus', 'จ้างพัฒนาระบบ HR Online', 'จัดซื้อ UPS สำรองไฟฟ้า',
  'จ้างบริการ Data Center', 'จัดซื้อเครื่องนับเงินอัตโนมัติ', 'จ้างออกแบบระบบ CRM',
  'จัดซื้อระบบ VPN', 'จ้างบริการ Cyber Security', 'เช่าเครื่อง Notebook',
  'จัดซื้อระบบ Backup', 'จ้างพัฒนาระบบ e-KYC', 'จ้างบำรุงรักษาลิฟต์',
  'จัดซื้อโทรศัพท์ IP Phone', 'จ้างบริการ Call Center', 'จัดซื้อระบบ Queue',
  'จ้างพัฒนา Internet Banking', 'จัดซื้อ Tablet สำหรับสาขา', 'จ้างติดตั้งระบบ WiFi',
  'จัดซื้อตู้เซฟนิรภัย', 'จ้างพัฒนาระบบ Loan Origination', 'เช่าเครื่อง Scanner',
  'จ้างบริการขนส่งเงินสด', 'จัดซื้อระบบ Digital Signature', 'จ้างพัฒนา API Gateway',
  'จัดซื้ออุปกรณ์ Video Conference', 'จ้างบำรุงรักษาระบบ SWIFT',
]

const otherProjectNames = [
  'จ้างบริการรักษาความปลอดภัย', 'จ้างบริการทำความสะอาดอาคาร', 'จัดซื้อกระดาษและวัสดุสำนักงาน',
  'จ้างปรับปรุงระบบปรับอากาศ', 'เช่าเครื่องถ่ายเอกสาร', 'จ้างบริการขนส่งเอกสาร',
  'จัดซื้อครุภัณฑ์คอมพิวเตอร์สาขา', 'จ้างพิมพ์แบบฟอร์มและสมุดบัญชี', 'จ้างซ่อมแซมอาคารสาขา',
  'จัดซื้อตู้นิรภัย', 'จ้างบริการกำจัดปลวก', 'จ้างปรับปรุงห้องประชุม',
  'จัดซื้อเครื่องดื่มสวัสดิการ', 'จ้างบริการรถรับส่งพนักงาน', 'จ้างซักรีดชุดยูนิฟอร์ม',
  'จัดซื้อเครื่องกรองน้ำ', 'จ้างบำรุงรักษาสวน', 'จัดซื้อผ้าม่านสำนักงาน',
  'จ้างบริการ Catering', 'จัดซื้ออุปกรณ์ดับเพลิง', 'จ้างตรวจสอบระบบไฟฟ้า',
  'จ้างทาสีอาคาร', 'จัดซื้อพรมปูพื้น', 'จ้างบริการจัดเก็บเอกสาร',
  'จัดซื้อเก้าอี้สำนักงาน', 'จ้างซ่อมแซมหลังคา', 'จัดซื้อโต๊ะทำงาน',
  'จ้างบริการ Pest Control', 'จัดซื้ออุปกรณ์สำนักงาน', 'จ้างปรับปรุงระบบประปา',
  'จ้างบริการจัดสวน', 'จัดซื้อตู้เอกสาร', 'จ้างซ่อมแซมพื้นอาคาร',
  'จัดซื้อม่านม้วน', 'จ้างบริการทำความสะอาดกระจก',
]

const methods = ['เฉพาะเจาะจง', 'คัดเลือก', 'e-bidding', 'e-market', 'พิเศษ']

// Deterministic pseudo-random based on seed
function seededRandom(seed: number) {
  let s = seed
  return () => {
    s = (s * 1664525 + 1013904223) & 0xffffffff
    return (s >>> 0) / 0xffffffff
  }
}

function addDays(date: string, days: number): string {
  const d = new Date(date + 'T00:00:00')
  d.setDate(d.getDate() + days)
  return d.toISOString().slice(0, 10)
}

function generateItems(
  names: string[],
  group: 'bank' | 'other',
  count: number,
  startId: number,
): ProcurementItem[] {
  const rand = seededRandom(group === 'bank' ? 12345 : 67890)
  const items: ProcurementItem[] = []

  // 4 months: Dec 2025, Jan 2026, Feb 2026, Mar 2026
  const monthStarts = ['2025-12-01', '2026-01-01', '2026-02-01', '2026-03-01']

  for (let i = 0; i < count; i++) {
    const nameIdx = Math.floor(rand() * names.length)
    const suffix = count > names.length ? ` (ครั้งที่ ${Math.floor(i / names.length) + 1})` : ''
    const branchNum = Math.floor(rand() * 50) + 1

    const projectName = `${names[nameIdx]}${suffix} สาขา ${branchNum}`
    const method = methods[Math.floor(rand() * methods.length)]

    // spread across 4 months
    const monthIdx = Math.floor(rand() * 4)
    const dayOffset = Math.floor(rand() * 28)
    const approvalDate = addDays(monthStarts[monthIdx], dayOffset)

    // PO: 2-5 days after approval
    const poDays = Math.floor(rand() * 4) + 2
    const poDate = addDays(approvalDate, poDays)

    // Doc prep: 2-7 days after PO (some null)
    const hasDoc = rand() > 0.15
    const docDays = Math.floor(rand() * 6) + 2
    const docPrepNoticeDate = hasDoc ? addDays(poDate, docDays) : null

    // Contract sign: some completed, some not
    const isComplete = rand() > 0.35
    let contractSignDate: string | null = null
    if (isComplete && docPrepNoticeDate) {
      const signDays = Math.floor(rand() * 5) + 1
      contractSignDate = addDays(docPrepNoticeDate, signDays)
    }

    const status = calculateStatus(approvalDate, contractSignDate ?? undefined)
    const totalDurationDays = calculateTotalDuration(approvalDate, contractSignDate)

    const item: ProcurementItem = {
      id: startId + i,
      projectName,
      method,
      approvalDate,
      poDate,
      docPrepNoticeDate,
      contractSignDate,
      totalDurationDays,
      status,
    }

    if (group === 'other') {
      item.budgetAmount = Math.floor(rand() * 20000000 / 100000) * 100000 + 100000
    }

    items.push(item)
  }

  return items
}

export const seedBank = generateItems(bankProjectNames, 'bank', 160, 1)
export const seedOther = generateItems(otherProjectNames, 'other', 160, 1001)

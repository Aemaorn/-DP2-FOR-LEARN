/**
 * **สถานะใบรับรองผลงาน (CA02Status)**
 * ใช้สำหรับระบุสถานะของใบรับรองผลงานในระบบ
 *
 * - All → ทั้งหมด (ใช้สำหรับ Criteria)
 * - Draft → แบบร่าง
 * - WaitingForCommitteeApproval → รอคณะกรรมการตรวจรับอนุมัติ
 * - Approved → อนุมัติแล้ว
 * - Rejected → ส่งกลับแก้ไข
 * - Edit → เรียกคืนแก้ไข
 * - Cancelled → ยกเลิก
 */
export enum CA02Status {
  /**
   * ทั้งหมด (ใช้สำหรับ Criteria)
   */
  All = 'All',
  /**
   * แบบร่าง
   */
  Draft = 'Draft',

  /**
   * รอคณะกรรมการตรวจรับอนุมัติ
   */
  WaitingForCommitteeApproval = 'WaitingForCommitteeApproval',

  /**
   * อนุมัติแล้ว
   */
  Approved = 'Approved',

  /**
   * ส่งกลับแก้ไข
   */
  Rejected = 'Rejected',

  /**
   * เรียกคืนแก้ไข
   */
  Edit = 'Edit',

  /**
   * ยกเลิก
   */
  Cancelled = 'Cancelled',
}

export enum CA02TabHeader {
  Detail = 'Detail',
  Document = 'Document',
}

export enum CA02Accordion {
  Committee = 'Committee'
}
export enum Cam02Status {
   /**
   * ทั้งหมด (ใช้สำหรับ Criteria)
   */
  All = 'All',
  /**
   * แบบร่าง
   */
  Draft = 'Draft',

  /**
   * อยู่ระหว่าง คกก. เห็นชอบ
   */
  WaitingCommitteeApproval = 'WaitingCommitteeApproval',

  /**
   * รอ จพ. มอบหมาย
   */
  WaitingAssign = 'WaitingAssign',

  /**
   * รอ จพ. ให้ความเห็น
   */
  WaitingComment = 'WaitingComment',

  /**
   * ส่งกลับแก้ไข (ถึงผู้รับมอบหมาย)
   */
  RejectToAssignee = 'RejectToAssignee',

  /**
   * รอคณะกรรมการตรวจรับอนุมัติ
   */
  WaitingApproval = 'WaitingApproval',

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
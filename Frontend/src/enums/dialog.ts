/**
 *  **Confirm Dialog Type**
 *    - ***( SendApprove )** - ส่งเห็นชอบ
 *    - ***( SendConfirm )** - ส่งอนุมัติ
 *    - ***( SendApproveConfirm )** - ส่งเห็นชอบ/อนุมัติ
 *    - ***( Edit )** - เรียกคืนแก้ไข
 *    - ***( Logout )** - ออกจากระบบ
 *    - ***( Delete )** - ลบข้อมูลออกจากระบบ
 *    - ***( ConfirmChange )** - เปลี่ยนรูปแบบข้อมูล
 *    - ***( ConfirmTemplate )** - เปลี่ยนรูปแบบเอกสาร
 *    - ***( Assigned )** - มอบหมายงาน
 *    - ***( ConfirmData )*** - ยืนยันข้อมูล
 *    - ***( SendEdit )*** - ส่งกลับแก้ไข
 *    - ***( AnnouncementPlan )*** - ประกาศเผยแพร่แผน
 */
export enum ConfirmDialogType {
  /**
   * - ส่งเห็นชอบ
   */
  SendApprove,
  /**
   * - ส่งอนุมัติ
   */
  SendConfirm,
  /**
  * - ส่งเห็นชอบ/อนุมัติ
  */
  SendApproveConfirm,
  /**
  * - เรียกคืนแก้ไข
  */
  Edit,
  /**
   * - ออกจากระบบ
   */
  Logout,
  /**
  * - ลบข้อมูลออกจากระบบ
  */
  Delete,
  /**
* - เปลี่ยนรูปแบบข้อมูล
*/
  ConfirmChange,
  /**
  * - เปลี่ยนรูปแบบเอกสาร
  */
  ConfirmTemplate,
  /**
   * - มอบหมายงาน
   */
  Assigned,
  /**
   * - ขอแก้ไข
   */
  Changed,
  /**
   * - ขอยกเลิก
   */
  Canceled,
  /**
   * - ยืนยันข้อมูล
   */
  ConfirmData,
  /**
    * - ส่งกลับแก้ไข
    */
  SendEdit,
  /**
    * - ประกาศเผยแพร่แผน
    */
  AnnouncementPlan,
  /**
    * - ยืนยันการเปลี่ยนแปลงข้อมูลในเอกสาร
    */
  ConfirmReplaceDocument,
  /**
    * - ยกเลิกปิดงาน
    */
  CancelClosePlan,
}

/**
 *  **Reason Dialog Type**
 *    - ***( Approve )** - หมายเหตุอนุมัติ
 *    - ***( Accepted )** - หมายเหตุเห็นชอบ
 *    - ***( RemarkOfficer )** - ความเห็นเจ้าหน้าที่พัสดุ
 *    - ***( Reject )** - ส่งกลับแก้ไข
 *    - ***( RequestCancel )** - ขอยกเลิก
 *    - ***( RequestChange )** - ขอเปลี่ยนแปลง
 */
export enum ReasonDialogType {
  /**
   * - หมายเหตุอนุมัติ
   */
  Approve,
  /**
   * - หมายเหตุเห็นชอบ
   */
  Accepted,
  /**
   * - ความเห็นเจ้าหน้าที่พัสดุ
   */
  RemarkOfficer,
  /**
   * - ส่งกลับแก้ไข
   */
  Reject,
  /**
   * - ขอยกเลิก
   */
  RequestCancel,
  /**
   * - ขอเปลี่ยนแปลง
   */
  RequestChange,
  /**
   * - ไม่เห็นชอบ
   */
  NotAgree,
  /**
   * - ไม่สามารถปฏิบัติหน้าที่ได้
   */
  UnableToPerformDuties,
    /**
   * - หมายเหตุ
   */
  Confirm,
  /**
   * - ปิดงาน
   */
  ClosePlan,
}

export enum DialogDescription {
  ConfirmReplaceDocument = 'หากยืนยันการเปลี่ยนแปลง ระบบจะนำข้อมูลจากหน้าจอปัจจุบันไปแทนที่ข้อมูลในเอกสาร'
}

/**
 *  **Save Option Type**
 *    - ***( FormOnly )** - บันทึกแบบฟอร์มอย่างเดียว
 *    - ***( FormWithReset )** - บันทึกแบบฟอร์มพร้อมรีเซตเอกสาร
 */
export enum SaveOptionType {
  /**
   * - บันทึกแบบฟอร์มอย่างเดียว
   */
  FormOnly = 'formOnly',
  /**
   * - บันทึกแบบฟอร์มพร้อมรีเซตเอกสาร
   */
  FormWithReset = 'formWithReset',
}
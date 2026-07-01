export enum AssigneeGroup {
  /**
   *  - กลุ่ม จพ.
   */
  JorPor = 'JorPor',

  /**
   *  - กลุ่ม สัญญา
   */
  Contract = 'Contract',

  /**
   *  - กลุ่ม บัญชี
   */
  Accounting = 'Accounting',

  /**
   *  - กลุ่ม ผู้จัดทำเอกสารบันทึกต่อท้าย
   */
  AddendumDrafter = 'AddendumDrafter',
}

/**
 *  * **ประเภทผู้มอบหมาย**
 *    - **( Director )** - ผอ. จพ. มอบหมาย
 *    - **( Assignee )** - ผู้ที่ได้รับมอบหมาย
 */
enum AssigneeType {
  /**
   *  - ผอ. จพ. มอบหมาย
   */
  Director = "Director",
  /**
   *  - ผู้ที่ได้รับมอบหมาย
   */
  Assignee = "Assignee",
}


/**
 *  * **สถานะผู้มอบหมาย**
 *    - **( Draft )** - แบบร่าง
 *    - **( Pending )** - รออนุมัติ
 *    - **( Assigned )** - มอบหมายแล้ว
 *    - **( Rejected )** - ปฏิเสธ
 */
enum AssigneeStatus {
  /**
   *  - แบบร่าง
   */
  Draft = 'Draft',
  /**
   *  - รออนุมัติ
   */
  Pending = 'Pending',
  /**
   *  - มอบหมายแล้ว
   */
  Assigned = 'Assigned',
  /**
   *  - ปฏิเสธ
   */
  Rejected = 'Rejected',
}

/**
 *  * **ประเภทผู้เห็นชอบ**
 *    - **( DepartmentDirectorAgree )** - ส่วนสายงานเห็นชอบ
 *    - **( Approver )** - ส่วนผู้มีอำนาจเห็นชอบ
 *    - **( TorDraftCommittee )** - คณะกรรมการที่จัดทำหรือร่าง TOR
 *    - **( MedianPriceCommittee )** - คณะกรรมการที่จัดทำหรือร่างราคากลาง
 *    - **( ProcurementCommittee )** - ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง
 *    - **( Jp005Committee )** - ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง pp005
 *    - **( RentCommittee )** - คณะกรรมการจัดเช่า
 *    - **( AcceptanceCommittee )** - คณะกรรมการตรวจรับพัสดุ
 *    - **( InspectionCommittee )** - คณะกรรมการตรวจรับพัสดุ
 */
enum AcceptorType {
  /**
   *  - ส่วนสายงานเห็นชอบ
   */
  DepartmentDirectorAgree = "DepartmentDirectorAgree",

  /**
    *  - คณะกรรมการที่จัดทำหรือร่าง TOR
    */
  TorDraftCommittee = 'TorDraftCommittee',

  /**
  *  - คณะกรรมการที่จัดทำหรือร่างราคากลาง
  */
  MedianPriceCommittee = 'MedianPriceCommittee',

  /**
  *  - ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง
  */
  ProcurementCommittee = 'ProcurementCommittee',

  /**
*  - ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง pp005
*/
  Jp005Committee = 'Jp005Committee',

  /**
  *  - ส่วนผู้มีอำนาจเห็นชอบ
  */
  Approver = "Approver",

  /**
  *  - คณะกรรมการจัดเช่า
  */
  RentCommittee = 'RentCommittee',

  /**
  *  - คณะกรรมการจัดเช่า
  */
  AcceptanceCommittee = 'AcceptanceCommittee',

  /**
  *  - คณะตรวจรับพัสดุ
  */
  InspectionCommittee = 'InspectionCommittee',

  /**
  *  - คณะตรวจรับพัสดุ
  */
  AcceptorSign = 'AcceptorSign',

  Accounting = 'Accounting',
  /**
*  - บัญชีเห็นชอบ
*/
  AccountingApprover = 'AccountingApprover',

  AccountingConfirmer = 'AccountingConfirmer',

  /**
  *  - ผู้ตรวจสอบเอกสารบันทึกต่อท้าย
  */
  Reviewer = 'Reviewer',

  /**
  *  - บัญชีดำเนินการ
  */
  AccountingOperator = 'AccountingOperator',
}

/**
 *  * **สถานะผู้เห็นชอบ**
 *    - **( Draft )** - แบบร่าง
 *    - **( Pending )** - รออนุมัติ
 *    - **( Approved )** - อนุมัติแล้ว
 *    - **( Rejected )** - ปฏิเสธ
 */
enum AcceptorStatus {
  /**
   *  * แบบร่าง
   */
  Draft = 'Draft',
  /**
   *  * รออนุมัติ
   */
  Pending = 'Pending',
  /**
   *  * อนุมัติแล้ว
   */
  Approved = 'Approved',
  /**
   *  * ปฏิเสธ
   */
  Rejected = 'Rejected',
  /**
   *  * ไม่สามารถปฏิบัติหน้าที่ได้
   */
  UnableToPerformDuties = 'UnableToPerformDuties',
}

export {
  AcceptorType,
  AssigneeType,
  AcceptorStatus,
  AssigneeStatus,
};

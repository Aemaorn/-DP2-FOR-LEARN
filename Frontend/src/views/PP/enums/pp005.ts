/**
 * **สถานะ จพ. 005**
 *  - **( Draft )** - สถานะ แบบร่าง
 *  - **( Edit )** - สถานะ เรียกคืนแก้ไข
 *  - **( WaitingApproval )** - สถานะ รอเห็นชอบ
 *  - **( Approved )** - สถานะ เห็นชอบ
 *  - **( Rejected )** - สถานะ ส่งกลับแก้ไข
 *  - **( Cancelled )** - สถานะ ยกเลิกรายการ
 */
export enum PP005Status {
  /**
   * * สถานะ แบบร่าง
  */
  Draft = 'Draft',
  /**
   * * สถานะ เรียกคืนแก้ไข
  */
  Edit = 'Edit',
  /**
   * * สถานะ รอเห็นชอบ
  */
  WaitingApproval = 'WaitingApproval',
  /**
   * * สถานะ เห็นชอบ
  */
  Approved = 'Approved',
  /**
   * * สถานะ ส่งกลับแก้ไข
  */
  Rejected = 'Rejected',
  /**
   * * สถานะ ยกเลิกรายการ
  */
  Cancelled = 'Cancelled',
};

/**
 * **Card AcceptorType**
 *  - **( Acceptor )** - ผู้มีอำนาจเห็นชอบ/อนุมัติ
 *  - **( Segment )** - ส่วนงาน
 */
export enum PP005Accordion {
  /**
  * * ผู้มีอำนาจเห็นชอบ/อนุมัติ
  */
  Acceptor = 'Acceptor',
  /**
  * * ส่วนงาน
  */
  Segment = 'Segment',

  Document = 'Document',
};

/**
 * **ประเภทเจ้าหน้าที่รับผิดชอบ ( จาก จพ.04 )**
 *  - **( ProcurementCommittee )** - คณะกรรมการจัดซื้อจัดจ้าง
 *  - **( Assignee )** - ผู้ที่ได้รับมอบหมาย
 */
export enum OperatorType {
  /**
   * * คณะกรรมการจัดซื้อจัดจ้าง
   */
  ProcurementCommittee = 'ProcurementCommittee',
  /**
   * * ผู้ที่ได้รับมอบหมาย
   */
  Assignee = 'Assignee',
}
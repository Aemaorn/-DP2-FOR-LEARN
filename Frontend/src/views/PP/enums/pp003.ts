/**
 * **สถานะราคากลาง**
 * - **( Draft )** - สถานะ แบบร่าง
 * - **( Edit )** - สถานะ เรียกคืนแก้ไข
 * - **( WaitingCommitteeApproval )** - สถานะ อยู่ระหว่าง คกก. เห็นชอบ
 * - **( WaitingUnitApproval )** - สถานะ อยู่ระหว่างหน่วยงานเห็นชอบ
 * - **( WaitingAssign )** - สถานะ รอ จพ. มอบหมาย
 * - **( WaitingComment )** - สถานะ อยู่ระหว่าง จพ. ให้ความเห็น
 * - **( WaitingApproval )** - สถานะ รออนุมัติ
 * - **( Approved )** - สถานะ อนุมัติ
 * - **( RejectToAssignee )** - สถานะ ส่งกลับแก้ไข (ส่งกลับไปยัง จพ.)
 * - **( Rejected )** - สถานะ ส่งกลับแก้ไข
 * - **( Cancelled )** - สถานะ ยกเลิกรายการ
 */
export enum PP003Status {
  /**
   * สถานะ แบบร่าง
   */
  Draft = 'Draft',

  /**
   * สถานะ เรียกคืนแก้ไข
   */
  Edit = 'Edit',

  /**
   * สถานะ อยู่ระหว่าง คกก. เห็นชอบ
   */
  WaitingCommitteeApproval = 'WaitingCommitteeApproval',

  /**
   * สถานะ อยู่ระหว่างหน่วยงานเห็นชอบ
   */
  WaitingUnitApproval = 'WaitingUnitApproval',

  /**
   * สถานะ รอ จพ. มอบหมาย
   */
  WaitingAssign = 'WaitingAssign',

  /**
   * สถานะ อยู่ระหว่าง จพ. ให้ความเห็น
   */
  WaitingComment = 'WaitingComment',

  /**
   * สถานะ รออนุมัติ
   */
  WaitingApproval = 'WaitingApproval',

  /**
   * สถานะ อนุมัติ
   */
  Approved = 'Approved',

  /**
   * สถานะ ส่งกลับแก้ไข (ส่งกลับไปยัง จพ.)
   */
  RejectToAssignee = 'RejectToAssignee',

  /**
   * สถานะ ส่งกลับแก้ไข
   */
  Rejected = 'Rejected',

  /**
   * สถานะ ยกเลิกรายการ
   */
  Cancelled = 'Cancelled',
}


/**
 * * Card AcceptorType
 */
export enum MedianPriceAccordion {
  /**
  * * บุคคล/คณะกรรมการกำหนดราคากลาง
  */
  MedianPriceCommittee = 'MedianPriceCommittee',
  /**
  * * สายงานเห็นชอบ
  */
  Units = 'Units',
  /**
  * * เจ้าหน้าที่พัสดุให้ความเห็น
  */
  JorPorSuggestion = 'JorPorSuggestion',
  /**
  * * ผู้มีอำนาจเห็นชอบ/อนุมัติ
  */
  Acceptor = 'Acceptor',
}

export enum PP003Template {
  /**
   * ราคากลางในงานจ้างก่อสร้าง - Template - 01
   */
  MedianPriceBoKor0180 = 'MedianPriceBoKor0180',
  MedianPriceBoKor01ForJorporComment80 = 'MedianPriceBoKor01ForJorporComment80',
  /**
   * ราคากลางในงานจ้างก่อสร้าง - Template - 01
   */
  MedianPriceBoKor0160 = 'MedianPriceBoKor0160',
  MedianPriceBoKor01ForJorporComment60 = 'MedianPriceBoKor01ForJorporComment60',
  /**
   * ราคากลางในงานจ้างควบคุมงานก่อสร้าง - Template - 02
   */
  MedianPriceBoKor0280 = 'MedianPriceBoKor0280',
  MedianPriceBoKor02ForJorporComment80 = 'MedianPriceBoKor02ForJorporComment80',
  /**
   * ราคากลางในงานจ้างควบคุมงานก่อสร้าง - Template - 02
   */
  MedianPriceBoKor0260 = 'MedianPriceBoKor0260',
  MedianPriceBoKor02ForJorporComment60 = 'MedianPriceBoKor02ForJorporComment60',
  /**
   * ราคากลางในงานจ้างออกแบบ - Template - 03
   */
  MedianPriceBoKor0380 = 'MedianPriceBoKor0380',
  MedianPriceBoKor03ForJorporComment80 = 'MedianPriceBoKor03ForJorporComment80',
  /**
   * ราคากลางในงานจ้างออกแบบ - Template - 03
   */
  MedianPriceBoKor0360 = 'MedianPriceBoKor0360',
  MedianPriceBoKor03ForJorporComment60 = 'MedianPriceBoKor03ForJorporComment60',
  /**
   * รายละเอียดค่าใช้ง่ายการจ้างที่ปรึกษา - Template - 04
   */
  MedianPriceBoKor0480 = 'MedianPriceBoKor0480',
  MedianPriceBoKor04ForJorporComment80 = 'MedianPriceBoKor04ForJorporComment80',
  /**
   * รายละเอียดค่าใช้ง่ายการจ้างที่ปรึกษา - Template - 04
   */
  MedianPriceBoKor0460 = 'MedianPriceBoKor0460',
  MedianPriceBoKor04ForJorporComment60 = 'MedianPriceBoKor04ForJorporComment60',
  /**
   * รายละเอียดค่าใช้จ่ายการจ้างพัฒนาระบบคอมพิวเตอร์ - Template - 05
   */
  MedianPriceBoKor0580 = 'MedianPriceBoKor0580',
  MedianPriceBoKor05ForJorporComment80 = 'MedianPriceBoKor05ForJorporComment80',
  /**
   * รายละเอียดค่าใช้จ่ายการจ้างพัฒนาระบบคอมพิวเตอร์ - Template - 05
   */
  MedianPriceBoKor0560 = 'MedianPriceBoKor0560',
  MedianPriceBoKor05ForJorporComment60 = 'MedianPriceBoKor05ForJorporComment60',
  /**
   * รายละเอียดค่าใช่จ่ายการจัดซื้อจัดจ้างที่มิใช่งานก่อสร้าง - Template - 06
   */
  MedianPriceBoKor0680 = 'MedianPriceBoKor0680',
  MedianPriceBoKor06ForJorporComment80 = 'MedianPriceBoKor06ForJorporComment80',
  /**
   * รายละเอียดค่าใช่จ่ายการจัดซื้อจัดจ้างที่มิใช่งานก่อสร้าง - Template - 06
   */
  MedianPriceBoKor0660 = 'MedianPriceBoKor0660',
  MedianPriceBoKor06ForJorporComment60 = 'MedianPriceBoKor06ForJorporComment60',



  /**
   * Cancel
   */

  /**
   * ราคากลางในงานจ้างก่อสร้าง - Template - 01
   */
  MedianPriceCancelBoKor0160 = 'MedianPriceCancelBoKor0160',
  /**
   * ราคากลางในงานจ้างก่อสร้าง - Template - 01
   */
  MedianPriceCancelBoKor0180 = 'MedianPriceCancelBoKor0180',
  /**
   * ราคากลางในงานจ้างก่อสร้าง - Template - 01
   */
  MedianPriceCancelBoKor01ForJorporComment60 = 'MedianPriceCancelBoKor01ForJorporComment60',
  /**
   * ราคากลางในงานจ้างก่อสร้าง - Template - 01
   */
  MedianPriceCancelBoKor01ForJorporComment80 = 'MedianPriceCancelBoKor01ForJorporComment80',
  /**
   * ราคากลางในงานจ้างควบคุมงานก่อสร้าง - Template - 02
   */
  MedianPriceCancelBoKor0260 = 'MedianPriceCancelBoKor0260',
  /**
   * ราคากลางในงานจ้างควบคุมงานก่อสร้าง - Template - 02
   */
  MedianPriceCancelBoKor0280 = 'MedianPriceCancelBoKor0280',
  /**
   * ราคากลางในงานจ้างควบคุมงานก่อสร้าง - Template - 02
   */
  MedianPriceCancelBoKor02ForJorporComment60 = 'MedianPriceCancelBoKor02ForJorporComment60',
  /**
   * ราคากลางในงานจ้างควบคุมงานก่อสร้าง - Template - 02
   */
  MedianPriceCancelBoKor02ForJorporComment80 = 'MedianPriceCancelBoKor02ForJorporComment80',
  /**
   * ราคากลางในงานจ้างออกแบบ - Template - 03
   */
  MedianPriceCancelBoKor0360 = 'MedianPriceCancelBoKor0360',
  /**
   * ราคากลางในงานจ้างออกแบบ - Template - 03
   */
  MedianPriceCancelBoKor0380 = 'MedianPriceCancelBoKor0380',
  /**
   * ราคากลางในงานจ้างออกแบบ - Template - 03
   */
  MedianPriceCancelBoKor03ForJorporComment60 = 'MedianPriceCancelBoKor03ForJorporComment60',
  /**
   * ราคากลางในงานจ้างออกแบบ - Template - 03
   */
  MedianPriceCancelBoKor03ForJorporComment80 = 'MedianPriceCancelBoKor03ForJorporComment80',
  /**
   * รายละเอียดค่าใช้ง่ายการจ้างที่ปรึกษา - Template - 04
   */
  MedianPriceCancelBoKor0460 = 'MedianPriceCancelBoKor0460',
  /**
   * รายละเอียดค่าใช้ง่ายการจ้างที่ปรึกษา - Template - 04
   */
  MedianPriceCancelBoKor0480 = 'MedianPriceCancelBoKor0480',
  /**
   * รายละเอียดค่าใช้ง่ายการจ้างที่ปรึกษา - Template - 04
   */
  MedianPriceCancelBoKor04ForJorporComment60 = 'MedianPriceCancelBoKor04ForJorporComment60',
  /**
   * รายละเอียดค่าใช้ง่ายการจ้างที่ปรึกษา - Template - 04
   */
  MedianPriceCancelBoKor04ForJorporComment80 = 'MedianPriceCancelBoKor04ForJorporComment80',
  /**
   * รายละเอียดค่าใช้จ่ายการจ้างพัฒนาระบบคอมพิวเตอร์ - Template - 05
   */
  MedianPriceCancelBoKor0560 = 'MedianPriceCancelBoKor0560',
  /**
   * รายละเอียดค่าใช้จ่ายการจ้างพัฒนาระบบคอมพิวเตอร์ - Template - 05
   */
  MedianPriceCancelBoKor0580 = 'MedianPriceCancelBoKor0580',
  /**
   * รายละเอียดค่าใช้จ่ายการจ้างพัฒนาระบบคอมพิวเตอร์ - Template - 05
   */
  MedianPriceCancelBoKor05ForJorporComment60 = 'MedianPriceCancelBoKor05ForJorporComment60',
  /**
   * รายละเอียดค่าใช้จ่ายการจ้างพัฒนาระบบคอมพิวเตอร์ - Template - 05
   */
  MedianPriceCancelBoKor05ForJorporComment80 = 'MedianPriceCancelBoKor05ForJorporComment80',
  /**
   * รายละเอียดค่าใช่จ่ายการจัดซื้อจัดจ้างที่มิใช่งานก่อสร้าง - Template - 06
   */
  MedianPriceCancelBoKor0660 = 'MedianPriceCancelBoKor0660',
  /**
   * รายละเอียดค่าใช่จ่ายการจัดซื้อจัดจ้างที่มิใช่งานก่อสร้าง - Template - 06
   */
  MedianPriceCancelBoKor0680 = 'MedianPriceCancelBoKor0680',
  /**
   * รายละเอียดค่าใช่จ่ายการจัดซื้อจัดจ้างที่มิใช่งานก่อสร้าง - Template - 06
   */
  MedianPriceCancelBoKor06ForJorporComment60 = 'MedianPriceCancelBoKor06ForJorporComment60',
  /**
   * รายละเอียดค่าใช่จ่ายการจัดซื้อจัดจ้างที่มิใช่งานก่อสร้าง - Template - 06
   */
  MedianPriceCancelBoKor06ForJorporComment80 = 'MedianPriceCancelBoKor06ForJorporComment80',

  /**
 * Change
 */

  /**
 * ราคากลางในงานจ้างก่อสร้าง - Template - 01
 */
  MedianPriceChangeBoKor0160 = 'MedianPriceChangeBoKor0160',
  /**
   * ราคากลางในงานจ้างก่อสร้าง - Template - 01
   */
  MedianPriceChangeBoKor0180 = 'MedianPriceChangeBoKor0180',
  /**
   * ราคากลางในงานจ้างก่อสร้าง - Template - 01
   */
  MedianPriceChangeBoKor01ForJorporComment60 = 'MedianPriceChangeBoKor01ForJorporComment60',
  /**
   * ราคากลางในงานจ้างก่อสร้าง - Template - 01
   */
  MedianPriceChangeBoKor01ForJorporComment80 = 'MedianPriceChangeBoKor01ForJorporComment80',
  /**
   * ราคากลางในงานจ้างควบคุมงานก่อสร้าง - Template - 02
   */
  MedianPriceChangeBoKor0260 = 'MedianPriceChangeBoKor0260',
  /**
   * ราคากลางในงานจ้างควบคุมงานก่อสร้าง - Template - 02
   */
  MedianPriceChangeBoKor0280 = 'MedianPriceChangeBoKor0280',
  /**
   * ราคากลางในงานจ้างควบคุมงานก่อสร้าง - Template - 02
   */
  MedianPriceChangeBoKor02ForJorporComment60 = 'MedianPriceChangeBoKor02ForJorporComment60',
  /**
   * ราคากลางในงานจ้างควบคุมงานก่อสร้าง - Template - 02
   */
  MedianPriceChangeBoKor02ForJorporComment80 = 'MedianPriceChangeBoKor02ForJorporComment80',
  /**
   * ราคากลางในงานจ้างออกแบบ - Template - 03
   */
  MedianPriceChangeBoKor0360 = 'MedianPriceChangeBoKor0360',
  /**
   * ราคากลางในงานจ้างออกแบบ - Template - 03
   */
  MedianPriceChangeBoKor0380 = 'MedianPriceChangeBoKor0380',
  /**
   * ราคากลางในงานจ้างออกแบบ - Template - 03
   */
  MedianPriceChangeBoKor03ForJorporComment60 = 'MedianPriceChangeBoKor03ForJorporComment60',
  /**
   * ราคากลางในงานจ้างออกแบบ - Template - 03
   */
  MedianPriceChangeBoKor03ForJorporComment80 = 'MedianPriceChangeBoKor03ForJorporComment80',
  /**
   * รายละเอียดค่าใช้ง่ายการจ้างที่ปรึกษา - Template - 04
   */
  MedianPriceChangeBoKor0460 = 'MedianPriceChangeBoKor0460',
  /**
   * รายละเอียดค่าใช้ง่ายการจ้างที่ปรึกษา - Template - 04
   */
  MedianPriceChangeBoKor0480 = 'MedianPriceChangeBoKor0480',
  /**
   * รายละเอียดค่าใช้ง่ายการจ้างที่ปรึกษา - Template - 04
   */
  MedianPriceChangeBoKor04ForJorporComment60 = 'MedianPriceChangeBoKor04ForJorporComment60',
  /**
   * รายละเอียดค่าใช้ง่ายการจ้างที่ปรึกษา - Template - 04
   */
  MedianPriceChangeBoKor04ForJorporComment80 = 'MedianPriceChangeBoKor04ForJorporComment80',
  /**
   * รายละเอียดค่าใช้จ่ายการจ้างพัฒนาระบบคอมพิวเตอร์ - Template - 05
   */
  MedianPriceChangeBoKor0560 = 'MedianPriceChangeBoKor0560',
  /**
   * รายละเอียดค่าใช้จ่ายการจ้างพัฒนาระบบคอมพิวเตอร์ - Template - 05
   */
  MedianPriceChangeBoKor0580 = 'MedianPriceChangeBoKor0580',
  /**
   * รายละเอียดค่าใช้จ่ายการจ้างพัฒนาระบบคอมพิวเตอร์ - Template - 05
   */
  MedianPriceChangeBoKor05ForJorporComment60 = 'MedianPriceChangeBoKor05ForJorporComment60',
  /**
   * รายละเอียดค่าใช้จ่ายการจ้างพัฒนาระบบคอมพิวเตอร์ - Template - 05
   */
  MedianPriceChangeBoKor05ForJorporComment80 = 'MedianPriceChangeBoKor05ForJorporComment80',
  /**
   * รายละเอียดค่าใช่จ่ายการจัดซื้อจัดจ้างที่มิใช่งานก่อสร้าง - Template - 06
   */
  MedianPriceChangeBoKor0660 = 'MedianPriceChangeBoKor0660',
  /**
   * รายละเอียดค่าใช่จ่ายการจัดซื้อจัดจ้างที่มิใช่งานก่อสร้าง - Template - 06
   */
  MedianPriceChangeBoKor0680 = 'MedianPriceChangeBoKor0680',
  /**
   * รายละเอียดค่าใช่จ่ายการจัดซื้อจัดจ้างที่มิใช่งานก่อสร้าง - Template - 06
   */
  MedianPriceChangeBoKor06ForJorporComment60 = 'MedianPriceChangeBoKor06ForJorporComment60',
  /**
   * รายละเอียดค่าใช่จ่ายการจัดซื้อจัดจ้างที่มิใช่งานก่อสร้าง - Template - 06
   */
  MedianPriceChangeBoKor06ForJorporComment80 = 'MedianPriceChangeBoKor06ForJorporComment80',

}
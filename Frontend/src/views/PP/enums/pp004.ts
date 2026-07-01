export enum pp004CommitteeType {
  /// <summary>
  /// คณะกรรมการจัดซื้อจัดจ้าง
  /// </summary>
  ProcurementCommittee = `ProcurementCommittee`,

  /// <summary>
  /// ผู้ตรวจรับพัสดุ-คณะกรรมการตรวจรับพัสดุ
  /// </summary>
  InspectionCommittee = `InspectionCommittee`,

  /// <summary>
  /// คณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)
  /// </summary>
  MaintenanceInspectionCommittee = `MaintenanceInspectionCommittee`,

  /// <summary>
  /// ผู้ควบคุมงาน (เฉพาะงานก่อสร้าง)
  /// </summary>
  ConstructionSupervisor = `ConstructionSupervisor`,
}

export enum pp004status {
  /// <summary>
  /// แบบร่าง
  /// </summary>
  Draft = "Draft",

  /// <summary>
  /// เรียกคืนแก้ไข
  /// </summary>
  Edit = "Edit",

  /// <summary>
  /// รอเห็นชอบ
  /// </summary>
  WaitingApproval = "WaitingApproval",

  /// <summary>
  /// รอ ผอ. จพ. มอบหมาย (มอบหมายให้เจ้าหน้าที่ทำ Module Procurement)
  /// </summary>
  WaitingAssign = "WaitingAssign",

  /// <summary>
  /// เห็นชอบ
  /// </summary>
  Approved = "Approved",

  /// <summary>
  /// ส่งกลับแก้ไข
  /// </summary>
  Rejected = "Rejected",

  /// <summary>
  /// ยกเลิกรายการ
  /// </summary>
  Cancelled = "Cancelled",
}
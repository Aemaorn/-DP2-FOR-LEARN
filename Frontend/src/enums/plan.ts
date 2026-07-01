enum PlanStatus {
  All = "All",
  DraftPlan = "DraftPlan",
  EditPlan = "EditPlan",
  WaitingApprovePlan = "WaitingApprovePlan",
  RejectPlan = "RejectPlan",
  WaitingAssign = 'WaitingAssign',
  Assigned = "Assigned",
  DraftRecordDocument = "DraftRecordDocument",
  RejectToAssignee = 'RejectToAssignee',
  WaitingAcceptor = "WaitingAcceptor",
  ApprovePlan = "ApprovePlan",
  WaitingAnnouncement = "WaitingAnnouncement",
  Announcement = "Announcement",
  CancelPlan = "CancelPlan",
  Closed = "Closed",
};

enum PlanAction {
  RejectPlan = "RejectPlan",
  EditPlan = "EditPlan",
  ApprovePlan = "ApprovePlan",
  AssignAssignee = "AssignAssignee",
  ApprovedAssignee = "ApprovedAssignee",
  AssignAcceptor = "AssignAcceptor",
  ConfirmAcceptor = "ConfirmAcceptor",
  ApprovedAcceptor = "ApprovedAcceptor",
  RecallDocument = "RecallDocument",
  RejectedAcceptor = "RejectedAcceptor",
  Announcement = "Announcement",
  AssigneeRejected = 'AssigneeRejected',
  ClosePlan = "ClosePlan",
  CancelClosePlan = "CancelClosePlan",
}

enum PlanDepartmentCode {
  /**
  * - ฝ่ายสื่อสารองค์กร
  */
  CCD = '50004450',
  /**
  *   /**
  * - ฝ่ายสื่อสารการตลาด
  */
  MCD = '50004639',
  /**
   * ฝ่ายบริหารสำนักงานและกิจการสาขา
   */
  OABAD = '50004691',
  /**
   * ฝ่ายบริหาร NPA
   */
  NMD = '50004688',
  /**
   * ฝ่ายบริหารหนี้ภูมิภาค 1
   */
  RDMD1 = '50004684',
  /**
   * ฝ่ายบริหารหนี้ภูมิภาค 2
   */
  RDMD2 = '50000804',
  /**
   * ฝ่ายจัดหาและพัสดุ
   */
  JP = '50004689',
  /**
   * ลาออก หรือย้าย หรือไม่ใช้งาน
   */
  InActive = '00000000',

  /**
   * ส่วนบัญชีค่าใช้จ่าย
   */
  Accounting = '88811'
}

export {
  PlanStatus,
  PlanAction,
  PlanDepartmentCode,
}

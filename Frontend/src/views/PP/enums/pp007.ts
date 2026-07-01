enum PurchaseOrderStatus {
  /// <summary>
  /// แบบร่าง
  /// </summary>
  Draft = "Draft",

  /// <summary>
  /// เรียกคืนแก้ไข
  /// </summary>
  Edit = "Edit",

  /// <summary>
  /// อยู่ระหว่าง คกก. เห็นชอบ
  /// </summary>
  WaitingCommitteeApproval = "WaitingCommitteeApproval",

  /// <summary>
  /// รออนุมัติ
  /// </summary>
  WaitingApproval = "WaitingApproval",

  WaitingAssign = "WaitingAssign",

  /// <summary>
  /// อนุมัติ
  /// </summary>
  Approved = "Approved",

  WaitingComment = "WaitingComment",

  /// <summary>
  /// ส่งกลับแก้ไข
  /// </summary>
  Rejected = "Rejected",

  RejectToAssignee = "RejectToAssignee",

  /// <summary>
  /// ยกเลิกรายการ
  /// </summary>
  Cancelled = "Cancelled",
};

export {
  PurchaseOrderStatus
};
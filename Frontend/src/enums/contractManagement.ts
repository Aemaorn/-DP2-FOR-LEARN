enum ContractManagementStep {
  /**
   * บันทึกส่งมอบ และตรวจรับ
   */
  DeliveryAcceptance = 'DeliveryAcceptance',
  /**
   * ขออนุมัติเบิกจ่าย
   */
  DisbursementApproval = 'DisbursementApproval',
  /**
   * บอกเลิกสัญญา
   */
  ContractTermination = 'ContractTermination',
  /**
   * รายการคืนหลักประกันสัญญา
   */
  ContractGuaranteeReturn = 'ContractGuaranteeReturn',
}

enum ContractManagementStatus {
  /// <summary>
  /// แบบร่าง
  /// </summary>
  Draft = 'Draft',

  /// <summary>
  /// รออนุมัติ
  /// </summary>
  Pending = 'Pending',

  /// <summary>
  /// รออนุมัติ
  /// </summary>
  WaitingApproval = 'WaitingApproval',

  Edit = 'Edit',

  /// <summary>
  /// อนุมัติแล้ว
  /// </summary>
  Approved = 'Approved',

  /// <summary>
  /// ปฏิเสธ
  /// </summary>
  Rejected = 'Rejected',

  /// <summary>
  /// รอมอบหมาย
  /// </summary>
  WaitingCommitteeApproval = 'WaitingCommitteeApproval',

  /// <summary>
  /// รอมอบหมายงาน
  /// </summary>
  WaitingAssign = 'WaitingAssign',

  /// <summary>
  /// ส่งกลับแก้ไขไปยังผู้รับมอบหมาย
  /// </summary>
  RejectToAssignee = 'RejectToAssignee',

  /// <summary>
  /// รอความเห็น
  /// </summary>
  WaitingComment = 'WaitingComment',

  /// <summary>
  /// รอตรวจรับ
  /// </summary>
  WaitingAcceptance = 'WaitingAcceptance',
}

export { ContractManagementStep, ContractManagementStatus };

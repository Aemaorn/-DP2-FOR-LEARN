export enum souceType {
  Plan = 'Plan',
  ContractDraftVendor = 'ContractDraftVendor',
  Procurement = 'Procurement',
  ContractDraftVendorEdit = 'ContractDraftVendorEdit',
  Manual = 'Manual',
}

export enum CM001Status {
  All = 'All',
  InProgress = 'InProgress',
  Completed = 'Completed',
};

export enum CM001PeriodStatus {
  /**
   * Draft -> (แบบร่าง)
   */
  Draft = 'Draft',
  /**
   * WaitingCommitteeApproval -> (อยู่ระหว่างบุคคล/คณะกรรมการเห็นชอบอนุมัติ)
   */
  WaitingCommitteeApproval = 'WaitingCommitteeApproval',
  /**
   * WaitingAssign -> (ยืนยันมอบหมายงาน)
   */
  WaitingAssign = 'WaitingAssign',
  /**
   * WaitingAssign -> (ยืนยันมอบหมายงาน)
   */
  WaitingComment = 'WaitingComment',
  /**
   * WaitingAcceptance -> (รอผู้มีอำนาจเห็นชอบ/อนุมัติ)
   */
  WaitingAcceptance = 'WaitingAcceptance',
  /**
   * Approved -> (อนุมัติ)
   */
  Approved = 'Approved',
  /**
   * Edit -> (เรียกคืนแก้ไข)
   */
  Edit = 'Edit',
  /**
   * Rejected -> (ส่งกลับแก้ไข)
   */
  Rejected = 'Rejected',
  /**
   * RejectToAssignee -> (ส่งกลับแก้ไข)
   */
  RejectToAssignee = 'RejectToAssignee',
};

export enum CmDeliveryAcceptancePeriodAccountStatus {
  WaitingAccountingApproval = 'WaitingAccountingApproval',
  AccountingRejected = 'AccountingRejected',
  WaitingDisbursementDate = 'WaitingDisbursementDate',
  Paid = 'Paid',
}

export enum CM001PeriodTabHeader {
  Detail = 'Detail',
  Document = 'Document',
}

export enum CM001PeriodAccordion {
  Committee = 'Committee',
  Assignee = 'Assignee',
  Acceptor = 'Acceptor',
  Accounting = 'Accounting',
  AccountingConfirmer = 'AccountingConfirmer',
}
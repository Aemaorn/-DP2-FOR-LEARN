enum Pcm007Status {
  All = 'All',
  Draft = 'Draft',
  Edit = 'Edit',
  WaitingApproval = 'WaitingApproval',
  WaitingCommitteeApprove = 'WaitingCommitteeApprove',
  WaitingAccounting = 'WaitingAccounting',
  WaitingDisbursementDate = 'WaitingDisbursementDate',
  Paid = 'Paid',
  Rejected = 'Rejected',
}

enum Pcm007Action {
  ApproveAcceptor = 'ApproveAcceptor',
  RejectAcceptor = 'RejectAcceptor',
  CommitteeApprove = 'CommitteeApprove',
  CommitteeReject = 'CommitteeReject',
  AccountingApprove = 'AccountingApprove',
  AccountingReject = 'AccountingReject',
}

enum Pcm007CommitteeType {
  ProcurementCommittee = 'ProcurementCommittee',
  InspectionCommittee = 'InspectionCommittee',
}

export { Pcm007Status, Pcm007Action, Pcm007CommitteeType }

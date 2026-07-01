enum PrincipleStatus {
  Draft = 'Draft',
  Rejected = 'Rejected',
  Edit = 'Edit',
  WaitingUnitApproval = 'WaitingUnitApproval',
  RejectToAssignee = 'RejectToAssignee',
  WaitingAssign = 'WaitingAssign',
  WaitingComment = 'WaitingComment',
  WaitingAcceptance = 'WaitingAcceptance',
  Approved = 'Approved',
}

enum CommitteeGroupType {
  RentCommittee = 'RentCommittee',
  AcceptanceCommittee = 'AcceptanceCommittee',
}

enum PerformanceResultGroup {
  DepositRemaining = 'DepositRemaining',
  LoanExisting = 'LoanExisting',
  LoanNew = 'LoanNew',
}

enum RentalAnalysisType {
  General = "General",
  ProfitAndLoss = "ProfitAndLoss",
}

enum CommitteePositions {
  PosBoard006 = 'PosBoard006', //ผู้จัดทำ
  PosBoard001 = 'PosBoard001', //ประทานกรรมการ
}

export {
  PrincipleStatus,
  CommitteeGroupType,
  PerformanceResultGroup,
  RentalAnalysisType,
  CommitteePositions,
}
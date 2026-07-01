enum PrincipleApprovalRentalStatus {
  Draft = 'Draft',
  Rejected = 'Rejected',
  Edit = 'Edit',
  WaitingCommitteeApproval = 'WaitingCommitteeApproval',
  RejectToAssignee = 'RejectToAssignee',
  WaitingAssign = 'WaitingAssign',
  WaitingComment = 'WaitingComment',
  WaitingAcceptance = 'WaitingAcceptance',
  Approved = 'Approved',
  WaitingContractAssign = 'WaitingContractAssign',
  ContractAssigned = 'ContractAssigned',
  Cancelled = 'Cancelled',
}

enum UseContractType {
  CentralContract = 'CentralContract',
  Vendor = 'Vendor',
}

export {
  PrincipleApprovalRentalStatus,
  UseContractType,
}
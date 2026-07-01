enum Pcm003Status {
  All = "All",
  Draft = "Draft",
  WaitingApproval = "WaitingApproval",
  Edit = "Edit",
  // Approved = "Approved",
  Rejected = "Rejected",
  WaitingAccountingApproval = "WaitingAccountingApproval",
  WaitingDisbursementDate = "WaitingDisbursementDate",
  Paid = "Paid"
};

enum Pcm003Action {
  Draft = "Draft",
  Edit = "Edit",
  Recall = "Recall",
  RequestApproval = "RequestApproval",
  ApprovedAcceptor = "ApprovedAcceptor",
  RejectedAcceptor = "RejectedAcceptor",
  ConfirmDisbursement = "ConfirmDisbursement",
}

export {
  Pcm003Status,
  Pcm003Action,
}
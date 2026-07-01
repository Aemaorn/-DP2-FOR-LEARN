enum Pcm002Status {
  All = "All",
  Draft = "Draft",
  Edit = "Edit",
  WaitingApproval = "WaitingApproval",
  // Approved = "Approved",
  Rejected = "Rejected",
  WaitingAccountingApproval = "WaitingAccountingApproval",
  WaitingDisbursementDate = "WaitingDisbursementDate",
  Paid = "Paid"
};

enum Pcm002Action {
  Draft = "Draft",
  Edit = "Edit",
  Recall = "Recall",
  RequestApproval = "RequestApproval",
  ApprovedAcceptor = "ApprovedAcceptor",
  RejectedAcceptor = "RejectedAcceptor",
  ConfirmDisbursement = "ConfirmDisbursement",
}

export {
  Pcm002Status,
  Pcm002Action,
}
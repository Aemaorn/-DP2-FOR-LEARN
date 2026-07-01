enum PP006Status {
  Draft = "Draft",
  WaitingApproval = "WaitingApproval",
  Edit = "Edit",
  Rejected = "Rejected",
  Approved = "Approved",
  NotInvited = "NotInvited",
}

enum PP006SearchType {
  TaxID = "TaxID",
  Name = "Name",
}

enum PP006UserType {
  ShareHolder = "shareHolder",
  Director = "director",
}

enum PP006EntrepreneurType {
  COI = 'COI',
  Watchlist = 'Watchlist',
  EGP = 'e-GP',
}

enum QualificationResult {
  Pass = "Pass",
  Fail = "Fail",
  UnKnow = "UnKnow",
}

export {
  PP006Status,
  PP006SearchType,
  PP006UserType,
  PP006EntrepreneurType,
  QualificationResult
}
enum Pcm004Status {
  All = "All",
  Draft = "Draft",
  Edit = "Edit",
  WaitingApproval = "WaitingApproval",
  WaitingForInspector = "WaitingForInspector",
  WaitingForAssignment = "WaitingForAssignment",
  WaitingForCompletion = "WaitingForCompletion",
  Completed = "Completed",
  Rejected = "Rejected",
};

enum CashType {
  Standard = "Standard",
  Convenient = "Convenient",
}

enum Pcm004Action {
  Draft = "Draft",
  Edit = "Edit",
  Recall = "Recall",
  RequestToDirectorAgree = "RequestToDirectorAgree",
  DirectorAgreeApproved = "DirectorAgreeApproved",
  DirectorAgreeRejected = "DirectorAgreeRejected",
  InspectionCommitteeApproved = "InspectionCommitteeApproved",
  InspectionCommitteeRejected = "InspectionCommitteeRejected",
  Assignment = "Assignment",
  ConfirmAssignment = "ConfirmAssignment",
  ConfirmCompleted = "ConfirmCompleted",
  SendToAssignee = "SendToAssignee",
  AssigneeRejected = "AssigneeRejected",
}

enum Pcm004CommitteeType {
  /// <summary>
  /// ผู้ขอซื้อขอจ้าง
  /// </summary>
  ProcurementCommittee = `ProcurementCommittee`,

  /// <summary>
  /// ผู้ตรวจรับพัสดุ
  /// </summary>
  InspectionCommittee = `InspectionCommittee`,
}

export {
  Pcm004Status,
  Pcm004Action,
  Pcm004CommitteeType,
  CashType,
}
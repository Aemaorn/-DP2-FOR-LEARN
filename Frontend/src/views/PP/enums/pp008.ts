enum PP008Status {
  Draft = 'Draft',
  Edit = 'Edit',
  WaitingApproval = 'WaitingApproval',
  WaitingAssign = 'WaitingAssign',
  Rejected = 'Rejected',
  Assigned = 'Assigned',
}

enum PP008SearchType {
  TaxID = "TaxID",
  Name = "Name",
}

export {
  PP008Status,
  PP008SearchType,
}

export enum pp008CommitteeType {
  /// <summary>
  /// ผู้ตรวจรับพัสดุ-คณะกรรมการตรวจรับพัสดุ
  /// </summary>
  InspectionCommittee = `InspectionCommittee`,
}

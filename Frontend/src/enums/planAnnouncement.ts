enum PlanAnnouncementAction {
  /// เรียกคืนแก้ไข
  Recall = "Recall",
  /// ผู้มีอำนาจเห็นชอบ/อนุมัติ (ส่งกลับแก้ไข)
  AcceptorReject = "AcceptorReject",
  /// ผู้มีอำนาจเห็นชอบ/อนุมัติ (อนุมัติ)
  AcceptorApprove = "AcceptorApprove",
  /// ผอ.จพ. เผยแพร่แผน
  DirectorAnnouncement = "DirectorAnnouncement",
}

enum PlanAnnouncementStatus {
  All = "All",
  /// แบบร่าง
  Draft = "Draft",
  /// จพ. มอบหมายงาน
  WaitingAssign = "WaitingAssign",
  /// รออนุมัติ
  WaitingAcceptor = "WaitingAcceptor",
  /// อนุมัติ
  WaitingAnnouncement = "WaitingAnnouncement",
  /// ประกาศเผยแพร่แผน
  Announcement = "Announcement",
  /// ส่งกลับแก้ไข
  Rejected = "Rejected",
  /// ยกเลิกรายการ
  Cancelled = "Cancelled",
}

export {
  PlanAnnouncementAction,
  PlanAnnouncementStatus
}
namespace GHB.DP2.Domain.Common;

using LanguageExt;

public static class ActivityLogActionTypeConstant
{
    public const string Create = "สร้างข้อมูล";
    public const string Update = "แก้ไขข้อมูล";
    public const string SendHeadApprove = "ส่งหัวหน้าส่วนเห็นชอบ/อนุมัติ";
    public const string SendCommitteeApprove = "ส่งคณะกรรมการเห็นชอบ/อนุมัติ";
    public const string SendUnitApprove = "ส่งสายงานเห็นชอบ/อนุมัติ";
    public const string SendApprove = "ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ";
    public const string SendApproveDepartment = "ส่งฝ่ายเห็นชอบ";
    public const string SendApproveSegment = "ส่งส่วนงานเห็นชอบ";
    public const string WaitingAssign = "รอมอบหมายผู้รับผิดชอบ";
    public const string WaitingComment = "รอเจ้าหน้าที่พัสดุให้ความเห็น";
    public const string Reject = "ผู้มีอำนาจเห็นชอบส่งกลับแก้ไข";
    public const string DepartmentReject = "ฝ่ายเห็นชอบส่งกลับแก้ไข";
    public const string CommitteeReject = "คณะกรรมการไม่เห็นชอบ/ส่งกลับแก้ไข";
    public const string AssigneeReject = "ผู้รับผิดชอบส่งกลับแก้ไข";
    public const string SegmentReject = "ส่วนงานส่งกลับแก้ไข";
    public const string HeadReject = "หัวหน้าส่วนส่งกลับแก้ไข";
    public const string Recall = "เรียกคืนแก้ไข";
    public const string CommitteeApproved = "คณะกรรมการเห็นชอบ/อนุมัติ";
    public const string UnitApproved = "สายงานเห็นชอบ/อนุมัติ";
    public const string ApprovedSegment = "ส่วนงานเห็นชอบ";
    public const string Approved = "ผู้มีอำนาจเห็นชอบ/อนุมัติ";
    public const string ApprovedDepartment = "ฝ่ายเห็นชอบ";
    public const string ApprovedHead = "หัวหน้าส่วนเห็นชอบ/อนุมัติ";
    public const string Assign = "มอบหมาย";
    public const string Assigned = "ยืนยันมอบหมาย";
    public const string Cancelled = "ขอยกเลิกสำเร็จ";
    public const string Changed = "ขอเปลี่ยนแปลงสำเร็จ";
    public const string RequestCancel = "ขอยกเลิก";
    public const string RequestChange = "ขอเปลี่ยนแปลง";
    public const string Delete = "ลบข้อมูล";
    public const string Comment = "เจ้าหน้าที่ให้ความเห็น";
    public const string WaitingAnnouncement = "อนุมัติ/รอเผยแพร่";
    public const string Announcement = "ประกาศเผยแพร่";
    public const string SendChange = "ส่งขอเปลี่ยนแปลงข้อมูล";
    public const string SendCancel = "ส่งขอยกเลิก";
    public const string SendEdit = "ส่งกลับแก้ไข";
    public const string RestoreState = "ยกเลิกคำขอเปลี่ยนแปลง/ยกเลิก";
    public const string SubmitToAccounting = "ส่งข้อมูลไปยังฝ่ายการเงินและบัญชี";
    public const string AccountingApproved = "ฝ่ายบัญชีเห็นชอบ/อนุมัติ";
    public const string AccountingReject = "ฝ่ายบัญชีส่งกลับแก้ไข";
    public const string DisbursementPaid = "เบิกจ่ายเงินเรียบร้อยแล้ว";
    public const string ConfirmDisbursement = "ยืนยันเบิกจ่าย";
    public const string SendEmail = "ส่งอีเมล";
    public const string UploadFile = "อัปโหลดไฟล์";
    public const string DeleteFile = "ลบไฟล์";
    public const string AddAcceptor = "เพิ่มรายชื่อ";
    public const string RemoveAcceptor = "ลบรายชื่อ";
    public const string Closed = "ปิดงาน";
    public const string CancelClosed = "ยกเลิกปิดงาน";
}

public record ActivityInfo(
    string Type,
    string Action,
    string Status,
    string? Remark = null,
    string? ProgramCode = null,
    string? Ip = null);

public class ActivityLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Key { get; set; } = default!;

    public ActivityInfo ActivityInfo { get; set; } = default!;

    public AuditInfo AuditInfo { get; set; } = default!;

    // Parameterless constructor for EF Core
    public ActivityLog()
    {
    }

    // Constructor for domain usage
    public ActivityLog(string key, ActivityInfo activityInfo, AuditInfo auditInfo)
    {
        this.Key = key;
        this.ActivityInfo = activityInfo;
        this.AuditInfo = auditInfo;
    }

    // Copy constructor
    public ActivityLog(ActivityLog original)
    {
        this.Key = original.Key;
        this.ActivityInfo = original.ActivityInfo;
        this.AuditInfo = original.AuditInfo;
    }
}

public interface IHasActivityInfo
{
    IReadOnlyCollection<ActivityInfo> Activities { get; }

    Unit AddActivity(ActivityInfo activityInfo);

    Unit ClearActivity();
}
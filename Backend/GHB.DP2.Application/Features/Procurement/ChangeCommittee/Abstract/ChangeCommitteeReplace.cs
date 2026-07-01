namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee.Abstract;

using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Domain.Common;
using System.ComponentModel;

public record CommitteeChangeReplaceDto(
    [property: Description("อำนาจอนุมัติตามคำสั่งธนาคาร")]
    string? CommandText,
    [property: Description("หมายเลขโทรศัพท์")]
    string? TelephoneNumber,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementReplaceDto Procurement,
    [property: Description("ผู้อนุมัติ")]
    IEnumerable<AcceptorReplace>? Acceptors,
    [property: Description("วันที่เอกสาร")]
    string? AcceptorDate,
    string? CommitteesTypeName,
    string? Remark,
    string? SourceType,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติ")]
    IEnumerable<SectionApprove>? SectionApproveName,
    IEnumerable<CommitteeReplace> OldCommittees,
    IEnumerable<CommitteeReplace> NewCommittees,
    [property: Description("ตารางเปรียบเทียบคณะกรรมการชุดเดิมและชุดใหม่")]
    IEnumerable<CommitteeChangeRowReplace> CommitteeRows,
    [property: Description("คณะกรรมการ")]
    IEnumerable<AcceptorReplace>? Committees,
    [property: Description("ผู้รับมอบหมาย")]
    JorPorCommentReplace? JorPorComment);

public record CommitteeChangeRowReplace(
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("ชื่อคณะกรรมการชุดเดิม")]
    string? OldFullName,
    [property: Description("ตำแหน่งจริงคณะกรรมการชุดเดิม")]
    string? OldFullPositionName,
    [property: Description("ตำแหน่งในคณะกรรมการชุดเดิม")]
    string? OldPositionOnBoard,
    [property: Description("ชื่อคณะกรรมการชุดใหม่")]
    string? NewFullName,
    [property: Description("ตำแหน่งจริงคณะกรรมการชุดใหม่")]
    string? NewFullPositionName,
    [property: Description("ตำแหน่งในคณะกรรมการชุดใหม่")]
    string? NewPositionOnBoard,
    [property: Description("หมายเหตุ")]
    string? Remark);

public record CommitteeReplace(
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("การดำเนินการ")]
    string Action,
    [property: Description("คณะกรรมการหรือผู้จัดทำ เห็นชอบ")]
    string Signature,
    [property: Description("ชื่อคณะกรรมการหรือผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งจริง คณะกรรมการ หรือผู้จัดทำ")]
    string? FullPositionName,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? PositionOnBoard,
    [property: Description("หมายเหตุ")]
    string? Remark);

public record AcceptorReplace(
    [property: Description("รหัสผู้อนุมัติ")]
    Guid Id,
    [property: Description("ประเภทผู้อนุมัติ")]
    AcceptorType AcceptorType,
    [property: Description("รหัสผู้ใช้")] Guid UserId,
    [property: Description("รหัสพนักงาน")] string EmployeeCode,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("หน่วยงาน")] string DepartmentName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รหัสผู้รับมอบอำนาจ")]
    Guid? DelegateeId,
    [property: Description("สถานะการอนุมัติ")]
    AcceptorStatus Status,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน ผู้เห็นชอบ หรืออนุมัติ")]
    string? Delegate,
    [property: Description("ลายเซ็นต์")]
    string Signature,
    [property: Description("การดำเนินการ")]
    string Action,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? PositionOnBoard);

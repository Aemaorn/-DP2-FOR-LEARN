namespace GHB.DP2.Application.Dtos;

using System.ComponentModel;
using GHB.DP2.Domain.Common;

public record AssigneeRequest(
    Guid? Id,
    AssigneeGroup AssigneeGroup,
    AssigneeType AssigneeType,
    Guid UserId,
    int Sequence);

public record AssigneeResponseBase<T>(
    [property: Description("รหัสผู้รับมอบหมาย")]
    T Id,
    [property: Description("กลุ่มผู้รับมอบหมาย")]
    AssigneeGroup AssigneeGroup,
    [property: Description("ประเภทผู้รับมอบหมาย")]
    AssigneeType AssigneeType,
    [property: Description("รหัสผู้ใช้")]
    Guid UserId,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("ชื่อ-สกุล")]
    string FullName,
    [property: Description("ตำแหน่ง")]
    string PositionName,
    [property: Description("หน่วยงาน")]
    string DepartmentName,
    [property: Description("สถานะการมอบหมาย")]
    AssigneeStatus Status,
    [property: Description("หมายเหตุ")]
    string? Remark = default,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt = default,
    [property: Description("รหัสผู้ปฏิบัติหน้าที่แทนผู้รับมอบหมาย")]
    Guid? DelegateeUserId = default);

public record AssigneeResponse(
    [param: Description("รหัสผู้รับมอบหมาย")]
    Guid? Id,
    [param: Description("กลุ่มผู้รับมอบหมาย")]
    AssigneeGroup AssigneeGroup,
    [param: Description("ประเภทผู้รับมอบหมาย")]
    AssigneeType AssigneeType,
    [param: Description("รหัสผู้ใช้")]
    Guid UserId,
    [param: Description("ลำดับ")]
    int Sequence,
    [param: Description("ชื่อ-สกุล")]
    string FullName,
    [param: Description("ตำแหน่ง")]
    string PositionName,
    [param: Description("หน่วยงาน")]
    string DepartmentName,
    [param: Description("สถานะการมอบหมาย")]
    AssigneeStatus Status,
    [param: Description("หมายเหตุ")]
    string? Remark = default,
    [param: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt = default,
    [param: Description("รหัสผู้ปฏิบัติหน้าที่แทนผู้รับมอบหมาย")]
    Guid? DelegateeUserId = default)
    : AssigneeResponseBase<Guid?>(Id, AssigneeGroup, AssigneeType, UserId, Sequence, FullName, PositionName, DepartmentName, Status, Remark, ActionAt, DelegateeUserId);

public record AssigneeNoIdResponse(
    [param: Description("รหัสผู้รับมอบหมาย")]
    Guid? Id,
    [param: Description("กลุ่มผู้รับมอบหมาย")]
    AssigneeGroup AssigneeGroup,
    [param: Description("ประเภทผู้รับมอบหมาย")]
    AssigneeType AssigneeType,
    [param: Description("รหัสผู้ใช้")]
    Guid UserId,
    [param: Description("ลำดับ")]
    int Sequence,
    [param: Description("ชื่อ-สกุล")]
    string FullName,
    [param: Description("ตำแหน่ง")]
    string PositionName,
    [param: Description("หน่วยงาน")]
    string DepartmentName,
    [param: Description("สถานะการมอบหมาย")]
    AssigneeStatus Status,
    [param: Description("หมายเหตุ")]
    string? Remark = default,
    [param: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt = default,
    [param: Description("รหัสผู้ปฏิบัติหน้าที่แทนผู้รับมอบหมาย")]
    Guid? DelegateeUserId = default)
    : AssigneeResponseBase<Guid?>(Id, AssigneeGroup, AssigneeType, UserId, Sequence, FullName, PositionName, DepartmentName, Status, Remark, ActionAt, DelegateeUserId);

public record JorPorCommentReplace(
    [property: Description("รหัสผู้ใช้งาน")] Guid UserId,
    [property: Description("ลายเซ็นต์")] string Signature,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ความเห็น จพ.")] string? Remark,
    [property: Description("การดำเนินการ")] string Action);
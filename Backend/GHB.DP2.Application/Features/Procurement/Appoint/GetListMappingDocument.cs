namespace GHB.DP2.Application.Features.Procurement.Appoint;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record AppointMappingRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id);

public record AppointReplaceDto(
    [property: Description("เหตุผลในการแก้ไข")]
    string? Reason,
    [property: Description("เหตุผลในการแก้ไข")]
    string? Remark,
    [property: Description("วันที่แต่งตั้ง")]
    string? MemorandumDate,
    [property: Description("คำสั่งอนุมัติ")]
    string? CommandText,
    [property: Description("รูปแบบคณะกรรมการ หรือผู้จัดทำ ขอบเขตงาน")]
    string? CommitteeTorSection,
    [property: Description("รูปแบบคณะกรรมการ หรือผู้จัดทำ ราคากลาง")]
    string? CommitteeMedianPriceSection,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติ")]
    IEnumerable<SectionApprove>? SectionApproveName,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementReplaceDto Procurement,
    [property: Description("ข้อมูลการแต่งตั้ง")]
    AppointReplace Appoint,
    [property: Description("รายชื่อคณะกรรมการร่าง TOR")]
    IEnumerable<AppointTorDraftCommitteeReplace> TorDraftCommittees,
    [property: Description("หน้าที่คณะกรรมการร่าง TOR")]
    IEnumerable<DutiesReplace> TorDraftCommitteeDuties,
    [property: Description("รายชื่อคณะกรรมการกำหนดราคากลาง")]
    IEnumerable<AppointMedianPriceCommitteeReplace> MedianPriceCommittees,
    [property: Description("หน้าที่คณะกรรมการกำหนดราคากลาง")]
    IEnumerable<DutiesReplace> MedianPriceCommitteeDuties,
    [property: Description("รายชื่อผู้อนุมัติ")]
    IEnumerable<AppointAcceptorReplace> Acceptors,
    [property: Description("รหัสเอกสารการแต่งตั้ง")]
    Guid? AppointDocumentId,
    [property: Description("เป็นคณะกรรมการร่างขอบเขตงาน")]
    bool IsTorCommittee,
    [property: Description("เป็นคณะกรรมการราคากลาง")]
    bool IsMedianPriceCommittee,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    AppointCreatorDto? Creator);

public record AppointCreatorDto(
    [property: Description("รหัสผู้ใช้งาน")] UserId UserId,
    [property: Description("ลายเซ็นต์")] string Signature,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? PositionOnBoard);

public record AppointReplace(
    [property: Description("รหัสการแต่งตั้ง")]
    Guid Id,
    [property: Description("รหัสการจัดซื้อ")]
    Guid ProcurementId,
    [property: Description("เลขที่การแต่งตั้ง")]
    string AppointNumber,
    [property: Description("วันที่บันทึกข้อความ")]
    string MemorandumDate,
    [property: Description("เลขที่บันทึกข้อความ")]
    string? MemorandumNumber,
    [property: Description("หมายเลขโทรศัพท์")]
    string? Telephone,
    [property: Description("เหตุผลการแต่งตั้ง")]
    string? Reason,
    [property: Description("สถานะการแต่งตั้ง")]
    AppointStatus Status,
    [property: Description("เหตุผลขอเปลี่ยนแปลง")]
    string? ChangeReason,
    [property: Description("เหตุผลขอยกเลิก")]
    string? CancelReason,
    [property: Description("ขอเปลี่ยนแปลง")]
    bool IsChange,
    [property: Description("ขอยกเลิก")] bool IsCancel);

public record AppointTorDraftCommitteeReplace(
    [property: Description("รหัสคณะกรรมการร่าง TOR")]
    Guid Id,
    [property: Description("รหัสผู้ใช้")] Guid UserId,
    [property: Description("ชื่อ-สกุล")] string? FullName,
    [property: Description("ชื่อตำแหน่งเต็ม")]
    string? FullPositionName,
    [property: Description("รหัสหน่วยงาน")]
    string? DepartmentCode,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ TOR")]
    string? PositionOnBoard,
    [property: Description("รหัสตำแหน่งในคณะกรรมการ")]
    string CommitteePositionsCode,
    [property: Description("ลำดับ")] int Sequence);

public record AppointMedianPriceCommitteeReplace(
    [property: Description("รหัสคณะกรรมการราคากลาง")]
    Guid Id,
    [property: Description("รหัสผู้ใช้")] Guid UserId,
    [property: Description("ชื่อ-สกุล")] string? FullName,
    [property: Description("ชื่อตำแหน่งเต็ม")]
    string? FullPositionName,
    [property: Description("รหัสหน่วยงาน")]
    string? DepartmentCode,
    [property: Description("รหัสตำแหน่งในคณะกรรมการ")]
    string CommitteePositionsCode,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ ราคากลาง")]
    string? PositionOnBoard,
    [property: Description("ลำดับ")] int Sequence);

public record DutiesReplace(
    [property: Description("รหัสหน้าที่")] Guid Id,
    [property: Description("รายละเอียดหน้าที่")]
    string Description,
    [property: Description("ลำดับ")] int Sequence);

public record AppointAcceptorReplace(
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
    [property: Description("สถานะการใช้งาน")]
    bool IsActive,
    [property: Description("เป็นผู้อนุมัติปัจจุบัน")]
    bool? IsCurrent,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน ผู้เห็นชอบ หรืออนุมัติ")]
    string? Delegate,
    [property: Description("ลายเซ็นต์")]
    string Signature,
    [property: Description("การดำเนินการ")]
    string Action);

public record AppointAcceptorDelegateReplace(
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
    [property: Description("สถานะการใช้งาน")]
    bool IsActive,
    [property: Description("เป็นผู้อนุมัติปัจจุบัน")]
    bool? IsCurrent);

public class GetListMappingAppointDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingAppointDocumentEndpoint(ILogger<GetListMappingAppointDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Appoint"));
        this.Get("appointments/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(AppointReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}
namespace GHB.DP2.Application.Features.Procurement.Appoint;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Appoint.Abstract;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetByIdRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id);

public record AppointDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record GetAppointResponseDto(
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementDto Procurement,
    [property: Description("ข้อมูลการแต่งตั้ง")]
    AppointResponseDto Appoint,
    [property: Description("รายชื่อคณะกรรมการร่าง TOR")]
    IEnumerable<AppointTorDraftCommitteeResponseDto> TorDraftCommittees,
    [property: Description("หน้าที่คณะกรรมการร่าง TOR")]
    IEnumerable<DutiesResponseDto> TorDraftCommitteeDuties,
    [property: Description("รายชื่อคณะกรรมการกำหนดราคากลาง")]
    IEnumerable<AppointMedianPriceCommitteeResponseDto> MedianPriceCommittees,
    [property: Description("หน้าที่คณะกรรมการกำหนดราคากลาง")]
    IEnumerable<DutiesResponseDto> MedianPriceCommitteeDuties,
    [property: Description("รายชื่อผู้อนุมัติ")]
    IEnumerable<AppointAcceptorResponseDto> Acceptors,
    [property: Description("รหัสเอกสารการแต่งตั้ง")]
    Guid? AppointDocumentId,
    [property: Description("รหัสเอกสารการแต่งตั้ง")]
    bool? IsAppointDocumentIdReplaced,
    [property: Description("ประวัติเอกสารการแต่งตั้ง")]
    AppointDocumentVersionResponse[] AppointDocumentVersions,
    [property: Description("เป็นคณะกรรมการร่างขอบเขตงาน")]
    bool IsTorCommittee,
    [property: Description("เป็นคณะกรรมการราคากลาง")]
    bool IsMedianPriceCommittee,
    [property: Description("มีสิทธิ์ในการจัดการข้อมูล")]
    bool HasPermission);

public record AppointResponseDto(
    [property: Description("รหัสการแต่งตั้ง")]
    Guid Id,
    [property: Description("รหัสการจัดซื้อ")]
    Guid ProcurementId,
    [property: Description("เลขที่การแต่งตั้ง")]
    string AppointNumber,
    [property: Description("วันที่บันทึกข้อความ")]
    DateTimeOffset MemorandumDate,
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
    [property: Description("ขอยกเลิก")]
    bool IsCancel);

public record AppointTorDraftCommitteeResponseDto(
    [property: Description("รหัสคณะกรรมการร่าง TOR")]
    Guid Id,
    [property: Description("รหัสผู้ใช้")] Guid UserId,
    [property: Description("ชื่อ-สกุล")] string? FullName,
    [property: Description("ชื่อตำแหน่งเต็ม")]
    string? FullPositionName,
    [property: Description("รหัสหน่วยงาน")]
    BusinessUnitId? DepartmentCode,
    [property: Description("รหัสตำแหน่งในคณะกรรมการ")]
    string CommitteePositionsCode,
    [property: Description("ลำดับ")] int Sequence);

public record AppointMedianPriceCommitteeResponseDto(
    [property: Description("รหัสคณะกรรมการราคากลาง")]
    Guid Id,
    [property: Description("รหัสผู้ใช้")] Guid UserId,
    [property: Description("ชื่อ-สกุล")] string? FullName,
    [property: Description("ชื่อตำแหน่งเต็ม")]
    string? FullPositionName,
    [property: Description("รหัสหน่วยงาน")]
    BusinessUnitId? DepartmentCode,
    [property: Description("รหัสตำแหน่งในคณะกรรมการ")]
    string CommitteePositionsCode,
    [property: Description("ลำดับ")] int Sequence);

public record DutiesResponseDto(
    [property: Description("รหัสหน้าที่")] Guid Id,
    [property: Description("รายละเอียดหน้าที่")]
    string Description,
    [property: Description("ลำดับ")] int Sequence);

public record AppointAcceptorResponseDto(
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
    [property: Description("รหัสผู้ใช้งานผู้ปฏิบัติหน้าที่แทน")]
    Guid? DelegateeUserId);

public class GetByIdRequestValidator : Validator<GetByIdRequest>
{
    public GetByIdRequestValidator()
    {
        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required.");
    }
}

public class GetByIdEndpoint : AppointEndpointBase<GetByIdRequest, Results<Ok<GetAppointResponseDto>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetByIdEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<GetByIdEndpoint> logger)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Appoint"));
        this.Get("appointments/{id}");
    }

    protected override async ValueTask<Results<Ok<GetAppointResponseDto>, NotFound<string>>> HandleRequestAsync(GetByIdRequest req, CancellationToken ct)
    {
        var appointId = PpAppointId.From(req.Id);

        var appoint = await this.dbContext.PpAppoints
                                .AsNoTracking()
                                .Include(x => x.TorDraftCommittees)
                                .ThenInclude(ppAppointTorDraftCommittee => ppAppointTorDraftCommittee.User)
                                .ThenInclude(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .Include(x => x.TorDraftCommitteeDuties)
                                .Include(x => x.MedianPriceCommittees)
                                .ThenInclude(ppAppointMedianPriceCommittee => ppAppointMedianPriceCommittee.User)
                                .ThenInclude(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .Include(x => x.MedianPriceCommitteeDuties)
                                .Include(x => x.Acceptors)
                                .ThenInclude(p => p.User)
                                .ThenInclude(p => p.Employee)
                                .Include(ppAppoint => ppAppoint.DocumentHistories)
                                .Include(auditableEntity => auditableEntity.AuditInfo)
                                .Include(ppAppoint => ppAppoint.Procurement)
                                .ThenInclude(procurement => procurement.Department)
                                .Include(ppAppoint => ppAppoint.Procurement)
                                .ThenInclude(procurement => procurement.SupplyMethod)
                                .Include(ppAppoint => ppAppoint.Procurement)
                                .ThenInclude(procurement => procurement.SupplyMethodType)
                                .Include(ppAppoint => ppAppoint.Procurement)
                                .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                                .Include(ppAppoint => ppAppoint.Procurement)
                                .ThenInclude(procurement => procurement.Plan)
                                .AsSplitQuery()
                                .FirstOrDefaultAsync(x => x.Id == appointId, ct);

        if (appoint is null)
        {
            return TypedResults.NotFound($"Appointment with ID {req.Id} not found.");
        }

        var response = this.MapToResponseDto(appoint, req.UserId);

        return TypedResults.Ok(response);
    }
}
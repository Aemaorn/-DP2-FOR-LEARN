namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

public record GetChangeCommitteeByIdRequest(Guid Id);

public record ChangeCommitteeAcceptorResponseDto(
    [property: Description("รหัสผู้อนุมัติ")]
    Guid Id,
    [property: Description("ประเภทผู้อนุมัติ")]
    AcceptorType AcceptorType,
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("ชื่อ-สกุล")]
    string FullName,
    [property: Description("ตำแหน่ง")]
    string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("สถานะการอนุมัติ")]
    AcceptorStatus Status,
    [property: Description("หมายเหตุ")]
    string? Remark,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt,
    [property: Description("รหัสตำแหน่งคณะกรรมการ")]
    string? CommitteePositionsCode,
    [property: Description("ชื่อตำแหน่งคณะกรรมการ")]
    string? CommitteePositionName,
    [property: Description("ไม่สามารถปฏิบัติหน้าที่ได้")]
    bool? IsUnableToPerformDuties,
    [property: Description("เป็นผู้อนุมัติปัจจุบัน")]
    bool IsCurrent,
    [property: Description("รหัสหน่วยงาน")]
    string? DepartmentCode,
    [property: Description("รหัสผู้ใช้งานผู้ปฏิบัติหน้าที่แทน")]
    Guid? DelegateeUserId);

public record ChangeCommitteeProcurement(
    Guid? PlanId,
    string? PlanNumber,
    string? PlanType,
    string? DepartmentName,
    string? PlanName,
    decimal? Budget,
    decimal? BudgetYear,
    string? SupplyMethod,
    ParameterCode? SupplyMethodCode,
    string? SupplyMethodType,
    ParameterCode? SupplyMethodTypeCode,
    string? SupplyMethodSpecialType,
    ParameterCode? SupplyMethodSpecialTypeCode,
    bool IsStock,
    bool IsCommercialMaterial);

public record ChangeCommitteeResponseDto(
    Guid Id,
    Guid ProcurementId,
    string? ProcurementType,
    ChangeCommitteeProcurement Procurement,
    SourceType SourceType,
    Guid SourceId,
    CommitteeType CommitteeType,
    CommitteeChangeStatus Status,
    IEnumerable<CommitteeMember> OldCommittees,
    IEnumerable<CommitteeMember> NewCommittees,
    string? Remark,
    IEnumerable<ChangeCommitteeAcceptorResponseDto> Acceptors,
    IEnumerable<ChangeCommitteeAttachmentsResponseDto> Attachments,
    Guid? DocumentId,
    ChangeCommitteeDocumentVersionResponse[] DocumentVersions,
    bool IsJorPorComment,
    IEnumerable<AssigneeResponse> Assignees,
    [property: Description("วันที่เอกสาร")]
    DateTimeOffset? DocumentDate = null);

public record ChangeCommitteeDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record ChangeCommitteeAttachmentsResponseDto(
    CommitteeChangeAttachmentId Id,
    string DocumentTypeCode,
    int Sequence,
    string? Remark,
    IEnumerable<ChangeCommitteeFileAttachmentsResponse> FileAttachments);

public record ChangeCommitteeFileAttachmentsResponse(
    CommitteeChangeAttachmentInfoId Id,
    Guid FileId,
    string FileName,
    int Sequence,
    bool IsPublic,
    Guid CreatedBy);

public class GetChangeCommitteeByIdEndpoint : EndpointBase<GetChangeCommitteeByIdRequest, Results<Ok<ChangeCommitteeResponseDto>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetChangeCommitteeByIdEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetChangeCommitteeByIdEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ChangeCommittee"));
        this.Get("change-committee/{id}");
    }

    protected override async ValueTask<Results<Ok<ChangeCommitteeResponseDto>, NotFound<string>>> HandleRequestAsync(GetChangeCommitteeByIdRequest req, CancellationToken ct)
    {
        var changeCommitteeId = CommitteeChangeId.From(req.Id);

        var changeCommittee = await this.dbContext.CommitteeChanges
                                        .Include(c => c.Acceptors)
                                        .ThenInclude(a => a.User)
                                        .ThenInclude(u => u.Employee)
                                        .ThenInclude(e => e.View)
                                        .Include(c => c.Procurement)
                                        .ThenInclude(p => p.Plan)
                                        .Include(c => c.Procurement)
                                        .ThenInclude(p => p.Department)
                                        .Include(c => c.Procurement)
                                        .ThenInclude(p => p.SupplyMethod)
                                        .Include(c => c.Procurement)
                                        .ThenInclude(p => p.SupplyMethodType)
                                        .Include(c => c.Procurement)
                                        .ThenInclude(p => p.SupplyMethodSpecialType)
                                        .Include(c => c.DocumentHistories)
                                        .Include(c => c.Assignees)
                                        .FirstOrDefaultAsync(x => x.Id == changeCommitteeId, ct);

        if (changeCommittee is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการเปลี่ยนแปลงคณะกรรมการ");
        }

        var response = MapToResponseDto(changeCommittee);

        return TypedResults.Ok(response);
    }

    private static ChangeCommitteeResponseDto MapToResponseDto(CommitteeChanges changeCommittee)
    {
        var procurement = changeCommittee.Procurement;

        var lastedHistory =
            changeCommittee.DocumentHistories
                           .OrderVersions()
                           .FirstOrDefault();

        var documentVersions =
            changeCommittee.DocumentHistories
                           .OrderVersions()
                           .Select((d, index) => new ChangeCommitteeDocumentVersionResponse(
                               d.FileId.Value,
                               d.Version,
                               d.CreatedAt,
                               d.CreatedByName ?? string.Empty,
                               index == 0))
                           .ToArray();

        var acceptorsApprover =
            changeCommittee.Acceptors
                .Where(a => a.Type == AcceptorType.Approver)
                .Select(DelegatorExtensions.DelegatorToAcceptor)
                .ToList();

        var committeesApprover =
            changeCommittee.Acceptors
                       .Where(a => a.Type != AcceptorType.Approver)
                       .ToList();

        var acceptors =
            acceptorsApprover
                .Union(committeesApprover)
                .Map(MapAcceptor)
                .OrderBy(o => o.AcceptorType)
                .ThenBy(o => o.Sequence)
                .ToArray();

        return new ChangeCommitteeResponseDto(
            changeCommittee.Id.Value,
            changeCommittee.ProcurementId.Value,
            changeCommittee.Procurement.Type.ToString(),
            new ChangeCommitteeProcurement(
                procurement.Plan?.Id.Value,
                procurement.Plan?.PlanNumber.Value ?? procurement.ProcurementNumber?.Value,
                procurement.Plan?.Type.ToString(),
                procurement.Plan?.Department?.Name ?? procurement.Department?.Name,
                procurement.Plan?.Name ?? procurement.Name,
                procurement.Plan?.Budget ?? procurement.Budget,
                procurement.Plan?.BudgetYear ?? (decimal?)procurement.BudgetYear,
                procurement.SupplyMethod.Label,
                procurement.SupplyMethodCode,
                procurement.SupplyMethodType?.Label,
                procurement.SupplyMethodTypeCode,
                procurement.SupplyMethodSpecialType?.Label,
                procurement.SupplyMethodSpecialTypeCode,
                procurement.IsStock,
                procurement.IsCommercialMaterial),
            changeCommittee.SourceType,
            changeCommittee.SourceId,
            changeCommittee.CommitteeType,
            changeCommittee.Status,
            changeCommittee.OldCommittees,
            changeCommittee.NewCommittees,
            changeCommittee.Remark,
            acceptors,
            changeCommittee.Attachments.Select(a => new ChangeCommitteeAttachmentsResponseDto(
                a.Id,
                a.TypeCode.ToString(),
                a.Sequence,
                a.Remark,
                a.CommitteeChangeAttachmentInfos.OrderBy(p => p.Sequence)
                 .Select(info => new ChangeCommitteeFileAttachmentsResponse(
                     info.Id,
                     info.FileId.Value,
                     info.FileName,
                     info.Sequence,
                     info.IsPublic,
                     info.AuditInfo.CreatedBy)))), // TODO: Check if current user created this
            lastedHistory?.FileId.Value,
            documentVersions,
            changeCommittee.IsJorPorComment,
            changeCommittee.Assignees
                           .OrderBy(a => a.Sequence)
                           .Select(a => new AssigneeResponse(
                               a.Id.Value,
                               a.Group,
                               a.Type,
                               a.UserId.Value,
                               a.Sequence,
                               a.FullName,
                               a.PositionName,
                               a.BusinessUnitName,
                               a.Status,
                               a.Remark,
                               a.ActionAt,
                               a.Delegatee?.SuUserId.Value)),
            changeCommittee.DocumentDate);
    }

    private static ChangeCommitteeAcceptorResponseDto MapAcceptor(CommitteeChangeAcceptor acceptor)
    {
        return new ChangeCommitteeAcceptorResponseDto(
            acceptor.Id.Value,
            acceptor.Type,
            acceptor.UserId.Value,
            acceptor.Sequence,
            acceptor.FullName,
            acceptor.PositionName,
            acceptor.BusinessUnitName,
            acceptor.Status,
            acceptor.Remark,
            acceptor.ActionAt,
            Optional(acceptor.CommitteePositionsCode)
                .Map(v => v.Value)
                .IfNoneUnsafe((string?)null),
            acceptor.CommitteePosition?.Label,
            acceptor.IsUnableToPerformDuties,
            true,
            acceptor.User.Employee.PrimaryDepartment != null ? (string)acceptor.User.Employee.PrimaryDepartment.Id : string.Empty,
            acceptor.Delegatee?.SuUserId.Value);
    }

    private static bool CurrentAcceptor(IEnumerable<CommitteeChangeAcceptor> acceptors, Guid acceptorId, CommitteeChangeStatus status)
    {
        if (status != CommitteeChangeStatus.WaitingApproval)
        {
            return false;
        }

        var requiredType = AcceptorType.Approver;

        var current = acceptors.FirstOrDefault(a =>
            a.Id.Value == acceptorId && a.Type == requiredType);

        if (current == null)
        {
            return false;
        }

        var prev = acceptors
                   .Where(a =>
                       a.Type == requiredType &&
                       a.Sequence < current.Sequence &&
                       a.IsActive)
                   .OrderByDescending(a => a.Sequence)
                   .FirstOrDefault();

        if (prev == null)
        {
            return current.Status != AcceptorStatus.Approved;
        }

        return prev.Status == AcceptorStatus.Approved;
    }
}
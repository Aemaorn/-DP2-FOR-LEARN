namespace GHB.DP2.Application.Features.Procurement.Invite;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetByIdInviteRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid? Id);

public class GetByIdInviteEndpoint : InviteEndpointBase<GetByIdInviteRequest, Results<Ok<InviteResponseDto>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetByIdInviteEndpoint(
        ILogger<GetByIdInviteEndpoint> logger,
        Dp2DbContext dbContext)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/invite/{Id:guid?}");
        this.Description(b => b
                              .WithTags("Procurement/Invite")
                              .WithName("GetInviteById")
                              .Produces<InviteResponseDto>()
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<InviteResponseDto>, NotFound<string>>> HandleRequestAsync(GetByIdInviteRequest req, CancellationToken ct)
    {
        var committees = await this.dbContext.PJp005S
                                   .Where(c => c.ProcurementId == ProcurementId.From(req.ProcurementId))
                                   .SelectMany(s => s.Committees)
                                   .Where(w => w.GroupType == PJp005CommitteeGroupType.ProcurementCommittee)
                                   .ToArrayAsync(ct);

        var jp004 = await this.dbContext.PpPurchaseRequisitions
                              .Include(ppPurchaseRequisition => ppPurchaseRequisition.Assignees)
                              .FirstOrDefaultAsync(w => w.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        var operators = committees.Select(s => s.SuUserId).ToArray();

        if (jp004 is { LastedAssignee: not null })
        {
            operators = [.. operators, .. jp004.Assignees.Select(a => a.UserId).ToArray()];
            operators = [.. operators.Distinct()];
        }

        if (req.Id.HasValue)
        {
            var invite = await this.GetPInviteById(PInviteId.From(req.Id.Value), ProcurementId.From(req.ProcurementId), ct);

            var procurementSuppliesDivisions =
                invite.Procurement.Jp005
                      .FirstOrDefault()?
                      .ProcurementSuppliesDivisions
                      .Select(s => s.SuUserId) ?? [];

            if (!committees.Any())
            {
                return TypedResults.NotFound($"ไม่พบข้อมูลผู้มีสิทธิ์จัดการข้อมูลหนังสือเชิญชวน");
            }

            return TypedResults.Ok(this.MapToResponseDto(invite, operators, procurementSuppliesDivisions, req.UserId));
        }

        var emptyResponse = await this.BuildDefaultResponseAsync(ProcurementId.From(req.ProcurementId), operators, req.UserId, ct);

        if (emptyResponse is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลโครงการ");
        }

        return TypedResults.Ok(emptyResponse);
    }

    private async Task<InviteResponseDto?> BuildDefaultResponseAsync(
        ProcurementId procurementId,
        UserId[] operators,
        Guid userId,
        CancellationToken ct)
    {
        var procurementSuppliesDivisions = await this.dbContext.PJp005S
                                                     .Where(j => j.ProcurementId == procurementId)
                                                     .SelectMany(j => j.ProcurementSuppliesDivisions)
                                                     .Select(s => s.SuUserId)
                                                     .ToArrayAsync(ct);

        var procurement = await this.dbContext.Procurements
                                    .Include(p => p.Department)
                                    .Include(p => p.SupplyMethod)
                                    .Include(p => p.SupplyMethodType)
                                    .Include(p => p.SupplyMethodSpecialType)
                                    .Include(p => p.Plan)
                                    .FirstOrDefaultAsync(p => p.Id == procurementId, ct);

        if (procurement is null)
        {
            return null;
        }

        var hasEditPermission =
            operators.Any(c => c == userId) ||
            procurementSuppliesDivisions.Any(c => c == userId);

        var workBusinessUnitId = procurement.Department.Id;

        var suppliesDivisions = await this.dbContext.PJp005S
                                          .Include(x => x.ProcurementSuppliesDivisions)
                                              .ThenInclude(x => x.SuUser)
                                              .ThenInclude(x => x.Employee)
                                          .Where(j => j.IsActive && j.ProcurementId == procurementId)
                                          .SelectMany(j => j.ProcurementSuppliesDivisions)
                                          .ToArrayAsync(ct);

        var committeeMembers = await this.dbContext.PJp005S
                                         .Include(x => x.Committees)
                                             .ThenInclude(x => x.User)
                                             .ThenInclude(x => x.Employee)
                                         .Where(j => j.IsActive && j.ProcurementId == procurementId)
                                         .SelectMany(j => j.Committees)
                                         .Where(w => w.GroupType == PJp005CommitteeGroupType.ProcurementCommittee)
                                         .ToArrayAsync(ct);

        var suppliesAcceptors = suppliesDivisions.Select(s => new AcceptorInviteResponseDto(
            Guid.Empty,
            AcceptorType.ProcurementCommittee,
            s.SuUserId.Value,
            s.SuUser.EmployeeCode.Value,
            s.SuUser.Employee.View!.FullName,
            s.SuUser.Employee.ConvertPositionName(workBusinessUnitId),
            s.SuUser.Employee.View.BusinessUnitName,
            s.Sequence,
            null,
            AcceptorStatus.Draft,
            null,
            null,
            false,
            false,
            ParameterCode.From(SuParameterCodeConstant.PosBoard006),
            null)).ToArray();

        var maxSequence = suppliesAcceptors.Length > 0 ? suppliesAcceptors.Max(s => s.Sequence) : 0;

        var committeeAcceptors = committeeMembers.Select((s, index) => new AcceptorInviteResponseDto(
            Guid.Empty,
            AcceptorType.ProcurementCommittee,
            s.SuUserId.Value,
            s.User.EmployeeCode.Value,
            s.User.Employee.View!.FullName,
            s.User.Employee.ConvertPositionName(workBusinessUnitId),
            s.User.Employee.View.BusinessUnitName,
            maxSequence == 0 ? s.Sequence : maxSequence + index + 1,
            null,
            AcceptorStatus.Draft,
            null,
            null,
            false,
            false,
            s.CommitteePositionsCode,
            null)).ToArray();

        var acceptors = suppliesAcceptors
                        .Union(committeeAcceptors)
                        .OrderBy(o => o.CommitteePositionsCode == ParameterCode.From(SuParameterCodeConstant.PosBoard006) ? 0 : 1)
                        .ThenBy(o => o.Sequence)
                        .ToArray();

        return new InviteResponseDto(
            ProcurementDto.Map(procurement),
            null,
            procurementId.Value,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            PInviteStatus.Draft,
            acceptors,
            [],
            null,
            hasEditPermission);
    }
}

public record DocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record InviteResponseDto(
    ProcurementDto Procurement,
    Guid? Id,
    Guid ProcurementId,
    bool IsInvite,
    DateTimeOffset? SubmitProposalStartDate,
    DateTimeOffset? SubmitProposalEndDate,
    DateTimeOffset? SubmitProposalStartTime,
    DateTimeOffset? SubmitProposalEndTime,
    DateTimeOffset? NeedToKnowWithinDate,
    DateTimeOffset? ClarifyDetailViaDate,
    string? PhoneNumber,
    PInviteStatus Status,
    IEnumerable<AcceptorInviteResponseDto>? Acceptors,
    IEnumerable<InviteEntrepreneurDto>? InvitedEntrepreneurs,
    bool? IsDocumentReplace,
    bool HasEditPermission = false,
    [property: Description("วันที่เอกสาร")]
    DateTimeOffset? DocumentDate = null);

public record InviteEntrepreneurDto(
    Guid Id,
    Guid VendorId,
    int Sequence,
    string EntrepreneurTaxId,
    string EntrepreneurType,
    string EntrepreneurName,
    string EntrepreneurEmail,
    bool WatchlistResult,
    string? WatchlistResultRemark,
    DateTimeOffset? WatchlistResultAt,
    bool CoiResult,
    string? CoiResultRemark,
    DateTimeOffset? CoiResultAt,
    bool EgpResult,
    string? EgpResultRemark,
    DateTimeOffset? EgpResultAt,
    bool EmailSend,
    SuVendorNationality? Nationality,
    SuVendorType? Type,
    string? Tel,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult,
    InviteEntrepreneurShareholderDto[]? Shareholders,
    string? Email,
    string? EmailTemplate,
    EmailAttachment[] Attachments,
    Guid? DocumentId,
    bool? IsDocumentReplace,
    DocumentVersionResponse[] DocumentVersions,
    string? SapBranchNumber = null);

public record InviteEntrepreneurShareholderDto(
    [property: Description("รหัสผู้ถือหุ้น")]
    Guid Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("เลขที่ผู้เสียภาษี")]
    string? TaxId,
    [property: Description("ชื่อจริง")] string? FirstName,
    [property: Description("นามสกุล")] string? LastName,
    [property: Description("เป็นกรรมการหรือถือหุ้น 20%")]
    bool? IsDirector,
    [property: Description("เป็นผู้ถือหุ้น")]
    bool? IsShareholder,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    bool WatchlistResult,
    [property: Description("หมายเหตุผลการตรวจสอบ Watchlist")]
    string? WatchlistResultRemark,
    [property: Description("วันที่ตรวจสอบ Watchlist")]
    DateTimeOffset? WatchlistResultAt,
    [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    bool CoiResult,
    [property: Description("หมายเหตุผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    string? CoiResultRemark,
    [property: Description("วันที่ตรวจสอบความขัดแย้งทางผลประโยชน์")]
    DateTimeOffset? CoiResultAt,
    [property: Description("ผลการตรวจสอบ eGP")]
    bool EgpResult,
    [property: Description("หมายเหตุ eGP")]
    string? EgpRemark,
    [property: Description("วันที่ตรวจสอบ eGP")]
    DateTimeOffset? EgpResultAt,
    [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    QualificationResultDto? CoiCheckerResult,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    QualificationResultDto? WatchlistCheckerResult);

public record AcceptorInviteResponseDto(
    [property: Description("รหัสผู้อนุมัติ")]
    Guid Id,
    [property: Description("ประเภทผู้อนุมัติ")]
    AcceptorType AcceptorType,
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("รหัสพนักงาน")] string EmployeeCode,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รหัสผู้รับมอบอำนาจ")]
    Guid? DelegateeUserId,
    [property: Description("สถานะการอนุมัติ")]
    AcceptorStatus Status,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("เป็นผู้อนุมัติปัจจุบัน")]
    bool IsCurrent,
    bool IsUnableToPerformDuties,
    ParameterCode? CommitteePositionsCode,
    string? CommitteePositionName
);
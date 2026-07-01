namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListContractTerminationRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    DateTimeOffset? ContractSignedDate,
    string? ContractType,
    CmContractTerminationStatus? Status,
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    WorkProcess WorkProcess);

public record GetListContractTerminationResponse(
    Guid ProcurementId,
    Guid ContractId,
    Guid Id,
    ProcurementType ProcurementType,
    string ContractNumber,
    string PoNumber,
    string? TerminateTypeName,
    DateTimeOffset? ContractSignedDate,
    string EntrepreneurName,
    string ContractName,
    decimal Budget,
    string? ContractTypeName,
    CmContractTerminationStatus Status);

public record GetStatusCount(
    int All,
    int Draft,
    int WaitingCommitteeApproval,
    int WaitingAssign,
    int WaitingComment,
    int WaitingAcceptance,
    int Approved,
    int Rejected);

public record GetContractTerminationResult(
    GetStatusCount StatusCount,
    PaginatedQueryResult<GetListContractTerminationResponse> Data
);

public class GetContractTerminationListEndpoint : ContractTerminationEndpoint<GetListContractTerminationRequest, Ok<GetContractTerminationResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractTerminationListEndpoint(
        ILogger<GetContractTerminationListEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("contract/contract-termination");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractTermination")
                              .WithName("GetContractTerminationList")
                              .AllowAnonymous()
                              .Produces<ContractVendorTerminalResponse>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<GetContractTerminationResult>> HandleRequestAsync(GetListContractTerminationRequest req, CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);

        var query = this.dbContext.CmContractTerminations
                        .Include(t => t.TerminateTypeNavigation)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            x => EF.Functions.ILike(x.CaContractDraftVendor.ContractNumber, $"%{req.Keyword}%") ||
                                 EF.Functions.ILike(x.CaContractDraftVendor.ContractName, $"%{req.Keyword}%") ||
                                 EF.Functions.ILike(x.CaContractDraftVendor.PoNumber, $"%{req.Keyword}%"))
                        .WhereIfTrue(
                            !req.ContractSignedDate.IsNull(),
                            x => x.CaContractDraftVendor.ContractSignedDate.HasValue &&
                                 x.CaContractDraftVendor.ContractSignedDate.Value.Date == req.ContractSignedDate!.Value.Date)
                        .WhereIfTrue(
                            !req.ContractType.IsNull() && req.ContractType != "CMType003",
                            x => x.CaContractDraftVendor.ContractType!.Code == ParameterCode.From(req.ContractType!))
                        .WhereIfTrue(
                            !req.ContractType.IsNull() && req.ContractType == "CMType003",
                            x => x.CaContractDraftVendor.ContractType!.Code == ParameterCode.From("CMRentalType001"));

        if (req.WorkProcess == WorkProcess.InProcess)
        {
            query = GetInprogressCmContractTermination(query, userId);
        }

        query = query.OrderByDescending(o => o.AuditInfo.LastModifiedAt != null ? o.AuditInfo.LastModifiedAt : o.AuditInfo.CreatedAt);

        var paginatedQuery =
            query.WhereIfTrue(
            !req.Status.IsNull(),
            p => req.Status == CmContractTerminationStatus.Rejected || req.Status == CmContractTerminationStatus.RejectToAssignee
                ? p.Status == CmContractTerminationStatus.Rejected || p.Status == CmContractTerminationStatus.RejectToAssignee
                : p.Status == req.Status);

        var paginated =
            await PaginatedList<CmContractTermination>
                .CreateAsync(
                    paginatedQuery,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var result = await query.ToListAsync(ct);

        var statusCount =
            new GetStatusCount(
                result.Count,
                result.Count(s => s.Status is CmContractTerminationStatus.Draft),
                result.Count(s => s.Status is CmContractTerminationStatus.WaitingCommitteeApproval),
                result.Count(s => s.Status is CmContractTerminationStatus.WaitingAssign),
                result.Count(s => s.Status is CmContractTerminationStatus.WaitingComment),
                result.Count(s => s.Status is CmContractTerminationStatus.WaitingApproval),
                result.Count(s => s.Status is CmContractTerminationStatus.Approved),
                result.Count(s => s.Status is CmContractTerminationStatus.Rejected or CmContractTerminationStatus.RejectToAssignee));

        var data =
            paginated.ToResult(c =>
            {
                var vendor = this.MapSuVendorByType(
                    c.CaContractDraftVendor.ContractInvitationVendors,
                    c.CaContractDraftVendor.ContractDraft.Procurement.Type);

                return
                    new GetListContractTerminationResponse(
                        c.CaContractDraftVendor.ContractDraft.ProcurementId.Value,
                        c.CaContractDraftVendor.Id.Value,
                        c.Id.Value,
                        c.CaContractDraftVendor.ContractDraft.Procurement.Type,
                        c.CaContractDraftVendor.ContractNumber,
                        c.CaContractDraftVendor.PoNumber,
                        c.TerminateTypeNavigation?.Label,
                        c.CaContractDraftVendor.ContractSignedDate,
                        vendor != null ? vendor.EstablishmentName : string.Empty,
                        c.CaContractDraftVendor.ContractName,
                        c.CaContractDraftVendor.Budget,
                        c.CaContractDraftVendor.ContractType?.Label,
                        c.Status);
            });

        return
            TypedResults.Ok(
                new GetContractTerminationResult(
                    statusCount,
                    data));
    }

    private static IQueryable<CmContractTermination> GetInprogressCmContractTermination(IQueryable<CmContractTermination> query, UserId userId)
    {
        return query.Where(x =>
            (x.CaContractDraftVendor.ContractDraft.Procurement.Jp005.Any(j =>
                 j.Committees.Any(c => c.SuUserId == userId && c.GroupType == PJp005CommitteeGroupType.InspectionCommittee)) ||
             x.CaContractDraftVendor.ContractDraft.Procurement.PrincipleApprovals.Any(j =>
                 j.PrincipleApprovalCommittees.Any(c => c.SuUserId == userId && c.GroupType == CommitteeGroupType.AcceptanceCommittee))
             ||
             x.Assignees.Any(td => (x.Status == CmContractTerminationStatus.WaitingAssign || x.Status == CmContractTerminationStatus.RejectToAssignee) && (td.UserId == userId && td.Type == AssigneeType.Director)) ||
             x.Assignees.Any(td => x.Status == CmContractTerminationStatus.WaitingComment && (td.UserId == userId && td.Type == AssigneeType.Assignee)) ||
             x.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId)));
    }
}
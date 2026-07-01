namespace GHB.DP2.Application.Features.Procurement.PPettyCash;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.WorkList;
using GHB.DP2.Application.Features.WorkList.Programs;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPPettyCashListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    int? BudgetYear,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    PettyCashStatus? Status,
    WorkProcess? WorkProcess,
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId) : IProcurementBaseQuery
{
    public string? SupplyMethodTypeCode { get; }

    public bool? IsMd { get; }

    public bool? IsPendingDepartment => false;
}

public record GetPPettyCashListResponse(
    Guid Id,
    string PPettyCashNumber,
    string Subject,
    decimal Budget,
    string DepartmentName,
    string SupplyMethodName,
    string? SupplyMethodSpecialName,
    PettyCashStatus Status);

public record GetStatusCount(
    int All,
    int Draft,
    int Edit,
    int WaitingApproval,
    int WaitingForInspector,
    int WaitingForAssignment,
    int WaitingForCompletion,
    int Completed,
    int Rejected,
    int Cancelled);

public record GetPPettyCashListResult(
    GetStatusCount StatusCount,
    PaginatedQueryResult<GetPPettyCashListResponse> Data
);

public class GetPPettyCashList : EndpointBase<GetPPettyCashListRequest, Ok<GetPPettyCashListResult>>
{
    private readonly Dp2DbContext dbContext;
    private readonly ProcurementProgram procurementProgram;

    public GetPPettyCashList(Dp2DbContext dbContext, ILogger<GetPPettyCashList> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.procurementProgram = new ProcurementProgram();
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("PPettyCash")
             .WithName("GetPPettyCashList")
             .Produces<Ok>());
        this.Get("PPettyCash");
    }

    protected override async ValueTask<Ok<GetPPettyCashListResult>> HandleRequestAsync(GetPPettyCashListRequest req, CancellationToken ct)
    {
        var query =
            req.WorkProcess switch
            {
                WorkProcess.InProcess => await this.GetInprogressPettyCash(req, ct),
                WorkProcess.Related => this.GetRelatedPettyCash(req),
                WorkProcess.Completed => this.GetCompletedPettyCash(req),
                _ => this.PettyCashQueryBase(req),
            };

        query = query.OrderByDescending(o => o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt);

        var paginatedQuery =
            query.WhereIfTrue(
                !req.Status.IsNull(),
                p => p.Status == req.Status);

        var paginated = await PaginatedList<PPettyCash>.CreateAsync(paginatedQuery, req.PageNumber, req.PageSize, ct);

        var result = await query.ToListAsync(ct);
        var statusCount = new GetStatusCount(
            result.Count,
            result.Count(s => s.Status == PettyCashStatus.Draft),
            result.Count(s => s.Status == PettyCashStatus.Edit),
            result.Count(s => s.Status == PettyCashStatus.WaitingApproval),
            result.Count(s => s.Status == PettyCashStatus.WaitingForInspector),
            result.Count(s => s.Status == PettyCashStatus.WaitingForAssignment),
            result.Count(s => s.Status == PettyCashStatus.WaitingForCompletion),
            result.Count(s => s.Status == PettyCashStatus.Completed),
            result.Count(s => s.Status == PettyCashStatus.Rejected),
            result.Count(s => s.Status == PettyCashStatus.Cancelled));

        var data = paginated.ToResult(static x => new GetPPettyCashListResponse(
            x.Id.Value,
            x.PettyCashNumber.Value,
            x.Subject,
            x.Budget,
            x.Department.Name,
            x.SupplyMethod.Label,
            x.SupplyMethodSpecialType?.Label,
            x.Status));

        return TypedResults.Ok(new GetPPettyCashListResult(statusCount, data));
    }

    private IQueryable<PPettyCash> PettyCashQueryBase(GetPPettyCashListRequest req)
        => this.dbContext.PPettyCashs
               .Where(p => p.IsActive)
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.Keyword),
                   x => EF.Functions.ILike(x.Subject, $"%{req.Keyword}%") || EF.Functions.ILike((string)x.PettyCashNumber, $"%{req.Keyword}%"))
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.DepartmentCode),
                   x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
               .WhereIfTrue(
                   !req.BudgetYear.IsNull(),
                   x => x.BudgetYear == req.BudgetYear)
               .WhereIfTrue(
                   !string.IsNullOrEmpty(req.SupplyMethodCode),
                   x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
               .WhereIfTrue(
                   !string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode),
                   x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
               .AsQueryable();

    private async Task<IQueryable<PPettyCash>> GetInprogressPettyCash(GetPPettyCashListRequest req, CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);
        var userContext = await this.GetUserContextAsync(userId, ct);

        if (userContext is null)
        {
            return null!;
        }

        var query =
            this.procurementProgram.BuildPPettyCashBaseQuery(this.dbContext, req, userContext);

        return query;
    }

    private async Task<UserContext?> GetUserContextAsync(UserId userId, CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.Positions)
                             .ThenInclude(p => p.BusinessUnit)
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.View)
                             .Include(e => e.Delegatees)
                             .ThenInclude(e => e.SuDelegator)
                             .ThenInclude(suDelegator => suDelegator.SuUser)
                             .ThenInclude(suUser => suUser.Employee)
                             .ThenInclude(rawEmployee => rawEmployee.View)
                             .FirstOrDefaultAsync(u => u.Id == userId, ct);

        var viewUser = user?.Employee?.View;

        if (viewUser is null)
        {
            return null;
        }

        var effectiveUserIds = new List<EffectiveUserId>();

        if (user?.EffectiveDelegator is { SuUserId: var uid })
        {
            effectiveUserIds.Add(new EffectiveUserId(uid, true));
        }

        effectiveUserIds.Add(new EffectiveUserId(user!.Id, false));

        return new UserContext(
            user!,
            viewUser,
            user!.Employee?.PrimaryBusinessUnit?.Id,
            user.Employee?.IsJorPor ?? false,
            user.Employee?.PrimaryOrganizationLevel,
            effectiveUserIds);
    }

    private IQueryable<PPettyCash> GetRelatedPettyCash(GetPPettyCashListRequest req)
    {
        var userIdVal = UserId.From(req.UserId);
        var query = this.PettyCashQueryBase(req);

        return query.Where(x =>
            ((x.Status != PettyCashStatus.Draft || x.Status != PettyCashStatus.Rejected) && x.AuditInfo.CreatedBy == req.UserId) ||
            (x.Status == PettyCashStatus.WaitingApproval && x.Acceptors.Any(ac =>
                !ac.IsCurrent && ac.UserId == userIdVal && (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) && ac.Type == AcceptorType.DepartmentDirectorAgree)) ||
            (x.Status == PettyCashStatus.WaitingForInspector && x.Acceptors.Any(ac =>
                !ac.IsCurrent && ac.UserId == userIdVal && (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) && ac.Type == AcceptorType.InspectionCommittee)) ||
            (x.Status == PettyCashStatus.WaitingForCompletion && x.Assignees.Any(ac => ac.UserId == userIdVal && ac.Group == AssigneeGroup.JorPor)));
    }

    private IQueryable<PPettyCash> GetCompletedPettyCash(GetPPettyCashListRequest req)
    {
        var userIdVal = UserId.From(req.UserId);
        var query = this.PettyCashQueryBase(req);

        return query.Where(x =>
            (x.Status != PettyCashStatus.Completed && x.AuditInfo.CreatedBy == req.UserId) ||
            (x.Status == PettyCashStatus.Completed && x.Acceptors.Any(ac => ac.UserId == userIdVal)) ||
            (x.Status == PettyCashStatus.Completed && x.Assignees.Any(ac => ac.UserId == userIdVal)));
    }
}
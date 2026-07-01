namespace GHB.DP2.Application.Features.Procurement.Pw119;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.WorkList;
using GHB.DP2.Application.Features.WorkList.Programs;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPw119ListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    int? BudgetYear,
    int? Quarter,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    Pw119Status? Status,
    WorkProcess? WorkProcess,
    DateTimeOffset? ActionAtFrom,
    DateTimeOffset? ActionAtTo,
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId) : IProcurementBaseQuery
{
    public string? SupplyMethodTypeCode { get; }

    public bool? IsMd { get; }

    public bool? IsPendingDepartment => false;
}

public record GetPw119ListResponse(
    Guid Id,
    string Pw119Number,
    string Subject,
    decimal Budget,
    string DepartmentName,
    string SupplyMethodName,
    string? SupplyMethodSpecialName,
    Pw119Status Status,
    IReadOnlyCollection<string> GLAccounts);

public record GetStatusCount(
    int All,
    int Draft,
    int Edit,
    int WaitingApproval,
    int Approved,
    int Rejected,
    int Cancelled,
    int WaitingAccountingApproval,
    int WaitingDisbursementDate,
    int Paid);

public record GetPw119ListResult(
    GetStatusCount StatusCount,
    PaginatedQueryResult<GetPw119ListResponse> Data
);

public class GetPw119List : EndpointBase<GetPw119ListRequest, Ok<GetPw119ListResult>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly ProcurementProgram procurementProgram;

    public GetPw119List(Dp2DbContext dbContext, IOperationService operationService, ILogger<GetPw119List> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.procurementProgram = new ProcurementProgram();
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw119")
             .WithName("GetPw119List")
             .Produces<Ok>());
        this.Get("pw119");
    }

    protected override async ValueTask<Ok<GetPw119ListResult>> HandleRequestAsync(GetPw119ListRequest req, CancellationToken ct)
    {
        var query = await this.ApplyWorkProcessFilter(req, ct);
        query = query.OrderByDescending(o => o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt);

        var paginatedQuery = query
            .WhereIfTrue(!req.Status.IsNull(), p => p.Status == req.Status)
            .Include(p => p.GLAccounts)
                .ThenInclude(g => g.GLAccount)
            .AsSplitQuery();
        var paginated = await PaginatedList<Pw119>.CreateAsync(paginatedQuery, req.PageNumber, req.PageSize, ct);

        var result = await query.ToListAsync(ct);
        var statusCount = CreateStatusCount(result);

        var data = paginated.ToResult(static x => new GetPw119ListResponse(
            x.Id.Value,
            x.Pw119Number.Value,
            x.Subject,
            x.Budget,
            x.Department.Name,
            x.SupplyMethod.Label,
            x.SupplyMethodSpecialType?.Label,
            x.Status,
            x.GLAccounts.Select(g => g.GLAccount.Label).Distinct().ToList()));

        return TypedResults.Ok(new GetPw119ListResult(statusCount, data));
    }

    private async Task<IQueryable<Pw119>> ApplyWorkProcessFilter(GetPw119ListRequest req, CancellationToken ct)
    {
        return req.WorkProcess switch
        {
            WorkProcess.InProcess => await this.ApplyInProcessFilter(req, ct),

            WorkProcess.Related => this.ApplyRelatedFilter(req),

            WorkProcess.Completed => this.ApplyCompletedFilter(req),

            _ => this.ApplyAllFilter(req),
        };
    }

    private async Task<IQueryable<Pw119>> ApplyInProcessFilter(GetPw119ListRequest req, CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);
        var userContext = await this.GetUserContextAsync(userId, ct);

        if (userContext is null)
        {
            return null!;
        }

        var segmentAccountingMembers = await this.operationService.GetSegmentAccountingMembersAsync(ct);
        var isSegmentAccountingMember = ProcurementProgram.IsSegmentAccountingMember(segmentAccountingMembers, userContext);

        var query =
            this.procurementProgram.BuildPw119BaseQuery(this.dbContext, req, userContext, isSegmentAccountingMember);

        return ApplyAdditionalFilters(query, req);
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

    private IQueryable<Pw119> ApplyAllFilter(GetPw119ListRequest req)
    {
        var query = this.dbContext.Pw119s.AsNoTracking();

        query = ApplyAdditionalFilters(query, req);

        return query;
    }

    private IQueryable<Pw119> ApplyCompletedFilter(GetPw119ListRequest req)
    {
        var userId = UserId.From(req.UserId);
        var query =
            this.dbContext.Pw119s
                .Where(x =>
                    (x.Status != Pw119Status.Paid && x.AuditInfo.CreatedBy == req.UserId) ||
                    (x.Status == Pw119Status.Paid && x.Acceptors.Any(ac => ac.UserId == userId)))
                .AsQueryable();

        query = ApplyAdditionalFilters(query, req);

        return query;
    }

    private IQueryable<Pw119> ApplyRelatedFilter(GetPw119ListRequest req)
    {
        var userId = UserId.From(req.UserId);
        var query =
            this.dbContext.Pw119s
                .Where(x =>
                    ((x.Status != Pw119Status.Draft || x.Status != Pw119Status.Rejected) &&
                     x.AuditInfo.CreatedBy == req.UserId) ||
                    (x.Status == Pw119Status.WaitingApproval &&
                     x.Acceptors.Any(ac =>
                         !ac.IsCurrent &&
                         ac.UserId == userId &&
                         (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))))
                .AsQueryable();

        query = ApplyAdditionalFilters(query, req);

        return query;
    }

    private static IQueryable<Pw119> ApplyAdditionalFilters(IQueryable<Pw119> query, GetPw119ListRequest req)
    {
        query = query
               .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x => EF.Functions.ILike(x.Subject, $"%{req.Keyword}%") || EF.Functions.ILike((string)x.Pw119Number, $"%{req.Keyword}%") || x.GLAccounts.Any(g => EF.Functions.ILike(g.GLAccount.Label, $"%{req.Keyword}%")))
               .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
               .WhereIfTrue(!req.BudgetYear.IsNull(), x => x.BudgetYear == req.BudgetYear)
               .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodCode), x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
               .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode), x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
               .WhereIfTrue(req.ActionAtFrom.HasValue, x => x.Status == Pw119Status.Approved && (x.AuditInfo.LastModifiedAt ?? x.AuditInfo.CreatedAt) >= req.ActionAtFrom!.Value)
               .WhereIfTrue(req.ActionAtTo.HasValue, x => x.Status == Pw119Status.Approved && (x.AuditInfo.LastModifiedAt ?? x.AuditInfo.CreatedAt) <= req.ActionAtTo!.Value);

        if (req.Quarter.HasValue)
        {
            var (startMonth, endMonth) = req.Quarter.Value switch
            {
                1 => (1, 3),
                2 => (4, 6),
                3 => (7, 9),
                4 => (10, 12),
                _ => (1, 12),
            };

            query = query.Where(x => x.Pw119Date.Month >= startMonth && x.Pw119Date.Month <= endMonth);

            if (req.BudgetYear.HasValue)
            {
                var ceYear = req.BudgetYear.Value - 543;
                query = query.Where(x => x.Pw119Date.Year == ceYear);
            }
        }

        return query;
    }

    private static GetStatusCount CreateStatusCount(List<Pw119> result)
    {
        return new GetStatusCount(
            result.Count,
            result.Count(s => s.Status == Pw119Status.Draft),
            result.Count(s => s.Status == Pw119Status.Edit),
            result.Count(s => s.Status == Pw119Status.WaitingApproval),
            result.Count(s => s.Status == Pw119Status.Approved),
            result.Count(s => s.Status == Pw119Status.Rejected),
            result.Count(s => s.Status == Pw119Status.Cancelled),
            result.Count(s => s.Status == Pw119Status.WaitingAccountingApproval),
            result.Count(s => s.Status == Pw119Status.WaitingDisbursementDate),
            result.Count(s => s.Status == Pw119Status.Paid));
    }
}
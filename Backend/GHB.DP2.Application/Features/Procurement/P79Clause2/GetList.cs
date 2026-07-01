namespace GHB.DP2.Application.Features.Procurement.P79Clause2;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.WorkList;
using GHB.DP2.Application.Features.WorkList.Programs;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetP79Clause2ListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    int? BudgetYear,
    int? Quarter,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    P79Clause2Status? Status,
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

public record GetP79Clause2ListResponse(
    Guid Id,
    string P79Clause2Number,
    string Subject,
    decimal Budget,
    string DepartmentName,
    string SupplyMethodName,
    string? SupplyMethodSpecialName,
    P79Clause2Status Status,
    IReadOnlyCollection<string> GLAccounts);

public record GetStatusCount(
    int All,
    int Draft,
    int Edit,
    int Rejected,
    int WaitingApproval,
    int Approved,
    int Cancelled,
    int WaitingAccountingApproval,
    int WaitingDisbursementDate,
    int Paid);

public record GetP79Clause2ListResult(
    GetStatusCount StatusCount,
    PaginatedQueryResult<GetP79Clause2ListResponse> Data
);

public class GetP79Clause2List : EndpointBase<GetP79Clause2ListRequest, Ok<GetP79Clause2ListResult>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly ProcurementProgram procurementProgram;

    public GetP79Clause2List(Dp2DbContext dbContext, IOperationService operationService, ILogger<GetP79Clause2List> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.procurementProgram = new ProcurementProgram();
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("P79Clause2")
             .WithName("GetP79Clause2List")
             .Produces<Ok>());
        this.Get("p79Clause2");
    }

    protected override async ValueTask<Ok<GetP79Clause2ListResult>> HandleRequestAsync(GetP79Clause2ListRequest req, CancellationToken ct)
    {
        var query = req.WorkProcess switch
        {
            WorkProcess.InProcess => await this.GetInprogressP79Clause2(req, ct),
            WorkProcess.Related => this.GetRelatedP79Clause2(req),
            WorkProcess.Completed => this.GetCompletedP79Clause2(req),
            _ => this.P79Clause2QueryBase(req),
        };

        query = query.AsNoTracking()
                     .OrderByDescending(o =>
                         o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt);

        var paginatedQuery =
            query.WhereIfTrue(
                    !req.Status.IsNull(),
                    p => p.Status == req.Status)
                 .Include(p => p.GLAccounts)
                     .ThenInclude(g => g.GLAccount)
                 .AsSplitQuery();

        var paginated = await PaginatedList<P79Clause2>.CreateAsync(paginatedQuery, req.PageNumber, req.PageSize, ct);

        var result = await query.ToListAsync(ct);
        var statusCount = new GetStatusCount(
            result.Count,
            result.Count(s => s.Status == P79Clause2Status.Draft),
            result.Count(s => s.Status == P79Clause2Status.Edit),
            result.Count(s => s.Status == P79Clause2Status.Rejected),
            result.Count(s => s.Status == P79Clause2Status.WaitingApproval),
            result.Count(s => s.Status == P79Clause2Status.Approved),
            result.Count(s => s.Status == P79Clause2Status.Cancelled),
            result.Count(s => s.Status == P79Clause2Status.WaitingAccountingApproval),
            result.Count(s => s.Status == P79Clause2Status.WaitingDisbursementDate),
            result.Count(s => s.Status == P79Clause2Status.Paid));

        var data = paginated.ToResult(static x => new GetP79Clause2ListResponse(
            x.Id.Value,
            x.P79Clause2Number.Value,
            x.Subject,
            x.Budget,
            x.Department.Name,
            x.SupplyMethod.Label,
            x.SupplyMethodSpecialType?.Label,
            x.Status,
            x.GLAccounts.Select(g => g.GLAccount.Label).Distinct().ToList()));

        return TypedResults.Ok(new GetP79Clause2ListResult(statusCount, data));
    }

    private IQueryable<P79Clause2> P79Clause2QueryBase(GetP79Clause2ListRequest req)
    {
        var query = this.dbContext.P79Clause2s.AsQueryable();
        return ApplyAdditionalFilters(query, req);
    }

    private async Task<IQueryable<P79Clause2>> GetInprogressP79Clause2(GetP79Clause2ListRequest req, CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);
        var userContext = await this.GetUserContextAsync(userId, ct);

        if (userContext is null)
        {
            return null!;
        }

        var segmentAccountingMembers = await this.operationService.GetSegmentAccountingMembersAsync(ct);
        var isSegmentAccountingMember = ProcurementProgram.IsSegmentAccountingMember(segmentAccountingMembers, userContext);

        var query = this.procurementProgram.BuildP79Clause2BaseQuery(this.dbContext, req, userContext, isSegmentAccountingMember);

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

    private IQueryable<P79Clause2> GetRelatedP79Clause2(GetP79Clause2ListRequest req)
    {
        var userId = UserId.From(req.UserId);
        var query = this.P79Clause2QueryBase(req);

        return query.Where(x =>
            ((x.Status != P79Clause2Status.Draft || x.Status != P79Clause2Status.Rejected) && x.AuditInfo.CreatedBy == req.UserId) ||
            (x.Status == P79Clause2Status.WaitingApproval && x.Acceptors.Any(ac =>
                !ac.IsCurrent && ac.UserId == userId &&
                (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))));
    }

    private IQueryable<P79Clause2> GetCompletedP79Clause2(GetP79Clause2ListRequest req)
    {
        var userId = UserId.From(req.UserId);
        var query = this.P79Clause2QueryBase(req);

        return query.Where(x =>
            (x.Status != P79Clause2Status.Paid && x.AuditInfo.CreatedBy == req.UserId) ||
            (x.Status == P79Clause2Status.Paid && x.Acceptors.Any(ac => ac.UserId == userId)));
    }

    private static IQueryable<P79Clause2> ApplyAdditionalFilters(IQueryable<P79Clause2> query, GetP79Clause2ListRequest req)
    {
        query = query
               .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x => EF.Functions.ILike(x.Subject, $"%{req.Keyword}%") || EF.Functions.ILike((string)x.P79Clause2Number, $"%{req.Keyword}%") || x.GLAccounts.Any(g => EF.Functions.ILike(g.GLAccount.Label, $"%{req.Keyword}%")))
               .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
               .WhereIfTrue(!req.BudgetYear.IsNull(), x => x.BudgetYear == req.BudgetYear)
               .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodCode), x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
               .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode), x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
               .WhereIfTrue(req.ActionAtFrom.HasValue, x => x.Status == P79Clause2Status.Approved && (x.AuditInfo.LastModifiedAt ?? x.AuditInfo.CreatedAt) >= req.ActionAtFrom!.Value)
               .WhereIfTrue(req.ActionAtTo.HasValue, x => x.Status == P79Clause2Status.Approved && (x.AuditInfo.LastModifiedAt ?? x.AuditInfo.CreatedAt) <= req.ActionAtTo!.Value);

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

            query = query.Where(x => x.P79Clause2Date.Month >= startMonth && x.P79Clause2Date.Month <= endMonth);

            if (req.BudgetYear.HasValue)
            {
                var ceYear = req.BudgetYear.Value - 543;
                query = query.Where(x => x.P79Clause2Date.Year == ceYear);
            }
        }

        return query;
    }
}
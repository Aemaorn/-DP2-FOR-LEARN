namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetChangeCommitteeListRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    string? Keyword,
    string? DepartmentCode,
    int? BudgetYear,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    CommitteeType? CommitteeGroupType,
    CommitteeChangeStatus? Status = null,
    WorkProcess? WorkProcess = null,
    int PageNumber = 1,
    int PageSize = 10);

public record ChangeCommitteeListItemDto(
    Guid Id,
    Guid ProcurementId,
    string? PlanNumber,
    string? ProcurementNumber,
    string? ProcurementName,
    string? DepartmentName,
    string? SupplyMethod,
    CommitteeType CommitteeType,
    CommitteeChangeStatus Status,
    string? Remark,
    DateTimeOffset CreatedAt);

public record GetChangeCommitteeListResult(
    int All,
    int Draft,
    int Edit,
    int WaitingApproval,
    int Approved,
    int Rejected,
    int Cancelled,
    PaginatedQueryResult<ChangeCommitteeListItemDto> Data);

public class GetChangeCommitteeListEndpoint : EndpointBase<GetChangeCommitteeListRequest, Ok<GetChangeCommitteeListResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetChangeCommitteeListEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetChangeCommitteeListEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ChangeCommittee"));
        this.Get("change-committee");
    }

    protected override async ValueTask<Ok<GetChangeCommitteeListResult>> HandleRequestAsync(GetChangeCommitteeListRequest req, CancellationToken ct)
    {
        var (userPrimaryDepartmentId, isJorPor) = await this.GetUserContextAsync(req.UserId, ct);
        var baseQuery = this.BuildFilterQuery(req, userPrimaryDepartmentId, isJorPor);

        var paginatedQuery = baseQuery.WhereIfTrue(req.Status.HasValue, c => c.Status == req.Status.Value);
        var paginated = await PaginatedList<CommitteeChanges>.CreateAsync(paginatedQuery, req.PageNumber, req.PageSize, ct);
        var data = paginated.ToResult(this.MapToChangeCommitteeResponse);

        var fullList = await GetFullListAsync(baseQuery, ct);
        var statusCounts = CalculateStatusCounts(fullList);

        return TypedResults.Ok(new GetChangeCommitteeListResult(
            statusCounts.All,
            statusCounts.Draft,
            statusCounts.Edit,
            statusCounts.WaitingApproval,
            statusCounts.Approved,
            statusCounts.Rejected,
            statusCounts.Cancelled,
            data));
    }

    private IQueryable<CommitteeChanges> BuildFilterQuery(GetChangeCommitteeListRequest req, BusinessUnitId? userPrimaryDepartmentId = null, bool isJorPor = false)
    {
        var userId = UserId.From(req.UserId);
        var query = this.dbContext.CommitteeChanges
                        .AsNoTracking()
                        .Include(c => c.Procurement)
                        .Include(c => c.Acceptors)
                        .ThenInclude(a => a.User)
                        .AsQueryable();

        query = this.ApplyWorkProcessFilter(query, req.WorkProcess, userId, userPrimaryDepartmentId, isJorPor);
        query = ApplyRemainingFilters(query, req);

        return query.OrderByDescending(o => o.AuditInfo.LastModifiedAt != null ? o.AuditInfo.LastModifiedAt : o.AuditInfo.CreatedAt);
    }

    private async Task<(BusinessUnitId? UserPrimaryDepartmentId, bool IsJorPor)> GetUserContextAsync(Guid userId, CancellationToken ct)
    {
        var userIdObj = UserId.From(userId);
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.Positions)
                             .ThenInclude(p => p.BusinessUnit)
                             .FirstOrDefaultAsync(u => u.Id == userIdObj, ct);

        if (user?.Employee == null)
        {
            return (null, false);
        }

        return (user.Employee.PrimaryBusinessUnit?.Id, user.Employee.IsJorPor);
    }

    private ChangeCommitteeListItemDto MapToChangeCommitteeResponse(CommitteeChanges c)
    {
        return new ChangeCommitteeListItemDto(
            c.Id.Value,
            c.ProcurementId.Value,
            c.Procurement.Plan?.PlanNumber.Value.ToString(),
            c.Procurement.ProcurementNumber.Value.ToString(),
            c.Procurement.Name,
            c.Procurement.Department.Name,
            c.Procurement.SupplyMethod.Label,
            c.CommitteeType,
            c.Status,
            c.Remark,
            c.AuditInfo.CreatedAt);
    }

    private IQueryable<CommitteeChanges> ApplyWorkProcessFilter(IQueryable<CommitteeChanges> query, WorkProcess? workProcess, UserId userId, BusinessUnitId? userPrimaryDepartmentId, bool isJorPor)
    {
        return workProcess switch
        {
            WorkProcess.InProcess => this.ApplyInProcessFilter(query, userId, userPrimaryDepartmentId, isJorPor),
            WorkProcess.Related => this.ApplyRelatedFilter(query, userId),
            WorkProcess.Completed => this.ApplyCompletedFilter(query, userId),
            _ => query,
        };
    }

    private IQueryable<CommitteeChanges> ApplyInProcessFilter(IQueryable<CommitteeChanges> query, UserId userId, BusinessUnitId? userPrimaryDepartmentId, bool isJorPor)
    {
        var expr = HasCurrentAcceptorRole(userId)
                       .Or(HasProcurementAccess(userPrimaryDepartmentId, isJorPor));

        return query.Where(expr);
    }

    private static Expression<Func<CommitteeChanges, bool>> HasCurrentAcceptorRole(UserId userId)
    {
        return c => c.Acceptors.Any(a =>
            a.IsCurrent &&
            a.UserId == userId &&
            c.Status == CommitteeChangeStatus.WaitingApproval);
    }

    private static Expression<Func<CommitteeChanges, bool>> HasProcurementAccess(BusinessUnitId? userPrimaryDepartmentId, bool isJorPor)
    {
        return c => (c.Status == CommitteeChangeStatus.Draft ||
                     c.Status == CommitteeChangeStatus.Edit ||
                     c.Status == CommitteeChangeStatus.Rejected) &&
                    (isJorPor || userPrimaryDepartmentId == null || c.Procurement.DepartmentId == userPrimaryDepartmentId);
    }

    private IQueryable<CommitteeChanges> ApplyRelatedFilter(IQueryable<CommitteeChanges> query, UserId userId)
    {
        var expression = HasRelatedAcceptorHistory(userId);

        return query.Where(expression);
    }

    private static Expression<Func<CommitteeChanges, bool>> HasRelatedAcceptorHistory(UserId userId)
    {
        return c => c.Acceptors.Any(a => !a.IsCurrent && a.UserId == userId &&
                                         (a.Status == AcceptorStatus.Approved || a.Status == AcceptorStatus.Rejected));
    }

    private IQueryable<CommitteeChanges> ApplyCompletedFilter(IQueryable<CommitteeChanges> query, UserId userId)
    {
        var expression = IsCompletedCommitteeChange(userId);

        return query.Where(expression);
    }

    private static Expression<Func<CommitteeChanges, bool>> IsCompletedCommitteeChange(UserId userId)
    {
        return c => c.Status == CommitteeChangeStatus.Approved &&
                    c.Acceptors.Any(a => a.UserId == userId && a.Status == AcceptorStatus.Approved);
    }

    private static IQueryable<CommitteeChanges> ApplyRemainingFilters(IQueryable<CommitteeChanges> query, GetChangeCommitteeListRequest req)
    {
        return query
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.Keyword),
                   c => EF.Functions.ILike(c.Procurement.Name, $"%{req.Keyword}%") ||
                        (c.Procurement.ProcurementNumber != null && EF.Functions.ILike((string)c.Procurement.ProcurementNumber, $"%{req.Keyword}%")) ||
                        EF.Functions.ILike(c.Remark ?? string.Empty, $"%{req.Keyword}%"))
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.DepartmentCode),
                   c => c.Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
               .WhereIfTrue(req.BudgetYear.HasValue, c => c.Procurement.BudgetYear == req.BudgetYear)
               .WhereIfTrue(
                   !string.IsNullOrEmpty(req.SupplyMethodCode),
                   c => c.Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
               .WhereIfTrue(
                   !string.IsNullOrEmpty(req.SupplyMethodTypeCode),
                   c => c.Procurement.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
               .WhereIfTrue(
                   !string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode),
                   c => c.Procurement.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
               .WhereIfTrue(req.CommitteeGroupType.HasValue, c => c.CommitteeType == req.CommitteeGroupType!);
    }

    private static async Task<List<CommitteeChanges>> GetFullListAsync(IQueryable<CommitteeChanges> baseQuery, CancellationToken ct)
    {
        return await baseQuery.ToListAsync(ct);
    }

    private static (int All, int Draft, int Edit, int WaitingApproval, int Approved, int Rejected, int Cancelled) CalculateStatusCounts(List<CommitteeChanges> fullList)
    {
        return (
            All: fullList.Count,
            Draft: fullList.Count(c => c.Status == CommitteeChangeStatus.Draft),
            Edit: fullList.Count(c => c.Status == CommitteeChangeStatus.Edit),
            WaitingApproval: fullList.Count(c => c.Status == CommitteeChangeStatus.WaitingApproval),
            Approved: fullList.Count(c => c.Status == CommitteeChangeStatus.Approved),
            Rejected: fullList.Count(c => c.Status == CommitteeChangeStatus.Rejected),
            Cancelled: fullList.Count(c => c.Status == CommitteeChangeStatus.Cancelled));
    }
}
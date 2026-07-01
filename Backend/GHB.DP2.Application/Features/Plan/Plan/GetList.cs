namespace GHB.DP2.Application.Features.Plan.Plan;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.WorkList;
using GHB.DP2.Application.Features.WorkList.Programs;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPlanListRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber,
    int PageSize,
    string? Keyword,
    PlanType? Type,
    string? DepartmentCode,
    int? BudgetYear,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    PlanStatus? Status,
    bool? IsChange,
    bool? IsCancel,
    WorkProcess WorkProcess = WorkProcess.InProcess)
    : IPlanBaseQuery;

public record GetPlanListResponse(
    Guid Id,
    string PlanNumber,
    string Name,
    int BudgetYear,
    decimal Budget,
    PlanType Type,
    string DepartmentName,
    BusinessUnitId DepartmentCode,
    string SupplyMethod,
    bool IsChange,
    bool IsCancel,
    PlanStatus Status,
    bool OldData);

public record GetStatusCount(
    int All,
    int DraftPlan,
    int EditPlan,
    int WaitingApprovePlan,
    int WaitingAssign,
    int Assigned,
    int DraftRecordDocument,
    int RejectToAssignee,
    int WaitingAcceptor,
    int ApprovePlan,
    int WaitingAnnouncement,
    int Announcement,
    int RejectPlan,
    int CancelPlan,
    int Closed);

public record GetPlanListResult(
    GetStatusCount StatusCount,
    PaginatedQueryResult<GetPlanListResponse> Data
);

public class GetPlanList : EndpointBase<GetPlanListRequest, Ok<GetPlanListResult>>
{
    private readonly Dp2DbContext dbContext;
    private readonly PlanProgram planProgram;

    public GetPlanList(
        Dp2DbContext dbContext,
        ILogger<GetPlanList> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.planProgram = new PlanProgram();
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Plan")
             .WithName("GetPlanList")
             .Produces<Ok>());
        this.Get("plan");
    }

    protected override async ValueTask<Ok<GetPlanListResult>> HandleRequestAsync(GetPlanListRequest req, CancellationToken ct)
    {
        var query = req.WorkProcess switch
        {
            WorkProcess.InProcess => await this.GetInprogressPlansAsync(req, ct),
            WorkProcess.Related => await this.GetRelatedPlansAsync(req, ct),
            WorkProcess.Completed => await this.GetCompletedPlansAsync(req, ct),
            _ => this.PlansListBaseQuery(req),
        };

        query = query.OrderByDescending(o => o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt);

        var paginatedQuery =
            query
                .Where(x => !x.IsDeleted)
                .WhereIfTrue(
                    !req.Status.IsNull(),
                    p => p.Status == req.Status)
                .Select(x => new GetPlanListResponse(
                    x.Id.Value,
                    x.PlanNumber.Value,
                    x.Name,
                    x.BudgetYear,
                    x.Budget,
                    x.Type,
                    x.Department.Name,
                    x.DepartmentId,
                    x.SupplyMethod.Label,
                    x.IsChange,
                    x.IsCancel,
                    x.Status,
                    false));

        var paginated =
            await PaginatedList<GetPlanListResponse>
                .CreateAsync(paginatedQuery, req.PageNumber, req.PageSize, ct);

        var result = await
            query.Select(p => new { p.Id, p.Status })
                 .ToListAsync(ct);

        var statusCount = new GetStatusCount(
            result.Count,
            result.Count(s => s.Status == PlanStatus.DraftPlan),
            result.Count(s => s.Status == PlanStatus.EditPlan),
            result.Count(s => s.Status == PlanStatus.WaitingApprovePlan),
            result.Count(s => s.Status is PlanStatus.WaitingAssign),
            result.Count(s => s.Status == PlanStatus.Assigned),
            result.Count(s => s.Status == PlanStatus.DraftRecordDocument),
            result.Count(s => s.Status == PlanStatus.RejectToAssignee),
            result.Count(s => s.Status == PlanStatus.WaitingAcceptor),
            result.Count(s => s.Status == PlanStatus.ApprovePlan),
            result.Count(s => s.Status == PlanStatus.WaitingAnnouncement),
            result.Count(s => s.Status == PlanStatus.Announcement),
            result.Count(s => s.Status == PlanStatus.RejectPlan),
            result.Count(s => s.Status == PlanStatus.CancelPlan),
            result.Count(s => s.Status == PlanStatus.Closed));

        var data = paginated.ToResult();

        return TypedResults.Ok(new GetPlanListResult(statusCount, data));
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

    private async Task<IQueryable<Plan>> GetInprogressPlansAsync(GetPlanListRequest req, CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);
        var userContext = await this.GetUserContextAsync(userId, ct);

        if (userContext is null)
        {
            return null!;
        }

        var workProcessStatus =
            new[]
            {
                PlanStatus.DraftPlan,
                PlanStatus.RejectPlan,
                PlanStatus.EditPlan,
            };

        var query =
            this.planProgram.BuildPlanBaseQuery(this.dbContext, req, userContext);

        return query
               .Where(p => workProcessStatus.Contains(p.Status))
               .WhereIfTrue(req.IsChange is true, x => x.IsChange)
               .WhereIfTrue(req.IsCancel is true, x => x.IsCancel);
    }

    private IQueryable<Plan> PlansListBaseQuery(GetPlanListRequest req)
        => this.dbContext.Plans
               .Where(p =>
                   (req.IsChange != true && req.IsCancel != true && p.IsActive) ||
                   (req.IsChange == true && !p.IsActive && !p.IsDeleted && p.IsChange && !p.IsCancel) ||
                   (req.IsCancel == true && !p.IsActive && !p.IsDeleted && p.IsCancel))
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.Keyword),
                   x =>
                       EF.Functions.ILike(x.Name, $"%{req.Keyword}%") ||
                       EF.Functions.ILike((string)x.PlanNumber, $"%{req.Keyword}%"))
               .WhereIfTrue(
                   !req.Type.IsNull(),
                   x => x.Type == req.Type)
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
                   !string.IsNullOrEmpty(req.SupplyMethodTypeCode),
                   x => x.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
               .WhereIfTrue(
                   !string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode),
                   x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
               .WhereIfTrue(req.IsChange is true, x => x.IsChange)
               .WhereIfTrue(req.IsCancel is true, x => x.IsCancel);

    private async Task<List<UserId>> GetUsersAsync(GetPlanListRequest req, CancellationToken ct)
    {
        var userIdObj = UserId.From(req.UserId);

        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.Positions)
                             .ThenInclude(p => p.BusinessUnit)
                             .Include(suUser => suUser.Employee)
                             .ThenInclude(rawEmployee => rawEmployee.View)
                             .Include(suUser => suUser.Delegatees)
                             .ThenInclude(suDelegatee => suDelegatee.SuDelegator)
                             .FirstOrDefaultAsync(u => u.Id == userIdObj, ct);

        var viewUser = user?.Employee?.View;

        if (viewUser is null)
        {
            return null!;
        }

        var userDelegatee = user!.Delegatees.Select(d => d.SuDelegator.SuUserId);

        return [.. userDelegatee, user.Id];
    }

    private async Task<IQueryable<Plan>> GetRelatedPlansAsync(
        GetPlanListRequest req,
        CancellationToken ct)
    {
        var workProcessStatus =
            new[]
            {
                PlanStatus.WaitingApprovePlan,
                PlanStatus.Assigned,
                PlanStatus.DraftRecordDocument,
                PlanStatus.WaitingAcceptor,
                PlanStatus.WaitingAnnouncement,
            };

        var notInWorkProcessStatus =
            new[]
            {
                PlanStatus.ApprovePlan,
                PlanStatus.Announcement,
            };

        var userIds = await this.GetUsersAsync(req, ct);

        return this.PlansListBaseQuery(req)
                   .Where(p =>
                       (
                           !notInWorkProcessStatus.Contains(p.Status) &&
                           p.Acceptors.Any(a =>
                               (!a.IsCurrent && userIds.Contains(a.UserId) && a.Status == AcceptorStatus.Approved) ||
                               userIds.Select(u => u.Value).Contains(p.AuditInfo.CreatedBy))
                       ) ||
                       (
                           p.Status == PlanStatus.Assigned &&
                           p.Assignees.Any(a => userIds.Contains(a.UserId) && a.Type == AssigneeType.Director)));
    }

    private async Task<IQueryable<Plan>> GetCompletedPlansAsync(GetPlanListRequest req, CancellationToken ct)
    {
        var workProcessStatus =
            new[]
            {
                PlanStatus.Announcement,
                PlanStatus.ApprovePlan,
                PlanStatus.Closed,
            };

        var userIds = await this.GetUsersAsync(req, ct);

        return this.PlansListBaseQuery(req)
                   .Where(p =>
                       p.Acceptors.Any(a =>
                           userIds.Contains(a.UserId) &&
                           a.Status == AcceptorStatus.Approved) ||
                       workProcessStatus.Contains(p.Status));
    }
}
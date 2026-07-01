namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.WorkList;
using GHB.DP2.Application.Features.WorkList.Programs;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetPlanAnnouncementListRequest : IPlanAnnouncementBaseQuery
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public WorkProcess WorkProcess { get; init; } = WorkProcess.InProcess;

    public string? SearchText { get; init; }

    public string? Keyword => this.SearchText;

    public int? BudgetYear => this.FromBudgetYear;

    public string? SupplyMethodCode { get; init; }

    public int? FromBudgetYear { get; init; }

    public int? ToBudgetYear { get; init; }

    public DateTimeOffset? FromAnnouncementDate { get; init; }

    public DateTimeOffset? ToAnnouncementDate { get; init; }

    public PlanAnnouncementStatus? Status { get; init; }
}

public record GetPlanAnnouncementListResponse(
    Guid Id,
    string AnnouncementNumber,
    string? AnnouncementName,
    int Year,
    int PlanCount,
    decimal SummaryBudget,
    string SupplyMethodName,
    DateTimeOffset? AnnouncementDate,
    PlanAnnouncementStatus Status);

public record PlanAnnouncementStatusCount(
    int All,
    int Draft,
    int WaitingAssign,
    int WaitingAcceptor,
    int WaitingAnnouncement,
    int Announcement,
    int Rejected);

public record GetPlanAnnouncementListResult(
    PlanAnnouncementStatusCount Counts,
    PaginatedQueryResult<GetPlanAnnouncementListResponse> Data);

public class GetPlanAnnouncementList : EndpointBase<GetPlanAnnouncementListRequest, Ok<GetPlanAnnouncementListResult>>
{
    private readonly Dp2DbContext dbContext;
    private readonly PlanAnnouncementProgram planAnnouncementProgram;

    public GetPlanAnnouncementList(
        Dp2DbContext dbContext,
        ILogger<GetPlanAnnouncementList> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.planAnnouncementProgram = new PlanAnnouncementProgram();
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(PlanAnnouncement))
             .WithName("GetPlanAnnouncementList"));
        this.Get("plan/announcement");
    }

    protected override async ValueTask<Ok<GetPlanAnnouncementListResult>> HandleRequestAsync(
        GetPlanAnnouncementListRequest req,
        CancellationToken ct)
    {
        var query = req.WorkProcess switch
        {
            WorkProcess.InProcess => await this.GetInprogressPlanAnnouncementsAsync(req, ct),
            WorkProcess.Related => this.GetRelatedPlanAnnouncements(req),
            WorkProcess.Completed => this.GetCompletedPlanAnnouncements(req),
            _ => this.PlanAnnouncementBaseQuery(req),
        };

        query =
            query.OrderByDescending(o =>
                o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt);

        var paginated = await PaginatedList<PlanAnnouncement>.CreateAsync(
            query.WhereIfTrue(
                     req.Status.HasValue,
                     x => x.Status == req.Status)
                 .AsNoTracking(),
            req.PageNumber,
            req.PageSize,
            ct);

        var result =
            await query
                  .Select(p => new { p.Id, p.Status })
                  .AsNoTracking().ToListAsync(ct);

        var statusCount = new PlanAnnouncementStatusCount(
            result.Count,
            result.Count(s => s.Status == PlanAnnouncementStatus.Draft),
            result.Count(s => s.Status == PlanAnnouncementStatus.WaitingAssign),
            result.Count(s => s.Status == PlanAnnouncementStatus.WaitingAcceptor),
            result.Count(s => s.Status == PlanAnnouncementStatus.WaitingAnnouncement),
            result.Count(s => s.Status == PlanAnnouncementStatus.Announcement),
            result.Count(s => s.Status == PlanAnnouncementStatus.Rejected));

        var data = paginated.ToResult(static p =>
            new GetPlanAnnouncementListResponse(
                p.Id.Value,
                p.PlanAnnouncementNumber.Value,
                p.AnnouncementTitle,
                p.Year,
                p.AnnouncementSelectedInformations.Count,
                p.AnnouncementSelectedInformations.Select(s => s.Plan).Sum(s => s.Budget),
                p.SupplyMethodInfo.Label,
                p.AnnouncementDate,
                p.Status));

        return TypedResults.Ok(new GetPlanAnnouncementListResult(statusCount, data));
    }

    private IQueryable<PlanAnnouncement> PlanAnnouncementBaseQuery(GetPlanAnnouncementListRequest req)
        => this.dbContext.PlanAnnouncements
               .Include(pas => pas.AnnouncementSelectedInformations)
               .ThenInclude(p => p.Plan)
               .Include(pas => pas.Acceptors) // ensure acceptors loaded for filtering (if navigation exists)
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.SearchText),
                   x => EF.Functions.ILike((string)x.PlanAnnouncementNumber, $"%{req.SearchText}%") || EF.Functions.ILike(x.AnnouncementTitle ?? string.Empty, $"%{req.SearchText}%"))
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                   x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
               .WhereIfTrue(
                   req.FromBudgetYear.HasValue,
                   x => x.Year >= req.FromBudgetYear)
               .WhereIfTrue(
                   req.ToBudgetYear.HasValue,
                   x => x.Year <= req.ToBudgetYear)
               .WhereIfTrue(
                   req.FromAnnouncementDate.HasValue,
                   x => x.AnnouncementDate >= req.FromAnnouncementDate.Value)
               .WhereIfTrue(
                   req.ToAnnouncementDate.HasValue,
                   x => x.AnnouncementDate <= req.ToAnnouncementDate.Value);

    private async Task<UserContext?> GetUserContextAsync(
        UserId userId,
        CancellationToken ct)
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

    private async Task<IQueryable<PlanAnnouncement>> GetInprogressPlanAnnouncementsAsync(
        GetPlanAnnouncementListRequest req,
        CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);

        var userContext = await this.GetUserContextAsync(userId, ct);

        if (userContext is null)
        {
            return null!;
        }

        var query =
            this.planAnnouncementProgram
                .BuildPlanAnnouncementBaseQuery(
                    this.dbContext,
                    req,
                    userContext.EffectiveUserIds);

        return query;
    }

    private IQueryable<PlanAnnouncement> GetRelatedPlanAnnouncements(GetPlanAnnouncementListRequest req)
    {
        var userId = UserId.From(req.UserId);

        return this.PlanAnnouncementBaseQuery(req)
                   .Where(a =>
                       ((a.Status == PlanAnnouncementStatus.WaitingAcceptor ||
                         a.Status == PlanAnnouncementStatus.WaitingAnnouncement) &&
                        a.Acceptors.Any(ac =>
                            !ac.IsCurrent &&
                            ac.UserId == userId &&
                            (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))) ||
                       (a.Status == PlanAnnouncementStatus.WaitingAcceptor &&
                        a.Assignees.Any(assignee => assignee.UserId == userId && assignee.Type == AssigneeType.Assignee)));
    }

    private IQueryable<PlanAnnouncement> GetCompletedPlanAnnouncements(GetPlanAnnouncementListRequest req)
    {
        var userId = UserId.From(req.UserId);

        return this.PlanAnnouncementBaseQuery(req)
                   .Where(a =>
                       (a.Status == PlanAnnouncementStatus.Announcement &&
                        a.Acceptors.Any(ac =>
                            ac.UserId == userId &&
                            ac.Status == AcceptorStatus.Approved)) ||
                       (a.Status == PlanAnnouncementStatus.Announcement &&
                        a.Assignees.Any(assignee => assignee.UserId == userId && assignee.Type == AssigneeType.Director)) ||
                       (a.Status == PlanAnnouncementStatus.Announcement &&
                        a.Assignees.Any(assignee => assignee.UserId == userId && assignee.Type == AssigneeType.Assignee)));
    }
}
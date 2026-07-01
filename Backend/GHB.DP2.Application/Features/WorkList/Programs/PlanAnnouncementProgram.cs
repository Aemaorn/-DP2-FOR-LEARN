namespace GHB.DP2.Application.Features.WorkList.Programs;

using GHB.DP2.Domain.SystemUtility;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;

public interface IPlanAnnouncementBaseQuery
{
    string? Keyword { get; }

    int? BudgetYear { get; }

    string? SupplyMethodCode { get; }
}

public sealed class PlanAnnouncementProgram
{
    public async Task<(int Count, SectionResult<PlanAnnouncementItem>? Section)> ProcessPlanAnnouncementSectionAsync(
        Dp2DbContext dbContext,
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        CancellationToken ct)
    {
        var baseQuery = this.BuildPlanAnnouncementBaseQuery(dbContext, req, userContext.EffectiveUserIds);
        var count = await baseQuery.CountAsync(ct);

        SectionResult<PlanAnnouncementItem>? section = null;

        if (req.IncludeAnnouncements)
        {
            var pageQuery = baseQuery.OrderByDescending(o => o.AuditInfo.LastModifiedAt).ThenByDescending(x => x.PlanAnnouncementNumber).AsNoTracking();
            var page = await PaginatedList<PlanAnnouncement>.CreateAsync(pageQuery, req.PageNumber, req.PageSize, ct);
            var pageDto = page.ToResult(static p => new PlanAnnouncementItem(
                p.Id.Value,
                p.PlanAnnouncementNumber.Value,
                p.AnnouncementTitle,
                p.Year,
                p.AnnouncementSelectedInformations.Count,
                p.AnnouncementSelectedInformations.Any() ? p.AnnouncementSelectedInformations.Sum(s => s.Plan?.Budget ?? 0m) : 0m,
                p.SupplyMethodInfo.Label,
                p.AnnouncementDate,
                p.Status));
            section = new SectionResult<PlanAnnouncementItem>(pageDto);
        }

        return (count, section);
    }

    public IQueryable<PlanAnnouncement> BuildPlanAnnouncementBaseQuery(
        Dp2DbContext dbContext,
        IPlanAnnouncementBaseQuery req,
        IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userIds = effectiveUserIds.Select(e => e.UserId);

        return dbContext.PlanAnnouncements
                        .Include(pas => pas.AnnouncementSelectedInformations)
                        .ThenInclude(p => p.Plan)
                        .Include(pas => pas.Acceptors)
                        .Include(pas => pas.SupplyMethodInfo)
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x => EF.Functions.ILike((string)x.PlanAnnouncementNumber, $"%{req.Keyword}%"))
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.SupplyMethodCode), x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                        .WhereIfTrue(req.BudgetYear.HasValue, x => x.Year >= req.BudgetYear)
                        .WhereIfTrue(req.BudgetYear.HasValue, x => x.Year <= req.BudgetYear)
                        .Where(a =>
                            (
                                (a.Status == PlanAnnouncementStatus.Draft ||
                                 a.Status == PlanAnnouncementStatus.WaitingAnnouncement ||
                                 a.Status == PlanAnnouncementStatus.WaitingAssign ||
                                 a.Status == PlanAnnouncementStatus.Rejected) &&
                                a.Assignees.Any(s => userIds.Contains(s.UserId))
                            ) ||
                            (
                                a.Status == PlanAnnouncementStatus.WaitingAssign &&
                                userIds.Contains(a.Assignees.Where(a => a.Type == AssigneeType.Assignee)
                                                  .OrderBy(a => a.Sequence)
                                                  .Last().UserId)
                            ) ||
                            (
                                a.Status == PlanAnnouncementStatus.WaitingAcceptor &&
                                a.Acceptors.Any(ac =>
                                    ac.IsCurrent &&
                                    (
                                        userIds.Contains(ac.UserId) ||
                                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                                    ))
                            )
                            ||
                            (
                                a.Status == PlanAnnouncementStatus.WaitingAnnouncement &&
                                a.Assignees.Any(a => a.Type == AssigneeType.Director && a.Group == AssigneeGroup.JorPor && userIds.Contains(a.UserId))));
    }
}

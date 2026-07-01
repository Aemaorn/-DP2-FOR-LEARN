namespace GHB.DP2.Application.Features.WorkList.Programs;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

public interface IPlanBaseQuery
{
    string? Keyword { get; }

    string? DepartmentCode { get; }

    int? BudgetYear { get; }

    string? SupplyMethodCode { get; }

    string? SupplyMethodTypeCode { get; }

    string? SupplyMethodSpecialTypeCode { get; }
}

public sealed class PlanProgram
{
    public async Task<(int Count, SectionResult<PlanItem>? Section)> ProcessPlanSectionAsync(
        Dp2DbContext dbContext,
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        CancellationToken ct)
    {
        var baseQuery = this.BuildPlanBaseQuery(dbContext, req, userContext);
        var count = await baseQuery.CountAsync(ct);

        SectionResult<PlanItem>? section = null;

        if (req.IncludePlans)
        {
            var pageQuery = baseQuery.OrderByDescending(o => o.AuditInfo.LastModifiedAt).ThenByDescending(x => x.PlanNumber).AsNoTracking();
            var page = await PaginatedList<Plan>.CreateAsync(pageQuery, req.PageNumber, req.PageSize, ct);
            var pageDto = page.ToResult(static x => new PlanItem(
                x.Id.Value,
                x.PlanNumber.Value,
                x.Name,
                x.BudgetYear,
                x.Budget,
                x.Type,
                x.Department?.Name ?? string.Empty,
                x.SupplyMethod.Label,
                x.IsChange,
                x.IsCancel,
                x.Status));
            section = new SectionResult<PlanItem>(pageDto);
        }

        return (count, section);
    }

    public IQueryable<Plan> BuildPlanBaseQuery(
        Dp2DbContext dbContext,
        IPlanBaseQuery req,
        UserContext userContext)
    {
        var userIds = userContext.EffectiveUserIds.Select(e => e.UserId);
        var createdBys = userContext.EffectiveUserIds.Select(e => e.UserId.Value);

        return dbContext.Plans
                        .Where(p => p.IsActive)
                        .Where(p => p.Status != PlanStatus.Closed)
                        .Include(r => r.SupplyMethod)
                        .Include(d => d.Department)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            x =>
                                EF.Functions.ILike(x.Name, $"%{req.Keyword}%") ||
                                EF.Functions.ILike((string)x.PlanNumber, $"%{req.Keyword}%"))
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
                            !string.IsNullOrEmpty(req.SupplyMethodTypeCode), x => x.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                        .WhereIfTrue(
                            !string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode),
                            x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                        .Where(p =>
                            (
                                (p.Status == PlanStatus.DraftPlan || p.Status == PlanStatus.RejectPlan || p.Status == PlanStatus.EditPlan) &&
                                createdBys.Contains(p.AuditInfo.CreatedBy)
                            )
                            ||
                            (
                                (p.Status == PlanStatus.WaitingApprovePlan || p.Status == PlanStatus.WaitingAcceptor) &&
                                p.Acceptors.Any(a =>
                                    (a.Type == AcceptorType.Approver || a.Type == AcceptorType.DepartmentDirectorAgree) &&
                                    a.Status == AcceptorStatus.Pending &&
                                    (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                    !p.Acceptors.Any(b =>
                                        (b.Type == AcceptorType.Approver || b.Type == AcceptorType.DepartmentDirectorAgree) &&
                                        b.Status == AcceptorStatus.Pending &&
                                        b.Sequence < a.Sequence))
                            )
                            ||
                            (
                                (p.Status == PlanStatus.WaitingAssign || p.Status == PlanStatus.WaitingAnnouncement) &&
                                p.Assignees.Any() &&
                                userIds.Contains(p.Assignees.OrderBy(a => a.Sequence).Last().UserId)
                            )
                            ||
                            (
                                (p.Status == PlanStatus.DraftRecordDocument || p.Status == PlanStatus.RejectToAssignee || p.Status == PlanStatus.Assigned) &&
                                p.Assignees.Any(a => a.Type == AssigneeType.Assignee) &&
                                userIds.Contains(
                                    p.Assignees
                                     .Where(a => a.Type == AssigneeType.Assignee)
                                     .OrderBy(a => a.Sequence)
                                     .Last().UserId)
                            )
                            ||
                            (p.Status == PlanStatus.WaitingAnnouncement &&
                             p.Assignees.Any(a => a.Type == AssigneeType.Director && a.Group == AssigneeGroup.JorPor) &&
                             userIds.Contains(
                                 p.Assignees
                                  .First(a => a.Type == AssigneeType.Director && a.Group == AssigneeGroup.JorPor).UserId)
                            ));
    }
}
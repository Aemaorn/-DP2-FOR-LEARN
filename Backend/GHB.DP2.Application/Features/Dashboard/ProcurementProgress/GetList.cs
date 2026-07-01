namespace GHB.DP2.Application.Features.Dashboard.ProcurementProgress;

using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Dashboard;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public enum DateFilterType
{
    PlanDate,
    PurchaseOrderDate,
    DocPrepareNotifyDate,
    ContractDate,
}

public record GetProcurementProgressListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? SupplyMethodSpecialTypeLabel,
    ProcurementProgressStatus? Status,
    DateFilterType? DateType,
    DateOnly? DateFrom,
    DateOnly? DateTo);

public record GetProcurementProgressListItemResponse(
    Guid PlanId,
    string PlanNumber,
    int BudgetYear,
    string DepartmentName,
    string ProjectName,
    string SupplyMethod,
    string? SupplyMethodSpecialType,
    Guid? SummaryId,
    string? PlanDate,
    string? PurchaseOrderDate,
    string? DocPrepareNotifyDate,
    string? ContractDate,
    ProcurementProgressStatus? Status);

file sealed record ProcurementProgressListIntermediate(
    Guid PlanId,
    string PlanNumber,
    int BudgetYear,
    string DepartmentName,
    string ProjectName,
    string SupplyMethod,
    string? SupplyMethodSpecialType,
    Guid? SummaryId,
    string? PlanDate,
    string? PurchaseOrderDate,
    string? DocPrepareNotifyDate,
    string? SummaryContractDate,
    DateTimeOffset? VendorContractSignedDate,
    ProcurementProgressStatus? Status);

public class GetProcurementProgressList
    : EndpointBase<GetProcurementProgressListRequest, Ok<PaginatedQueryResult<GetProcurementProgressListItemResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetProcurementProgressList(Dp2DbContext dbContext, ILogger<GetProcurementProgressList> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Dashboard")
             .WithName("GetProcurementProgressList")
             .Produces<Ok>());
        this.Get("dashboard/procurement-progress");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetProcurementProgressListItemResponse>>> HandleRequestAsync(
        GetProcurementProgressListRequest req,
        CancellationToken ct)
    {
        var targetStatuses = new[] { PlanStatus.ApprovePlan, PlanStatus.Announcement };

        var plansQuery = this.dbContext.Plans
            .Where(p => p.IsActive && !p.IsDeleted && targetStatuses.Contains(p.Status))
            .WhereIfTrue(
                !string.IsNullOrWhiteSpace(req.Keyword),
                p => EF.Functions.ILike(p.Name, $"%{req.Keyword}%") ||
                     EF.Functions.ILike((string)p.PlanNumber, $"%{req.Keyword}%"))
            .WhereIfTrue(
                !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeLabel),
                p => p.SupplyMethodSpecialType != null &&
                     EF.Functions.ILike(p.SupplyMethodSpecialType.Label, $"%{req.SupplyMethodSpecialTypeLabel}%"));

        var status = req.Status;
        var dateType = req.DateType;
        var dateFrom = req.DateFrom;
        var dateTo = req.DateTo;
        var hasDateFilter = dateType.HasValue && (dateFrom.HasValue || dateTo.HasValue);

        var vendorSignedDateQuery = this.dbContext.CaContractDraftVendors
            .Select(v => new
            {
                v.ContractSignedDate,
                ProcurementId = v.ContractDraft.ProcurementId,
            })
            .Join(
                this.dbContext.Procurements,
                v => v.ProcurementId,
                proc => proc.Id,
                (v, proc) => new { v.ContractSignedDate, proc.PlanId })
            .Where(x => x.PlanId != null && x.ContractSignedDate != null)
            .GroupBy(x => x.PlanId)
            .Select(g => new { PlanId = g.Key, ContractSignedDate = g.Max(x => x.ContractSignedDate) });

        var projected = plansQuery
            .GroupJoin(
                this.dbContext.ProcurementProgressSummaries,
                p => p.Id,
                s => s.PlanId,
                (p, sGroup) => new { p, sGroup })
            .SelectMany(
                x => x.sGroup.DefaultIfEmpty(),
                (x, s) => new { x.p, s })
            .GroupJoin(
                vendorSignedDateQuery,
                x => x.p.Id,
                vsd => vsd.PlanId,
                (x, vsdGroup) => new { x.p, x.s, vsdGroup })
            .SelectMany(
                x => x.vsdGroup.DefaultIfEmpty(),
                (x, vsd) => new { x.p, x.s, vsd })
            .Where(x => !status.HasValue || (x.s != null && x.s.Status == status.Value))
            .Where(x => !hasDateFilter
                || (dateType == DateFilterType.PlanDate && x.s != null
                    && (!dateFrom.HasValue || x.s.PlanDate >= dateFrom.Value)
                    && (!dateTo.HasValue || x.s.PlanDate <= dateTo.Value))
                || (dateType == DateFilterType.PurchaseOrderDate && x.s != null
                    && (!dateFrom.HasValue || x.s.PurchaseOrderDate >= dateFrom.Value)
                    && (!dateTo.HasValue || x.s.PurchaseOrderDate <= dateTo.Value))
                || (dateType == DateFilterType.DocPrepareNotifyDate && x.s != null
                    && (!dateFrom.HasValue || x.s.DocPrepareNotifyDate >= dateFrom.Value)
                    && (!dateTo.HasValue || x.s.DocPrepareNotifyDate <= dateTo.Value))
                || (dateType == DateFilterType.ContractDate && x.s != null
                    && (!dateFrom.HasValue || x.s.ContractDate >= dateFrom.Value)
                    && (!dateTo.HasValue || x.s.ContractDate <= dateTo.Value)))
            .OrderByDescending(x => x.p.AuditInfo.LastModifiedAt ?? x.p.AuditInfo.CreatedAt)
            .Select(x => new ProcurementProgressListIntermediate(
                x.p.Id.Value,
                x.p.PlanNumber.Value,
                x.p.BudgetYear,
                x.p.Department.Name,
                x.p.Name,
                x.p.SupplyMethod.Label,
                x.p.SupplyMethodSpecialType != null ? x.p.SupplyMethodSpecialType.Label : null,
                x.s != null ? (Guid?)x.s.Id.Value : null,
                x.s != null && x.s.PlanDate.HasValue ? x.s.PlanDate.Value.ToString("yyyy-MM-dd") : null,
                x.s != null && x.s.PurchaseOrderDate.HasValue ? x.s.PurchaseOrderDate.Value.ToString("yyyy-MM-dd") : null,
                x.s != null && x.s.DocPrepareNotifyDate.HasValue ? x.s.DocPrepareNotifyDate.Value.ToString("yyyy-MM-dd") : null,
                x.s != null && x.s.ContractDate.HasValue ? x.s.ContractDate.Value.ToString("yyyy-MM-dd") : null,
                x.vsd != null ? x.vsd.ContractSignedDate : null,
                x.s != null ? x.s.Status : null));

        var paginated = await Codehard.Infrastructure.EntityFramework.PaginatedList<ProcurementProgressListIntermediate>
            .CreateAsync(projected, req.PageNumber, req.PageSize, ct);

        return TypedResults.Ok(paginated.ToResult(x =>
        {
            var contractDate = x.SummaryContractDate ?? x.VendorContractSignedDate?.ToString("yyyy-MM-dd");

            return new GetProcurementProgressListItemResponse(
                x.PlanId,
                x.PlanNumber,
                x.BudgetYear,
                x.DepartmentName,
                x.ProjectName,
                x.SupplyMethod,
                x.SupplyMethodSpecialType,
                x.SummaryId,
                x.PlanDate,
                x.PurchaseOrderDate,
                x.DocPrepareNotifyDate,
                contractDate,
                x.Status);
        }));
    }
}

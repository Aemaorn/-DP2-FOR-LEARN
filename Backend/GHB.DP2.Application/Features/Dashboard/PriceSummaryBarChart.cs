namespace GHB.DP2.Application.Features.Dashboard;

using GHB.DP2.Domain.Common;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPriceSummaryBarChartRequest(
    int? BudgetYear,
    string? Keyword,
    string? DepartmentId,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    int? Month,
    int? Quarter);

public record PriceSummaryBarChartItem(
    string Period,
    decimal TotalBudget,
    decimal TotalMedianPrice,
    decimal TotalOfferedPrice,
    decimal TotalAgreedPrice);

public class GetPriceSummaryBarChartEndpoint
    : EndpointBase<GetPriceSummaryBarChartRequest, Ok<List<PriceSummaryBarChartItem>>>
{
    private readonly Dp2DbContext dbContext;

    public GetPriceSummaryBarChartEndpoint(
        ILogger<GetPriceSummaryBarChartEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("dashboard/price-summary/bar-chart");
        this.Options(o => o.WithTags("Dashboard"));
    }

    protected override async ValueTask<Ok<List<PriceSummaryBarChartItem>>> HandleRequestAsync(
        GetPriceSummaryBarChartRequest req, CancellationToken ct)
    {
        var filter = new PriceSummaryFilter(req.BudgetYear, req.Keyword, req.SupplyMethodCode, req.SupplyMethodSpecialTypeCode, req.Month, req.Quarter, req.DepartmentId);
        var query = await PriceSummaryQuery.BuildAsync(this.dbContext, filter, ct);

        var rawItems = await query
            .Include(po => po.Procurement)
            .ThenInclude(p => p.PurchaseRequisitions)
            .Include(po => po.Entrepreneurs.Where(e => e.IsWinner))
            .ThenInclude(e => e.PJp006PriceDetails)
            .Include(po => po.Acceptors)
            .AsSplitQuery()
            .ToListAsync(ct);

        var rows = rawItems.Select(po =>
        {
            var budget = po.Procurement.Budget ?? 0m;
            var median = po.Procurement.PurchaseRequisitions
                .Select(pr => pr.MedianPriceAmount).FirstOrDefault() ?? 0m;
            var offered = po.Entrepreneurs
                .Where(e => e.IsWinner)
                .SelectMany(e => e.PJp006PriceDetails)
                .Sum(pd => pd.OfferedPrice * pd.ParcelQuantity);
            var agreed = po.Entrepreneurs
                .Where(e => e.IsWinner)
                .SelectMany(e => e.PJp006PriceDetails)
                .Sum(pd => pd.AgreedPrice * pd.ParcelQuantity);
            var approvedAt = po.Acceptors
                .Where(a => a.Type == AcceptorType.Approver && a.Status == AcceptorStatus.Approved)
                .OrderByDescending(a => a.Sequence)
                .Select(a => a.ActionAt)
                .FirstOrDefault();
            return new { budget, median, offered, agreed, approvedAt };
        }).ToList();

        // Summary (ภาพรวม)
        var summary = new PriceSummaryBarChartItem(
            "ภาพรวม",
            rows.Sum(r => r.budget),
            rows.Sum(r => r.median),
            rows.Sum(r => r.offered),
            rows.Sum(r => r.agreed));

        // Quarter
        var quarterLabels = new[] { "ไตรมาส 1", "ไตรมาส 2", "ไตรมาส 3", "ไตรมาส 4" };
        var quarters = quarterLabels.Select((label, i) =>
        {
            var qRows = rows.Where(r =>
                r.approvedAt.HasValue &&
                (r.approvedAt.Value.Month - 1) / 3 == i).ToList();
            return new PriceSummaryBarChartItem(
                label,
                qRows.Sum(r => r.budget),
                qRows.Sum(r => r.median),
                qRows.Sum(r => r.offered),
                qRows.Sum(r => r.agreed));
        }).ToList();

        // Month
        var monthLabels = new[] { "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };
        var months = monthLabels.Select((label, i) =>
        {
            var mRows = rows.Where(r =>
                r.approvedAt.HasValue &&
                r.approvedAt.Value.Month - 1 == i).ToList();
            return new PriceSummaryBarChartItem(
                label,
                mRows.Sum(r => r.budget),
                mRows.Sum(r => r.median),
                mRows.Sum(r => r.offered),
                mRows.Sum(r => r.agreed));
        }).ToList();

        var result = new[] { summary }
            .Concat(quarters)
            .Concat(months)
            .ToList();

        return TypedResults.Ok(result);
    }
}

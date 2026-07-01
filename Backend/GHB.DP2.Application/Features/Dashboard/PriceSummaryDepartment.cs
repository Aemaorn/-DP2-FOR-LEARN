namespace GHB.DP2.Application.Features.Dashboard;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPriceSummaryDepartmentRequest(
    int? BudgetYear,
    string? Keyword,
    string? DepartmentId,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    int? Month,
    int? Quarter);

public record PriceSummaryDepartmentItem(
    string DepartmentName,
    int ProjectCount,
    decimal TotalBudget,
    decimal TotalMedianPrice,
    decimal TotalAgreedPrice);

public class GetPriceSummaryDepartmentEndpoint
    : EndpointBase<GetPriceSummaryDepartmentRequest, Ok<List<PriceSummaryDepartmentItem>>>
{
    private readonly Dp2DbContext dbContext;

    public GetPriceSummaryDepartmentEndpoint(
        ILogger<GetPriceSummaryDepartmentEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("dashboard/price-summary/department");
        this.Options(o => o.WithTags("Dashboard"));
    }

    protected override async ValueTask<Ok<List<PriceSummaryDepartmentItem>>> HandleRequestAsync(
        GetPriceSummaryDepartmentRequest req, CancellationToken ct)
    {
        var filter = new PriceSummaryFilter(req.BudgetYear, req.Keyword, req.SupplyMethodCode, req.SupplyMethodSpecialTypeCode, req.Month, req.Quarter, req.DepartmentId);
        var query = await PriceSummaryQuery.BuildAsync(this.dbContext, filter, ct);

        var rawItems = await query
            .Include(po => po.Procurement)
            .ThenInclude(p => p.Department)
            .Include(po => po.Procurement)
            .ThenInclude(p => p.PurchaseRequisitions)
            .Include(po => po.Entrepreneurs.Where(e => e.IsWinner))
            .ThenInclude(e => e.PJp006PriceDetails)
            .AsSplitQuery()
            .ToListAsync(ct);

        var result = rawItems
            .GroupBy(po => po.Procurement.Department?.Name ?? "ไม่ระบุฝ่าย")
            .Select(g =>
            {
                var totalBudget = g.Sum(po => po.Procurement.Budget ?? 0m);
                var totalMedianPrice = g.Sum(po =>
                    po.Procurement.PurchaseRequisitions
                        .Select(pr => pr.MedianPriceAmount)
                        .FirstOrDefault() ?? 0m);
                var totalAgreedPrice = g.Sum(po =>
                    po.Entrepreneurs
                        .Where(e => e.IsWinner)
                        .SelectMany(e => e.PJp006PriceDetails)
                        .Sum(pd => pd.AgreedPrice * pd.ParcelQuantity));

                return new PriceSummaryDepartmentItem(
                    DepartmentName: g.Key,
                    ProjectCount: g.Count(),
                    TotalBudget: totalBudget,
                    TotalMedianPrice: totalMedianPrice,
                    TotalAgreedPrice: totalAgreedPrice);
            })
            .OrderBy(i => i.DepartmentName)
            .ToList();

        return TypedResults.Ok(result);
    }
}

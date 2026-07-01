namespace GHB.DP2.Application.Features.Dashboard;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPriceSummarySpecialTypeChartRequest(
    int? BudgetYear,
    string? Keyword,
    string? DepartmentId,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    int? Month,
    int? Quarter);

public record PriceSummarySpecialTypeChartItem(
    string SpecialTypeName,
    int ProjectCount,
    decimal TotalAgreedPrice);

public class GetPriceSummarySpecialTypeChartEndpoint
    : EndpointBase<GetPriceSummarySpecialTypeChartRequest, Ok<List<PriceSummarySpecialTypeChartItem>>>
{
    private readonly Dp2DbContext dbContext;

    public GetPriceSummarySpecialTypeChartEndpoint(
        ILogger<GetPriceSummarySpecialTypeChartEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("dashboard/price-summary/special-type-chart");
        this.Options(o => o.WithTags("Dashboard"));
    }

    protected override async ValueTask<Ok<List<PriceSummarySpecialTypeChartItem>>> HandleRequestAsync(
        GetPriceSummarySpecialTypeChartRequest req, CancellationToken ct)
    {
        var filter = new PriceSummaryFilter(req.BudgetYear, req.Keyword, req.SupplyMethodCode, req.SupplyMethodSpecialTypeCode, req.Month, req.Quarter, req.DepartmentId);
        var query = await PriceSummaryQuery.BuildAsync(this.dbContext, filter, ct);

        var result = await query
            .GroupBy(po => po.Procurement.SupplyMethodSpecialType == null
                ? null
                : po.Procurement.SupplyMethodSpecialType.Label)
            .Select(g => new PriceSummarySpecialTypeChartItem(
                g.Key ?? "ไม่ระบุ",
                g.Count(),
                g.SelectMany(po => po.Entrepreneurs
                    .Where(e => e.IsWinner)
                    .SelectMany(e => e.PJp006PriceDetails))
                 .Sum(pd => pd.AgreedPrice * pd.ParcelQuantity)))
            .ToListAsync(ct);

        return TypedResults.Ok(result);
    }
}

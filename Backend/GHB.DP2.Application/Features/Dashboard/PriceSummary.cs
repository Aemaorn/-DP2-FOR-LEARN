namespace GHB.DP2.Application.Features.Dashboard;

using GHB.DP2.Application.Common;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPriceSummaryRequest(
    int PageNumber,
    int PageSize,
    int? BudgetYear,
    string? Keyword,
    string? DepartmentId,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    int? Month,
    int? Quarter);

public record PriceSummaryResponse(
    Guid ProcurementId,
    Guid Jp006Id,
    string ProcurementNumber,
    string ProjectName,
    string SupplyMethodName,
    string? SupplyMethodSpecialTypeName,
    DateTimeOffset? ApprovedDate,
    string VendorName,
    decimal Budget,
    decimal MedianPrice,
    decimal TotalOfferedPrice,
    decimal TotalAgreedPrice,
    decimal BudgetDiff,
    decimal BudgetDiffPercent,
    bool IsUnderBudget,
    decimal MedianPriceDiff,
    decimal MedianPriceDiffPercent,
    bool IsUnderMedianPrice,
    decimal OfferedPriceDiff,
    decimal OfferedPriceDiffPercent,
    bool IsUnderOfferedPrice,
    string? SelectionReasonName,
    string? Remark);

public class GetPriceSummaryEndpoint
    : EndpointBase<GetPriceSummaryRequest, Ok<PaginatedQueryResult<PriceSummaryResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetPriceSummaryEndpoint(
        ILogger<GetPriceSummaryEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("dashboard/price-summary");
        this.Options(o => o.WithTags("Dashboard"));
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<PriceSummaryResponse>>> HandleRequestAsync(GetPriceSummaryRequest req, CancellationToken ct)
    {
        var filter = new PriceSummaryFilter(req.BudgetYear, req.Keyword, req.SupplyMethodCode, req.SupplyMethodSpecialTypeCode, req.Month, req.Quarter, req.DepartmentId);
        var query = await PriceSummaryQuery.BuildAsync(this.dbContext, filter, ct);

        var totalCount = await query.CountAsync(ct);

        var rawItems = await query
            .OrderByDescending(po => po.Procurement.Plan.PlanNumber)
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .Include(po => po.Procurement)
            .ThenInclude(p => p.SupplyMethod)
            .Include(po => po.Procurement)
            .ThenInclude(p => p.SupplyMethodSpecialType)
            .Include(po => po.Procurement)
            .ThenInclude(p => p.PurchaseRequisitions)
            .Include(po => po.Entrepreneurs.Where(e => e.IsWinner && e.PJp006PriceDetails.Any()))
            .ThenInclude(e => e.PJp006PriceDetails)
            .Include(po => po.Entrepreneurs.Where(e => e.IsWinner && e.PJp006PriceDetails.Any()))
            .ThenInclude(e => e.SuVendor)
            .Include(po => po.Acceptors)
            .Include(po => po.Procurement)
            .ThenInclude(p => p.Plan)
            .AsSplitQuery()
            .ToListAsync(ct);

        // Load SelectionReasonCode labels
        var selectionReasonCodes = rawItems
            .SelectMany(po => po.Entrepreneurs)
            .Where(e => e.SelectionReasonCode != null)
            .Select(e => ParameterCode.From(e.SelectionReasonCode!))
            .Distinct()
            .ToList();

        var selectionReasonMap = selectionReasonCodes.Count > 0
            ? await this.dbContext.SuParameters
                .AsNoTracking()
                .Where(p => selectionReasonCodes.Contains(p.Code))
                .Select(p => new { p.Code, p.Label })
                .ToDictionaryAsync(k => k.Code, v => v.Label, ct)
            : new Dictionary<ParameterCode, string>();

        var responses = rawItems
            .SelectMany(po => po.Entrepreneurs
                .Where(e => e.IsWinner && e.PJp006PriceDetails.Any())
                .Select(e => MapToResponse(po, e, selectionReasonMap)))
            .ToList();

        var result = new PaginatedQueryResult<PriceSummaryResponse>(responses, totalCount);

        return TypedResults.Ok(result);
    }

    internal static PriceSummaryResponse MapToResponse(
        Domain.Procurement.PPurchaseOrder.PPurchaseOrder po,
        Domain.Procurement.PPurchaseOrder.PPurchaseOrderEntrepreneur entrepreneur,
        Dictionary<ParameterCode, string> selectionReasonMap)
    {
        var budget = po.Procurement.Budget ?? 0m;
        var medianPrice = po.Procurement.PurchaseRequisitions
            .Select(pr => pr.MedianPriceAmount)
            .FirstOrDefault() ?? 0m;
        var totalOfferedPrice = entrepreneur.PJp006PriceDetails
            .Sum(pd => pd.OfferedPrice * pd.ParcelQuantity);
        var totalAgreedPrice = entrepreneur.PJp006PriceDetails
            .Sum(pd => pd.AgreedPrice * pd.ParcelQuantity);

        var approvedDate = po.Acceptors
            .Where(a => a.Type == AcceptorType.Approver && a.Status == AcceptorStatus.Approved)
            .OrderByDescending(a => a.Sequence)
            .Select(a => a.ActionAt)
            .FirstOrDefault();

        var selectionReasonName = entrepreneur.SelectionReasonCode != null &&
            selectionReasonMap.TryGetValue(ParameterCode.From(entrepreneur.SelectionReasonCode), out var label)
            ? label
            : null;

        var budgetDiff = Math.Abs(totalAgreedPrice - budget);
        var isUnderBudget = totalAgreedPrice <= budget;
        var budgetDiffPercent = isUnderBudget
            ? (budget > 0 ? Math.Round(budgetDiff / budget * 100m, 2) : 0m)
            : (totalAgreedPrice > 0 ? Math.Round(budgetDiff / totalAgreedPrice * 100m, 2) : 0m);

        var medianPriceDiff = Math.Abs(medianPrice - totalAgreedPrice);
        var isUnderMedianPrice = totalAgreedPrice <= medianPrice;
        var medianPriceDiffPercent = isUnderMedianPrice
            ? (medianPrice > 0 ? Math.Round(medianPriceDiff / medianPrice * 100m, 2) : 0m)
            : (totalAgreedPrice > 0 ? Math.Round(medianPriceDiff / totalAgreedPrice * 100m, 2) : 0m);

        var offeredPriceDiff = Math.Abs(totalAgreedPrice - totalOfferedPrice);
        var isUnderOfferedPrice = totalAgreedPrice <= totalOfferedPrice;
        var offeredPriceDiffPercent = totalOfferedPrice > 0
            ? Math.Round(offeredPriceDiff / totalOfferedPrice * 100m, 2)
            : 0m;

        string planNumber = po.Procurement.Plan.PlanNumber != null
                ? (string)po.Procurement.Plan.PlanNumber.Value
                : string.Empty;

        return new PriceSummaryResponse(
            ProcurementId: po.ProcurementId.Value,
            Jp006Id: po.Id.Value,
            ProcurementNumber: planNumber,
            ProjectName: po.Procurement.Name,
            SupplyMethodName: po.Procurement.SupplyMethod?.Label ?? string.Empty,
            SupplyMethodSpecialTypeName: po.Procurement.SupplyMethodSpecialType?.Label,
            ApprovedDate: approvedDate,
            VendorName: entrepreneur.SuVendor.EstablishmentName,
            Budget: budget,
            MedianPrice: medianPrice,
            TotalOfferedPrice: totalOfferedPrice,
            TotalAgreedPrice: totalAgreedPrice,
            BudgetDiff: budgetDiff,
            BudgetDiffPercent: budgetDiffPercent,
            IsUnderBudget: isUnderBudget,
            MedianPriceDiff: medianPriceDiff,
            MedianPriceDiffPercent: medianPriceDiffPercent,
            IsUnderMedianPrice: isUnderMedianPrice,
            OfferedPriceDiff: offeredPriceDiff,
            OfferedPriceDiffPercent: offeredPriceDiffPercent,
            IsUnderOfferedPrice: isUnderOfferedPrice,
            SelectionReasonName: selectionReasonName,
            Remark: entrepreneur.Remark);
    }
}

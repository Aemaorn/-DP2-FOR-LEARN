namespace GHB.DP2.Application.Features.Dashboard;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;

public record PriceSummaryFilter(
    int? BudgetYear,
    string? Keyword,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    int? Month,
    int? Quarter,
    string? DepartmentId = null);

internal static class PriceSummaryQuery
{
    internal static async Task<IQueryable<Domain.Procurement.PPurchaseOrder.PPurchaseOrder>> BuildAsync(
        Dp2DbContext dbContext,
        PriceSummaryFilter filter,
        CancellationToken ct = default)
    {
        var query = dbContext.PJp006S
            .AsNoTracking()
            .Where(po => !po.IsDeleted)
            .Where(po => po.Status == Domain.Procurement.PPurchaseOrder.PurchaseOrderStatus.Approved)
            .Where(po => po.Entrepreneurs.Any(e => e.IsWinner && e.PJp006PriceDetails.Any()));

        if (filter.BudgetYear.HasValue)
        {
            query = query.Where(po => po.Procurement.BudgetYear == filter.BudgetYear.Value);
        }

        if (!string.IsNullOrEmpty(filter.SupplyMethodCode))
        {
            var supplyMethodCode = ParameterCode.From(filter.SupplyMethodCode);
            query = query.Where(po => po.Procurement.SupplyMethodCode == supplyMethodCode);
        }

        if (!string.IsNullOrEmpty(filter.SupplyMethodSpecialTypeCode))
        {
            var supplyMethodSpecialTypeCode = ParameterCode.From(filter.SupplyMethodSpecialTypeCode);
            query = query.Where(po => po.Procurement.SupplyMethodSpecialTypeCode == supplyMethodSpecialTypeCode);
        }

        if (filter.Month.HasValue)
        {
            var month = filter.Month.Value;
            query = query.Where(po => po.Acceptors.Any(a =>
                a.Type == AcceptorType.Approver &&
                a.Status == AcceptorStatus.Approved &&
                a.ActionAt != null &&
                a.ActionAt.Value.Month == month));
        }

        if (filter.Quarter.HasValue)
        {
            var startMonth = ((filter.Quarter.Value - 1) * 3) + 1;
            var endMonth = startMonth + 2;
            query = query.Where(po => po.Acceptors.Any(a =>
                a.Type == AcceptorType.Approver &&
                a.Status == AcceptorStatus.Approved &&
                a.ActionAt != null &&
                a.ActionAt.Value.Month >= startMonth &&
                a.ActionAt.Value.Month <= endMonth));
        }

        if (!string.IsNullOrEmpty(filter.DepartmentId))
        {
            var departmentId = BusinessUnitId.From(filter.DepartmentId);
            query = query.Where(po => po.Procurement.DepartmentId == departmentId);
        }

        if (!string.IsNullOrEmpty(filter.Keyword))
        {
            var keyword = $"%{filter.Keyword}%";
            var matchingPoIds = await dbContext.PJp006Entrepreneurs
                .AsNoTracking()
                .Where(e => e.IsWinner && EF.Functions.ILike(e.SuVendor.EstablishmentName, keyword))
                .Select(e => e.PurchaseOrderId)
                .ToListAsync(ct);

            query = query.Where(po =>
                EF.Functions.ILike(po.Procurement.Name, keyword) ||
                (po.Procurement.Plan.PlanNumber != null &&
                 EF.Functions.ILike((string)po.Procurement.Plan.PlanNumber, keyword)) ||
                matchingPoIds.Contains(po.Id));
        }

        return query;
    }
}

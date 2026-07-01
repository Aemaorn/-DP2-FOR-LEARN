namespace GHB.DP2.Application.Features.Plan.Plan;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPlanListWithSupplyMethodCountsRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    string? SupplyMethodCode);

public record PlanListItem(
    Guid Id,
    string PlanNumber,
    string Name,
    int BudgetYear,
    decimal Budget,
    string DepartmentName,
    string SupplyMethodName,
    bool IsChange,
    bool IsCancel,
    string StatusCode);

public record PlanSupplyMethodCounts(
    int All,
    int Sixty,
    int Eighty);

public record GetPlanListWithSupplyMethodCountsResult(
    PlanSupplyMethodCounts Counts,
    PaginatedQueryResult<PlanListItem> Data);

public class GetPlanListWithSupplyMethodCounts : EndpointBase<GetPlanListWithSupplyMethodCountsRequest, Ok<GetPlanListWithSupplyMethodCountsResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetPlanListWithSupplyMethodCounts(
        Dp2DbContext dbContext,
        ILogger<GetPlanListWithSupplyMethodCounts> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(o => o.WithTags("Plan").WithName("GetPlanListWithSupplyMethodCounts"));
        this.Get("plan/list-with-supplymethod-counts");
    }

    protected override async ValueTask<Ok<GetPlanListWithSupplyMethodCountsResult>> HandleRequestAsync(
        GetPlanListWithSupplyMethodCountsRequest req,
        CancellationToken ct)
    {
        if (req.PageNumber <= 0)
        {
            req = req with { PageNumber = 1 };
        }

        if (req.PageSize <= 0)
        {
            req = req with { PageSize = 20 };
        }

        // Base query without SupplyMethod filter (used for counts)
        var baseQuery = this.dbContext.Plans
            .Where(p => p.IsActive)
            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x => EF.Functions.ILike(x.Name, $"%{req.Keyword}%") || EF.Functions.ILike((string)x.PlanNumber, $"%{req.Keyword}%"))
            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!));

        var allCount = await baseQuery.CountAsync(ct);
        var sixtyCount = await baseQuery
            .Where(p => p.SupplyMethodCode == ParameterCode.From(SupplyMethodConstant.Sixty))
            .CountAsync(ct);
        var eightyCount = await baseQuery
            .Where(p => p.SupplyMethodCode == ParameterCode.From(SupplyMethodConstant.Eighty))
            .CountAsync(ct);

        // Apply optional supply method filter for page data
        var listQuery = baseQuery
            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.SupplyMethodCode), p => p.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
            .OrderByDescending(p => p.PlanNumber)
            .AsNoTracking();

        var page = await PaginatedList<Domain.Plan.Plan>.CreateAsync(listQuery, req.PageNumber, req.PageSize, ct);
        var pageResult = page.ToResult(static p => new PlanListItem(
            p.Id.Value,
            p.PlanNumber.Value,
            p.Name,
            p.BudgetYear,
            p.Budget,
            p.Department.Name,
            p.SupplyMethod.Label,
            p.IsChange,
            p.IsCancel,
            p.Status.ToString()));

        var counts = new PlanSupplyMethodCounts(allCount, sixtyCount, eightyCount);
        var response = new GetPlanListWithSupplyMethodCountsResult(counts, pageResult);
        return TypedResults.Ok(response);
    }
}

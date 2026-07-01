namespace GHB.DP2.Application.Features.Dashboard;

using System.Linq;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DashboardTotals(
    int PlanCount,
    int ProcurementCount,
    int ContractDraftVendorCount,
    int PrincipleApprovalCount);

public record ChartSlice(string Label, int Value);

public record ChartSliceAmount(string Label, decimal Value);

public record DashboardCharts(
    IEnumerable<ChartSlice> BySupplyMethodPlan,
    IEnumerable<ChartSlice> BySupplyMethodProcurement,
    IEnumerable<ChartSlice> BySupplyMethodContract,
    IEnumerable<ChartSlice> BySupplyMethodPrinciple,
    IEnumerable<ChartSliceAmount> PlanBudgetBySupplyMethod,
    IEnumerable<ChartSliceAmount> PlanBudgetBySupplyMethodType,
    IEnumerable<ChartSliceAmount> CombinedBudgetPlanPw119P79PettyCash // NEW donut
);

public record DashboardSupplyMethodSummaryResponse(
    string SupplyMethodCode,
    string SupplyMethodName,
    int PlanCount,
    int ProcurementCount,
    int ContractDraftVendorCount,
    int PrincipleApprovalCount);

public record DashboardSupplyMethodSummaryEnvelope(
    DashboardTotals Totals,
    IEnumerable<DashboardSupplyMethodSummaryResponse> Items,
    DashboardCharts Charts
);

public record GetDashboardSupplyMethodSummaryRequest(
    int? BudgetYear,
    string? SupplyMethodCode,
    string? GroupCode,
    string? LineCode,
    string? DepartmentCode);

public class GetDashboardSupplyMethodSummaryEndpoint
    : EndpointBase<GetDashboardSupplyMethodSummaryRequest, Ok<DashboardSupplyMethodSummaryEnvelope>>
{
    private readonly Dp2DbContext dbContext;

    public GetDashboardSupplyMethodSummaryEndpoint(
        ILogger<GetDashboardSupplyMethodSummaryEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("dashboard/summary");
        this.Options(o => o.WithTags("Dashboard"));
    }

    protected override async ValueTask<Ok<DashboardSupplyMethodSummaryEnvelope>> HandleRequestAsync(
        GetDashboardSupplyMethodSummaryRequest req,
        CancellationToken ct)
    {
        var supplyMethodCode = ParseSupplyMethodCode(req.SupplyMethodCode);

        var queryResults = await this.ExecuteDataQueriesAsync(req, supplyMethodCode, ct);

        if (this.ShouldReturnEmptyResult(supplyMethodCode, queryResults))
        {
            return TypedResults.Ok(CreateEmptyEnvelope());
        }

        var parameterMap = await this.GetParameterMapAsync(queryResults, ct);

        var envelope = this.CreateResponseEnvelope(queryResults, parameterMap);

        return TypedResults.Ok(envelope);
    }

    private static ParameterCode? ParseSupplyMethodCode(string? supplyMethodCode)
    {
        return string.IsNullOrWhiteSpace(supplyMethodCode)
            ? null
            : ParameterCode.From(supplyMethodCode);
    }

    private async Task<DashboardQueryResults> ExecuteDataQueriesAsync(
        GetDashboardSupplyMethodSummaryRequest req,
        ParameterCode? supplyMethodCode,
        CancellationToken ct)
    {
        var planQuery = this.BuildPlanQuery(req, supplyMethodCode);
        var planTypeQuery = this.BuildPlanTypeQuery(req, supplyMethodCode);

        var results = new DashboardQueryResults
        {
            PlanGroups = await ExecutePlanGroupsAsync(planQuery, ct),
            PlanBudgetGroups = await ExecutePlanBudgetGroupsAsync(planQuery, ct),
            PlanTypeGroups = await ExecutePlanTypeGroupsAsync(planTypeQuery, ct),
            PlanTypeBudgetGroups = await ExecutePlanTypeBudgetGroupsAsync(planTypeQuery, ct),
            ProcurementGroups = await this.ExecuteProcurementGroupsAsync(req, supplyMethodCode, ct),
            ContractDraftVendorGroups = await this.ExecuteContractDraftVendorGroupsAsync(req, supplyMethodCode, ct),
            PrincipleApprovalGroups = await this.ExecutePrincipleApprovalGroupsAsync(req, supplyMethodCode, ct),
            PrincipleApprovalRentalGroups = await this.ExecutePrincipleApprovalRentalGroupsAsync(req, supplyMethodCode, ct),
            BudgetTotals = await this.CalculateBudgetTotalsAsync(req, supplyMethodCode, planQuery, ct),
        };

        return results;
    }

    private static IQueryable<Plan> ApplyOrgFilter(IQueryable<Plan> query, GetDashboardSupplyMethodSummaryRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.DepartmentCode))
        {
            var id = BusinessUnitId.From(req.DepartmentCode);
            return query.Where(p => p.Department.Id == id);
        }

        if (!string.IsNullOrWhiteSpace(req.LineCode))
        {
            var id = BusinessUnitId.From(req.LineCode);
            return query.Where(p => p.Department.Id == id || p.Department.ParentId == id);
        }

        if (!string.IsNullOrWhiteSpace(req.GroupCode))
        {
            var id = BusinessUnitId.From(req.GroupCode);
            return query.Where(p =>
                p.Department.Id == id ||
                p.Department.ParentId == id ||
                (p.Department.ParentId != null && p.Department.Parent.ParentId == id));
        }

        return query;
    }

    private static IQueryable<GHB.DP2.Domain.Procurement.Procurement> ApplyOrgFilterToProcurement(
        IQueryable<GHB.DP2.Domain.Procurement.Procurement> query,
        GetDashboardSupplyMethodSummaryRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.DepartmentCode))
        {
            var id = BusinessUnitId.From(req.DepartmentCode);
            return query.Where(p => p.Department.Id == id);
        }

        if (!string.IsNullOrWhiteSpace(req.LineCode))
        {
            var id = BusinessUnitId.From(req.LineCode);
            return query.Where(p => p.Department.Id == id || p.Department.ParentId == id);
        }

        if (!string.IsNullOrWhiteSpace(req.GroupCode))
        {
            var id = BusinessUnitId.From(req.GroupCode);
            return query.Where(p =>
                p.Department.Id == id ||
                p.Department.ParentId == id ||
                (p.Department.ParentId != null && p.Department.Parent.ParentId == id));
        }

        return query;
    }

    private static IQueryable<GHB.DP2.Domain.Procurement.PPrincipleApproval.PPrincipleApproval> ApplyOrgFilterToPrincipleApproval(
        IQueryable<GHB.DP2.Domain.Procurement.PPrincipleApproval.PPrincipleApproval> query,
        GetDashboardSupplyMethodSummaryRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.DepartmentCode))
        {
            var id = BusinessUnitId.From(req.DepartmentCode);
            return query.Where(a => a.Procurement.Department.Id == id);
        }

        if (!string.IsNullOrWhiteSpace(req.LineCode))
        {
            var id = BusinessUnitId.From(req.LineCode);
            return query.Where(a => a.Procurement.Department.Id == id || a.Procurement.Department.ParentId == id);
        }

        if (!string.IsNullOrWhiteSpace(req.GroupCode))
        {
            var id = BusinessUnitId.From(req.GroupCode);
            return query.Where(a =>
                a.Procurement.Department.Id == id ||
                a.Procurement.Department.ParentId == id ||
                (a.Procurement.Department.ParentId != null && a.Procurement.Department.Parent.ParentId == id));
        }

        return query;
    }

    private static IQueryable<GHB.DP2.Domain.Procurement.PPrincipleApprovalRental.PPrincipleApprovalRental> ApplyOrgFilterToPrincipleApprovalRental(
        IQueryable<GHB.DP2.Domain.Procurement.PPrincipleApprovalRental.PPrincipleApprovalRental> query,
        GetDashboardSupplyMethodSummaryRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.DepartmentCode))
        {
            var id = BusinessUnitId.From(req.DepartmentCode);
            return query.Where(r => r.Procurement.Department.Id == id);
        }

        if (!string.IsNullOrWhiteSpace(req.LineCode))
        {
            var id = BusinessUnitId.From(req.LineCode);
            return query.Where(r => r.Procurement.Department.Id == id || r.Procurement.Department.ParentId == id);
        }

        if (!string.IsNullOrWhiteSpace(req.GroupCode))
        {
            var id = BusinessUnitId.From(req.GroupCode);
            return query.Where(r =>
                r.Procurement.Department.Id == id ||
                r.Procurement.Department.ParentId == id ||
                (r.Procurement.Department.ParentId != null && r.Procurement.Department.Parent.ParentId == id));
        }

        return query;
    }

    private IQueryable<Plan> BuildPlanQuery(GetDashboardSupplyMethodSummaryRequest req, ParameterCode? supplyMethodCode)
    {
        var query = this.dbContext.Plans.AsNoTracking().Where(p => !p.IsDeleted);

        if (req.BudgetYear.HasValue)
        {
            query = query.Where(p => p.BudgetYear == req.BudgetYear.Value);
        }

        if (supplyMethodCode != null)
        {
            query = query.Where(p => p.SupplyMethodCode == supplyMethodCode);
        }

        return ApplyOrgFilter(query, req);
    }

    private IQueryable<Plan> BuildPlanTypeQuery(GetDashboardSupplyMethodSummaryRequest req, ParameterCode? supplyMethodCode)
    {
        var query = this.dbContext.Plans
                        .AsNoTracking()
                        .Where(p => !p.IsDeleted && p.SupplyMethodTypeCode != null);

        if (req.BudgetYear.HasValue)
        {
            query = query.Where(p => p.BudgetYear == req.BudgetYear.Value);
        }

        if (supplyMethodCode != null)
        {
            query = query.Where(p => p.SupplyMethodCode == supplyMethodCode);
        }

        return ApplyOrgFilter(query, req);
    }

    private static async Task<List<dynamic>> ExecutePlanGroupsAsync(IQueryable<Plan> planQuery, CancellationToken ct)
    {
        return await planQuery
                     .GroupBy(p => p.SupplyMethodCode)
                     .Select(g => new { Code = g.Key, Count = g.Count() })
                     .ToListAsync<dynamic>(ct);
    }

    private static async Task<List<dynamic>> ExecutePlanBudgetGroupsAsync(IQueryable<Plan> planQuery, CancellationToken ct)
    {
        return await planQuery
                     .GroupBy(p => p.SupplyMethodCode)
                     .Select(g => new { Code = g.Key, Amount = g.Sum(p => p.Budget) })
                     .ToListAsync<dynamic>(ct);
    }

    private static async Task<List<dynamic>> ExecutePlanTypeGroupsAsync(IQueryable<Plan> planTypeQuery, CancellationToken ct)
    {
        return await planTypeQuery
                     .GroupBy(p => p.SupplyMethodTypeCode)
                     .Select(g => new { Code = g.Key, Count = g.Count() })
                     .ToListAsync<dynamic>(ct);
    }

    private static async Task<List<dynamic>> ExecutePlanTypeBudgetGroupsAsync(IQueryable<Plan> planTypeQuery, CancellationToken ct)
    {
        return await planTypeQuery
                     .GroupBy(p => p.SupplyMethodTypeCode)
                     .Select(g => new { Code = g.Key, Amount = g.Sum(p => p.Budget) })
                     .ToListAsync<dynamic>(ct);
    }

    private async Task<List<dynamic>> ExecuteProcurementGroupsAsync(
        GetDashboardSupplyMethodSummaryRequest req,
        ParameterCode? supplyMethodCode,
        CancellationToken ct)
    {
        var query = this.dbContext.Procurements.AsNoTracking().Where(p => !p.IsDeleted);

        if (req.BudgetYear.HasValue)
        {
            query = query.Where(p => p.BudgetYear == req.BudgetYear.Value);
        }

        if (supplyMethodCode != null)
        {
            query = query.Where(p => p.SupplyMethodCode == supplyMethodCode);
        }

        query = ApplyOrgFilterToProcurement(query, req);

        return await query
                     .GroupBy(p => p.SupplyMethodCode)
                     .Select(g => new { Code = g.Key, Count = g.Count() })
                     .ToListAsync<dynamic>(ct);
    }

    private async Task<List<dynamic>> ExecuteContractDraftVendorGroupsAsync(
        GetDashboardSupplyMethodSummaryRequest req,
        ParameterCode? supplyMethodCode,
        CancellationToken ct)
    {
        var query = this.dbContext.CaContractDraftVendors
                        .AsNoTracking()
                        .Where(v => !v.IsDeleted && !v.ContractDraft.IsDeleted && !v.ContractDraft.Procurement.IsDeleted);

        if (req.BudgetYear.HasValue)
        {
            query = query.Where(v => v.ContractDraft.Procurement.BudgetYear == req.BudgetYear.Value);
        }

        if (supplyMethodCode != null)
        {
            query = query.Where(v => v.ContractDraft.Procurement.SupplyMethodCode == supplyMethodCode);
        }

        if (!string.IsNullOrWhiteSpace(req.DepartmentCode))
        {
            var id = BusinessUnitId.From(req.DepartmentCode);
            query = query.Where(v => v.ContractDraft.Procurement.Department.Id == id);
        }
        else if (!string.IsNullOrWhiteSpace(req.LineCode))
        {
            var id = BusinessUnitId.From(req.LineCode);
            query = query.Where(v => v.ContractDraft.Procurement.Department.Id == id || v.ContractDraft.Procurement.Department.ParentId == id);
        }
        else if (!string.IsNullOrWhiteSpace(req.GroupCode))
        {
            var id = BusinessUnitId.From(req.GroupCode);
            query = query.Where(v =>
                v.ContractDraft.Procurement.Department.Id == id ||
                v.ContractDraft.Procurement.Department.ParentId == id ||
                (v.ContractDraft.Procurement.Department.ParentId != null && v.ContractDraft.Procurement.Department.Parent.ParentId == id));
        }

        return await query
                     .GroupBy(v => v.ContractDraft.Procurement.SupplyMethodCode)
                     .Select(g => new { Code = g.Key, Count = g.Count() })
                     .ToListAsync<dynamic>(ct);
    }

    private async Task<List<dynamic>> ExecutePrincipleApprovalGroupsAsync(
        GetDashboardSupplyMethodSummaryRequest req,
        ParameterCode? supplyMethodCode,
        CancellationToken ct)
    {
        var query = this.dbContext.PPrincipleApprovals.AsNoTracking()
                        .Where(a => !a.IsDeleted && !a.Procurement.IsDeleted);

        if (req.BudgetYear.HasValue)
        {
            query = query.Where(a => a.Procurement.BudgetYear == req.BudgetYear.Value);
        }

        if (supplyMethodCode != null)
        {
            query = query.Where(a => a.Procurement.SupplyMethodCode == supplyMethodCode);
        }

        query = ApplyOrgFilterToPrincipleApproval(query, req);

        return await query
                     .GroupBy(a => a.Procurement.SupplyMethodCode)
                     .Select(g => new { Code = g.Key, Count = g.Count() })
                     .ToListAsync<dynamic>(ct);
    }

    private async Task<List<dynamic>> ExecutePrincipleApprovalRentalGroupsAsync(
        GetDashboardSupplyMethodSummaryRequest req,
        ParameterCode? supplyMethodCode,
        CancellationToken ct)
    {
        var query = this.dbContext.PPrincipleApprovalRentals.AsNoTracking()
                        .Where(r => !r.IsDeleted && !r.Procurement.IsDeleted);

        if (req.BudgetYear.HasValue)
        {
            query = query.Where(r => r.Procurement.BudgetYear == req.BudgetYear.Value);
        }

        if (supplyMethodCode != null)
        {
            query = query.Where(r => r.Procurement.SupplyMethodCode == supplyMethodCode);
        }

        query = ApplyOrgFilterToPrincipleApprovalRental(query, req);

        return await query
                     .GroupBy(r => r.Procurement.SupplyMethodCode)
                     .Select(g => new { Code = g.Key, Count = g.Count() })
                     .ToListAsync<dynamic>(ct);
    }

    private async Task<BudgetTotals> CalculateBudgetTotalsAsync(
        GetDashboardSupplyMethodSummaryRequest req,
        ParameterCode? supplyMethodCode,
        IQueryable<Plan> planQuery,
        CancellationToken ct)
    {
        var pw119Query = this.dbContext.Pw119s.AsNoTracking().Where(x => !x.IsDeleted);
        var p79Query = this.dbContext.P79Clause2s.AsNoTracking().Where(x => !x.IsDeleted);
        var pettyCashQuery = this.dbContext.PPettyCashs.AsNoTracking().Where(x => !x.IsDeleted);

        if (req.BudgetYear.HasValue)
        {
            pw119Query = pw119Query.Where(x => x.BudgetYear == req.BudgetYear.Value);
            p79Query = p79Query.Where(x => x.BudgetYear == req.BudgetYear.Value);
            pettyCashQuery = pettyCashQuery.Where(x => x.BudgetYear == req.BudgetYear.Value);
        }

        if (supplyMethodCode != null)
        {
            pw119Query = pw119Query.Where(x => x.SupplyMethodCode == supplyMethodCode);
            p79Query = p79Query.Where(x => x.SupplyMethodCode == supplyMethodCode);
            pettyCashQuery = pettyCashQuery.Where(x => x.SupplyMethodCode == supplyMethodCode);
        }

        if (!string.IsNullOrWhiteSpace(req.DepartmentCode))
        {
            var id = BusinessUnitId.From(req.DepartmentCode);
            pw119Query = pw119Query.Where(x => x.Department.Id == id);
            p79Query = p79Query.Where(x => x.Department.Id == id);
            pettyCashQuery = pettyCashQuery.Where(x => x.Department.Id == id);
        }
        else if (!string.IsNullOrWhiteSpace(req.LineCode))
        {
            var id = BusinessUnitId.From(req.LineCode);
            pw119Query = pw119Query.Where(x => x.Department.Id == id || x.Department.ParentId == id);
            p79Query = p79Query.Where(x => x.Department.Id == id || x.Department.ParentId == id);
            pettyCashQuery = pettyCashQuery.Where(x => x.Department.Id == id || x.Department.ParentId == id);
        }
        else if (!string.IsNullOrWhiteSpace(req.GroupCode))
        {
            var id = BusinessUnitId.From(req.GroupCode);
            pw119Query = pw119Query.Where(x =>
                x.Department.Id == id ||
                x.Department.ParentId == id ||
                (x.Department.ParentId != null && x.Department.Parent.ParentId == id));
            p79Query = p79Query.Where(x =>
                x.Department.Id == id ||
                x.Department.ParentId == id ||
                (x.Department.ParentId != null && x.Department.Parent.ParentId == id));
            pettyCashQuery = pettyCashQuery.Where(x =>
                x.Department.Id == id ||
                x.Department.ParentId == id ||
                (x.Department.ParentId != null && x.Department.Parent.ParentId == id));
        }

        return new BudgetTotals
        {
            Pw119Total = await pw119Query.Select(x => (decimal?)x.Budget).SumAsync(ct) ?? 0m,
            P79Total = await p79Query.Select(x => (decimal?)x.Budget).SumAsync(ct) ?? 0m,
            PettyCashTotal = await pettyCashQuery.Select(x => (decimal?)x.Budget).SumAsync(ct) ?? 0m,
            PlanTotal = await planQuery.Select(p => (decimal?)p.Budget).SumAsync(ct) ?? 0m,
        };
    }

    private bool ShouldReturnEmptyResult(ParameterCode? supplyMethodCode, DashboardQueryResults queryResults)
    {
        return supplyMethodCode != null && GetAllSupplyMethodCodes(queryResults).Count == 0;
    }

    private static List<ParameterCode> GetAllSupplyMethodCodes(DashboardQueryResults queryResults)
    {
        return [.. queryResults.PlanGroups.Select(g => (ParameterCode)g.Code)
                           .Concat(queryResults.ProcurementGroups.Select(g => (ParameterCode)g.Code))
                           .Concat(queryResults.ContractDraftVendorGroups.Select(g => (ParameterCode)g.Code))
                           .Concat(queryResults.PrincipleApprovalGroups.Select(g => (ParameterCode)g.Code))
                           .Concat(queryResults.PrincipleApprovalRentalGroups.Select(g => (ParameterCode)g.Code))
                           .Concat(queryResults.PlanBudgetGroups.Select(g => (ParameterCode)g.Code))
                           .Distinct()];
    }

    private static DashboardSupplyMethodSummaryEnvelope CreateEmptyEnvelope()
    {
        return new DashboardSupplyMethodSummaryEnvelope(
            new DashboardTotals(0, 0, 0, 0),
            [],
            new DashboardCharts(
                [],
                [],
                [],
                [],
                [],
                [],
                []));
    }

    private async Task<Dictionary<ParameterCode, string>> GetParameterMapAsync(DashboardQueryResults queryResults, CancellationToken ct)
    {
        var supplyMethodCodes = GetAllSupplyMethodCodes(queryResults);

        var supplyMethodTypeCodes = queryResults.PlanTypeGroups
                                                .Where(g => g.Code != null)
                                                .Select(g => (ParameterCode)g.Code)
                                                .Concat(queryResults.PlanTypeBudgetGroups.Where(g => g.Code != null).Select(g => (ParameterCode)g.Code))
                                                .Distinct()
                                                .ToList();

        var allParameterCodes = supplyMethodCodes
                                .Concat(supplyMethodTypeCodes)
                                .Distinct()
                                .ToList();

        return await this.dbContext.SuParameters.AsNoTracking()
                         .Where(p => allParameterCodes.Contains(p.Code))
                         .Select(p => new { p.Code, p.Label })
                         .ToDictionaryAsync(k => k.Code, v => v.Label, ct);
    }

    private DashboardSupplyMethodSummaryEnvelope CreateResponseEnvelope(
        DashboardQueryResults queryResults,
        Dictionary<ParameterCode, string> parameterMap)
    {
        var supplyMethodCodes = GetAllSupplyMethodCodes(queryResults);

        var items = this.CreateSummaryItems(supplyMethodCodes, queryResults, parameterMap);
        var totals = CalculateTotals(items);
        var charts = this.CreateCharts(items, queryResults, parameterMap);

        return new DashboardSupplyMethodSummaryEnvelope(
            Totals: totals,
            Items: items,
            Charts: charts);
    }

    private List<DashboardSupplyMethodSummaryResponse> CreateSummaryItems(
        List<ParameterCode> supplyMethodCodes,
        DashboardQueryResults queryResults,
        Dictionary<ParameterCode, string> parameterMap)
    {
        return [.. supplyMethodCodes
               .Select(code => new DashboardSupplyMethodSummaryResponse(
                   SupplyMethodCode: code.Value,
                   SupplyMethodName: parameterMap.TryGetValue(code, out var label) ? label : code.Value,
                   PlanCount: GetCountForCode(queryResults.PlanGroups, code),
                   ProcurementCount: GetCountForCode(queryResults.ProcurementGroups, code),
                   ContractDraftVendorCount: GetCountForCode(queryResults.ContractDraftVendorGroups, code),
                   PrincipleApprovalCount: GetCountForCode(queryResults.PrincipleApprovalGroups, code) + GetCountForCode(queryResults.PrincipleApprovalRentalGroups, code)))];
    }

    private static int GetCountForCode(IEnumerable<dynamic> groups, ParameterCode code)
    {
        return groups.FirstOrDefault(x => x.Code == code)?.Count ?? 0;
    }

    private static DashboardTotals CalculateTotals(List<DashboardSupplyMethodSummaryResponse> items)
    {
        return new DashboardTotals(
            PlanCount: items.Sum(i => i.PlanCount),
            ProcurementCount: items.Sum(i => i.ProcurementCount),
            ContractDraftVendorCount: items.Sum(i => i.ContractDraftVendorCount),
            PrincipleApprovalCount: items.Sum(i => i.PrincipleApprovalCount));
    }

    private DashboardCharts CreateCharts(
        List<DashboardSupplyMethodSummaryResponse> items,
        DashboardQueryResults queryResults,
        Dictionary<ParameterCode, string> parameterMap)
    {
        var planBudgetSlices = CreatePlanBudgetSlices(queryResults.PlanBudgetGroups, parameterMap);
        var planBudgetByTypeSlices = CreatePlanBudgetByTypeSlices(queryResults.PlanTypeBudgetGroups, parameterMap);
        var combinedBudgetSlices = CreateCombinedBudgetSlices(queryResults.BudgetTotals);

        return new DashboardCharts(
            BySupplyMethodPlan: CreateChartSeries(items, i => i.PlanCount),
            BySupplyMethodProcurement: CreateChartSeries(items, i => i.ProcurementCount),
            BySupplyMethodContract: CreateChartSeries(items, i => i.ContractDraftVendorCount),
            BySupplyMethodPrinciple: CreateChartSeries(items, i => i.PrincipleApprovalCount),
            PlanBudgetBySupplyMethod: planBudgetSlices,
            PlanBudgetBySupplyMethodType: planBudgetByTypeSlices,
            CombinedBudgetPlanPw119P79PettyCash: combinedBudgetSlices);
    }

    private static IEnumerable<ChartSlice> CreateChartSeries(
        List<DashboardSupplyMethodSummaryResponse> items,
        Func<DashboardSupplyMethodSummaryResponse, int> selector)
    {
        return items.Select(i => new ChartSlice(i.SupplyMethodName, selector(i)))
                    .Where(s => s.Value > 0)
                    .OrderByDescending(s => s.Value);
    }

    private static List<ChartSliceAmount> CreatePlanBudgetSlices(
        List<dynamic> planBudgetGroups,
        Dictionary<ParameterCode, string> parameterMap)
    {
        return [.. planBudgetGroups
               .Select(g => new ChartSliceAmount(
                   parameterMap.TryGetValue((ParameterCode)g.Code, out var label) ? label : ((ParameterCode)g.Code).Value,
                   (decimal)g.Amount))
               .Where(s => s.Value > 0m)
               .OrderByDescending(s => s.Value)];
    }

    private static List<ChartSliceAmount> CreatePlanBudgetByTypeSlices(
        List<dynamic> planTypeBudgetGroups,
        Dictionary<ParameterCode, string> parameterMap)
    {
        return [.. planTypeBudgetGroups
               .Where(g => g.Code != null && (decimal)g.Amount > 0)
               .Select(g =>
               {
                   var code = (ParameterCode)g.Code;
                   var resolvedLabel = parameterMap.TryGetValue(code, out var label) ? label : code.Value;

                   return new ChartSliceAmount(resolvedLabel, (decimal)g.Amount);
               })
               .OrderByDescending(s => s.Value)];
    }

    private static IEnumerable<ChartSliceAmount> CreateCombinedBudgetSlices(BudgetTotals budgetTotals)
    {
        return new[]
               {
                   new ChartSliceAmount("จัดซื้อจัดจ้าง", budgetTotals.PlanTotal),
                   new ChartSliceAmount("ว 119", budgetTotals.Pw119Total),
                   new ChartSliceAmount("กรณีเร่งด่วน", budgetTotals.P79Total),
                   new ChartSliceAmount("Petty Cash", budgetTotals.PettyCashTotal),
               }
               .Where(s => s.Value > 0)
               .OrderByDescending(s => s.Value);
    }

    private class DashboardQueryResults
    {
        public List<dynamic> PlanGroups { get; set; } = [];

        public List<dynamic> PlanBudgetGroups { get; set; } = [];

        public List<dynamic> PlanTypeGroups { get; set; } = [];

        public List<dynamic> PlanTypeBudgetGroups { get; set; } = [];

        public List<dynamic> ProcurementGroups { get; set; } = [];

        public List<dynamic> ContractDraftVendorGroups { get; set; } = [];

        public List<dynamic> PrincipleApprovalGroups { get; set; } = [];

        public List<dynamic> PrincipleApprovalRentalGroups { get; set; } = [];

        public BudgetTotals BudgetTotals { get; set; } = new();
    }

    private class BudgetTotals
    {
        public decimal Pw119Total { get; set; }

        public decimal P79Total { get; set; }

        public decimal PettyCashTotal { get; set; }

        public decimal PlanTotal { get; set; }
    }
}
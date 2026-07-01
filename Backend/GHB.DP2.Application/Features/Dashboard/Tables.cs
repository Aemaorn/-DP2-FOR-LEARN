// EndpointBase

namespace GHB.DP2.Application.Features.Dashboard;

using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetOperationGroupTableRequest(
    int? BudgetYear,
    string? SupplyMethodCode,
    string? DepartmentCode,
    int? UserOrgLevel, // 200(Group)/300(Line)/400(Department)
    string? UserOrgId
);

public record OperationGroupTableRow(
    string GroupCode,
    string DivisionCode,
    string DivisionName,
    string? DepartmentCode,
    string? DepartmentName,
    bool IsDivisionHeader,
    int OrgLevel,
    decimal PlanAmount,
    decimal PlanPercent,
    decimal ProcurementAmount,
    decimal ProcurementPercent,
    decimal ContractAmount,
    decimal ContractPercent
);

public record OperationGroupTableSummary(
    decimal TotalPlanAmount,
    decimal TotalProcurementAmount,
    decimal TotalContractAmount
);

public record GetOperationGroupTableResponse(
    OperationGroupTableSummary Summary,
    IEnumerable<OperationGroupTableRow> Rows
);

public class GetOperationGroupTableEndpoint
    : EndpointBase<GetOperationGroupTableRequest, Ok<GetOperationGroupTableResponse>>
{
    private readonly Dp2DbContext dbContext;

    public GetOperationGroupTableEndpoint(
        ILogger<GetOperationGroupTableEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("dashboard/tables");
        this.Options(o => o.WithTags("Dashboard"));
    }

    protected override async ValueTask<Ok<GetOperationGroupTableResponse>> HandleRequestAsync(
        GetOperationGroupTableRequest req,
        CancellationToken ct)
    {
        var supplyMethodVo = GetSupplyMethodValueObject(req.SupplyMethodCode);

        var planQ = this.ApplyFiltersToPlans(req, supplyMethodVo);
        var proQ = this.ApplyFiltersToProcurements(req, supplyMethodVo);
        var conQ = this.ApplyFiltersToContracts(req, supplyMethodVo);

        var planRaw = await GetPlanRawDataAsync(planQ, ct);
        var proRaw = await GetProcurementRawDataAsync(proQ, ct);
        var conRaw = await GetContractRawDataAsync(conQ, ct);

        var filteredData = ApplyOrgLevelFilter(planRaw, proRaw, conRaw, req);
        planRaw = filteredData.PlanRaw;
        proRaw = filteredData.ProRaw;
        conRaw = filteredData.ConRaw;

        var aggregationResult = this.AggregateDataByOrgLevels(planRaw, proRaw, conRaw);
        var rows = BuildTableRows(aggregationResult);

        rows = ApplyUserLevelPostFilter(rows, req);
        rows = AdjustDivisionHeaders(rows, req);

        var summary = BuildSummary(rows);
        var resp = new GetOperationGroupTableResponse(Summary: summary, Rows: rows);

        return TypedResults.Ok(resp);
    }

    private static ParameterCode? GetSupplyMethodValueObject(string? supplyMethodCode)
    {
        if (string.IsNullOrWhiteSpace(supplyMethodCode))
        {
            return null;
        }

        return ParameterCode.From(supplyMethodCode);
    }

    private IQueryable<Plan> ApplyFiltersToPlans(GetOperationGroupTableRequest req, ParameterCode? supplyMethodVo)
    {
        var query = this.dbContext.Plans.AsNoTracking().Where(x => !x.IsDeleted);

        if (req.BudgetYear.HasValue)
        {
            query = query.Where(x => x.BudgetYear == req.BudgetYear.Value);
        }

        if (supplyMethodVo.HasValue)
        {
            query = query.Where(x => x.SupplyMethodCode == supplyMethodVo.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.DepartmentCode))
        {
            query = query.Where(x => x.Department.Id == BusinessUnitId.From(req.DepartmentCode));
        }

        return query;
    }

    private IQueryable<Procurement> ApplyFiltersToProcurements(GetOperationGroupTableRequest req, ParameterCode? supplyMethodVo)
    {
        var query = this.dbContext.Procurements.AsNoTracking().Where(x => !x.IsDeleted);

        if (req.BudgetYear.HasValue)
        {
            query = query.Where(x => x.BudgetYear == req.BudgetYear.Value);
        }

        if (supplyMethodVo.HasValue)
        {
            query = query.Where(x => x.SupplyMethodCode == supplyMethodVo.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.DepartmentCode))
        {
            var deptVo = BusinessUnitId.From(req.DepartmentCode);
            query = query.Where(x => x.Department.Id == deptVo);
        }

        return query;
    }

    private IQueryable<CaContractDraftVendor> ApplyFiltersToContracts(GetOperationGroupTableRequest req, ParameterCode? supplyMethodVo)
    {
        var query = this.dbContext.CaContractDraftVendors.AsNoTracking();

        if (req.BudgetYear.HasValue)
        {
            query = query.Where(v => v.ContractDraft.Procurement.BudgetYear == req.BudgetYear.Value);
        }

        if (supplyMethodVo.HasValue)
        {
            query = query.Where(v => v.ContractDraft.Procurement.SupplyMethodCode == supplyMethodVo.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.DepartmentCode))
        {
            var deptVo = BusinessUnitId.From(req.DepartmentCode);
            query = query.Where(v => v.ContractDraft.Procurement.Department.Id == deptVo);
        }

        return query;
    }

    private sealed record DepartmentData(
        string DeptId,
        string DeptName,
        string? ParentId,
        string? ParentName,
        string? GrandParentId,
        string? GrandParentName,
        decimal Amount
    );

    private static async Task<List<DepartmentData>> GetPlanRawDataAsync(IQueryable<Plan> planQ, CancellationToken ct)
    {
        return await planQ.Select(p => new DepartmentData(
            p.Department.Id.Value,
            p.Department.Name,
            p.Department.ParentId != null ? p.Department.Parent.Id.Value : null,
            p.Department.ParentId != null ? p.Department.Parent.Name : null,
            p.Department.Parent != null && p.Department.Parent.ParentId != null ? p.Department.Parent.Parent.Id.Value : null,
            p.Department.Parent != null && p.Department.Parent.ParentId != null ? p.Department.Parent.Parent.Name : null,
            p.Budget)).ToListAsync(ct);
    }

    private static async Task<List<DepartmentData>> GetProcurementRawDataAsync(IQueryable<Procurement> proQ, CancellationToken ct)
    {
        return await proQ.Select(p => new DepartmentData(
            p.Department.Id.Value,
            p.Department.Name,
            p.Department.ParentId != null ? p.Department.Parent.Id.Value : null,
            p.Department.ParentId != null ? p.Department.Parent.Name : null,
            p.Department.Parent != null && p.Department.Parent.ParentId != null ? p.Department.Parent.Parent.Id.Value : null,
            p.Department.Parent != null && p.Department.Parent.ParentId != null ? p.Department.Parent.Parent.Name : null,
            p.Budget ?? 0m)).ToListAsync(ct);
    }

    private static async Task<List<DepartmentData>> GetContractRawDataAsync(IQueryable<CaContractDraftVendor> conQ, CancellationToken ct)
    {
        return await conQ.Select(v => new DepartmentData(
            v.ContractDraft.Procurement.Department.Id.Value,
            v.ContractDraft.Procurement.Department.Name,
            v.ContractDraft.Procurement.Department.ParentId != null ? v.ContractDraft.Procurement.Department.Parent.Id.Value : null,
            v.ContractDraft.Procurement.Department.ParentId != null ? v.ContractDraft.Procurement.Department.Parent.Name : null,
            (v.ContractDraft.Procurement.Department.Parent != null && v.ContractDraft.Procurement.Department.Parent.ParentId != null) ? v.ContractDraft.Procurement.Department.Parent.Parent.Id.Value : null,
            (v.ContractDraft.Procurement.Department.Parent != null && v.ContractDraft.Procurement.Department.Parent.ParentId != null) ? v.ContractDraft.Procurement.Department.Parent.Parent.Name : null,
            v.Budget)).ToListAsync(ct);
    }

    private record FilteredData(
        List<DepartmentData> PlanRaw,
        List<DepartmentData> ProRaw,
        List<DepartmentData> ConRaw
    );

    private static FilteredData ApplyOrgLevelFilter(
        List<DepartmentData> planRaw,
        List<DepartmentData> proRaw,
        List<DepartmentData> conRaw,
        GetOperationGroupTableRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UserOrgId) || !req.UserOrgLevel.HasValue)
        {
            return new FilteredData(planRaw, proRaw, conRaw);
        }

        return req.UserOrgLevel.Value switch
        {
            400 => new FilteredData(
                [.. planRaw.Where(x => x.DeptId == req.UserOrgId)],
                [.. proRaw.Where(x => x.DeptId == req.UserOrgId)],
                [.. conRaw.Where(x => x.DeptId == req.UserOrgId)]),
            300 or 200 => new FilteredData(
                [.. planRaw.Where(x => x.ParentId == req.UserOrgId || x.DeptId == req.UserOrgId)],
                [.. proRaw.Where(x => x.ParentId == req.UserOrgId || x.DeptId == req.UserOrgId)],
                [.. conRaw.Where(x => x.ParentId == req.UserOrgId || x.DeptId == req.UserOrgId)]),
            _ => new FilteredData(planRaw, proRaw, conRaw),
        };
    }

    private record AggregationResult(
        List<GroupAggregation> GroupAgg
    );

    private record GroupAggregation(
        string GroupCode,
        string GroupName,
        decimal PlanAmount,
        decimal ProcurementAmount,
        decimal ContractAmount,
        List<LineAggregation> Lines
    );

    private record LineAggregation(
        string GroupCode,
        string GroupName,
        string LineCode,
        string LineName,
        decimal PlanAmount,
        decimal ProcurementAmount,
        decimal ContractAmount,
        List<OperationGroupTableRow> Departments
    );

    private static string GetLineCode(DepartmentData x) => x.ParentId ?? x.DeptId;

    private static string GetLineName(DepartmentData x) => x.ParentName ?? x.DeptName;

    private static string GetGroupCode(DepartmentData x) => x.GrandParentId ?? x.ParentId ?? x.DeptId;

    private static string GetGroupName(DepartmentData x) => x.GrandParentName ?? x.ParentName ?? x.DeptName;

    private AggregationResult AggregateDataByOrgLevels(
        List<DepartmentData> planRaw,
        List<DepartmentData> proRaw,
        List<DepartmentData> conRaw)
    {
        var planDept = GroupDepartmentData(planRaw, d => d.Amount);
        var proDept = GroupDepartmentData(proRaw, d => d.Amount);
        var conDept = GroupDepartmentData(conRaw, d => d.Amount);

        var deptKeys = planDept.Keys
                               .Concat(proDept.Keys)
                               .Concat(conDept.Keys)
                               .Distinct()
                               .ToList();

        var deptRows = deptKeys.Select(k =>
        {
            var p = planDept.GetValueOrDefault(k, 0m);
            var pr = proDept.GetValueOrDefault(k, 0m);
            var c = conDept.GetValueOrDefault(k, 0m);

            return new OperationGroupTableRow(
                GroupCode: k.GroupCode,
                DivisionCode: k.LineCode,
                DivisionName: k.LineName,
                DepartmentCode: k.DepartmentCode,
                DepartmentName: k.DepartmentName,
                IsDivisionHeader: false,
                OrgLevel: 400,
                PlanAmount: p,
                PlanPercent: 0,
                ProcurementAmount: pr,
                ProcurementPercent: 0,
                ContractAmount: c,
                ContractPercent: 0);
        }).ToList();

        var lineAgg = deptRows.GroupBy(r => new
        {
            r.DivisionCode,
            r.DivisionName,
            GroupCode = deptKeys.First(k => k.LineCode == r.DivisionCode && k.LineName == r.DivisionName).GroupCode,
            GroupName = deptKeys.First(k => k.LineCode == r.DivisionCode && k.LineName == r.DivisionName).GroupName,
        })
                              .Select(g => new LineAggregation(
                                  GroupCode: g.Key.GroupCode,
                                  GroupName: g.Key.GroupName,
                                  LineCode: g.Key.DivisionCode,
                                  LineName: g.Key.DivisionName,
                                  PlanAmount: g.Sum(x => x.PlanAmount),
                                  ProcurementAmount: g.Sum(x => x.ProcurementAmount),
                                  ContractAmount: g.Sum(x => x.ContractAmount),
                                  Departments: [.. g]))
                              .ToList();

        var groupAgg = lineAgg.GroupBy(x => new { x.GroupCode, x.GroupName })
                              .Select(g => new GroupAggregation(
                                  GroupCode: g.Key.GroupCode,
                                  GroupName: g.Key.GroupName,
                                  PlanAmount: g.Sum(x => x.PlanAmount),
                                  ProcurementAmount: g.Sum(x => x.ProcurementAmount),
                                  ContractAmount: g.Sum(x => x.ContractAmount),
                                  Lines: [.. g]))
                              .ToList();

        return new AggregationResult(groupAgg);
    }

    private static Dictionary<DepartmentKey, decimal> GroupDepartmentData(List<DepartmentData> data, Func<DepartmentData, decimal> amountSelector)
    {
        return data.GroupBy(x => new DepartmentKey(
                       GroupCode: GetGroupCode(x),
                       GroupName: GetGroupName(x),
                       LineCode: GetLineCode(x),
                       LineName: GetLineName(x),
                       DepartmentCode: x.DeptId,
                       DepartmentName: x.DeptName))
                   .ToDictionary(g => g.Key, g => g.Sum(amountSelector));
    }

    private record DepartmentKey(
        string GroupCode,
        string GroupName,
        string LineCode,
        string LineName,
        string DepartmentCode,
        string DepartmentName
    );

    private static decimal CalculatePercent(decimal numerator, decimal denominator)
    {
        return denominator <= 0 ? 0m : Math.Round(numerator / denominator * 100m, 2, MidpointRounding.AwayFromZero);
    }

    private static List<OperationGroupTableRow> BuildTableRows(AggregationResult aggregationResult)
    {
        var rows = new List<OperationGroupTableRow>();

        foreach (var grp in aggregationResult.GroupAgg.OrderBy(x => x.GroupName))
        {
            rows.Add(new OperationGroupTableRow(
                GroupCode: grp.GroupCode,
                DivisionCode: grp.GroupCode,
                DivisionName: grp.GroupName,
                DepartmentCode: null,
                DepartmentName: grp.GroupName,
                IsDivisionHeader: false,
                OrgLevel: 200,
                PlanAmount: grp.PlanAmount,
                PlanPercent: 100m,
                ProcurementAmount: grp.ProcurementAmount,
                ProcurementPercent: CalculatePercent(grp.ProcurementAmount, grp.PlanAmount),
                ContractAmount: grp.ContractAmount,
                ContractPercent: CalculatePercent(grp.ContractAmount, grp.PlanAmount)));

            foreach (var line in grp.Lines.OrderBy(l => l.LineName))
            {
                rows.Add(new OperationGroupTableRow(
                    GroupCode: grp.GroupCode,
                    DivisionCode: line.LineCode,
                    DivisionName: line.LineName,
                    DepartmentCode: null,
                    DepartmentName: line.LineName,
                    IsDivisionHeader: false,
                    OrgLevel: 300,
                    PlanAmount: line.PlanAmount,
                    PlanPercent: 100m,
                    ProcurementAmount: line.ProcurementAmount,
                    ProcurementPercent: CalculatePercent(line.ProcurementAmount, line.PlanAmount),
                    ContractAmount: line.ContractAmount,
                    ContractPercent: CalculatePercent(line.ContractAmount, line.PlanAmount)));

                foreach (var dep in line.Departments.OrderBy(d => d.DepartmentName))
                {
                    rows.Add(new OperationGroupTableRow(
                        GroupCode: line.GroupCode,
                        DivisionCode: dep.DivisionCode,
                        DivisionName: dep.DivisionName,
                        DepartmentCode: dep.DepartmentCode,
                        DepartmentName: dep.DepartmentName,
                        IsDivisionHeader: false,
                        OrgLevel: 400,
                        PlanAmount: dep.PlanAmount,
                        PlanPercent: dep.PlanAmount > 0 ? 100m : 0m,
                        ProcurementAmount: dep.ProcurementAmount,
                        ProcurementPercent: CalculatePercent(dep.ProcurementAmount, dep.PlanAmount),
                        ContractAmount: dep.ContractAmount,
                        ContractPercent: CalculatePercent(dep.ContractAmount, dep.PlanAmount)));
                }
            }
        }

        return rows;
    }

    private static List<OperationGroupTableRow> ApplyUserLevelPostFilter(List<OperationGroupTableRow> rows, GetOperationGroupTableRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UserOrgId) || !req.UserOrgLevel.HasValue)
        {
            return rows;
        }

        return req.UserOrgLevel.Value switch
        {
            400 => [.. rows.Where(r => r.OrgLevel == 400 && r.DepartmentCode == req.UserOrgId)],
            300 => [.. rows.Where(r => (r.OrgLevel == 300 && r.DivisionCode == req.UserOrgId) || (r.OrgLevel == 400 && r.DivisionCode == req.UserOrgId))],
            200 => [.. rows.Where(r => (r.OrgLevel == 200 && r.GroupCode == req.UserOrgId) || r.GroupCode == req.UserOrgId)],
            _ => rows,
        };
    }

    private static List<OperationGroupTableRow> AdjustDivisionHeaders(List<OperationGroupTableRow> rows, GetOperationGroupTableRequest req)
    {
        if (!req.UserOrgLevel.HasValue)
        {
            return rows;
        }

        return req.UserOrgLevel.Value switch
        {
            400 => [.. rows.Select(r => r.OrgLevel == 400 ? r with { IsDivisionHeader = true } : r)],
            300 => [.. rows.Select(r => r.OrgLevel == 300 ? r with { IsDivisionHeader = true } : r)],
            200 => [.. rows.Select(r => r.OrgLevel == 200 ? r with { IsDivisionHeader = true } : r)],
            _ => rows,
        };
    }

    private static OperationGroupTableSummary BuildSummary(List<OperationGroupTableRow> rows)
    {
        var headerRows = rows.Where(r => r.IsDivisionHeader);

        return new OperationGroupTableSummary(
            TotalPlanAmount: headerRows.Sum(r => r.PlanAmount),
            TotalProcurementAmount: headerRows.Sum(r => r.ProcurementAmount),
            TotalContractAmount: headerRows.Sum(r => r.ContractAmount));
    }
}
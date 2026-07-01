namespace GHB.DP2.Application.Features.Plan.Plan;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListDialogRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    PlanType? Type,
    string? DepartmentCode,
    int? BudgetYear,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodFilter);

public record GetListDialogResponse(
    PlanId Id,
    PlanStatus Status,
    string PlanNumber,
    string PlanName,
    decimal Budget,
    PlanType Type,
    string DepartmentName,
    BusinessUnitId DepartmentCode,
    string SupplyMethod,
    ParameterCode SupplyMethodCode,
    string? SupplyMethodType,
    ParameterCode? SupplyMethodTypeCode,
    string? SupplyMethodSpecialType,
    ParameterCode? SupplyMethodSpecialTypeCode,
    DateTimeOffset ExpectingProcurementAt,
    bool IsStock,
    bool IsCommercialMaterial,
    decimal BudgetYear);

public record GetSupplyMethodCount(ParameterCode Code, string Name, int Count);

public record GetListDialogResult(
    GetSupplyMethodCount[] SupplyMethodCount,
    PaginatedQueryResult<GetListDialogResponse> Data
);

public class GetListDialog : EndpointBase<GetListDialogRequest, Ok<GetListDialogResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetListDialog(Dp2DbContext dbContext, ILogger<GetListDialog> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Plan")
             .WithName("GetListDialog")
             .Produces<Ok>());
        this.Get("plan/dialog");
    }

    protected override async ValueTask<Ok<GetListDialogResult>> HandleRequestAsync(GetListDialogRequest req, CancellationToken ct)
    {
        var query = this.dbContext.Plans
                        .OrderByDescending(x => x.AuditInfo.LastModifiedAt)
                        .ThenByDescending(x => x.AuditInfo.CreatedAt)
                        .ThenByDescending(x => x.PlanNumber)
                        .Where(w => w.IsActive)
                        .Where(x => !x.IsDeleted)
                        .Where(w => !w.IsCancel)
                        .Where(w => w.Status != PlanStatus.Closed)
                        .Where(w => (w.Status == PlanStatus.Announcement && w.Budget > 500000) || (w.Status == PlanStatus.ApprovePlan && w.Budget <= 500000))
                        .Where(w => (this.dbContext.Procurements
                                        .Where(p => p.PlanId == w.Id && p.IsCancelled == false && p.Status != ProcurementStatus.Cancelled)
                                        .Sum(p => (decimal?)p.Budget) ?? 0m) < w.Budget)
                        .WhereIfTrue(!string.IsNullOrEmpty(req.Keyword), x => EF.Functions.ILike(x.Name, $"%{req.Keyword}%") || EF.Functions.ILike((string)x.PlanNumber, $"%{req.Keyword}%"))
                        .WhereIfTrue(!req.Type.IsNull(), x => x.Type == req.Type)
                        .WhereIfTrue(!string.IsNullOrEmpty(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                        .WhereIfTrue(!req.BudgetYear.IsNull(), x => x.BudgetYear == req.BudgetYear)
                        .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodCode), x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                        .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodTypeCode), x => x.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!));

        var paginatedQuery =
            query.WhereIfTrue(
                !string.IsNullOrEmpty(req.SupplyMethodFilter),
                p => p.SupplyMethodCode == ParameterCode.From(req.SupplyMethodFilter!));

        var paginated = await PaginatedList<Plan>.CreateAsync((IQueryable<Plan>)paginatedQuery, req.PageNumber, req.PageSize, ct);

        var supplyMethodCount =
            await this.dbContext.SuParameters
                      .Where(p =>
                          p.GroupCode == GroupCode.From(ParameterGroupConstant.SupplyMethod) &&
                          p.ParentId == null)
                      .GroupJoin(
                          query,
                          p => p.Code,
                          pl => pl.SupplyMethodCode,
                          (p, pl) => new GetSupplyMethodCount(
                              p.Code,
                              p.Label,
                              pl.Count()))
                      .ToListAsync(ct);

        supplyMethodCount.Insert(0, new GetSupplyMethodCount(
            ParameterCode.From("ALL"),
            "ทั้งหมด",
            supplyMethodCount.Count));

        var procurementSumBudget = await this.dbContext.Procurements
                                             .Where(p => p.IsCancelled == false && p.Status != ProcurementStatus.Cancelled)
                                             .GroupBy(x => x.PlanId)
                                             .Select(g => new
                                             {
                                                 PlanId = g.Key,
                                                 totalBudget = g.Sum(x => x.Budget),
                                             })
                                             .ToListAsync(ct);

        var data = paginated.ToResult(x => new GetListDialogResponse(
            x.Id,
            x.Status,
            x.PlanNumber.Value,
            x.Name,
            x.Budget - procurementSumBudget.Where(p => p.PlanId == x.Id).Sum(x => x.totalBudget) ?? 0m,
            x.Type,
            x.Department.Name,
            x.DepartmentId,
            x.SupplyMethod.Label,
            x.SupplyMethodCode,
            x.SupplyMethodType?.Label,
            x.SupplyMethodTypeCode,
            x.SupplyMethodSpecialType?.Label,
            x.SupplyMethodSpecialTypeCode,
            x.ExpectingProcurementAt,
            x.IsStock,
            x.IsCommercialMaterial ?? false,
            x.BudgetYear));

        return TypedResults.Ok(new GetListDialogResult([.. supplyMethodCount], data));
    }
}
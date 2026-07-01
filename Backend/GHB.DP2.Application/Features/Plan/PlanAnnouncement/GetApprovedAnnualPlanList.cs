namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using FluentValidation;
using GHB.DP2.Application.Features.Plan.PlanAnnouncement.DTO;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetApprovedAnnualPlanListRequest
{
    public int Year { get; init; }

    public string SupplyMethodCode { get; init; }
}

public class GetApprovedAnnualPlanListRequestValidator : Validator<GetApprovedAnnualPlanListRequest>
{
    public GetApprovedAnnualPlanListRequestValidator()
    {
        this.RuleFor(p => p.Year)
            .NotEmpty()
            .WithMessage("กรุณาระบุปีงบประมาณ");

        this.RuleFor(p => p.SupplyMethodCode)
            .NotEmpty()
            .WithMessage("กรุณาระบุวิธีการจัดหา");
    }
}

public class GetApprovedAnnualPlanList : EndpointBase<GetApprovedAnnualPlanListRequest, Ok<IEnumerable<PlanSelectedInfo>>>
{
    private readonly Dp2DbContext dbContext;

    public GetApprovedAnnualPlanList(Dp2DbContext dbContext, ILogger<GetApprovedAnnualPlanList> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(PlanAnnouncement))
             .WithName("GetApprovedAnnualPlanList"));
        this.Get("plan/announcement/get-list-annual-plan");
    }

    protected override async ValueTask<Ok<IEnumerable<PlanSelectedInfo>>> HandleRequestAsync(
        GetApprovedAnnualPlanListRequest req,
        CancellationToken ct)
    {
        var planSelected = this.dbContext.PlanAnnouncementSelecteds
                               .Include(w => w.Plan)
                               .Where(w =>
                                   w.Plan.BudgetYear == req.Year &&
                                   w.Plan.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode) &&
                                   w.Plan.Budget > 500000 &&
                                   !w.Plan.IsCancel && !w.Plan.IsChange &&
                                   w.Plan.IsActive)
                               .AsNoTracking()
                               .Select(s => s.Plan);

        var annualPlanData = await this.dbContext.Plans
                                       .Except(planSelected)
                                       .Include(plan => plan.Department)
                                       .Include(plan => plan.SupplyMethod)
                                       .Include(plan => plan.SupplyMethodType)
                                       .Where(w =>
                                           w.Status == PlanStatus.ApprovePlan &&
                                           w.Type == PlanType.AnnualPlan &&
                                           w.BudgetYear == req.Year &&
                                           w.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode) &&
                                           w.Budget > 500000 &&
                                           !w.IsCancel && !w.IsChange
                                           && w.IsActive)
                                       .AsNoTracking()
                                       .ToArrayAsync(ct);

        var result = annualPlanData.OrderBy(x => x.PlanNumber).Map(s => new PlanSelectedInfo
        {
            PlanId = s.Id.Value,
            PlanNumber = s.PlanNumber.Value,
            PlanTitle = s.Name,
            Budget = s.Budget,
            DepartmentName = s.Department.Name,
            SupplyMethodName = s.SupplyMethod.Label,
            SupplyMethodTypeName = s.SupplyMethodType?.Label,
            EgpNumber = s.EgpNumber,
        });

        return TypedResults.Ok(result);
    }
}
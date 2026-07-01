namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RejectAnnualPlanByIdRequest(
    Guid PlanId);

public class RejectAnnualPlanByIdEndpoint : EndpointBase<RejectAnnualPlanByIdRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectAnnualPlanByIdEndpoint(
        Dp2DbContext dbContext,
        ILogger<RejectAnnualPlanByIdEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(PlanAnnouncement))
             .WithName("RejectAnnualPlanById")
             .Accepts<RejectAnnualPlanByIdRequest>());
        this.Put("plan/announcement/reject-annual-plan/{PlanId:guid}");
        this.AuditLog("ขออนุมัติเผยแพร่จัดซื้อจัดจ้าง", "ปฏิเสธแผนประจำปี");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        RejectAnnualPlanByIdRequest req,
        CancellationToken ct)
    {
        var planData = await this.dbContext.Plans.SingleOrDefaultAsync(
            w => w.Id == PlanId.From(req.PlanId),
            ct);

        if (planData is null)
        {
            return TypedResults.NotFound($"Plan {req.PlanId} id not found");
        }

        planData.SetRejected(PlanStatus.RejectPlan);
        this.dbContext.Plans.Update(planData);

        var planSelectedData = await this.dbContext.PlanAnnouncementSelecteds
                                         .SingleOrDefaultAsync(
                                             w => w.PlanId == planData.Id,
                                             ct);

        if (planSelectedData is not null)
        {
            this.dbContext.PlanAnnouncementSelecteds.Remove(planSelectedData);
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
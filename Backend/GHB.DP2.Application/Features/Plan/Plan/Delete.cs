namespace GHB.DP2.Application.Features.Plan.Plan;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeletePlanRequest(
    Guid Id);

public class DeletePlan : EndpointBase<DeletePlanRequest, Results<NoContent, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeletePlan(
        Dp2DbContext dbContext,
        ILogger<DeletePlan> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Plan")
             .WithName("DeletePlan")
             .Produces<NoContent>()
             .Produces<NotFound>());
        this.Delete("plan/{Id:guid}");
        this.AuditLog("รายการจัดซื้อจัดจ้าง", "ลบแผนจัดซื้อจัดจ้าง");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>, Conflict<string>>> HandleRequestAsync(DeletePlanRequest req, CancellationToken ct)
    {
        var data = await this.dbContext
                             .Plans
                             .AsNoTracking()
                             .Include(plan => plan.Acceptors)
                             .Include(plan => plan.Assignees)
                             .Include(plan => plan.Attachments)
                             .SingleOrDefaultAsync(p => p.Id == PlanId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"ไม่พบแผน");
        }

        var usedPlan = await this.dbContext.Procurements
                                 .Where(w => w.PlanId == data.Id)
                                 .AnyAsync(ct);

        if (usedPlan)
        {
            return TypedResults.Conflict("แผนถูกใช้งานบนจัดซื้อจัดจ้างแล้ว");
        }

        var announcement = await this.dbContext.PlanAnnouncementSelecteds
                                     .Where(w => w.PlanId == data.Id)
                                     .AnyAsync(ct);

        if (announcement)
        {
            return TypedResults.Conflict("แผนถูกใช้งานบนเผยแพร่แผนแล้ว");
        }

        data.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Delete,
                $"ลบแผน {data.Name}",
                nameof(PlanStatus.DraftPlan)));

        data.SetPlanDelete();

        this.dbContext.Plans.Update(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
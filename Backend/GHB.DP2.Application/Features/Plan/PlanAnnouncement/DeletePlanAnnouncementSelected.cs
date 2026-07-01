namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeletePlanAnnouncementSelectedRequest(
    Guid PlanAnnouncementSelectedId);

public class DeletePlanAnnouncementSelected : EndpointBase<DeletePlanAnnouncementSelectedRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeletePlanAnnouncementSelected(Dp2DbContext dbContext, ILogger<DeletePlanAnnouncementSelected> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(PlanAnnouncement))
             .WithName("DeletePlanAnnouncementSelected"));
        this.Delete("plan/announcement/plan-announcement-selected/{PlanAnnouncementSelectedId:guid}");
        this.AuditLog("ขออนุมัติเผยแพร่จัดซื้อจัดจ้าง", "ลบรายการเผยแพร่แผน");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(
        DeletePlanAnnouncementSelectedRequest req,
        CancellationToken ct)
    {
        var planAnnouncementSelected = await this.dbContext.PlanAnnouncementSelecteds
                                                 .SingleOrDefaultAsync(
                                                     w => w.Id == PlanAnnouncementSelectedId.From(req.PlanAnnouncementSelectedId),
                                                     ct);

        if (planAnnouncementSelected is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        this.dbContext.PlanAnnouncementSelecteds.Remove(planAnnouncementSelected);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
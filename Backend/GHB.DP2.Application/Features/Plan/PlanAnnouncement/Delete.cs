namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeletePlanAnnouncementRequest(
    Guid Id);

public class DeletePlanAnnouncement : EndpointBase<DeletePlanAnnouncementRequest, NoContent>
{
    private readonly Dp2DbContext dbContext;

    public DeletePlanAnnouncement(
        Dp2DbContext dbContext,
        ILogger<DeletePlanAnnouncement> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(PlanAnnouncement))
             .WithName("DeletePlanAnnouncement")
             .Accepts<DeletePlanAnnouncementRequest>());
        this.Delete("plan/announcement/{Id:guid}");
        this.AuditLog("ขออนุมัติเผยแพร่จัดซื้อจัดจ้าง", "ลบประกาศเผยแพร่แผน");
    }

    protected override async ValueTask<NoContent> HandleRequestAsync(
        DeletePlanAnnouncementRequest req,
        CancellationToken ct)
    {
        var announcement = await this.dbContext.PlanAnnouncements
                                     .SingleOrDefaultAsync(
                                         w => w.Id == PlanAnnouncementId.From(req.Id),
                                         ct);

        if (announcement is null)
        {
            this.ThrowError(
                r => r.Id,
                $"ไม่พบข้อมูลประกาศเผยแพร่แผน",
                StatusCodes.Status404NotFound);
        }

        if (announcement.Status is not PlanAnnouncementStatus.Draft and PlanAnnouncementStatus.Rejected)
        {
            this.ThrowError(
                $"ไม่สามารลบข้อมูลได้",
                StatusCodes.Status409Conflict);
        }

        var planAnnouncementSelectedData = await this.dbContext.PlanAnnouncementSelecteds
                                                     .Where(w => w.PlanAnnouncementId == announcement.Id)
                                                     .ToListAsync(ct);

        if (planAnnouncementSelectedData.Count > 0)
        {
            this.dbContext.PlanAnnouncementSelecteds.RemoveRange(planAnnouncementSelectedData);
        }

        this.dbContext.PlanAnnouncements.Remove(announcement);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
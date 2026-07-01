namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Plan.PlanAnnouncement.Abstract;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record PreviewPlanAnnouncementDocumentRequest(
    Guid Id,
    PlanAnnouncementDocumentType DocumentType);

public class PreviewPlanAnnouncementDocumentEndpoint : PlanAnnouncementEndpointBase<PreviewPlanAnnouncementDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public PreviewPlanAnnouncementDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewPlanAnnouncementDocumentEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(PlanAnnouncement))
             .WithName("PlanAnnouncementPreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("plan/announcement/{Id:guid}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewPlanAnnouncementDocumentRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.PlanAnnouncements
                             .AsNoTracking()
                             .Include(i => i.AnnouncementSelectedInformations)
                             .ThenInclude(planAnnouncementSelected => planAnnouncementSelected.Plan)
                             .ThenInclude(plan => plan.Department)
                             .Include(i => i.Attachments)
                             .Include(i => i.Assignees)
                             .Include(i => i.Acceptors)
                             .Include(planAnnouncement => planAnnouncement.AnnouncementSelectedInformations)
                             .ThenInclude(planAnnouncementSelected => planAnnouncementSelected.Plan)
                             .ThenInclude(plan => plan.SupplyMethod)
                             .Include(planAnnouncement => planAnnouncement.AnnouncementSelectedInformations)
                             .ThenInclude(planAnnouncementSelected => planAnnouncementSelected.Plan)
                             .ThenInclude(plan => plan.SupplyMethodType).Include(planAnnouncement => planAnnouncement.DocumentHistories)
                             .SingleOrDefaultAsync(p => p.Id == PlanAnnouncementId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบเอกสารที่ต้องการ");
        }

        return await this.PreviewPlanAnnouncementDocumentAsync(data, req.DocumentType, this.fileServiceClient, hasPublicPlan: false, hasAcceptors: false, hasAssignees: false, ct);
    }
}
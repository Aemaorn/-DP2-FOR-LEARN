namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Plan.PlanAnnouncement.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetPlanAnnouncementDocumentRequest(Guid Id, PlanAnnouncementDocumentType DocumentType);

public class ResetPlanAnnouncementDocumentEndpoint : PlanAnnouncementEndpointBase<ResetPlanAnnouncementDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetPlanAnnouncementDocumentEndpoint(
        ILogger<ResetPlanAnnouncementDocumentEndpoint> logger,
        Dp2DbContext dbContext)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(PlanAnnouncement))
             .WithName("ResetPlanAnnouncementDocument")
             .Accepts<ResetPlanAnnouncementDocumentRequest>("application/json"));
        this.Post("plan/announcement/{Id:guid}/reset-document");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetPlanAnnouncementDocumentRequest req,
        CancellationToken ct)
    {
        // 1. Get full PlanAnnouncement with all relationships needed for placeholder replacement
        var planAnnouncement = await this.dbContext.PlanAnnouncements
                                         .Include(p => p.DocumentHistories)
                                         .Include(p => p.AnnouncementSelectedInformations)
                                         .ThenInclude(asi => asi.Plan)
                                         .ThenInclude(plan => plan.SupplyMethod)
                                         .Include(p => p.AnnouncementSelectedInformations)
                                         .ThenInclude(asi => asi.Plan)
                                         .ThenInclude(plan => plan.SupplyMethodType)
                                         .Include(p => p.AnnouncementSelectedInformations)
                                         .ThenInclude(asi => asi.Plan)
                                         .ThenInclude(plan => plan.Department)
                                         .Include(p => p.Acceptors)
                                         .ThenInclude(a => a.Delegatee)
                                         .Include(p => p.Acceptors)
                                         .ThenInclude(a => a.User)
                                         .ThenInclude(u => u.Employee)
                                         .ThenInclude(e => e.Positions)
                                         .ThenInclude(pos => pos.BusinessUnit)
                                         .Include(p => p.Assignees)
                                         .ThenInclude(a => a.Delegatee)
                                         .Include(p => p.Assignees)
                                         .ThenInclude(a => a.User)
                                         .ThenInclude(u => u.Employee)
                                         .ThenInclude(e => e.Positions)
                                         .ThenInclude(pos => pos.BusinessUnit)
                                         .AsSplitQuery()
                                         .FirstOrDefaultAsync(p => p.Id == PlanAnnouncementId.From(req.Id), ct);

        if (planAnnouncement == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        // 2. Get latest template from SuDocumentTemplates
        var templateFileId = await this.GetDocumentTemplateForResetAsync(planAnnouncement, req.DocumentType, ct);

        var documentService = this.Resolve<IDocumentService>();

        // 3. Create replacement DTO using base class method
        var replaceDto = this.MapToPlanAnnouncementReplateAsync(planAnnouncement);

        var parentDirectory =
            $"{DocumentTemplateGroups.PlanAnnouncement}/{planAnnouncement.PlanAnnouncementNumber}_{req.DocumentType}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        // 4. Copy document WITH placeholder replacement
        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: parentDirectory,
            cancellationToken: ct);

        if (newFileId == null)
        {
            this.ThrowError(
                DocumentErrorMessages.CopyDocumentFailed,
                StatusCodes.Status500InternalServerError);
        }

        planAnnouncement.AddDocumentHistory(req.DocumentType, newFileId.Value, false, incrementMajor: true);

        this.dbContext.PlanAnnouncements.Update(planAnnouncement);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}

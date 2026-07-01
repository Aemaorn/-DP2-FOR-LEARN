namespace GHB.DP2.Application.Features.Plan.Plan;

using GHB.DP2.Application.Constants;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Plan.Plan.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetPlanDocumentRequest(Guid Id, PlanDocumentType DocumentType);

public class ResetPlanDocumentEndpoint : PlanEndpointBase<ResetPlanDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetPlanDocumentEndpoint(
        ILogger<ResetPlanDocumentEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Plan")
             .WithName("ResetPlanDocument")
             .Accepts<ResetPlanDocumentRequest>("application/json"));
        this.Post("plan/{Id:guid}/reset-document");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetPlanDocumentRequest req,
        CancellationToken ct)
    {
        // 1. Get full Plan with all relationships needed for placeholder replacement
        var plan = await this.dbContext.Plans
                             .Include(p => p.DocumentHistories)
                             .Include(p => p.Acceptors)
                             .ThenInclude(a => a.Delegatee)
                             .Include(p => p.Assignees)
                             .Include(p => p.Attachments)
                             .Include(p => p.AuditInfo)
                             .FirstOrDefaultAsync(p => p.Id == PlanId.From(req.Id), ct);

        if (plan == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        // 2. Get latest template from SuDocumentTemplates
        var templateFileId = await this.GetDocumentTemplateForResetAsync(plan, req.DocumentType, ct);

        var documentService = this.Resolve<IDocumentService>();

        // 3. Create replacement DTO using base class method
        var replaceDto = await this.MapToPlanReplaceAsync(plan, cancellationToken: ct);

        var parentDirectory =
            $"{DocumentTemplateGroups.Plan}/{plan.PlanNumber}_{req.DocumentType}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

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

        plan.AddDocumentHistory(req.DocumentType, newFileId.Value, false, incrementMajor: true);

        this.dbContext.Plans.Update(plan);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}

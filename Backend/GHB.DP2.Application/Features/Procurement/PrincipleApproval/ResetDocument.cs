namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ResetPrincipleApprovalDocumentRequest(Guid ProcurementId, Guid Id);

public class ResetPrincipleApprovalDocumentEndpoint : PrincipleApprovalEndpointBase<ResetPrincipleApprovalDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetPrincipleApprovalDocumentEndpoint(
        ILogger<ResetPrincipleApprovalDocumentEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/principle-approval/{Id:guid}/reset-document");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApproval")
                              .WithName("ResetPrincipleApprovalDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetPrincipleApprovalDocumentRequest req,
        CancellationToken ct)
    {
        // 1. Get full PrincipleApproval with all relationships
        var principleApproval = await this.GetPPrincipleApprovalById(
            PPrincipleApprovalId.From(req.Id),
            ProcurementId.From(req.ProcurementId),
            ct);

        // 2. Get latest template from SuDocumentTemplates
        var templateFileId = await this.GetDocumentTemplateForResetAsync(principleApproval, ct);

        // 3. Create replacement DTO
        var replaceDto = await this.MapToReplaceDtoAsync(principleApproval, hasAcceptor: true, ct, creatorUserId: null);

        var parentDirectory =
            $"{DocumentTemplateGroups.PrincipleApproval}/{principleApproval.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        // 4. Copy document WITH placeholder replacement
        var documentService = this.Resolve<IDocumentService>();

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: parentDirectory,
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.CopyDocumentFailed,
                StatusCodes.Status500InternalServerError);
        }

        principleApproval.AddDocumentHistory(newFileId.Value, false);

        this.dbContext.PPrincipleApprovals.Update(principleApproval);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}

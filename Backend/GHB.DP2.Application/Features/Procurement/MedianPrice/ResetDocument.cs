namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ResetMedianPriceDocumentRequest(Guid ProcurementId, Guid Id);

public class ResetMedianPriceDocumentEndpoint : MedianPriceEndpointBase<ResetMedianPriceDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetMedianPriceDocumentEndpoint(
        ILogger<ResetMedianPriceDocumentEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/median-price/{Id:guid}/reset-document");
        this.Description(b =>
            b.WithTags(nameof(MedianPrice))
             .WithName("ResetMedianPriceDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetMedianPriceDocumentRequest req,
        CancellationToken ct)
    {
        // 1. Get full MedianPrice with all relationships
        var medianPrice = await this.GetMedianPriceById(
            MedianPriceId.From(req.Id),
            ProcurementId.From(req.ProcurementId),
            ct);

        // 2. Get Procurement with Appoints for replacement
        var procurement = await this.GetProcurementById(
            ProcurementId.From(req.ProcurementId),
            ct);

        // 3. Get latest template from SuDocumentTemplates (using base class method)
        var templateFileId = await this.GetDocumentTemplateAsync(medianPrice, ct);

        // 4. Create replacement DTO
        var replaceDto = await this.MapToReplaceDtoAsync(procurement, medianPrice, ct, creatorUserId: null);

        var parentDirectory =
            $"{DocumentTemplateGroups.Mdp}/{medianPrice.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        // 5. Copy document WITH placeholder replacement
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

        medianPrice.AddDocumentHistory(newFileId.Value);

        this.dbContext.PpMedianPrices.Update(medianPrice);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}

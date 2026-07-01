namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ResetContractDraftVendorEditDocumentRequest(
    Guid Id,
    CaContractDraftEditVendorDocumentType DocumentType);

public class ResetContractDraftVendorEditDocumentEndpoint
    : ContractDraftVendorEditEndpoint<ResetContractDraftVendorEditDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetContractDraftVendorEditDocumentEndpoint(
        Dp2DbContext dbContext,
        ILogger<ResetContractDraftVendorEditDocumentEndpoint> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("contract/contract-draft-vendor-edit/{Id:guid}/reset-document");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractDraftVendorEdit")
                              .WithName("ResetContractDraftVendorEditDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(ResetContractDraftVendorEditDocumentRequest req, CancellationToken ct)
    {
        var entity = await this.GetEditByIdAsync(ContractDraftVendorEditId.From(req.Id), ct);

        var documentHistories = entity.DocumentHistories
                                       .Where(d => d.DocumentType == req.DocumentType)
                                       .ToList();

        if (documentHistories.Count == 0)
        {
            return TypedResults.BadRequest(DocumentErrorMessages.DocumentNotFound);
        }

        var supplyMethodCode = await this.GetSupplyMethodCodeAsync(entity, ct);

        // Get template
        var templateFileId = await this.GetDocumentTemplateByTypeAsync(req.DocumentType, supplyMethodCode, ct);

        // Copy document with placeholder replacement
        var documentService = this.Resolve<IDocumentService>();
        var parentDirectory =
            $"{DocumentTemplateGroups.CMContractDraftVendorEdit}/{entity.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var replaceDto = await this.GetMappingDtoAsync(entity, ct: ct);
        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: parentDirectory,
            cancellationToken: ct);

        if (newFileId == null)
        {
            this.ThrowError(DocumentErrorMessages.CopyDocumentFailed, StatusCodes.Status500InternalServerError);
        }

        entity.AddDocumentHistory(req.DocumentType, newFileId.Value, false, incrementMajor: true);

        this.dbContext.CaContractDraftVendorEdits.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}

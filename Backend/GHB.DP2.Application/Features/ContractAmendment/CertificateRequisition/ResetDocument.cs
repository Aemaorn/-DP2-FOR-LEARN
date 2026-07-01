namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetCertificateRequisitionDocumentRequest(
    Guid Id);

public class ResetCertificateRequisitionDocumentEndpoint
    : CertificateRequisitionEndpointBase<ResetCertificateRequisitionDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetCertificateRequisitionDocumentEndpoint(
        Dp2DbContext dbContext,
        ILogger<ResetCertificateRequisitionDocumentEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("certificate-requisition/{Id:guid}/reset-document");
        this.Description(b =>
            b.WithTags("ContractAmendment/CertificateRequisition")
             .WithName("ResetCertificateRequisitionDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetCertificateRequisitionDocumentRequest req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.CamCertificateRequisitions
            .Include(cr => cr.DocumentHistories)
            .Include(cr => cr.ContractDraftVendor!)
            .ThenInclude(v => v.Vendor)
            .ThenInclude(v => v.VendorInfo)
            .Include(cr => cr.ContractDraftVendor!)
            .ThenInclude(v => v.ContractDraft)
            .ThenInclude(c => c.Procurement)
            .ThenInclude(p => p.SupplyMethodType)
            .FirstOrDefaultAsync(
                cr => cr.Id == CamCertificateRequisitionId.From(req.Id),
                ct);

        if (entity == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        // 1. Get template from SuDocumentTemplates
        var templateFileId = await this.GetDocumentTemplateForResetAsync(ct);

        // 2. Create replacement DTO
        var replaceDto = await this.MapReplaceAsync(entity, ct);

        var parentDirectory =
            $"{DocumentTemplateGroups.CertificateRequisition}/{entity.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var documentService = this.Resolve<IDocumentService>();

        // 3. Copy document WITH placeholder replacement
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

        entity.AddDocumentHistory(newFileId.Value);

        this.dbContext.CamCertificateRequisitions.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            d =>
                d.Group == DocumentTemplateGroups.CertificateRequisition &&
                d.IsActive,
            ct);

        if (templateFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.TemplateNotFoundForReset,
                StatusCodes.Status404NotFound);
        }

        return templateFileId.Value;
    }
}

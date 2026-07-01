namespace GHB.DP2.Application.Features.Procurement.PPettyCash;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.PPettyCash.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ResetPPettyCashDocumentRequest(Guid Id);

public class ResetPPettyCashDocumentEndpoint : PPettyCashEndpointBase<ResetPPettyCashDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetPPettyCashDocumentEndpoint(
        Dp2DbContext dbContext,
        ILogger<ResetPPettyCashDocumentEndpoint> logger,
        IFileServiceClient fileServiceClient)
        : base(dbContext, logger, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("PPettyCash")
             .WithName("ResetPPettyCashDocument")
             .Accepts<ResetPPettyCashDocumentRequest>("application/json"));
        this.Post("PPettyCash/{Id:guid}/reset-document");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetPPettyCashDocumentRequest req,
        CancellationToken ct)
    {
        // Use NO tracking query to get full data for mapping
        var pettyCash = await this.GetPettyCashById(PettyCashId.From(req.Id), ct);

        // Get latest template from SuDocumentTemplates
        var templateFileId = await this.GetDocumentTemplateForResetAsync(pettyCash.IsFromJorPor001, ct);

        // Create replace DTO
        var replaceDto = await this.MapToReplaceDto(pettyCash, hasAcceptor: false, ct, userId: null);

        // Use CopyDocumentTemplateAsync with ReplaceOdtDocument
        var documentService = this.Resolve<IDocumentService>();
        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{DocumentTemplateGroups.PettyCash}/{pettyCash.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(
                "ไม่สามารถดึง template เอกสารได้",
                StatusCodes.Status500InternalServerError);
        }

        // Get tracking entity for update
        var pettyCashForUpdate = await this.GetPettyCashByIdForUpdateAsync(PettyCashId.From(req.Id), ct);

        // Use forceMinorVersion to only increment minor version (e.g., 2.3 -> 2.4 instead of 3.1)
        pettyCashForUpdate.AddDocumentHistory(newFileId.Value, isReplace: false, forceMinorVersion: true);

        // No need to call Update() since entity is tracked
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(bool? isFromJorPor001, CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateCode = PettyCashTemplateConstant.GetTemplateCode(isFromJorPor001);

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.PettyCash &&
                dt.IsActive &&
                dt.Code == templateCode,
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

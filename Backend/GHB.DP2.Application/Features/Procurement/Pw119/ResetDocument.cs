namespace GHB.DP2.Application.Features.Procurement.Pw119;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Pw119.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ResetPw119DocumentRequest(
    Guid Id,
    Pw119DocumentType DocumentType);

public class ResetPw119DocumentEndpoint : Pw119EndpointBase<ResetPw119DocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetPw119DocumentEndpoint(
        ILogger<ResetPw119DocumentEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw119")
             .WithName("ResetPw119Document")
             .Accepts<ResetPw119DocumentRequest>("application/json"));
        this.Post("pw119/{Id:guid}/reset-document");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetPw119DocumentRequest req,
        CancellationToken ct)
    {
        // 1. Get full Pw119 with all relationships
        var pw119 = await this.GetPw119ById(Pw119Id.From(req.Id), ct);

        // 2. Get latest template from SuDocumentTemplates
        var templateFileId = await this.GetDocumentTemplateForResetAsync(req.DocumentType, ct);

        // 3. Create replacement DTO
        var replaceDto = await this.MapToReplaceDtoAsync(pw119, ct, false, creatorUserId: null);

        var parentDirectory =
            $"{DocumentTemplateGroups.Pw119}/{pw119.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

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

        pw119.AddDocumentHistory(req.DocumentType, newFileId.Value, isReplace: false, incrementMajor: true);

        this.dbContext.Pw119s.Update(pw119);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(
        Pw119DocumentType documentType,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateCode = documentType == Pw119DocumentType.Approval
            ? Pw119TemplateConstant.ApprovalRequest60
            : Pw119TemplateConstant.WinnerAnnounce60;

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.Pw119 &&
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

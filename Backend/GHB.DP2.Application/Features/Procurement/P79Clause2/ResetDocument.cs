namespace GHB.DP2.Application.Features.Procurement.P79Clause2;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.P79Clause2.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ResetP79Clause2DocumentRequest(
    Guid Id,
    P79Clause2DocumentType DocumentType);

public class ResetP79Clause2DocumentEndpoint : P79Clause2EndpointBase<ResetP79Clause2DocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetP79Clause2DocumentEndpoint(
        ILogger<ResetP79Clause2DocumentEndpoint> logger,
        Dp2DbContext dbContext,
        IOperationService operationService,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, operationService, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("P79Clause2")
             .WithName("ResetP79Clause2Document")
             .Accepts<ResetP79Clause2DocumentRequest>("application/json"));
        this.Post("p79Clause2/{Id:guid}/reset-document");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetP79Clause2DocumentRequest req,
        CancellationToken ct)
    {
        // 1. Get full P79Clause2 with all relationships
        var p79Clause2 = await this.GetP79Clause2ById(P79Clause2Id.From(req.Id), ct);

        // 2. Get latest template from SuDocumentTemplates
        var templateFileId = await this.GetDocumentTemplateForResetAsync(req.DocumentType, ct);

        // 3. Create replacement DTO
        var replaceDto = await this.MapToReplaceDto(p79Clause2, ct, false, creatorUserId: null);

        var parentDirectory =
            $"{DocumentTemplateGroups.P79Clause2}/{p79Clause2.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

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

        p79Clause2.AddDocumentHistory(req.DocumentType, newFileId.Value, isReplace: false, incrementMajor: true);

        this.dbContext.P79Clause2s.Update(p79Clause2);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(
        P79Clause2DocumentType documentType,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateCode = documentType == P79Clause2DocumentType.Approval
            ? P79Clause2TemplateConstant.ApprovalRequest60
            : P79Clause2TemplateConstant.WinnerAnnounce60;

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.P79Clause2 &&
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

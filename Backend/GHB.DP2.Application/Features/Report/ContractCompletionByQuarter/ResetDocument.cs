namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetContractCompletionByQuarterDocumentRequest(Guid Id);

public class ResetContractCompletionByQuarterDocumentEndpoint
    : ContractCompletionByQuarterEndpoint<ResetContractCompletionByQuarterDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetContractCompletionByQuarterDocumentEndpoint(
        Dp2DbContext dbContext,
        ILogger<ResetContractCompletionByQuarterDocumentEndpoint> logger,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("report/contract-completion-by-quarter/{Id:guid}/reset-document");
        this.Description(b => b
            .WithTags("Report/ContractCompletionByQuarter")
            .WithName("ResetContractCompletionByQuarterDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetContractCompletionByQuarterDocumentRequest req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.RpContractCompletionByQuarters
            .Include(r => r.DocumentHistories)
            .FirstOrDefaultAsync(
                r => r.Id == RpContractCompletionByQuarterId.From(req.Id),
                ct);

        if (entity == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        if (entity.DocumentHistories.Count == 0)
        {
            return TypedResults.BadRequest(DocumentErrorMessages.DocumentNotFound);
        }

        var newFileId = await this.GetDocumentTemplateForResetAsync(entity, ct);

        entity.AddDocumentHistory(
            RpContractCompletionByQuarterDocumentType.Completion,
            newFileId);

        this.dbContext.RpContractCompletionByQuarters.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(
        RpContractCompletionByQuarter entity,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateFileId = await this.GetDocumentTemplateByCriteria(entity.Quarter, ct);

        // Create replacement DTO
        var replaceDto = await this.MapToReplaceDtoAsync(
            entity,
            UserId.From(Guid.Empty),
            hasCreator: false,
            hasAcceptor: false,
            ct);

        var parentDirectory =
            $"{DocumentTemplateGroups.QuarterlyCompletion}/{entity.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        // Copy document WITH placeholder replacement
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

        return newFileId.Value;
    }
}

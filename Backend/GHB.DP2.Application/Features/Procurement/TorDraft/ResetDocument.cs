namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ResetDocumentRequest(
    Guid UserId,
    Guid ProcurementId,
    Guid TorDraftId,
    PpTorDraftDocumentType DocumentType);

public class ResetDocumentEndpoint : TorDraftEndpointBase<ResetDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetDocumentEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<ResetDocumentEndpoint> logger)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/TorDraft")
             .WithName("ResetTorDraftDocument")
             .Accepts<ResetDocumentRequest>("application/json"));
        this.Post("procurement/{ProcurementId:guid}/tordraft/{TorDraftId:guid}/reset-document");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetDocumentRequest req,
        CancellationToken ct)
    {
        // 1. Get full TorDraft with all relationships
        var torDraft = await this.GetTorDraftById(
            PpTorDraftId.From(req.TorDraftId),
            ProcurementId.From(req.ProcurementId),
            ct);

        // 2. Get Appoint data for committee information
        var appoint = await this.GetAppointById(
            ProcurementId.From(req.ProcurementId),
            ct);

        // 3. Get template file using the dedicated method
        var templateFileId = await this.GetDocumentTemplateForResetAsync(
            torDraft,
            req.DocumentType,
            ct);

        // 4. Create replacement DTO
        var replaceDto = await this.MapToReplaceDto(torDraft, torDraft.Status, appoint, ct);

        var documentService = this.Resolve<IDocumentService>();
        var parentDirectory =
            $"{DocumentTemplateGroups.Tor}/{torDraft.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        // 5. Copy document WITH placeholder replacement
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

        torDraft.AddDocumentHistory(req.DocumentType, newFileId!.Value, incrementMajor: true);

        this.dbContext.PpTorDrafts.Update(torDraft);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(
        PpTorDraft torDraft,
        PpTorDraftDocumentType documentType,
        CancellationToken ct)
    {
        FileId templateFileId;

        if (documentType == PpTorDraftDocumentType.Tor)
        {
            // TOR document - use base class method
            templateFileId = await this.GetDocumentTemplateByCode(
                torDraft.DocumentTemplate?.Code,
                ct);
        }
        else
        {
            // Approval document - use base class method with reset defaults (isChange=false, isCancel=false)
            templateFileId = await this.GetDocumentApprovalTemplateByCriteria(
                torDraft.Procurement.SupplyMethodCode,
                torDraft.Procurement.HasMd,
                isChange: false,
                isCancel: false,
                ct);
        }

        return templateFileId;
    }
}
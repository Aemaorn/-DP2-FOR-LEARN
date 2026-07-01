namespace GHB.DP2.Application.Features.Procurement.Appoint;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Appoint.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetAppointDocumentRequest(Guid Id);

public class ResetAppointDocumentEndpoint : AppointEndpointBase<ResetAppointDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetAppointDocumentEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<ResetAppointDocumentEndpoint> logger)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Appoint"));
        this.Post("appointments/{Id:guid}/reset-document");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetAppointDocumentRequest req,
        CancellationToken ct)
    {
        var appoint = await this.dbContext.PpAppoints
                                .Include(a => a.DocumentHistories)
                                .Include(a => a.Procurement)
                                .Include(a => a.TorDraftCommittees)
                                .ThenInclude(c => c.User)
                                .ThenInclude(u => u.Employee)
                                .ThenInclude(e => e.View)
                                .Include(c => c.TorDraftCommittees)
                                .ThenInclude(p => p.CommitteePositions)
                                .Include(a => a.TorDraftCommitteeDuties)
                                .Include(a => a.MedianPriceCommittees)
                                .ThenInclude(c => c.User)
                                .ThenInclude(u => u.Employee)
                                .ThenInclude(e => e.View)
                                .Include(c => c.MedianPriceCommittees)
                                .ThenInclude(p => p.CommitteePositions)
                                .Include(a => a.MedianPriceCommitteeDuties)
                                .Include(a => a.Acceptors)
                                .ThenInclude(a => a.User)
                                .ThenInclude(u => u.Employee)
                                .ThenInclude(e => e.View)
                                .AsSplitQuery()
                                .FirstOrDefaultAsync(a => a.Id == PpAppointId.From(req.Id), ct);

        if (appoint == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        // Get latest template from SuDocumentTemplates
        var templateFileId = await this.GetDocumentTemplateForResetAsync(appoint, ct);

        // Create replace DTO
        var replaceDto = await this.MapToReplaceDto(appoint, ct, isPreview: true);

        // Use CopyDocumentTemplateAsync with ReplaceOdtDocument
        var documentService = this.Resolve<IDocumentService>();
        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{DocumentTemplateGroups.Ap}/{appoint.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newFileId is null)
        {
            return TypedResults.BadRequest(DocumentErrorMessages.CopyDocumentFailed);
        }

        appoint.AddDocumentHistory(newFileId.Value);

        this.dbContext.PpAppoints.Update(appoint);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(
        PpAppoint appoint,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var isChange = appoint.IsChange;
        var isCancel = appoint.IsCancel;
        var budget = appoint.Procurement.Budget;

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.Ap &&
                dt.IsActive &&
                dt.SupplyMethodCode == appoint.Procurement.SupplyMethodCode &&
                (
                    (dt.IsChange == null && isChange == false) ||
                    dt.IsChange == isChange
                ) &&
                (
                    (dt.IsCancel == null && isCancel == false) ||
                    dt.IsCancel == isCancel
                ) &&
                dt.BudgetForDocument.Min <= budget &&
                (dt.BudgetForDocument.Max == null || budget <= dt.BudgetForDocument.Max),
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

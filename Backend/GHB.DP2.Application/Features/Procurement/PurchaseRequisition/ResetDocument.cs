namespace GHB.DP2.Application.Features.Procurement.PurchaseRequisition;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.PurchaseRequisition.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetPurchaseRequisitionDocumentRequest(Guid ProcurementId, Guid Id);

public class ResetPurchaseRequisitionDocumentEndpoint : PurchaseRequisitionEndpointBase<ResetPurchaseRequisitionDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetPurchaseRequisitionDocumentEndpoint(
        ILogger<ResetPurchaseRequisitionDocumentEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/jorPor04/{Id:guid}/reset-document");
        this.Description(b =>
            b.WithTags("Procurement/PurchaseRequisition")
             .WithName("ResetPurchaseRequisitionDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetPurchaseRequisitionDocumentRequest req,
        CancellationToken ct)
    {
        var purchaseRequisition = await this.dbContext.PpPurchaseRequisitions
                                            .Include(pr => pr.DocumentHistories)
                                            .Include(pr => pr.Procurement)
                                            .ThenInclude(p => p.Department)
                                            .Include(pr => pr.Procurement)
                                            .ThenInclude(p => p.SupplyMethod)
                                            .Include(pr => pr.Procurement)
                                            .ThenInclude(p => p.SupplyMethodType)
                                            .Include(pr => pr.Procurement)
                                            .ThenInclude(p => p.SupplyMethodSpecialType)
                                            .Include(pr => pr.Procurement)
                                            .ThenInclude(p => p.Plan)
                                            .Include(pr => pr.Budgets)
                                            .ThenInclude(b => b.PpPurchaseRequisitionBudgetDetails)
                                            .Include(pr => pr.Warranties)
                                            .Include(pr => pr.PaymentTerms)
                                            .Include(pr => pr.FineRates)
                                            .Include(pr => pr.Committees)
                                            .ThenInclude(c => c.User)
                                            .ThenInclude(u => u.Employee)
                                            .ThenInclude(e => e.View)
                                            .Include(pr => pr.Acceptors)
                                            .ThenInclude(a => a.User)
                                            .ThenInclude(u => u.Employee)
                                            .ThenInclude(e => e.View)
                                            .Include(pr => pr.Assignees)
                                            .ThenInclude(a => a.User)
                                            .ThenInclude(u => u.Employee)
                                            .ThenInclude(e => e.View)
                                            .Include(pr => pr.TechnicalSpecifications)
                                            .Include(pr => pr.TorDraft)
                                            .ThenInclude(td => td!.PpTorDraftObjects)
                                            .Include(pr => pr.TorDraft)
                                            .ThenInclude(td => td!.PpTorDraftTechnicalSpecifications)
                                            .Include(pr => pr.EvaluationCriteria)
                                            .Include(pr => pr.DeliveryPeriodType)
                                            .Include(pr => pr.DeliveryCondition)
                                            .AsSplitQuery()
                                            .FirstOrDefaultAsync(
                                                pr => pr.Id == PpPurchaseRequisitionId.From(req.Id) &&
                                                      pr.ProcurementId == ProcurementId.From(req.ProcurementId),
                                                ct);

        if (purchaseRequisition == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        // Get latest template from SuDocumentTemplates (using base class method)
        var templateFileId = await this.GetDocumentTemplateAsync(
            purchaseRequisition,
            purchaseRequisition.Procurement.SupplyMethodCode,
            ct);

        // Create replace DTO
        var replaceDto = await this.MapToReplaceDto(purchaseRequisition);

        // Use CopyDocumentTemplateAsync with ReplaceOdtDocument
        var documentService = this.Resolve<IDocumentService>();
        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{DocumentTemplateGroups.Jp04}/{purchaseRequisition.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newFileId is null)
        {
            return TypedResults.BadRequest(DocumentErrorMessages.CopyDocumentFailed);
        }

        purchaseRequisition.AddDocumentHistory(newFileId.Value, false);

        this.dbContext.PpPurchaseRequisitions.Update(purchaseRequisition);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}

namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Abstact;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetPrincipleApprovalRentalDocumentRequest(
    Guid ProcurementId,
    Guid Id,
    PPrincipleApprovalRentalDocumentType DocumentType);

public class ResetPrincipleApprovalRentalDocumentEndpoint : PrincipleApprovalRentalEndpointBase<ResetPrincipleApprovalRentalDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetPrincipleApprovalRentalDocumentEndpoint(
        ILogger<ResetPrincipleApprovalRentalDocumentEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/principle-approval-rental/{Id:guid}/reset-document");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApprovalRental")
                              .WithName("ResetPrincipleApprovalRentalDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetPrincipleApprovalRentalDocumentRequest req,
        CancellationToken ct)
    {
        // 1. Get full PrincipleApprovalRental with all relationships
        var entity = await this.dbContext.PPrincipleApprovalRentals
                                         .Include(p => p.DocumentHistories)
                                         .Include(p => p.Procurement)
                                         .ThenInclude(proc => proc.PrincipleApprovals)
                                         .Include(p => p.PerfSupportData)
                                         .Include(p => p.PerfSupportDataDetails)
                                         .Include(p => p.RoiLoanAndDepositSummaries)
                                         .Include(p => p.RoiPerfResults)
                                         .Include(p => p.Acceptors)
                                         .Include(p => p.Assignees)
                                         .Include(p => p.Budgets)
                                         .Include(p => p.RentalAnalyses)
                                         .Include(p => p.Entrepreneurs)
                                         .AsSplitQuery()
                                         .FirstOrDefaultAsync(
                                            p => p.Id == PPrincipleApprovalRentalId.From(req.Id) &&
                                                 p.ProcurementId == ProcurementId.From(req.ProcurementId),
                                            ct);

        if (entity == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        // 2. Get PrincipleApproval entity for replacement
        var principleApproval = entity.Procurement.PrincipleApprovals.FirstOrDefault();

        // 3. Get latest template from SuDocumentTemplates
        var templateFileId = await this.GetDocumentTemplateForResetAsync(entity, req.DocumentType);

        // 4. Create replacement DTO
        var replaceDto = await this.MapToReplaceDto(entity, principleApproval!, ct, creatorUserId: null, false);

        var parentDirectory =
            $"{DocumentTemplateGroups.PrincipleApprovalRental}/{entity.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

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

        entity.AddDocumentHistory(req.DocumentType, newFileId.Value, false, incrementMajor: true);

        this.dbContext.PPrincipleApprovalRentals.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}

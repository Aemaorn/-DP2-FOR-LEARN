namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum.Abstract;
using GHB.DP2.Application.Services.Document;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetPoAddendumDocumentRequest(
    Guid CamContractAmendmentId,
    Guid Id,
    string DocumentType);

public class ResetPoAddendumDocumentEndpoint : PoAddendumAbstractEndpoint<ResetPoAddendumDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetPoAddendumDocumentEndpoint(
        ILogger<ResetPoAddendumDocumentEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("contract-amendment/{CamContractAmendmentId:guid}/po-addendum/{Id:guid}/reset-document");
        this.Description(b =>
            b.WithTags("ContractAmendment/PoAddendum")
             .WithName("ResetPoAddendumDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetPoAddendumDocumentRequest req,
        CancellationToken ct)
    {
        var poAddendum = await this.dbContext.CamContractAmendmentPoAddendums
                                     .Include(p => p.DocumentHistories)
                                     .Include(p => p.CamContractAmendment)
                                     .ThenInclude(c => c.ContractDraftVendor)
                                     .ThenInclude(v => v.Vendor)
                                     .ThenInclude(v => v.VendorInfo)
                                     .Include(p => p.CamContractAmendment)
                                     .ThenInclude(c => c.ContractDraftVendor)
                                     .ThenInclude(v => v.PaymentTerms)
                                     .Include(p => p.CamContractAmendment)
                                     .ThenInclude(c => c.ContractDraftVendor)
                                     .ThenInclude(v => v.ContractDraft)
                                     .ThenInclude(d => d.Procurement)
                                     .Include(p => p.Vendor)
                                     .Include(p => p.PaymentTerms)
                                     .Include(p => p.Acceptors)
                                     .ThenInclude(a => a.User)
                                     .ThenInclude(u => u.Employee)
                                     .ThenInclude(e => e.View)
                                     .Include(p => p.Acceptors)
                                     .ThenInclude(a => a.CommitteePosition)
                                     .Include(p => p.Assignees)
                                     .ThenInclude(a => a.User)
                                     .ThenInclude(u => u.Employee)
                                     .ThenInclude(e => e.View)
                                     .FirstOrDefaultAsync(
                                         p => p.Id == CamContractAmendmentPoAddendumId.From(req.Id) &&
                                              p.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId),
                                         ct);

        if (poAddendum == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        if (!Enum.TryParse<CamContractAmendmentPoAddendumDocumentType>(req.DocumentType, out var documentType))
        {
            return TypedResults.BadRequest("ประเภทเอกสารไม่ถูกต้อง");
        }

        // 1. Get template from SuDocumentTemplates based on document type
        var templateFileId = await this.GetDocumentTemplateForResetAsync(documentType, ct);

        // 2. Get user for creator info
        var userId = UserId.From(poAddendum.AuditInfo.CreatedBy);
        var user = await this.dbContext.SuUsers
            .Include(u => u.Employee)
            .ThenInclude(e => e.View)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        // 3. Create replacement DTO
        var replaceDto = await this.GetGetPoAddendumResponseReplaceDtoAsync(
            poAddendum.CamContractAmendment,
            poAddendum,
            userId,
            hasCreator: user != null,
            hasAcceptor: false,
            ct);

        var templateGroup = documentType switch
        {
            CamContractAmendmentPoAddendumDocumentType.ContractAddendum => "ContractPoAddendum",
            CamContractAmendmentPoAddendumDocumentType.ContractAmendmentRequest => "ContractPoAddendumApproved",
            _ => throw new ArgumentOutOfRangeException(nameof(documentType)),
        };

        var parentDirectory =
            $"{templateGroup}/{poAddendum.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var documentService = this.Resolve<IDocumentService>();

        // 4. Copy document WITH placeholder replacement
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

        poAddendum.AddDocumentHistory(documentType, newFileId.Value, false, incrementMajor: true);

        this.dbContext.CamContractAmendmentPoAddendums.Update(poAddendum);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(
        CamContractAmendmentPoAddendumDocumentType documentType,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateGroup = documentType switch
        {
            CamContractAmendmentPoAddendumDocumentType.ContractAddendum => "ContractPoAddendum",
            CamContractAmendmentPoAddendumDocumentType.ContractAmendmentRequest => "ContractPoAddendumApproved",
            _ => throw new ArgumentOutOfRangeException(nameof(documentType)),
        };

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            d => d.Group == templateGroup,
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

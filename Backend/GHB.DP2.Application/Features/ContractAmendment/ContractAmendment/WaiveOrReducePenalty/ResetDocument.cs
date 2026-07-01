namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty.Abstract;
using GHB.DP2.Application.Services.Document;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetWaiveOrReducePenaltyDocumentRequest(
    Guid CamContractAmendmentId,
    Guid Id,
    string DocumentType);

public class ResetWaiveOrReducePenaltyDocumentEndpoint : WaiveOrReducePenaltyEndpointBase<ResetWaiveOrReducePenaltyDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetWaiveOrReducePenaltyDocumentEndpoint(
        ILogger<ResetWaiveOrReducePenaltyDocumentEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("contract-amendments/{CamContractAmendmentId:guid}/waive-or-reduce-penalty/{Id:guid}/reset-document");
        this.Description(b =>
            b.WithTags("ContractAmendment/WaiveOrReducePenalty")
             .WithName("ResetWaiveOrReducePenaltyDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetWaiveOrReducePenaltyDocumentRequest req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.CamContractAmendmentWaiveOrReducePenalties
                                 .Include(p => p.DocumentHistories)
                                 .Include(p => p.CamContractAmendment)
                                 .ThenInclude(c => c.ContractDraftVendor)
                                 .ThenInclude(v => v.Vendor)
                                 .ThenInclude(v => v.VendorInfo)
                                 .Include(p => p.CamContractAmendment)
                                 .ThenInclude(c => c.ContractDraftVendor)
                                 .ThenInclude(v => v.ContractDraft)
                                 .ThenInclude(d => d.Procurement)
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
                                     p => p.Id == WaiveOrReducePenaltyId.From(req.Id) &&
                                          p.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId),
                                     ct);

        if (entity == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        if (!Enum.TryParse<WaiveOrReducePenaltyDocumentType>(req.DocumentType, out var documentType))
        {
            return TypedResults.BadRequest("ประเภทเอกสารไม่ถูกต้อง");
        }

        // 1. Get template from SuDocumentTemplates based on document type
        var templateFileId = await this.GetDocumentTemplateForResetAsync(documentType, ct);

        // 2. Get user for creator info
        var user = await this.dbContext.SuUsers
            .Include(u => u.Employee)
            .ThenInclude(e => e.View)
            .FirstOrDefaultAsync(u => u.Id == UserId.From(entity.AuditInfo.CreatedBy), ct);

        // 3. Create replacement DTO
        var replaceDto = WaiveOrReducePenaltyReplaceDto.MapToResponse(
            entity,
            user,
            hasCreator: user != null,
            hasAcceptor: false);

        if (replaceDto is null)
        {
            this.ThrowError(
                "ไม่สามารถดึงข้อมูล ขออนุมัติงด/ลดค่าปรับ",
                StatusCodes.Status500InternalServerError);
        }

        var templateGroup = documentType switch
        {
            WaiveOrReducePenaltyDocumentType.WaiveOrReducePenalty => "ContractWaiveOrReducePenalty",
            WaiveOrReducePenaltyDocumentType.Approved => "ContractWaiveOrReducePenaltyApproved",
            _ => throw new ArgumentOutOfRangeException(nameof(documentType)),
        };

        var parentDirectory =
            $"{templateGroup}/{entity.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

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

        entity.AddDocumentHistory(documentType, newFileId.Value, false, incrementMajor: true);

        this.dbContext.CamContractAmendmentWaiveOrReducePenalties.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(
        WaiveOrReducePenaltyDocumentType documentType,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateGroup = documentType switch
        {
            WaiveOrReducePenaltyDocumentType.WaiveOrReducePenalty => "ContractWaiveOrReducePenalty",
            WaiveOrReducePenaltyDocumentType.Approved => "ContractWaiveOrReducePenaltyApproved",
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

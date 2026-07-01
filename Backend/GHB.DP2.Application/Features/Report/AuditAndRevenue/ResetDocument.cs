namespace GHB.DP2.Application.Features.Report.AuditAndRevenue;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Report.AuditAndRevenue.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetAuditAndRevenueDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    Guid Id,
    RpAuditAndRevenueDocumentType DocumentType);

public class ResetAuditAndRevenueDocumentEndpoint
    : AuditAndRevenueEndpoint<ResetAuditAndRevenueDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetAuditAndRevenueDocumentEndpoint(
        Dp2DbContext dbContext,
        ILogger<ResetAuditAndRevenueDocumentEndpoint> logger,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("report/audit-revenue/{Id:guid}/reset-document");
        this.Description(b => b
            .WithTags("Report/AuditAndRevenue")
            .WithName("ResetAuditAndRevenueDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetAuditAndRevenueDocumentRequest req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.RpAuditAndRevenues
            .Include(x => x.Details)
            .ThenInclude(d => d.CaContractDraftVendor)
            .ThenInclude(v => v.Vendor)
            .ThenInclude(v => v.VendorInfo)
            .Include(x => x.Details)
            .ThenInclude(d => d.CaContractDraftVendor)
            .ThenInclude(v => v.ContractType)
            .Include(x => x.Acceptors)
            .ThenInclude(a => a.User)
            .ThenInclude(u => u.Employee)
            .Include(x => x.AuditInfo)
            .Include(x => x.DocumentHistories)
            .FirstOrDefaultAsync(x => x.Id == RpAuditAndRevenueId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        if (entity.Status is not (RpAuditAndRevenueStatus.Draft or RpAuditAndRevenueStatus.Edit or RpAuditAndRevenueStatus.Rejected))
        {
            return TypedResults.BadRequest("ไม่สามารถรีเซ็ตเอกสารได้ เนื่องจากสถานะเอกสารไม่ใช่แบบร่าง, เรียกคืนแก้ไข หรือส่งกลับแก้ไข");
        }

        var documentHistoriesOfType = entity.DocumentHistories
            .Where(d => d.DocumentType == req.DocumentType)
            .ToList();

        if (documentHistoriesOfType.Count == 0)
        {
            return TypedResults.BadRequest(DocumentErrorMessages.DocumentNotFound);
        }

        var newFileId = await this.GetDocumentTemplateForResetAsync(entity, req.DocumentType, UserId.From(req.UserId), ct);

        entity.AddDocumentHistory(req.DocumentType, newFileId, isReplace: true, true);

        this.dbContext.RpAuditAndRevenues.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(
        RpAuditAndRevenue entity,
        RpAuditAndRevenueDocumentType documentType,
        UserId userId,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateCode = documentType switch
        {
            RpAuditAndRevenueDocumentType.AuditReport => "AuditReport",
            RpAuditAndRevenueDocumentType.AuditGeneralReport => "AuditGeneralReport",
            RpAuditAndRevenueDocumentType.RevenueReport => "RevenueReport",
            _ => throw new ArgumentOutOfRangeException(nameof(documentType)),
        };

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            c =>
                c.Group == DocumentTemplateGroups.AuditAndRevenueReport &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.ContractTemplateCode))
                 .GetString() == templateCode,
            ct);

        if (templateFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.TemplateNotFoundForReset,
                StatusCodes.Status404NotFound);
        }

        var replaceDto = await this.GetAuditAndRevenueReplaceDto(
            entity,
            userId,
            hasCreator: false,
            hasAcceptor: false,
            hasPublisher: false,
            ct);

        var parentDirectory =
            $"{DocumentTemplateGroups.AuditAndRevenueReport}/{entity.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt";

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId.Value,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: parentDirectory,
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.CopyDocumentFailed,
                StatusCodes.Status500InternalServerError);
        }

        return newFileId.Value;
    }
}

namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft;

using GHB.DP2.Application.Constants;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Abstract;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Services.Document;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetContractDraftDocumentRequest(
    Guid ProcurementId,
    Guid ContractDraftId,
    Guid VendorId,
    CaContractDraftVendorDocumentType DocumentType);

public class ResetContractDraftDocumentEndpoint : ContractDraftEndpointBase<ResetContractDraftDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetContractDraftDocumentEndpoint(
        ILogger<ResetContractDraftDocumentEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/contract-draft/{ContractDraftId:guid}/vendor/{VendorId:guid}/reset-document");
        this.Description(b => b
                              .WithTags(nameof(ContractDraft))
                              .WithName("ResetContractDraftDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetContractDraftDocumentRequest req,
        CancellationToken ct)
    {
        var vendor = await this.dbContext.CaContractDraftVendors
                               .Include(v => v.DocumentHistories)
                               .Include(v => v.ContractDraft)
                               .ThenInclude(c => c.Procurement)
                               .ThenInclude(p => p.SupplyMethodType)
                               .Include(v => v.ContractDraft)
                               .ThenInclude(c => c.Procurement)
                               .ThenInclude(p => p.SupplyMethodSpecialType)
                               .Include(v => v.Vendor)
                               .ThenInclude(v => v.VendorInfo)
                               .Include(v => v.PaymentTerms)
                               .FirstOrDefaultAsync(
                                   v => v.Id == ContractDraftVendorId.From(req.VendorId) &&
                                        v.ContractDraft.Id == ContractDraftId.From(req.ContractDraftId) &&
                                        v.ContractDraft.ProcurementId == ProcurementId.From(req.ProcurementId),
                                   ct);

        if (vendor == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        var documentHistoriesOfType = vendor.DocumentHistories
                                            .Where(d => d.DocumentType == req.DocumentType)
                                            .ToList();

        if (documentHistoriesOfType.Count == 0)
        {
            return TypedResults.BadRequest(DocumentErrorMessages.DocumentNotFound);
        }

        // 1. Get template document from SuDocumentTemplates instead of version 1.0
        var supplyMethodCode = vendor.ContractDraft.Procurement.SupplyMethodCode;
        var templateFileId = await this.GetDocumentTemplateForResetAsync(
            vendor,
            req.DocumentType,
            supplyMethodCode,
            ct);

        var documentService = this.Resolve<IDocumentService>();

        // 2. Get purchase order for replacement DTO
        var purchaseOrder = await this.dbContext
                                      .PPurchaseOrder
                                      .FirstOrDefaultAsync(
                                          v => v.ProcurementId == vendor.ContractDraft.ProcurementId,
                                          ct);

        // 3. Get user for creator info - user who reset becomes the creator
        var currentUserId = Guid.TryParse(this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var parsedUserId) ? parsedUserId : Guid.Empty;
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.View)
                             .Where(u => u.Id == UserId.From(currentUserId))
                             .FirstOrDefaultAsync(ct);

        var creator = user != null
            ? new CreatorResponse(
                "ผู้จัดทำ",
                user.Employee.View?.FullName,
                user.Employee.View?.FullPositionName)
            : null;

        var location = await this.GetLocationDtoByVendor(vendor, ct);

        // 4. Create replacement DTO with current user as creator
        var replaceDto = GetVendorReplaceDto.FromEntity(
            vendor,
            commandText: null,
            purchaseOrder,
            creator,
            hasCreator: creator != null,
            hasAcceptor: false,
            location);

        var parentDirectory =
            $"{DocumentTemplateGroups.CA}/{vendor.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}_{req.DocumentType}.odt";

        // 6. Copy template document WITH placeholder replacement
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

        vendor.AddDocumentHistory(req.DocumentType, newFileId.Value, false, incrementMajor: true);

        this.dbContext.CaContractDraftVendors.Update(vendor);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
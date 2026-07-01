namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;
using GHB.DP2.Application.Services.Document;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ResetContractTerminationDocumentRequest(
    Guid ContractVendorId,
    Guid Id);

public class ResetContractTerminationDocumentEndpoint
    : ContractTerminationEndpoint<ResetContractTerminationDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetContractTerminationDocumentEndpoint(
        Dp2DbContext dbContext,
        ILogger<ResetContractTerminationDocumentEndpoint> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("contract/{ContractVendorId:guid}/contract-termination/{Id:guid}/reset-document");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractTermination")
                              .WithName("ResetContractTerminationDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetContractTerminationDocumentRequest req,
        CancellationToken ct)
    {
        var contractVendor = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractVendorId), ct);

        var termination = contractVendor.CmContractTerminations
                                        .FirstOrDefault(t => t.Id == CmContractTerminationId.From(req.Id));

        if (termination == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        var documentHistories = termination.DocumentHistories
                                           .Where(d => d.DocumentType == CmContractTerminationDocumentType.ContractTermination)
                                           .ToList();

        if (documentHistories.Count == 0)
        {
            return TypedResults.BadRequest(DocumentErrorMessages.DocumentNotFound);
        }

        var newFileId = await this.GetDocumentTemplateForResetAsync(contractVendor, ct);

        termination.AddDocumentHistory(newFileId, incrementMajor: true);

        this.dbContext.CmContractTerminations.Update(termination);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(
        CaContractDraftVendor contractVendor,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var termination = contractVendor.CmContractTerminations
                                        .FirstOrDefault(t => t.DocumentHistories.Any(d => d.DocumentType == CmContractTerminationDocumentType.ContractTermination));

        if (termination == null)
        {
            this.ThrowError(
                DocumentErrorMessages.DocumentNotFound,
                StatusCodes.Status404NotFound);
        }

        // Get template document from SuDocumentTemplates
        var supplyMethodCode = contractVendor.ContractDraft.Procurement.SupplyMethodCode;
        var templateFileId = await documentService.GetDocumentTemplateAsync(
            c => c.Group == DocumentTemplateGroups.CMTermination &&
                 c.SupplyMethodCode == supplyMethodCode,
            ct);

        if (templateFileId == null)
        {
            this.ThrowError(
                "ไม่พบเอกสาร Template",
                StatusCodes.Status404NotFound);
        }

        // Get vendor info using base class method
        var suVendor = this.MapSuVendorByType(contractVendor.ContractInvitationVendors, contractVendor.ContractDraft.Procurement.Type);

        if (suVendor == null)
        {
            this.ThrowError(
                "ไม่พบข้อมูลผู้ขาย",
                StatusCodes.Status404NotFound);
        }

        // Get delivery info from the contractVendor
        var delivery = contractVendor.Delivery;

        // Create replacement DTO - user who reset becomes the creator
        var currentUserId = Guid.TryParse(this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var parsedUserId) ? parsedUserId : Guid.Empty;
        var replaceDto = await this.GetContractVendorTerminalMappingDtoAsync(
            contractVendor,
            termination,
            delivery!,
            suVendor,
            currentUserId,
            hasCreator: true,
            hasAcceptor: false,
            false,
            ct);

        var parentDirectory =
            $"{DocumentTemplateGroups.CMTermination}/{termination.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        // Copy document WITH placeholder replacement
        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId.Value,
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
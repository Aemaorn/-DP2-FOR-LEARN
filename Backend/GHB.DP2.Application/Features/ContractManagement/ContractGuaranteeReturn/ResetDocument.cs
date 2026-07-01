namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ResetContractGuaranteeReturnDocumentRequest(
    Guid ContractVendorId,
    Guid Id,
    CmContractGuaranteeReturnDocumentType DocumentType);

public class ResetContractGuaranteeReturnDocumentEndpoint
    : ContractGuaranteeReturnEndpoint<ResetContractGuaranteeReturnDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetContractGuaranteeReturnDocumentEndpoint(
        Dp2DbContext dbContext,
        ILogger<ResetContractGuaranteeReturnDocumentEndpoint> logger,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("contract/{ContractVendorId:guid}/contract-guarantee-return/{Id:guid}/reset-document");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractGuaranteeReturn")
                              .WithName("ResetContractGuaranteeReturnDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetContractGuaranteeReturnDocumentRequest req,
        CancellationToken ct)
    {
        var contractVendor = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractVendorId), ct);

        var guarantee = contractVendor.CmContractGuaranteeReturns
                                      .SingleOrDefault(g => g.Id == CmContractGuaranteeReturnId.From(req.Id));

        if (guarantee == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        var documentHistories = guarantee.DocumentHistories
                                         .Where(d => d.DocumentType == req.DocumentType)
                                         .ToList();

        if (documentHistories.Count == 0)
        {
            return TypedResults.BadRequest(DocumentErrorMessages.DocumentNotFound);
        }

        var supplyMethodCode = contractVendor.ContractDraft.Procurement.SupplyMethodCode;

        // 1. Get template (shared logic with SetDefaultDocumentTemplate)
        var templateFileId = await this.GetDocumentTemplateByTypeAsync(req.DocumentType, supplyMethodCode, ct);

        // 2. Get vendor info
        var invitationVendor = contractVendor.ContractInvitationVendors;
        var suVendor = contractVendor.ContractDraft.Procurement.Type is ProcurementType.Procurement
            ? invitationVendor?.PurchaseOrderApprovalContract?.Entrepreneur?.SuVendor
            : invitationVendor?.PurchaseOrderApprovalContract?.PrincipleApprovalRentalEntrepreneurs?.Vendor;

        if (suVendor == null)
        {
            this.ThrowError(DocumentErrorMessages.VendorNotFound, StatusCodes.Status404NotFound);
        }

        // 3. Create replacement DTO
        var currentUserId = Guid.TryParse(this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var parsedUserId) ? parsedUserId : Guid.Empty;
        var replaceDto = await this.GetContractGuaranteeReturnMappingDtoAsync(
            contractVendor,
            guarantee,
            suVendor,
            hasCreator: true,
            hasAcceptor: false,
            hasPublisher: false,
            hasCommittee: false,
            ct);

        // 4. Copy document with placeholder replacement
        var documentService = this.Resolve<IDocumentService>();
        var documentGroup = req.DocumentType switch
        {
            CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn => CMDocumentTemplatesConstant.CMGuaranteeReturn,
            CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule => CMDocumentTemplatesConstant.CMGuaranteeReturnResule,
            _ => CMDocumentTemplatesConstant.CMGuaranteeReturn,
        };
        var parentDirectory = $"{documentGroup}/{guarantee.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: parentDirectory,
            cancellationToken: ct);

        if (newFileId == null)
        {
            this.ThrowError(DocumentErrorMessages.CopyDocumentFailed, StatusCodes.Status500InternalServerError);
        }

        guarantee.AddDocumentHistory(req.DocumentType, newFileId.Value, false, incrementMajor: true);

        this.dbContext.CmContractGuaranteeReturns.Update(guarantee);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
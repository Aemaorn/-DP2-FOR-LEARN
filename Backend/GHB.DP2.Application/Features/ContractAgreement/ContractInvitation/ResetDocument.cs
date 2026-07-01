namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ResetContractInvitationDocumentRequest(
    Guid UserId,
    Guid ProcurementId,
    Guid ContractInvitationId,
    Guid VendorId);

public class ResetContractInvitationDocumentEndpoint
    : ContractInvitationEndpointBase<ResetContractInvitationDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetContractInvitationDocumentEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        IFileServiceClient fileServiceClient,
        ILogger<ResetContractInvitationDocumentEndpoint> logger)
        : base(dbContext, operationService, fileServiceClient, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAgreement/ContractInvitation")
             .WithName("ResetContractInvitationDocument")
             .Accepts<ResetContractInvitationDocumentRequest>("application/json"));
        this.Post("procurement/{ProcurementId:guid}/contract-invitation/{ContractInvitationId:guid}/vendor/{VendorId:guid}/reset-document");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetContractInvitationDocumentRequest req,
        CancellationToken ct)
    {
        var contractInvitation = await this.GetById(
            ContractInvitationId.From(req.ContractInvitationId),
            ProcurementId.From(req.ProcurementId),
            ct);

        var vendor = contractInvitation.Vendors
            .FirstOrDefault(v => v.Id == ContractInvitationVendorsId.From(req.VendorId));

        if (vendor is null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        var version1Document =
            await this.GetDocumentTemplateByCriteria(
                vendor.DocumentTemplateCode.Value,
                vendor.HasContractGuarantee,
                ct);

        var replaceDto =
            await this.MapToInvitationVendorReplace(
                req.ContractInvitationId,
                req.VendorId,
                req.ProcurementId,
                false,
                ct);

        var documentService = this.Resolve<IDocumentService>();

        var parentDirectory =
            $"{DocumentTemplateGroups.CAInv}/{vendor.Id}_ResetFrom_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            version1Document,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: parentDirectory,
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.CopyDocumentFailed,
                StatusCodes.Status500InternalServerError);
        }

        var lastHistory = vendor.DocumentHistories
            .Where(dh => dh.DocumentType == CaContractInvitationDocumentType.ContractInvitation)
            .OrderVersions()
            .FirstOrDefault();

        var incrementMajor =
            lastHistory?.StatusState != contractInvitation.Status;

        var newVersion =
            vendor.DocumentHistories
                .NextVersion(incrementMajor);

        var newHistory = CaContractInvitationVendorsDocumentHistory.Create(
            CaContractInvitationDocumentType.ContractInvitation,
            contractInvitation.Status,
            newVersion,
            newFileId!.Value);

        vendor.AddDocumentHistory(newHistory);

        this.dbContext.CaContractInvitations.Update(contractInvitation);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
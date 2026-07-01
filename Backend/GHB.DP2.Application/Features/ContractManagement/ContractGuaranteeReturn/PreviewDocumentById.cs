namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PreviewContractGuaranteeReturnDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    CmContractGuaranteeReturnDocumentType DocumentType,
    Guid ContractVendorId,
    Guid Id);

public class PreviewContractGuaranteeReturnDocumentEndpoint : ContractGuaranteeReturnEndpoint<PreviewContractGuaranteeReturnDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public PreviewContractGuaranteeReturnDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewContractGuaranteeReturnDocumentEndpoint> logger,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractGuaranteeReturn")
             .WithName("ContractGuaranteeReturnPreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("contract/{contractVendorId:guid}/contract-guarantee-return/{id:guid}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewContractGuaranteeReturnDocumentRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractVendorId), ct);

        var invitationVendor = entity.ContractInvitationVendors;
        var suVendor = entity.ContractDraft.Procurement.Type is ProcurementType.Procurement
            ? invitationVendor?.PurchaseOrderApprovalContract?.Entrepreneur?.SuVendor
            : invitationVendor?.PurchaseOrderApprovalContract?.PrincipleApprovalRentalEntrepreneurs?.Vendor;

        if (suVendor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้ประกอบการ");
        }

        var guarantee = entity.CmContractGuaranteeReturns.SingleOrDefault(t => t.Id.Value == req.Id);

        if (guarantee is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลคืนหลักประกันสัญญา {req.Id}");
        }

        var lastedContractGuaranteeReturnDocument = guarantee.DocumentHistories
                                                             .Where(d =>
                                                                 d.DocumentType == req.DocumentType)
                                                             .OrderByDescending(d => d.CreatedAt)
                                                             .FirstOrDefault();

        if (lastedContractGuaranteeReturnDocument is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลแผนที่ร่าง");
        }

        var file = await this.fileServiceClient.DownloadAsync(lastedContractGuaranteeReturnDocument.FileId, cancellationToken: ct);

        if (file == null)
        {
            return TypedResults.NotFound("ไม่พบไฟล์แผนที่ร่าง");
        }

        var response =
            await this.GetContractGuaranteeReturnMappingDtoAsync(entity, guarantee, suVendor, ct: ct);

        var fileContent = OdtDocumentExtensions.ReplaceOdtDocument(file.Contents, response);

        var odt = DocumentService.DetectContentType(fileContent);
        var unixTimeOneDay = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds();
        var fileResult = await this.fileServiceClient.UploadFileAsync(
            fileContent,
            contentType: odt,
            expirationUnixSeconds: unixTimeOneDay,
            cancellationToken: ct);

        return TypedResults.Ok(fileResult.Id.Value);
    }
}
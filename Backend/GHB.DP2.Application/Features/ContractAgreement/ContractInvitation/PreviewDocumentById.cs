namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PreviewContractInvitationDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid Id,
    Guid VendorId);

public class PreviewContractInvitationDocumentEndpoint : ContractInvitationEndpointBase<PreviewContractInvitationDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public PreviewContractInvitationDocumentEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        IFileServiceClient fileServiceClient,
        ILogger<UpsertAttachmentsEndpoint> logger)
        : base(dbContext, operationService, fileServiceClient, logger)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractInvitation")
             .WithName("ContractInvitationPreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("procurement/{ProcurementId:guid}/contractInvitation/{Id:guid}/vendor/{VendorId:guid}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewContractInvitationDocumentRequest req, CancellationToken ct)
    {
        var contractInvitationExisting =
            await this.GetById(
                ContractInvitationId.From(req.Id),
                ProcurementId.From(req.ProcurementId),
                ct);

        var vendor = this.GetVendorReplaceById(contractInvitationExisting, req.VendorId);

        var response = await this.MapToInvitationVendorReplace(
            req.Id,
            req.VendorId,
            req.ProcurementId,
            false,
            ct);

        var getLastedDraftDocumentHistory = vendor.DocumentHistories
                                                  .Where(d => d.StatusState == ContractInvitationStatus.Draft)
                                                  .OrderByDescending(d => d.Version)
                                                  .FirstOrDefault();

        if (getLastedDraftDocumentHistory is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลแผนที่ร่าง");
        }

        var file = await this.fileServiceClient.DownloadAsync(getLastedDraftDocumentHistory.FileId, cancellationToken: ct);

        if (file == null)
        {
            return TypedResults.NotFound("ไม่พบไฟล์แผนที่ร่าง");
        }

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
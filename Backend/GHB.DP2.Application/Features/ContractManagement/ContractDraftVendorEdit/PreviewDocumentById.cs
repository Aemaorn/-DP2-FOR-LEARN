namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PreviewContractDraftVendorEditDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    CaContractDraftEditVendorDocumentType DocumentType,
    Guid Id);

public class PreviewContractDraftVendorEditDocumentEndpoint
    : ContractDraftVendorEditEndpoint<PreviewContractDraftVendorEditDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public PreviewContractDraftVendorEditDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewContractDraftVendorEditDocumentEndpoint> logger)
        : base(logger, dbContext)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Get("contract/contract-draft-vendor-edit/{id:guid}/review-document");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractDraftVendorEdit")
                              .WithName("ContractDraftVendorEditPreviewDocument")
                              .Produces<Ok>()
                              .Produces<NotFound>());
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>>
        HandleRequestAsync(PreviewContractDraftVendorEditDocumentRequest req, CancellationToken ct)
    {
        var entity = await this.GetEditByIdAsync(ContractDraftVendorEditId.From(req.Id), ct);

        var lastedDocument = entity.DocumentHistories
                                   .Where(d => d.DocumentType == req.DocumentType)
                                   .OrderByDescending(d => d.CreatedAt)
                                   .FirstOrDefault();

        if (lastedDocument is null)
        {
            return TypedResults.NotFound("ไม่พบเอกสาร");
        }

        var file = await this.fileServiceClient.DownloadAsync(lastedDocument.FileId, cancellationToken: ct);

        if (file == null)
        {
            return TypedResults.NotFound("ไม่พบไฟล์เอกสาร");
        }

        var replaceDto = await this.GetMappingDtoAsync(entity, ct: ct);
        var fileContent = OdtDocumentExtensions.ReplaceOdtDocument(file.Contents, replaceDto);

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

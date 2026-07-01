namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using GHB.DP2.Domain.Common;

public record PreviewContractVendorTerminalDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ContractVendorId,
    Guid Id);

public class PreviewContractVendorTerminalDocumentEndpoint : ContractTerminationEndpoint<PreviewContractVendorTerminalDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public PreviewContractVendorTerminalDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewContractVendorTerminalDocumentEndpoint> logger)
        : base(logger, dbContext)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractVendorTerminal")
             .WithName("ContractVendorTerminalPreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("contract/{contractVendorId:guid}/contract-termination/{id:guid}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewContractVendorTerminalDocumentRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractVendorId), ct);

        var termination = entity.CmContractTerminations.FirstOrDefault(s => s.Id == CmContractTerminationId.From(req.Id));

        var delivery = entity.Delivery;

        var suVendor = this.MapSuVendorByType(entity.ContractInvitationVendors, entity.ContractDraft.Procurement.Type);

        if (suVendor is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการ");
        }

        if (termination is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลการบอกเลิกสัญญารหัส {req.Id}");
        }

        var getLastedDraftDocumentHistory = termination.DocumentHistories
                                                       .Where(d =>
                                                           d is
                                                           {
                                                               DocumentType: CmContractTerminationDocumentType.ContractTermination,
                                                               StatusState: CmContractTerminationStatus.Draft
                                                           })
                                                       .OrderVersions()
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

        var response =
            await this.GetContractVendorTerminalMappingDtoAsync(entity, termination, delivery, suVendor, req.UserId, ct: ct);

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
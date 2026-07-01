namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PreviewDeliveryAcceptancePeriodDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid DeliveryAcceptanceId,
    Guid Id);

public class PreviewDeliveryAcceptancePeriodDocumentEndpoint : DeliveryAcceptancePeriodEndpointBase<PreviewDeliveryAcceptancePeriodDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public PreviewDeliveryAcceptancePeriodDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewDeliveryAcceptancePeriodDocumentEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Get("delivery-acceptance/{DeliveryAcceptanceId:guid?}/period/{Id:guid}/review-document");
        this.Description(b => b
                              .WithTags("ContractManagement/DeliveryAcceptance/Period")
                              .WithName("DeliveryAcceptancePeriodPreviewDocument")
                              .Produces<Guid>()
                              .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewDeliveryAcceptancePeriodDocumentRequest req, CancellationToken ct)
    {
        var periodExisting =
            await this.GetById(
                CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId),
                CmDeliveryAcceptancePeriodId.From(req.Id),
                ct);

        if (periodExisting is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลการส่งมอบตรวจรับ รหัส {req.Id}");
        }

        var response =
            await this.MapToReplaceDtoAsync(
                periodExisting,
                ct,
                req.UserId,
                UserId.From(req.UserId));

        var getLastedDraftDocumentHistory =
            periodExisting.DocumentHistories
                          .Where(dh => dh.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
                          .Where(dh => dh.StatusState == CmDeliveryAcceptancePeriodStatus.Draft ||
                                       dh.StatusState == CmDeliveryAcceptancePeriodStatus.Edit ||
                                       dh.StatusState == CmDeliveryAcceptancePeriodStatus.Rejected ||
                                       dh.StatusState == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval ||
                                       dh.StatusState == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance ||
                                       dh.StatusState == CmDeliveryAcceptancePeriodStatus.Approved)
                          .OrderVersions()
                          .FirstOrDefault();

        if (getLastedDraftDocumentHistory is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลเอกสาร");
        }

        var file =
            await this.fileServiceClient
                      .DownloadAsync(
                          getLastedDraftDocumentHistory.FileId,
                          cancellationToken: ct);

        if (file == null)
        {
            return TypedResults.NotFound("ไม่พบไฟล์เอกสาร");
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
namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PreviewMedianPriceDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ProcurementId);

public class PreviewMedianPriceDocumentEndpoint : MedianPriceEndpointBase<PreviewMedianPriceDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public PreviewMedianPriceDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<PreviewMedianPriceDocumentEndpoint> logger)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/MedianPrice/{Id:guid}/review-document");
        this.Description(b => b
                              .WithTags(nameof(MedianPrice))
                              .WithName("MedianPricePreviewDocument")
                              .Produces<Guid>()
                              .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewMedianPriceDocumentRequest req, CancellationToken ct)
    {
        var procurement = await this.GetProcurementById(ProcurementId.From(req.ProcurementId), ct);

        var medianPrice = procurement.MedianPrices.FirstOrDefault(x => x.Id == MedianPriceId.From(req.Id));

        if (medianPrice is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลการกำหนดราคากลาง รหัส {req.Id}");
        }

        var response =
            await this.MapToReplaceDtoAsync(
                procurement,
                medianPrice,
                ct,
                UserId.From(req.UserId),
                medianPrice.Status == MedianPriceStatus.WaitingApproval || medianPrice.Status == MedianPriceStatus.Approved);

        var getLastedDraftDocumentHistory =
            medianPrice.DocumentHistories
                       .Where(d => d.StatusState == MedianPriceStatus.Draft)
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
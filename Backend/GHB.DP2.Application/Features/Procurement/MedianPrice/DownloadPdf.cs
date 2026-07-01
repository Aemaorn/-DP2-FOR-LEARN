namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.ChEditor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record DownloadMedianPricePdfRequest(
    Guid ProcurementId,
    Guid MedianPriceId);

public class DownloadMedianPricePdfEndpoint : MedianPriceEndpointBase<DownloadMedianPricePdfRequest, Results<FileContentHttpResult, NotFound<string>>>
{
    private readonly IFileServiceClient fileService;
    private readonly IChEditorService chEditorService;

    public DownloadMedianPricePdfEndpoint(
        ILogger<DownloadMedianPricePdfEndpoint> logger,
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileService,
        IChEditorService chEditorService)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.fileService = fileService;
        this.chEditorService = chEditorService;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/median-price/{MedianPriceId:guid}/download-pdf");
        this.Description(b => b
                              .WithTags(nameof(MedianPrice))
                              .WithName("DownloadMedianPricePdf")
                              .Produces<FileContentHttpResult>(StatusCodes.Status200OK, contentType: "application/pdf")
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<FileContentHttpResult, NotFound<string>>> HandleRequestAsync(DownloadMedianPricePdfRequest req, CancellationToken ct)
    {
        var medianPrice = await this.GetMedianPriceById(
            MedianPriceId.From(req.MedianPriceId),
            ProcurementId.From(req.ProcurementId),
            ct);

        var lastedDocument = medianPrice.LastedDocument;

        if (lastedDocument is null)
        {
            return TypedResults.NotFound("ไม่พบเอกสารราคากลาง");
        }

        var fileResult = await this.fileService.DownloadAsStreamAsync(
            lastedDocument.FileId,
            cancellationToken: ct);

        if (fileResult is null)
        {
            return TypedResults.NotFound("ไม่สามารถดาวน์โหลดไฟล์เอกสารได้");
        }

        await using (fileResult.Stream)
        {
            await using var pdfStream = await this.chEditorService.ConvertToPdf(fileResult.Stream, ct);
            using var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream, ct);
            var pdfBytes = memoryStream.ToArray();

            return TypedResults.File(
                pdfBytes,
                contentType: "application/pdf",
                fileDownloadName: "ราคากลาง.pdf");
        }
    }
}

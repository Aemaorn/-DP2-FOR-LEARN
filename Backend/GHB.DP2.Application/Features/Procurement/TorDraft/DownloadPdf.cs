namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.ChEditor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record DownloadTorDraftPdfRequest(
    Guid ProcurementId,
    Guid TorDraftId);

public class DownloadTorDraftPdfEndpoint : TorDraftEndpointBase<DownloadTorDraftPdfRequest, Results<FileContentHttpResult, NotFound<string>>>
{
    private readonly IFileServiceClient fileService;
    private readonly IChEditorService chEditorService;

    public DownloadTorDraftPdfEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<DownloadTorDraftPdfEndpoint> logger,
        IFileServiceClient fileService,
        IChEditorService chEditorService)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.fileService = fileService;
        this.chEditorService = chEditorService;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/tordraft/{TorDraftId:guid}/download-pdf");
        this.Description(b => b
                              .WithTags("Procurement/TorDraft")
                              .WithName("DownloadTorDraftPdf")
                              .Produces<FileContentHttpResult>(StatusCodes.Status200OK, contentType: "application/pdf")
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<FileContentHttpResult, NotFound<string>>> HandleRequestAsync(DownloadTorDraftPdfRequest req, CancellationToken ct)
    {
        var torDraft = await this.GetTorDraftById(
            PpTorDraftId.From(req.TorDraftId),
            ProcurementId.From(req.ProcurementId),
            ct);

        var lastedDocument = torDraft.LastedDocument(PpTorDraftDocumentType.Tor);

        if (lastedDocument is null)
        {
            return TypedResults.NotFound("ไม่พบเอกสาร TOR");
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
                fileDownloadName: "TOR.pdf");
        }
    }
}

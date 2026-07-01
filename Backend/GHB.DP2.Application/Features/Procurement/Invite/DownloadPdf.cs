namespace GHB.DP2.Application.Features.Procurement.Invite;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.ChEditor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record DownloadInvitePdfRequest(
    Guid ProcurementId,
    Guid InviteId,
    Guid EntrepreneurId);

public class DownloadInvitePdfEndpoint : InviteEndpointBase<DownloadInvitePdfRequest, Results<FileContentHttpResult, NotFound<string>>>
{
    private readonly IFileServiceClient fileService;
    private readonly IChEditorService chEditorService;

    public DownloadInvitePdfEndpoint(
        Dp2DbContext dbContext,
        ILogger<DownloadInvitePdfEndpoint> logger,
        IFileServiceClient fileService,
        IChEditorService chEditorService)
        : base(dbContext, logger)
    {
        this.fileService = fileService;
        this.chEditorService = chEditorService;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/invite/{InviteId:guid}/entrepreneur/{EntrepreneurId:guid}/download-pdf");
        this.Description(b => b
                              .WithTags("Procurement/Invite")
                              .WithName("DownloadInvitePdf")
                              .Produces<FileContentHttpResult>(StatusCodes.Status200OK, contentType: "application/pdf")
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<FileContentHttpResult, NotFound<string>>> HandleRequestAsync(DownloadInvitePdfRequest req, CancellationToken ct)
    {
        var invite = await this.GetPInviteById(
            PInviteId.From(req.InviteId),
            ProcurementId.From(req.ProcurementId),
            ct);

        var entrepreneur = invite.InvitedEntrepreneurs
            .FirstOrDefault(e => e.Id == PInvitedEntrepreneursId.From(req.EntrepreneurId));

        if (entrepreneur is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้ประกอบการ");
        }

        var lastedDocument = entrepreneur.LastedDocument;

        if (lastedDocument is null)
        {
            return TypedResults.NotFound("ไม่พบเอกสารหนังสือเชิญชวน");
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
                fileDownloadName: "หนังสือเชิญชวน.pdf");
        }
    }
}

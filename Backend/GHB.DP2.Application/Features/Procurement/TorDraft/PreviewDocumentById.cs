namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PreviewTorDraftDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ProcurementId,
    PpTorDraftDocumentType DocumentType);

public class PreviewTorDraftDocumentEndpoint : TorDraftEndpointBase<PreviewTorDraftDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly Dp2DbContext dbContext;

    public PreviewTorDraftDocumentEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewTorDraftDocumentEndpoint> logger)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/tordraft/{Id:guid}/review-document");
        this.Description(b => b
                              .WithTags("Procurement/TorDraft")
                              .WithName("TorDraftPreviewDocument")
                              .Produces<Guid>()
                              .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<
        Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(
        PreviewTorDraftDocumentRequest req,
        CancellationToken ct)
    {
        var entity = await this.GetTorDraftById(
            PpTorDraftId.From(req.Id),
            ProcurementId.From(req.ProcurementId),
            ct);

        var appoint = await this.GetAppointById(ProcurementId.From(req.ProcurementId), ct);

        var response =
            await this.MapToReplaceDto(entity, entity.Status, appoint, ct, true);

        var getLastedDraftDocumentHistory =
            entity.DocumentHistories
                  .Where(d => d.DocumentType == req.DocumentType)
                  .Where(d => d.StatusState == TorDraftStatus.Draft)
                  .OrderVersions()
                  .FirstOrDefault();

        // Fallback to LastedDraftDocument if specific DocumentType not found
        if (getLastedDraftDocumentHistory is null)
        {
            getLastedDraftDocumentHistory = entity.LastedDraftDocument(req.DocumentType);
        }

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
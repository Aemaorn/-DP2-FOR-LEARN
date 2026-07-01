namespace GHB.DP2.Application.Features.Procurement.Jp005;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp005.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PreviewJp005DocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ProcurementId,
    PJp005DocumentType DocumentType);

public class PreviewJp005DocumentEndpoint : Jp005EndpointBase<PreviewJp005DocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public PreviewJp005DocumentEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewJp005DocumentEndpoint> logger)
        : base(dbContext, operationService, commandTextService, logger)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/jp005/{Id:guid}/review-document");
        this.Description(b => b
                              .WithTags("Procurement/JorPor005")
                              .Produces<Guid>()
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewJp005DocumentRequest req, CancellationToken ct)
    {
        var procurement = await this.GetProcurementById(
            ProcurementId.From(req.ProcurementId),
            ct);

        var jp005Existing = this.GetJp005ById(procurement.Jp005, PJp005Id.From(req.Id), ProcurementId.From(req.ProcurementId));

        var jp005Response = await this.GetJp005MapToResponseMappingDtoAsync(jp005Existing, procurement, req.UserId, cancellationToken: ct);

        var getLastedDraftDocumentHistory = jp005Existing.DocumentHistories
                                                         .Where(d => d.DocumentType == req.DocumentType)
                                                         .Where(d => d.StatusState == PJp005Status.Draft)
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

        var fileContent = OdtDocumentExtensions.ReplaceOdtDocument(file.Contents, jp005Response);

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
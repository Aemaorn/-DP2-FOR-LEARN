namespace GHB.DP2.Application.Features.Procurement.Pw119;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Pw119.Abstract;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PreviewPw119DocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Pw119DocumentType DocumentType);

public class PreviewPw119DocumentEndpoint : Pw119EndpointBase<PreviewPw119DocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly IOperationService operationService;

    public PreviewPw119DocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService,
        ILogger<PreviewPw119DocumentEndpoint> logger)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Get("pw119/{Id:guid}/review-document");
        this.Description(b => b
                              .WithTags("Pw119")
                              .WithName("Pw119PreviewDocument")
                              .Produces<Guid>()
                              .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewPw119DocumentRequest req, CancellationToken ct)
    {
        var data = await this.GetPw119ById(Pw119Id.From(req.Id), ct);

        var response = await this.MapToReplaceDtoAsync(data, ct, false, UserId.From(req.UserId));

        var getLastedDraftDocumentHistory = data.LastedDraftDocument(req.DocumentType);

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
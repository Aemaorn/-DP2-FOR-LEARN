namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PreviewContractCompletionByQuarterDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    RpContractCompletionByQuarterDocumentType DocumentType);

public class PreviewContractCompletionByQuarterDocumentEndpoint : ContractCompletionByQuarterEndpoint<PreviewContractCompletionByQuarterDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public PreviewContractCompletionByQuarterDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewContractCompletionByQuarterDocumentEndpoint> logger)
        : base(logger, dbContext, fileServiceClient)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Get("report/contract-completion-by-quarter/{id:guid}/review-document");
        this.Description(b => b
                              .WithTags("Report/ContractCompletionByQuarter")
                              .WithName("ContractCompletionByQuarterPreviewDocument")
                              .Produces<Guid>()
                              .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>>
        HandleRequestAsync(PreviewContractCompletionByQuarterDocumentRequest req, CancellationToken ct)
    {
        var entity = await this.GetById(RpContractCompletionByQuarterId.From(req.Id), ct);

        if (entity == null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานการส่งมอบสัญญา");
        }

        var response =
            await this.MapToReplaceDtoAsync(
                entity,
                UserId.From(req.UserId),
                hasCreator: true,
                hasAcceptor: true,
                ct);

        var getLastedDraftDocumentHistory = entity.LastedDraftDocument(req.DocumentType);

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
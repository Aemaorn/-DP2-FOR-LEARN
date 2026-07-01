namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PreviewPrincipleApprovalDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ProcurementId);

public class PreviewPrincipleApprovalDocumentEndpoint : PrincipleApprovalEndpointBase<PreviewPrincipleApprovalDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public PreviewPrincipleApprovalDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewPrincipleApprovalDocumentEndpoint> logger)
        : base(logger, dbContext)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/principle-approval/{Id:guid}/review-document");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApproval")
                              .WithName("PrincipleApprovalPreviewDocument")
                              .Produces<Guid>()
                              .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(
        PreviewPrincipleApprovalDocumentRequest req,
        CancellationToken ct)
    {
        var approval =
            await this.GetPPrincipleApprovalById(
                PPrincipleApprovalId.From(req.Id),
                ProcurementId.From(req.ProcurementId),
                ct);

        var response =
            await this.MapToReplaceDtoAsync(
                approval,
                false,
                ct,
                UserId.From(req.UserId));

        var getLastedDraftDocumentHistory = approval.LastedDraftDocument;

        if (getLastedDraftDocumentHistory is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลเอกสารที่ร่าง");
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
namespace GHB.DP2.Application.Features.Procurement.Jp006;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PreviewJp006DocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ProcurementId,
    PurchaseOrderDocumentType DocumentType);

public class PreviewJp006DocumentEndpoint : Jp006EndpointBase<PreviewJp006DocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public PreviewJp006DocumentEndpoint(
        ILogger<AssigneePurchaseOrderEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, operationService, commandTextService, fileServiceClient, dbContext)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/jp006/{Id:guid}/review-document");
        this.Description(b => b
                              .WithTags(nameof(Jp006))
                              .WithName("Jp006PreviewDocument")
                              .Produces<Guid>()
                              .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewJp006DocumentRequest req, CancellationToken ct)
    {
        var jp006 = await this.GetByIdAsync(
            ProcurementId.From(req.ProcurementId),
            PurchaseOrderId.From(req.Id),
            ct: ct);

        if (jp006 is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการจัดซื้อจัดจ้างที่ระบุ");
        }

        var getLastedDraftDocumentHistory = jp006.DocumentHistories
                                                 .Where(d => d.DocumentType == req.DocumentType)
                                                 .Where(d => d.StatusState == PurchaseOrderStatus.Draft)
                                                 .OrderVersions()
                                                 .FirstOrDefault();

        if (getLastedDraftDocumentHistory is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลแผนที่ร่าง");
        }

        var response = await this.MapPJp006Replace(jp006, UserId.From(req.UserId), false, false, false, false, ct);

        if (response is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลแผนที่ร่าง");
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
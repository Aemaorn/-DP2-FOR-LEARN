namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty.Abstract;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record WaiveOrReducePenaltyDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    WaiveOrReducePenaltyDocumentType DocumentType,
    Guid CamContractAmendmentId,
    Guid? Id);

public class PreviewWaiveOrReducePenaltyDocumentEndpoint : WaiveOrReducePenaltyEndpointBase<WaiveOrReducePenaltyDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly Dp2DbContext dbContext;

    public PreviewWaiveOrReducePenaltyDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewWaiveOrReducePenaltyDocumentEndpoint> logger)
        : base(logger, dbContext)
    {
        this.fileServiceClient = fileServiceClient;
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/WaiveOrReducePenalty")
             .WithName("ContractAmendmentWaiveOrReducePenaltyPreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("contract-amendment/{CamContractAmendmentId:guid}/waive-or-reduce-penalty/{Id:guid?}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(WaiveOrReducePenaltyDocumentRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.CamContractAmendmentWaiveOrReducePenalties
                         .Where(w => w.Id == WaiveOrReducePenaltyId.From(req.Id.Value)
                                     && w.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId))
                         .Include(w => w.CamContractAmendment)
                         .ThenInclude(c => c.ContractDraftVendor)
                         .ThenInclude(v => v.DraftTermsConditions)
                         .Select(w => w)
                         .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลบันทึกต่อท้าย ที่ระบุ");
        }

        var lastedDraftDocument = entity.DocumentHistories
                                        .Where(d =>
                                            d.DocumentType == req.DocumentType &&
                                            d.StatusState == CamContractAmendmentWaiveOrReducePenaltyStatus.Draft)
                                        .OrderByDescending(d => d.CreatedAt)
                                        .FirstOrDefault();

        if (lastedDraftDocument is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลแผนที่ร่าง");
        }

        var file = await this.fileServiceClient.DownloadAsync(lastedDraftDocument.FileId, cancellationToken: ct);

        if (file == null)
        {
            return TypedResults.NotFound("ไม่พบไฟล์แผนที่ร่าง");
        }

        var response = WaiveOrReducePenaltyReplaceDto.MapToResponse(entity, null, false, false);

        if (response is null)
        {
            this.ThrowError(
                "ไม่สามารถดึงข้อมูล ขออนุมัติงด/ลดค่าปรับ",
                StatusCodes.Status500InternalServerError);
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
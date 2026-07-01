namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Abstract;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record AdjustContractDurationDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    ExtendChangeAcceptorDocumentType DocumentType,
    Guid ContractAmendmentId,
    Guid? Id);

public class PreviewAdjustContractDurationDocumentEndpoint : AdjustContractDurationEndpointBase<AdjustContractDurationDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly Dp2DbContext dbContext;

    public PreviewAdjustContractDurationDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewAdjustContractDurationDocumentEndpoint> logger)
        : base(logger, dbContext)
    {
        this.fileServiceClient = fileServiceClient;
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/AdjustContractDuration")
             .WithName("ContractAmendmentAdjustContractDurationPreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("contract-amendment/{ContractAmendmentId:guid}/adjust-contract-duration/{Id:guid?}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(AdjustContractDurationDocumentRequest req, CancellationToken ct)
    {
        var extendChange =
            await this.dbContext.CamContractAmendmentExtendChanges
                      .Include(c => c.PaymentTerms)
                      .Include(e => e.Acceptors)
                      .Include(c => c.Assignees)
                      .Include(c => c.CamContractAmendment)
                      .ThenInclude(c => c.ContractDraftVendor)
                      .ThenInclude(c => c.ContractDraft)
                      .Where(c =>
                          c.Id == ContractAmendmentExtendChangeId.From(req.Id!.Value) &&
                          c.CamContractAmendmentId == CamContractAmendmentId.From(req.ContractAmendmentId))
                      .FirstOrDefaultAsync(ct);

        if (extendChange is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลบันทึกต่อท้าย ที่ระบุ");
        }

        var lastedDraftDocument = extendChange.DocumentHistories
                                              .Where(d =>
                                                  d.DocumentType == req.DocumentType &&
                                                  d.StatusState == ContractAmendmentExtendChangeStatus.Draft)
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

        var response = await this.GetAdjustContractDurationReplaceMappingDto(extendChange, ct: ct);

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
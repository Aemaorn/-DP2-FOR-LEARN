namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum.Abstract;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record PoAddendumDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    CamContractAmendmentPoAddendumDocumentType DocumentType,
    CamContractAmendmentId CamContractAmendmentId,
    CamContractAmendmentPoAddendumId? Id);

public class PreviewPoAddendumDocumentEndpoint : PoAddendumAbstractEndpoint<PoAddendumDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly Dp2DbContext dbContext;

    public PreviewPoAddendumDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewPoAddendumDocumentEndpoint> logger)
        : base(logger, dbContext)
    {
        this.fileServiceClient = fileServiceClient;
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/PoAddendum")
             .WithName("ContractAmendmentPoAddendumPreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("contract-amendment/{CamContractAmendmentId:guid}/po-addendum/{Id:guid?}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PoAddendumDocumentRequest req, CancellationToken ct)
    {
        var cam = await this.dbContext.CamContractAmendments
                            .Include(c => c.ContractDraftVendor)
                            .ThenInclude(v => v.PaymentTerms)
                            .Include(c => c.ContractDraftVendor)
                            .ThenInclude(v => v.Vendor)
                            .ThenInclude(v => v.VendorInfo)
                            .Include(c => c.ContractDraftVendor)
                            .ThenInclude(cd => cd.ContractDraft)
                            .ThenInclude(p => p.Procurement)
                            .SingleOrDefaultAsync(c => c.Id == req.CamContractAmendmentId, ct);

        if (cam is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาหรือคู่ค้าสัญญาที่เกี่ยวข้อง");
        }

        var po = await this.dbContext.CamContractAmendmentPoAddendums
                           .Include(p => p.Vendor)
                           .Include(p => p.Acceptors)
                           .ThenInclude(a => a.User)
                           .Include(p => p.Acceptors)
                           .ThenInclude(a => a.CommitteePosition)
                           .Include(p => p.Assignees)
                           .ThenInclude(a => a.User)
                           .Include(p => p.PaymentTerms)
                           .Include(p => p.CamContractAmendment)
                           .Include(camContractAmendmentPoAddendum => camContractAmendmentPoAddendum.DocumentHistories)
                           .SingleOrDefaultAsync(p => p.Id == req.Id && p.CamContractAmendmentId == req.CamContractAmendmentId, ct);

        if (po is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลบันทึกต่อท้าย ที่ระบุ");
        }

        var lastedDraftDocument = po.DocumentHistories
                                                      .Where(d =>
                                                          d.DocumentType == req.DocumentType &&
                                                          d.StatusState == CamContractAmendmentPoAddendumStatus.Draft)
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

        var response = await this.GetGetPoAddendumResponseReplaceDtoAsync(cam, po, UserId.From(req.UserId), ct: ct);

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
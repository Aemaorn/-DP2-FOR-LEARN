namespace GHB.DP2.Application.Features.Report.AuditAndRevenue;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Report.AuditAndRevenue.Abstract;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;

public record AuditAndRevenueDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    RpAuditAndRevenueDocumentType DocumentType,
    Guid Id);

public class PreviewAuditAndRevenueDocumentEndpoint : AuditAndRevenueEndpoint<AuditAndRevenueDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly Dp2DbContext dbContext;

    public PreviewAuditAndRevenueDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewAuditAndRevenueDocumentEndpoint> logger,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.fileServiceClient = fileServiceClient;
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Report/AuditAndRevenue")
             .WithName("ReportAuditAndRevenuePreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("report/audit-revenue/{id:guid}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(AuditAndRevenueDocumentRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpAuditAndRevenues
                               .Include(x => x.Details)
                               .ThenInclude(d => d.CaContractDraftVendor)
                               .ThenInclude(caContractDraftVendor => caContractDraftVendor.Vendor)
                               .ThenInclude(vendor => vendor.VendorInfo)
                               .Include(rpAuditAndRevenue => rpAuditAndRevenue.Acceptors)
                               .ThenInclude(a => a.User)
                               .ThenInclude(u => u.Employee)
                               .Include(rpAuditAndRevenue => rpAuditAndRevenue.Details)
                               .ThenInclude(rpAuditAndRevenueDetail => rpAuditAndRevenueDetail.CaContractDraftVendor)
                               .ThenInclude(caContractDraftVendor => caContractDraftVendor.ContractType)
                               .Include(auditableEntity => auditableEntity.AuditInfo)
                               .Include(rpAuditAndRevenue => rpAuditAndRevenue.DocumentHistories)
                               .FirstOrDefaultAsync(x => x.Id == RpAuditAndRevenueId.From(req.Id), cancellationToken: ct)!;

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        var lastedDraftDocument = entity.DocumentHistories
                                        .Where(d =>
                                            d.DocumentType == req.DocumentType &&
                                            d.StatusState == RpAuditAndRevenueStatus.Draft)
                                        .OrderVersions()
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

        var response = await this.GetAuditAndRevenueReplaceDto(entity, UserId.From(req.UserId), ct: ct);

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
namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Abstact;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record PreviewPrincipleApprovalRentalDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ProcurementId,
    PPrincipleApprovalRentalDocumentType DocumentType);

public class PreviewPrincipleApprovalRentalDocumentEndpoint : PrincipleApprovalRentalEndpointBase<PreviewPrincipleApprovalRentalDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly Dp2DbContext dbContext;

    public PreviewPrincipleApprovalRentalDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewPrincipleApprovalRentalDocumentEndpoint> logger)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/principle-approval-rental/{Id:guid}/review-document");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApprovalRental")
                              .WithName("PrincipleApprovalRentalPreviewDocument")
                              .Produces<Guid>()
                              .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewPrincipleApprovalRentalDocumentRequest req, CancellationToken ct)
    {
        var principleApproval = await this.dbContext.PPrincipleApprovals
                                          .Where(w => w.ProcurementId == ProcurementId.From(req.ProcurementId))
                                          .SelectMany(s => s.PrincipleApprovalCommittees)
                                          .Where(w => w.GroupType == CommitteeGroupType.RentCommittee)
                                          .ToListAsync(ct);

        if (!principleApproval.Any())
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลคณะกรรมการจัดเช่าที่มีอำนาจในการจัดการขออนุมัติเช่า");
        }

        var approvalRental = await this.dbContext.PPrincipleApprovalRentals
                                       .Where(x => x.Id == PPrincipleApprovalRentalId.From(req.Id))
                                       .Include(p => p.Acceptors)
                                       .ThenInclude(a => a.CommitteePosition)
                                       .Include(p => p.Budgets)
                                       .ThenInclude(b => b.PrincipleApprovalRentalBudgetDetails)
                                       .Include(p => p.RentalAnalyses)
                                       .ThenInclude(r => r.PrincipleApprovalRentalRentalAnalysisDetails)
                                       .Include(p => p.Entrepreneurs)
                                       .ThenInclude(e => e.Vendor)
                                       .Include(p => p.PerfSupportData)
                                       .Include(p => p.PerfSupportDataDetails)
                                       .Include(p => p.RoiLoanAndDepositSummaries)
                                       .Include(p => p.RoiPerfResults)
                                       .Include(p => p.Assignees)
                                       .Include(p => p.DocumentHistories)
                                       .AsSplitQuery()
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(ct);

        if (approvalRental is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลอนุมัติเช่า ที่มีรหัส {req.Id}");
        }

        var procurement = await this.dbContext.Procurements
                                    .Where(p => p.Id == approvalRental.ProcurementId)
                                    .Include(p => p.Department)
                                    .Include(p => p.SupplyMethod)
                                    .Include(p => p.SupplyMethodType)
                                    .Include(p => p.SupplyMethodSpecialType)
                                    .Include(p => p.Plan)
                                    .Include(p => p.PrincipleApprovals)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(ct);

        if (procurement != null)
        {
            var procurementProperty = approvalRental.GetType().GetProperty("Procurement");
            procurementProperty?.SetValue(approvalRental, procurement);
        }

        var response =
            await this.MapToReplaceDto(
                approvalRental,
                procurement!.PrincipleApprovals.FirstOrDefault()!,
                ct,
                UserId.From(req.UserId),
                false);

        var getLastedDraftDocumentHistory =
            approvalRental.DocumentHistories
                          .Where(d => d.DocumentType == req.DocumentType)
                          .Where(d => d.StatusState == PPrincipleApprovalRentalStatus.Draft)
                          .OrderVersions()
                          .FirstOrDefault();

        if (getLastedDraftDocumentHistory is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลแผนที่ร่าง");
        }

        var file =
            await this.fileServiceClient.DownloadAsync(
                getLastedDraftDocumentHistory.FileId,
                cancellationToken: ct);

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
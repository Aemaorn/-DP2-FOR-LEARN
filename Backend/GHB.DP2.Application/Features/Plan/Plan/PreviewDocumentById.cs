namespace GHB.DP2.Application.Features.Plan.Plan;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Plan.Plan.Abstract;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Domain.Common;

public record PreviewPlanDocumentRequest(
    Guid Id,
    PlanDocumentType DocumentType);

public class PreviewPlanDocumentEndpoint : PlanEndpointBase<PreviewPlanDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public PreviewPlanDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewPlanDocumentEndpoint> logger)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Plan")
             .WithName("PlanPreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("plan/{Id:guid}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewPlanDocumentRequest req, CancellationToken ct)
    {
        var data = await this.dbContext
                             .Plans
                             .AsNoTracking()
                             .Include(plan => plan.Acceptors)
                             .ThenInclude(a => a.Delegatee)
                             .Include(plan => plan.Assignees)
                             .Include(plan => plan.Attachments)
                             .Include(auditableEntity => auditableEntity.AuditInfo)
                             .Include(plan => plan.DocumentHistories)
                             .SingleOrDefaultAsync(p => p.Id == PlanId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลแผน");
        }

        var response = await this.MapToPlanReplaceAsync(data, cancellationToken: ct);

        var getLastedDraftDocumentHistory = data.DocumentHistories
                                                .Where(d =>
                                                    d.DocumentType == req.DocumentType
                                                    && (d.StatusState == PlanStatus.WaitingApprovePlan || d.StatusState == PlanStatus.WaitingAssign)
                                                    && d.IsReplaced == false)
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
namespace GHB.DP2.Application.Features.Announcement.AnnouncementReport;

using Codehard.FileService.Client;
using Codehard.FileService.Client.Abstractions;
using global::GHB.DP2.Application.Extensions;
using global::GHB.DP2.Domain.Common;
using global::GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpdateAnnouncementReportRequest
{
    public Guid Id { get; init; }

    public string? Discretion { get; init; }

    public IFormFile? DocumentInfo { get; init; }
}

public class UpdateAnnouncementReportEndpoint : EndpointBase<UpdateAnnouncementReportRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public UpdateAnnouncementReportEndpoint(
        ILogger<UpdateAnnouncementReportEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Put("announcement-report/{Id:guid}");
        this.AllowFileUploads();
        this.Description(b => b
            .WithTags("AnnouncementReport")
            .WithName("UpdateAnnouncementReport")
            .AllowAnonymous()
            .Accepts<UpdateAnnouncementReportRequest>("multipart/form-data")
            .Produces(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        UpdateAnnouncementReportRequest req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.AnnouncementReports
            .Where(x => !x.IsDeleted && x.Id == Domain.AnnouncementInfo.AnnouncementReportId.From(req.Id))
            .SingleOrDefaultAsync(ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        Guid? documentId = entity.DocumentId?.Value;
        string? documentName = entity.DocumentName;
        string? documentUrl = entity.DocumentUrl;
        if (req.DocumentInfo is { Length: > 0 })
        {
            var contents = await req.DocumentInfo.ReadFileAsync(ct);
            var type = new ContentType(Path.GetExtension(req.DocumentInfo.FileName).TrimStart('.'), req.DocumentInfo.ContentType);
            var uploadResult = await this.fileServiceClient.UploadFileAsync(
                contents,
                directoryPath: req.DocumentInfo.FileName,
                contentType: type,
                cancellationToken: CancellationToken.None);
            documentId = uploadResult.Id.Value;
            documentName = req.DocumentInfo.FileName;
            documentUrl = null;
        }

        entity.Update(
            req.Discretion,
            documentId,
            documentName,
            documentUrl);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Update,
            "แก้ไขข้อมูลรายงานประกาศ",
            entity.IsActive?.ToString() ?? string.Empty));

        this.dbContext.AnnouncementReports.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}

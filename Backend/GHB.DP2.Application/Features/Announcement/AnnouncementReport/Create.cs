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

public class CreateAnnouncementReportRequest
{
    public int? Year { get; init; }

    public string? Discretion { get; init; }

    public string? AnnouncementReportTypeCode { get; init; }

    public IFormFile? DocumentInfo { get; init; }
}

public class CreateAnnouncementReportEndpoint : EndpointBase<CreateAnnouncementReportRequest, Results<Ok<Guid>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public CreateAnnouncementReportEndpoint(
        ILogger<CreateAnnouncementReportEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Post("announcement-report");
        this.AllowFileUploads();
        this.Description(b => b
            .WithTags("AnnouncementReport")
            .WithName("CreateAnnouncementReport")
            .AllowAnonymous()
            .Accepts<CreateAnnouncementReportRequest>("multipart/form-data")
            .Produces<Guid>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status409Conflict));
    }

    protected override async ValueTask<Results<Ok<Guid>, Conflict<string>>> HandleRequestAsync(
        CreateAnnouncementReportRequest req,
        CancellationToken ct)
    {
        var isDuplicate = await this.dbContext.AnnouncementReports
            .AnyAsync(
                x => !x.IsDeleted
                    && (string?)x.AnnouncementReportTypeCode == req.AnnouncementReportTypeCode
                    && x.Year == req.Year,
                ct);

        if (isDuplicate)
        {
            this.ThrowError(
                "ข้อมูลประเภทรายงานและปีซ้ำ",
                StatusCodes.Status400BadRequest);
        }

        Guid? documentId = null;
        string? documentName = null;
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
        }

        var entity = Domain.AnnouncementInfo.AnnouncementReport.Create(
            req.Year,
            req.Discretion,
            req.AnnouncementReportTypeCode,
            documentId,
            documentName);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            "สร้างข้อมูลรายงานประกาศ",
            entity.IsActive?.ToString() ?? string.Empty));

        this.dbContext.AnnouncementReports.Add(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok<Guid>(entity.Id.Value);
    }
}

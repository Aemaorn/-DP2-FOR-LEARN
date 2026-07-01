namespace GHB.DP2.Application.Features.Announcement.AnnouncementInfo;

using Codehard.FileService.Client;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class CreateAnnouncementInfoRequest
{
    public string AnnouncementTitle { get; init; } = string.Empty;

    public string? AnnouncementName { get; init; }

    public DateTimeOffset AnnouncementDate { get; init; }

    public decimal BudgetAmount { get; init; }

    public string AnnouncementCategoryCode { get; init; } = string.Empty;

    public string? SupplyMethodCode { get; init; }

    public int? BudgetYear { get; init; }

    public string? Remark { get; init; }

    public string? Description { get; init; }

    public DateTimeOffset? ExpectedDate { get; init; }

    public decimal? ReferencePrice { get; init; }

    public DateTimeOffset? StartDate { get; init; }

    public DateTimeOffset? EndDate { get; init; }

    public IFormFile? DocumentInfo { get; init; }
}

public class CreateAnnouncementInfoEndpoint : EndpointBase<CreateAnnouncementInfoRequest, Ok<Guid>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public CreateAnnouncementInfoEndpoint(
        ILogger<CreateAnnouncementInfoEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Post("announcement-info");
        this.AllowFileUploads();
        this.Description(b => b
            .WithTags("AnnouncementInfo")
            .WithName("CreateAnnouncementInfo")
            .AllowAnonymous()
            .Accepts<CreateAnnouncementInfoRequest>("multipart/form-data")
            .Produces<Guid>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<Guid>> HandleRequestAsync(
        CreateAnnouncementInfoRequest req,
        CancellationToken ct)
    {
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

        var entity = Domain.AnnouncementInfo.AnnouncementInfo.Create(
            req.AnnouncementTitle,
            req.AnnouncementName,
            req.AnnouncementDate,
            req.BudgetAmount,
            req.AnnouncementCategoryCode,
            req.SupplyMethodCode,
            req.BudgetYear,
            req.Remark,
            req.Description,
            req.ExpectedDate,
            req.ReferencePrice,
            req.StartDate,
            req.EndDate,
            documentId,
            documentName);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            "สร้างข้อมูลประกาศ",
            entity.Status.ToString()));

        this.dbContext.AnnouncementInfos.Add(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(entity.Id.Value);
    }
}

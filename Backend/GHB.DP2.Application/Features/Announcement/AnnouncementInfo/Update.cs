namespace GHB.DP2.Application.Features.Announcement.AnnouncementInfo;

using Codehard.FileService.Client;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpdateAnnouncementInfoRequest
{
    public Guid Id { get; init; }

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

public class UpdateAnnouncementInfoEndpoint : EndpointBase<UpdateAnnouncementInfoRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public UpdateAnnouncementInfoEndpoint(
        ILogger<UpdateAnnouncementInfoEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Put("announcement-info/{Id:guid}");
        this.AllowFileUploads();
        this.Description(b => b
            .WithTags("AnnouncementInfo")
            .WithName("UpdateAnnouncementInfo")
            .AllowAnonymous()
            .Accepts<UpdateAnnouncementInfoRequest>("multipart/form-data")
            .Produces(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        UpdateAnnouncementInfoRequest req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.AnnouncementInfos
            .Where(x => !x.IsDeleted && x.Id == Domain.AnnouncementInfo.AnnouncementInfoId.From(req.Id))
            .SingleOrDefaultAsync(ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        Guid? documentId = entity.DocumentId?.Value;
        string? documentName = entity.DocumentName;
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

        entity.Update(
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
            ActivityLogActionTypeConstant.Update,
            "แก้ไขข้อมูลประกาศ",
            entity.Status.ToString()));

        this.dbContext.AnnouncementInfos.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}

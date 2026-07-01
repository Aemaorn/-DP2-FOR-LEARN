namespace GHB.DP2.Application.Features.Announcement.AnnouncementSorKorRor;

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

public class UpdateAnnouncementSorKorRorRequest
{
    public Guid Id { get; init; }

    public int? Year { get; init; }

    public int? Month { get; init; }

    public decimal? Amount { get; init; }

    public string? DepartmentTypeCode { get; init; }

    public IFormFile? DocumentInfo { get; init; }
}

public class UpdateAnnouncementSorKorRorEndpoint : EndpointBase<UpdateAnnouncementSorKorRorRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public UpdateAnnouncementSorKorRorEndpoint(
        ILogger<UpdateAnnouncementSorKorRorEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Put("announcement-sor-kor-ror/{Id:guid}");
        this.AllowFileUploads();
        this.Description(b => b
            .WithTags("AnnouncementSorKorRor")
            .WithName("UpdateAnnouncementSorKorRor")
            .AllowAnonymous()
            .Accepts<UpdateAnnouncementSorKorRorRequest>("multipart/form-data")
            .Produces(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        UpdateAnnouncementSorKorRorRequest req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.AnnouncementSorKorRors
            .Where(x => !x.IsDeleted && x.Id == Domain.AnnouncementInfo.AnnouncementSorKorRorId.From(req.Id))
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
            req.Year,
            req.Month,
            req.Amount,
            req.DepartmentTypeCode,
            documentId,
            documentName,
            documentUrl);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Update,
            "แก้ไขข้อมูลประกาศ สอ.กร.",
            entity.IsActive?.ToString() ?? string.Empty));

        this.dbContext.AnnouncementSorKorRors.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}

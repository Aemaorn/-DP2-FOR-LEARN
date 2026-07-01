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

public class CreateAnnouncementSorKorRorRequest
{
    public int? Year { get; init; }

    public int? Month { get; init; }

    public decimal? Amount { get; init; }

    public string? DepartmentTypeCode { get; init; }

    public IFormFile? DocumentInfo { get; init; }
}

public class CreateAnnouncementSorKorRorEndpoint : EndpointBase<CreateAnnouncementSorKorRorRequest, Results<Ok<Guid>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public CreateAnnouncementSorKorRorEndpoint(
        ILogger<CreateAnnouncementSorKorRorEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Post("announcement-sor-kor-ror");
        this.AllowFileUploads();
        this.Description(b => b
            .WithTags("AnnouncementSorKorRor")
            .WithName("CreateAnnouncementSorKorRor")
            .AllowAnonymous()
            .Accepts<CreateAnnouncementSorKorRorRequest>("multipart/form-data")
            .Produces<Guid>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status409Conflict));
    }

    protected override async ValueTask<Results<Ok<Guid>, Conflict<string>>> HandleRequestAsync(
        CreateAnnouncementSorKorRorRequest req,
        CancellationToken ct)
    {
        var isDuplicate = await this.dbContext.AnnouncementSorKorRors
            .AnyAsync(
                x => !x.IsDeleted
                    && x.Year == req.Year
                    && x.Month == req.Month
                    && (string?)x.DepartmentTypeCode == req.DepartmentTypeCode,
                ct);

        if (isDuplicate)
        {
            this.ThrowError(
                "ข้อมูลปี เดือน และประเภทหน่วยงานซ้ำ",
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

        var entity = Domain.AnnouncementInfo.AnnouncementSorKorRor.Create(
            req.Year,
            req.Month,
            req.Amount,
            req.DepartmentTypeCode,
            documentId,
            documentName);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            "สร้างข้อมูลประกาศ สอ.กร.",
            entity.IsActive?.ToString() ?? string.Empty));

        this.dbContext.AnnouncementSorKorRors.Add(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok<Guid>(entity.Id.Value);
    }
}

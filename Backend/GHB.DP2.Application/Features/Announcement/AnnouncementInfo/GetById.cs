namespace GHB.DP2.Application.Features.Announcement.AnnouncementInfo;

using global::GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetAnnouncementInfoByIdRequest(Guid Id);

public record GetAnnouncementInfoByIdResponse(
    Guid Id,
    int? OldId,
    string? AnnouncementName,
    string? AnnouncementTitle,
    string? AnnouncementCategoryCode,
    string? AnnouncementCategory,
    DateTimeOffset? AnnouncementDate,
    decimal? BudgetAmount,
    int? BudgetYear,
    DateTimeOffset? ExpectedDate,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    decimal? ReferencePrice,
    string? Annotation,
    string? Remark,
    string? Description,
    Guid? DocumentId,
    string? DocumentName,
    string? DocumentUrl,
    string? SupplyMethodCode,
    string? SupplyMethod,
    bool? IsDp,
    bool? IsActive);

public class GetAnnouncementInfoByIdEndpoint : EndpointBase<GetAnnouncementInfoByIdRequest, Results<Ok<GetAnnouncementInfoByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetAnnouncementInfoByIdEndpoint(
        ILogger<GetAnnouncementInfoByIdEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("announcement-info/detail/{Id:guid}");
        this.Description(b => b
            .WithTags("AnnouncementInfo")
            .WithName("GetAnnouncementInfoById")
            .AllowAnonymous()
            .Produces<GetAnnouncementInfoByIdResponse>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<GetAnnouncementInfoByIdResponse>, NotFound<string>>> HandleRequestAsync(
        GetAnnouncementInfoByIdRequest req,
        CancellationToken ct)
    {
        var data = await this.dbContext.AnnouncementInfos
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Id == Domain.AnnouncementInfo.AnnouncementInfoId.From(req.Id))
            .Select(x => new GetAnnouncementInfoByIdResponse(
                x.Id.Value,
                x.OldId,
                x.AnnouncementName,
                x.AnnouncementTitle,
                x.AnnouncementCategoryCode != null ? (string)x.AnnouncementCategoryCode! : null,
                x.AnnouncementCategory != null ? x.AnnouncementCategory.Label : null,
                x.AnnouncementDate,
                x.BudgetAmount,
                x.BudgetYear,
                x.ExpectedDate,
                x.StartDate,
                x.EndDate,
                x.ReferencePrice,
                x.Remark,
                x.Remark,
                x.Description,
                x.DocumentId.HasValue ? x.DocumentId.Value.Value : (Guid?)null,
                x.DocumentName,
                x.DocumentUrl,
                x.SupplyMethodCode != null ? (string)x.SupplyMethodCode! : null,
                x.SupplyMethod != null ? x.SupplyMethod.Label : null,
                x.IsDp,
                x.IsActive))
            .FirstOrDefaultAsync(ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        return TypedResults.Ok(data);
    }
}

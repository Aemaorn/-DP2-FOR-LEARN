namespace GHB.DP2.Application.Features.Announcement.AnnouncementSorKorRor;

using global::GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetAnnouncementSorKorRorByIdRequest(Guid Id);

public record GetAnnouncementSorKorRorByIdResponse(
    Guid Id,
    int? OldId,
    int? Year,
    int? Month,
    decimal? Amount,
    string? DepartmentTypeCode,
    string? DepartmentType,
    bool? IsDp,
    bool? IsActive,
    Guid? DocumentId,
    string? DocumentName,
    string? DocumentUrl);

public class GetAnnouncementSorKorRorByIdEndpoint : EndpointBase<GetAnnouncementSorKorRorByIdRequest, Results<Ok<GetAnnouncementSorKorRorByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetAnnouncementSorKorRorByIdEndpoint(
        ILogger<GetAnnouncementSorKorRorByIdEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("announcement-sor-kor-ror/detail/{Id:guid}");
        this.Description(b => b
            .WithTags("AnnouncementSorKorRor")
            .WithName("GetAnnouncementSorKorRorById")
            .AllowAnonymous()
            .Produces<GetAnnouncementSorKorRorByIdResponse>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<GetAnnouncementSorKorRorByIdResponse>, NotFound<string>>> HandleRequestAsync(
        GetAnnouncementSorKorRorByIdRequest req,
        CancellationToken ct)
    {
        var data = await this.dbContext.AnnouncementSorKorRors
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Id == Domain.AnnouncementInfo.AnnouncementSorKorRorId.From(req.Id))
            .Select(x => new GetAnnouncementSorKorRorByIdResponse(
                x.Id.Value,
                x.OldId,
                x.Year,
                x.Month,
                x.Amount,
                x.DepartmentTypeCode != null ? (string)x.DepartmentTypeCode! : null,
                x.DepartmentType != null ? x.DepartmentType.Label : null,
                x.IsDp,
                x.IsActive,
                x.DocumentId.HasValue ? x.DocumentId.Value.Value : (Guid?)null,
                x.DocumentName,
                x.DocumentUrl))
            .FirstOrDefaultAsync(ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        return TypedResults.Ok(data);
    }
}

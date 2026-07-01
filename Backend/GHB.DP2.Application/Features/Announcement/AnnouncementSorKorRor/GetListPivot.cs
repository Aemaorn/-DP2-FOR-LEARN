namespace GHB.DP2.Application.Features.Announcement.AnnouncementSorKorRor;

using Codehard.Infrastructure.EntityFramework;
using global::GHB.DP2.Application.Common;
using global::GHB.DP2.Application.Extensions;
using global::GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetAnnouncementSorKorRorListPivotRequest(
    int PageNumber,
    int PageSize,
    int? Year,
    int? Month,
    string? DepartmentTypeCode);

public record GetAnnouncementSorKorRorListPivotResponse(
    Guid Id,
    int? Year,
    int? Month,
    decimal? Amount,
    string? DepartmentTypeCode,
    string? DepartmentType,
    bool? IsActive,
    Guid? DocumentId,
    string? DocumentName,
    string? DocumentUrl);

public class GetAnnouncementSorKorRorListPivotEndpoint : EndpointBase<GetAnnouncementSorKorRorListPivotRequest, Ok<PaginatedQueryResult<GetAnnouncementSorKorRorListPivotResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetAnnouncementSorKorRorListPivotEndpoint(
        ILogger<GetAnnouncementSorKorRorListPivotEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("announcement-sor-kor-ror");
        this.Description(b => b
            .WithTags("AnnouncementSorKorRor")
            .WithName("GetAnnouncementSorKorRorListPivot")
            .AllowAnonymous()
            .Produces<PaginatedQueryResult<GetAnnouncementSorKorRorListPivotResponse>>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetAnnouncementSorKorRorListPivotResponse>>> HandleRequestAsync(
        GetAnnouncementSorKorRorListPivotRequest req,
        CancellationToken ct)
    {
        var query = this.dbContext.AnnouncementSorKorRors
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .WhereIfTrue(req.Year.HasValue, x => x.Year == req.Year)
            .WhereIfTrue(req.Month.HasValue, x => x.Month == req.Month)
            .WhereIfTrue(
                !string.IsNullOrWhiteSpace(req.DepartmentTypeCode),
                x => (string)x.DepartmentTypeCode! == req.DepartmentTypeCode)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .Select(x => new GetAnnouncementSorKorRorListPivotResponse(
                x.Id.Value,
                x.Year,
                x.Month,
                x.Amount,
                x.DepartmentTypeCode != null ? (string)x.DepartmentTypeCode! : null,
                x.DepartmentType != null ? x.DepartmentType.Label : null,
                x.IsActive,
                x.DocumentId.HasValue ? x.DocumentId.Value.Value : (Guid?)null,
                x.DocumentName,
                x.DocumentUrl));

        var paginated = await PaginatedList<GetAnnouncementSorKorRorListPivotResponse>
            .CreateAsync(query, req.PageNumber, req.PageSize, ct);

        return TypedResults.Ok(paginated.ToResult());
    }
}

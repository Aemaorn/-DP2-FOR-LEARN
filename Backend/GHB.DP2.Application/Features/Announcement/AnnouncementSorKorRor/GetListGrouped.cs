namespace GHB.DP2.Application.Features.Announcement.AnnouncementSorKorRor;

using global::GHB.DP2.Application.Common;
using global::GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetAnnouncementSorKorRorListGroupedRequest(
    int PageNumber,
    int PageSize,
    int? Year,
    string? DepartmentTypeCode,
    string? Keyword);

public record GetAnnouncementSorKorRorListGroupedResponse(
    int? Year,
    string? DepartmentTypeCode,
    string? DepartmentType,
    decimal? Month1, Guid? FileId1, string? DocumentUrl1,
    decimal? Month2, Guid? FileId2, string? DocumentUrl2,
    decimal? Month3, Guid? FileId3, string? DocumentUrl3,
    decimal? Month4, Guid? FileId4, string? DocumentUrl4,
    decimal? Month5, Guid? FileId5, string? DocumentUrl5,
    decimal? Month6, Guid? FileId6, string? DocumentUrl6,
    decimal? Month7, Guid? FileId7, string? DocumentUrl7,
    decimal? Month8, Guid? FileId8, string? DocumentUrl8,
    decimal? Month9, Guid? FileId9, string? DocumentUrl9,
    decimal? Month10, Guid? FileId10, string? DocumentUrl10,
    decimal? Month11, Guid? FileId11, string? DocumentUrl11,
    decimal? Month12, Guid? FileId12, string? DocumentUrl12,
    decimal? SumAmount);

public class GetAnnouncementSorKorRorListGroupedEndpoint : EndpointBase<GetAnnouncementSorKorRorListGroupedRequest, Ok<PaginatedQueryResult<GetAnnouncementSorKorRorListGroupedResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetAnnouncementSorKorRorListGroupedEndpoint(
        ILogger<GetAnnouncementSorKorRorListGroupedEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("announcement-sor-kor-ror/grouped");
        this.Description(b => b
            .WithTags("AnnouncementSorKorRor")
            .WithName("GetAnnouncementSorKorRorListGrouped")
            .AllowAnonymous()
            .Produces<PaginatedQueryResult<GetAnnouncementSorKorRorListGroupedResponse>>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetAnnouncementSorKorRorListGroupedResponse>>> HandleRequestAsync(
        GetAnnouncementSorKorRorListGroupedRequest req,
        CancellationToken ct)
    {
        int.TryParse(req.Keyword, out var keywordYear);

        var flat = await this.dbContext.AnnouncementSorKorRors
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .WhereIfTrue(req.Year.HasValue, x => x.Year == req.Year)
            .WhereIfTrue(
                !string.IsNullOrWhiteSpace(req.DepartmentTypeCode),
                x => (string)x.DepartmentTypeCode! == req.DepartmentTypeCode)
            .Select(x => new
            {
                Year = x.Year,
                DepartmentTypeCode = x.DepartmentTypeCode != null ? (string)x.DepartmentTypeCode! : null,
                DepartmentType = x.DepartmentType != null ? x.DepartmentType.Label : null,
                Month = x.Month,
                Amount = x.Amount,
                DocumentId = x.DocumentId.HasValue ? x.DocumentId.Value.Value : (Guid?)null,
                DocumentUrl = x.DocumentUrl,
            })
            .ToListAsync(ct);

        var groups = flat
            .GroupBy(x => new { x.Year, x.DepartmentTypeCode, x.DepartmentType })
            .OrderByDescending(g => g.Key.Year)
            .ThenBy(g => g.Key.DepartmentTypeCode)
            .ToList();

        var paged = groups
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(g =>
            {
                var m = g.ToDictionary(x => x.Month ?? 0);
                decimal? A(int i) => m.TryGetValue(i, out var v) ? v.Amount : null;
                Guid? F(int i) => m.TryGetValue(i, out var v) ? v.DocumentId : null;
                string? U(int i) => m.TryGetValue(i, out var v) ? v.DocumentUrl : null;

                return new GetAnnouncementSorKorRorListGroupedResponse(
                    g.Key.Year,
                    g.Key.DepartmentTypeCode,
                    g.Key.DepartmentType,
                    A(1),
                    F(1),
                    U(1),
                    A(2),
                    F(2),
                    U(2),
                    A(3),
                    F(3),
                    U(3),
                    A(4),
                    F(4),
                    U(4),
                    A(5),
                    F(5),
                    U(5),
                    A(6),
                    F(6),
                    U(6),
                    A(7),
                    F(7),
                    U(7),
                    A(8),
                    F(8),
                    U(8),
                    A(9),
                    F(9),
                    U(9),
                    A(10),
                    F(10),
                    U(10),
                    A(11),
                    F(11),
                    U(11),
                    A(12),
                    F(12),
                    U(12),
                    g.Sum(x => x.Amount));
            });

        return TypedResults.Ok(new PaginatedQueryResult<GetAnnouncementSorKorRorListGroupedResponse>(paged, groups.Count));
    }
}

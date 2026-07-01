namespace GHB.DP2.Application.Features.Announcement.AnnouncementReport;

using Codehard.Infrastructure.EntityFramework;
using global::GHB.DP2.Application.Common;
using global::GHB.DP2.Application.Extensions;
using global::GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetAnnouncementReportListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? AnnouncementReportTypeCode,
    int? Year);

public record GetAnnouncementReportListResponse(
    Guid Id,
    int? Year,
    string? Discretion,
    string? AnnouncementReportTypeCode,
    string? AnnouncementReportType,
    bool? IsActive,
    bool? IsDp,
    Guid? DocumentId,
    string? DocumentName,
    string? DocumentUrl);

public class GetAnnouncementReportListEndpoint : EndpointBase<GetAnnouncementReportListRequest, Ok<PaginatedQueryResult<GetAnnouncementReportListResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetAnnouncementReportListEndpoint(
        ILogger<GetAnnouncementReportListEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("announcement-report");
        this.Description(b => b
            .WithTags("AnnouncementReport")
            .WithName("GetAnnouncementReportList")
            .AllowAnonymous()
            .Produces<PaginatedQueryResult<GetAnnouncementReportListResponse>>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetAnnouncementReportListResponse>>> HandleRequestAsync(
        GetAnnouncementReportListRequest req,
        CancellationToken ct)
    {
        var query = this.dbContext.AnnouncementReports
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .WhereIfTrue(
                !string.IsNullOrWhiteSpace(req.AnnouncementReportTypeCode),
                x => (string)x.AnnouncementReportTypeCode! == req.AnnouncementReportTypeCode)
            .WhereIfTrue(
                req.Year.HasValue,
                x => x.Year == req.Year)
            .WhereIfTrue(
                !string.IsNullOrWhiteSpace(req.Keyword),
                x => EF.Functions.ILike(x.Discretion ?? string.Empty, $"%{req.Keyword}%")
                    || (x.Year.HasValue && EF.Functions.ILike(x.Year.Value.ToString(), $"%{req.Keyword}%")))
            .OrderByDescending(x => x.Year)
            .Select(x => new GetAnnouncementReportListResponse(
                x.Id.Value,
                x.Year,
                x.Discretion,
                x.AnnouncementReportTypeCode != null ? (string)x.AnnouncementReportTypeCode! : null,
                x.AnnouncementCategory != null ? x.AnnouncementCategory.Label : null,
                x.IsActive,
                x.IsDp,
                x.DocumentId.HasValue ? x.DocumentId.Value.Value : (Guid?)null,
                x.DocumentName,
                x.DocumentUrl));

        var paginated = await PaginatedList<GetAnnouncementReportListResponse>
            .CreateAsync(query, req.PageNumber, req.PageSize, ct);

        return TypedResults.Ok(paginated.ToResult());
    }
}

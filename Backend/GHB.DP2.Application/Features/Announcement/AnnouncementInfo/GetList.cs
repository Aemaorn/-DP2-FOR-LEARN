namespace GHB.DP2.Application.Features.Announcement.AnnouncementInfo;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetAnnouncementInfoListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? AnnouncementCategoryCode,
    string? SupplyMethodCode,
    int? BudgetYear,
    DateTimeOffset? AnnouncementDateFrom,
    DateTimeOffset? AnnouncementDateTo);

public record GetAnnouncementInfoListResponse(
    Guid Id,
    int? OldId,
    string? AnnouncementName,
    string? AnnouncementTitle,
    string? AnnouncementCategory,
    DateTimeOffset? AnnouncementDate,
    decimal? BudgetAmount,
    int? BudgetYear,
    DateTimeOffset? ExpectedDate,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    decimal? ReferencePrice,
    bool? IsDp,
    bool? IsActive,
    Guid? DocumentId,
    string? DocumentName,
    string? DocumentUrl,
    string? Description,
    string? SupplyMethod);

public record AnnouncementInfoSupplyMethodCount(string Code, int Count);

public record GetAnnouncementInfoListResult(
    int AllCount,
    IReadOnlyList<AnnouncementInfoSupplyMethodCount> Counts,
    PaginatedQueryResult<GetAnnouncementInfoListResponse> Data);

public class GetAnnouncementInfoListEndpoint : EndpointBase<GetAnnouncementInfoListRequest, Ok<GetAnnouncementInfoListResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetAnnouncementInfoListEndpoint(
        ILogger<GetAnnouncementInfoListEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("announcement-info/{announcementCategoryCode?}");
        this.Description(b => b
                              .WithTags("AnnouncementInfo")
                              .WithName("GetAnnouncementInfoList")
                              .AllowAnonymous()
                              .Produces<GetAnnouncementInfoListResult>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<GetAnnouncementInfoListResult>> HandleRequestAsync(
        GetAnnouncementInfoListRequest req,
        CancellationToken ct)
    {
        // Base query without SupplyMethodCode filter (used for counts)
        var baseQuery = this.dbContext.AnnouncementInfos
                            .AsNoTracking()
                            .Where(x => !x.IsDeleted)
                            .WhereIfTrue(
                                !string.IsNullOrWhiteSpace(req.AnnouncementCategoryCode),
                                x => x.AnnouncementCategoryCode == ParameterCode.From(req.AnnouncementCategoryCode!))
                            .WhereIfTrue(
                                !string.IsNullOrWhiteSpace(req.Keyword),
                                x => EF.Functions.ILike(x.AnnouncementName ?? string.Empty, $"%{req.Keyword}%") ||
                                     EF.Functions.ILike(x.AnnouncementTitle ?? string.Empty, $"%{req.Keyword}%") ||
                                     EF.Functions.ILike((string)x.AnnouncementCategoryCode! ?? string.Empty, $"%{req.Keyword}%") ||
                                     (x.BudgetYear.HasValue && EF.Functions.ILike(x.BudgetYear.Value.ToString(), $"%{req.Keyword}%")))
                            .WhereIfTrue(
                                req.BudgetYear.HasValue,
                                x => x.BudgetYear == req.BudgetYear)
                            .WhereIfTrue(
                                req.AnnouncementDateFrom.HasValue,
                                x => x.AnnouncementDate.HasValue &&
                                     x.AnnouncementDate.Value.Date >= req.AnnouncementDateFrom!.Value.Date)
                            .WhereIfTrue(
                                req.AnnouncementDateTo.HasValue,
                                x => x.AnnouncementDate.HasValue &&
                                     x.AnnouncementDate.Value.Date <= req.AnnouncementDateTo!.Value.Date);

        var allCount = await baseQuery.CountAsync(ct);

        var countsByMethod = await baseQuery
            .Where(x => x.SupplyMethodCode != null)
            .GroupBy(x => (string)x.SupplyMethodCode!)
            .Select(g => new AnnouncementInfoSupplyMethodCount(g.Key, g.Count()))
            .ToListAsync(ct);

        var listQuery = baseQuery
            .WhereIfTrue(
                !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                x => (string)x.SupplyMethodCode! == req.SupplyMethodCode)
            .OrderByDescending(x => x.AnnouncementDate)
            .Select(x => new GetAnnouncementInfoListResponse(
                x.Id.Value,
                x.OldId,
                x.AnnouncementName,
                x.AnnouncementTitle,
                x.AnnouncementCategory != null ? x.AnnouncementCategory.Label : null,
                x.AnnouncementDate,
                x.BudgetAmount,
                x.BudgetYear,
                x.ExpectedDate,
                x.StartDate,
                x.EndDate,
                x.ReferencePrice,
                x.IsDp,
                x.IsActive,
                x.DocumentId.HasValue ? x.DocumentId.Value.Value : (Guid?)null,
                x.DocumentName,
                x.DocumentUrl,
                x.Description,
                x.SupplyMethod != null ? x.SupplyMethod.Label : null));

        var paginated = await PaginatedList<GetAnnouncementInfoListResponse>
            .CreateAsync(listQuery, req.PageNumber, req.PageSize, ct);

        var pageResult = paginated.ToResult();

        var result = new GetAnnouncementInfoListResult(allCount, countsByMethod, pageResult);
        return TypedResults.Ok(result);
    }
}

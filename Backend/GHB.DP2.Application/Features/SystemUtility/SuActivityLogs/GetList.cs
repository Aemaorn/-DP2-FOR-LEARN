namespace GHB.DP2.Application.Features.SystemUtility.SuActivityLogs;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetSuActivityLogsRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate);

public record GetSuActivityLogItem(
    DateTimeOffset? CreatedAt,
    string CreatedBy,
    string Action,
    string? Remark,
    string? Program,
    string? Ip);

public class GetSuActivityLogs
    : EndpointBase<GetSuActivityLogsRequest, Ok<PaginatedQueryResult<GetSuActivityLogItem>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSuActivityLogs(
        Dp2DbContext dbContext,
        ILogger<GetSuActivityLogsById> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuActivityLogs"));
        this.Get("/su/logs");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetSuActivityLogItem>>> HandleRequestAsync(
        GetSuActivityLogsRequest req,
        CancellationToken ct)
    {
        var query =
            this.dbContext.ActivityLogs
                .WhereIfTrue(
                    req.StartDate != null,
                    x => x.AuditInfo.CreatedAt >= req.StartDate)
                .WhereIfTrue(
                    req.EndDate != null,
                    x => x.AuditInfo.CreatedAt <= req.StartDate)
                .WhereIfTrue(
                    !string.IsNullOrWhiteSpace(req.Keyword),
                    x =>
                        EF.Functions.ILike(x.ActivityInfo.Action, $"%{req.Keyword}%") ||
                        EF.Functions.ILike(x.ActivityInfo.Type, $"%{req.Keyword}%"));

        var paginated =
            await PaginatedList<GHB.DP2.Domain.Common.ActivityLog>
                .CreateAsync(
                    query,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var programs = await this.dbContext.SuPrograms.ToArrayAsync(cancellationToken: ct);

        var result =
            paginated.ToResult(
                al =>
                {
                    var programLabel =
                        string.IsNullOrWhiteSpace(al.ActivityInfo.ProgramCode)
                            ? string.Empty
                            : programs.FirstOrDefault(p => p.Code == al.ActivityInfo.ProgramCode)?.Label ?? string.Empty;

                    return new GetSuActivityLogItem(
                        al.AuditInfo.CreatedAt,
                        al.AuditInfo.CreatedByName,
                        al.ActivityInfo.Action,
                        al.ActivityInfo.Remark,
                        programLabel,
                        al.ActivityInfo.Ip);
                });

        return TypedResults.Ok(result);
    }
}
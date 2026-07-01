namespace GHB.DP2.Application.Features.SystemUtility.SuActivityLogs;

using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetSuActivityLogsByIdRequest
{
    public Guid EntityId { get; init; }

    public ProgramName? ProgramName { get; init; }
}

public record ActivityLogResponse(
    string GroupName,
    ActivityLog LastedActivity,
    IEnumerable<ActivityLog> ActivityLogs);

public record ActivityLog(
    Guid Id,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    string ActivityAction,
    string ActivityStatus,
    string ActivityType,
    string? ActivityRemark);

public record RawActivityLogWithGroupResponse(
    Guid Id,
    string ActivityAction,
    string? ActivityRemark,
    string ActivityStatus,
    string ActivityType,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    string NumberGroup);

public enum ProgramName
{
    Plan,
    Appoint,
    Tor,
    MedianPrice,
}

public class GetSuActivityLogsById : EndpointBase<GetSuActivityLogsByIdRequest, Ok<List<ActivityLogResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSuActivityLogsById(
        Dp2DbContext dbContext,
        ILogger<GetSuActivityLogsById> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuActivityLogs"));
        this.Get("/su/logs/{EntityId:guid}");
    }

    protected override async ValueTask<Ok<List<ActivityLogResponse>>> HandleRequestAsync(GetSuActivityLogsByIdRequest req, CancellationToken ct)
    {
        var query = await this.GetQueryAllRefId(req.EntityId, req.ProgramName);

        var logsRaw = await this.dbContext.Database
                                .SqlQueryRaw<RawActivityLogWithGroupResponse>(query)
                                .AsNoTracking()
                                .ToListAsync(ct);

        var groupedLogs = logsRaw
                          .GroupBy(log => log.NumberGroup)
                          .Select(g => new ActivityLogResponse(
                              g.Key,
                              new ActivityLog(
                                  g.First().Id,
                                  g.First().CreatedAt,
                                  g.First().CreatedByName,
                                  g.First().ActivityAction,
                                  g.First().ActivityStatus,
                                  g.First().ActivityType,
                                  g.First().ActivityRemark),
                              g.Select(log => new ActivityLog(
                                   log.Id,
                                   log.CreatedAt,
                                   log.CreatedByName,
                                   log.ActivityAction,
                                   log.ActivityStatus,
                                   log.ActivityType,
                                   log.ActivityRemark))
                               .ToList()))
                          .ToList();

        return TypedResults.Ok(groupedLogs);
    }

    private async Task<string> GetQueryAllRefId(Guid entityId, ProgramName? programName)
    {
        if (programName is null)
        {
            var id = new List<Guid> { entityId };

            return QueryString(id);
        }

        var ids = programName switch
        {
            ProgramName.Plan => await this.GetAllPlanRelatedIds(entityId),
            ProgramName.Appoint => await this.GetAllAppointRelatedIds(entityId),
            ProgramName.Tor => await this.GetAllTorRelatedIds(entityId),
            ProgramName.MedianPrice => await this.GetAllMedianPriceRelatedIds(entityId),
            _ => new List<Guid> { entityId },
        };

        return QueryString(ids);
    }

    private async Task<List<Guid>> GetAllPlanRelatedIds(Guid entityId)
    {
        var entityIds = new HashSet<Guid> { entityId };

        var queue = new Queue<Guid>();
        queue.Enqueue(entityId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            var childPlans = await this.dbContext.Plans
                                       .Where(p => p.Id == PlanId.From(currentId))
                                       .Select(p => p.ReferenceId)
                                       .ToListAsync();

            foreach (var childId in childPlans)
            {
                if (childId == null)
                {
                    return entityIds.ToList();
                }

                if (entityIds.Add(childId.Value.Value))
                {
                    queue.Enqueue(childId.Value.Value);
                }
            }
        }

        return entityIds.ToList();
    }

    private async Task<List<Guid>> GetAllAppointRelatedIds(Guid entityId)
    {
        var entityIds = new HashSet<Guid> { entityId };

        var queue = new Queue<Guid>();
        queue.Enqueue(entityId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            var childAppoints = await this.dbContext.PpAppoints
                                        .Where(p => p.Id == PpAppointId.From(currentId))
                                        .Select(p => p.ReferenceId)
                                        .ToListAsync();

            foreach (var childId in childAppoints)
            {
                if (childId == null)
                {
                    return entityIds.ToList();
                }

                if (entityIds.Add(childId.Value.Value))
                {
                    queue.Enqueue(childId.Value.Value);
                }
            }
        }

        return entityIds.ToList();
    }

    private async Task<List<Guid>> GetAllTorRelatedIds(Guid entityId)
    {
        var entityIds = new HashSet<Guid> { entityId };

        var queue = new Queue<Guid>();
        queue.Enqueue(entityId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            var childTors = await this.dbContext.PpTorDrafts
                                    .Where(p => p.Id == PpTorDraftId.From(currentId))
                                    .Select(p => p.ReferenceId)
                                    .ToListAsync();

            foreach (var childId in childTors)
            {
                if (childId == null)
                {
                    return entityIds.ToList();
                }

                if (entityIds.Add(childId.Value.Value))
                {
                    queue.Enqueue(childId.Value.Value);
                }
            }
        }

        return entityIds.ToList();
    }

    private async Task<List<Guid>> GetAllMedianPriceRelatedIds(Guid entityId)
    {
        var entityIds = new HashSet<Guid> { entityId };

        var queue = new Queue<Guid>();
        queue.Enqueue(entityId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            var childMedianPrices = await this.dbContext.PpMedianPrices
                                            .Where(p => p.Id == MedianPriceId.From(currentId))
                                            .Select(p => p.ReferenceId)
                                            .ToListAsync();

            foreach (var childId in childMedianPrices)
            {
                if (childId == null)
                {
                    return entityIds.ToList();
                }

                if (entityIds.Add(childId.Value.Value))
                {
                    queue.Enqueue(childId.Value.Value);
                }
            }
        }

        return entityIds.ToList();
    }

    private static string QueryString(List<Guid> key)
    {
        return $@"
        WITH ordered AS (
          SELECT *, ROW_NUMBER() OVER (ORDER BY ""CreatedAt"") AS rn
          FROM ""SystemUtility"".""ActivityLog""
          WHERE ""Key"" in ({string.Join(", ", key.Select(x => $"'{x.ToString()}'"))})
        ),
        labeled AS (
          SELECT *, CONCAT(""ActivityType"", '::', ""CreatedByName"") AS label
          FROM ordered
        ),
        grouped AS (
          SELECT *,
            CASE
              WHEN label = LAG(label) OVER (ORDER BY rn) THEN 0
              ELSE 1
            END AS new_group_flag
          FROM labeled
        ),
        group_tracking AS (
          SELECT *, MIN(rn) OVER (PARTITION BY label) AS first_seen_at
          FROM grouped
        ),
        final_group AS (
          SELECT *, DENSE_RANK() OVER (ORDER BY first_seen_at) AS numbergroup
          FROM group_tracking
        )
        SELECT
            ""Id"",
            ""ActivityAction"",
            ""ActivityRemark"",
            ""ActivityStatus"",
            ""ActivityType"",
            ""CreatedByName"",
            ""CreatedAt"",
            CONCAT(""ActivityType"", '_', numbergroup) AS ""NumberGroup""
        FROM final_group
        ORDER BY ""CreatedAt"" DESC";
    }
}
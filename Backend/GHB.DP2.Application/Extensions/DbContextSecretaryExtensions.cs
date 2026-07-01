namespace GHB.DP2.Application.Extensions;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;

public static class DbContextSecretaryExtensions
{
    /// <summary>
    /// Returns UserIds of all active secretaries for the given target user.
    /// Searches from two sources:
    /// - Source A: SuSecretaryOwner matched by SuUserId or PositionId
    /// - Source B: raw_employee_view joined with RawPosition where position name contains 'เลขา' in the same BusinessUnit
    /// </summary>
    public static async Task<IReadOnlyList<UserId>> GetSecretaryUserIdsAsync(
        this Dp2DbContext dbContext,
        UserId targetUserId,
        CancellationToken ct)
    {
        var targetUser = await dbContext.SuUsers
            .Include(u => u.Employee)
            .ThenInclude(e => e.View)
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct);

        if (targetUser is null)
        {
            return [];
        }

        var positionId = targetUser.Employee?.View?.PositionId;
        var businessUnitId = targetUser.Employee?.View?.BusinessUnitId;

        // Source A: SuSecretaryOwner by SuUserId or PositionId
        var secretaryOwners = await dbContext.SuSecretaryOwners
            .Include(o => o.Secretaries)
            .Where(o => o.SuUserId == targetUserId
                     || (positionId != null && o.PositionId == positionId))
            .ToListAsync(ct);

        var secretaryUserIds = secretaryOwners
            .SelectMany(o => o.GetActiveSecretaryUserIds())
            .Where(id => id != targetUserId)
            .ToHashSet();

        // Source B: raw_employee_view + RawPosition where BusinessUnitId matches and Name contains 'เลขา'
        // if (businessUnitId is not null)
        // {
        //     var secretaryEmployeeCodes = await dbContext.Set<RawEmployeeView>()
        //         .Join(
        //             dbContext.RawPositions,
        //             rev => rev.PositionId,
        //             rp => rp.Id,
        //             (rev, rp) => new { rev.EmployeeCode, rev.BusinessUnitId, PositionName = rp.Name })
        //         .Where(x => x.BusinessUnitId == businessUnitId && x.PositionName.Contains("เลขา"))
        //         .Select(x => x.EmployeeCode)
        //         .ToListAsync(ct);
        //
        //     if (secretaryEmployeeCodes.Count > 0)
        //     {
        //         var rawSecretaryUserIds = await dbContext.SuUsers
        //             .Where(u => secretaryEmployeeCodes.Contains(u.EmployeeCode))
        //             .Select(u => u.Id)
        //             .ToListAsync(ct);
        //
        //         secretaryUserIds.UnionWith(rawSecretaryUserIds.Where(id => id != targetUserId));
        //     }
        // }
        return [.. secretaryUserIds];
    }

    /// <summary>
    /// Returns the user's own UserId plus all active secretary UserIds.
    /// Use this when notifying a plain UserId (e.g. plan creator).
    /// </summary>
    public static async Task<IReadOnlyList<UserId>> GetNotificationTargetsForUserAsync(
        this Dp2DbContext dbContext,
        UserId userId,
        CancellationToken ct)
    {
        var targets = new HashSet<UserId> { userId };
        targets.UnionWith(await dbContext.GetSecretaryUserIdsAsync(userId, ct));
        return [.. targets];
    }

    /// <summary>
    /// Returns acceptor.GetNotificationTargets() (original + delegation) plus all active secretary UserIds.
    /// Mirrors the sync GetNotificationTargets pattern with secretary support.
    /// </summary>
    public static async Task<IReadOnlyList<UserId>> GetNotificationTargetsWithSecretariesAsync<TAcceptor>(
        this Dp2DbContext dbContext,
        TAcceptor acceptor,
        CancellationToken ct)
        where TAcceptor : IHasAcceptor
    {
        var primaryTargets = acceptor.GetNotificationTargets().ToList();
        var targets = new HashSet<UserId>(primaryTargets);
        var allSecretaries = await Task.WhenAll(primaryTargets.Select(id => dbContext.GetSecretaryUserIdsAsync(id, ct)));
        targets.UnionWith(allSecretaries.SelectMany(s => s));
        return [.. targets];
    }

    /// <summary>
    /// Returns assignee.GetAssigneeNotificationTargets() (original + delegation) plus all active secretary UserIds.
    /// Mirrors the sync GetAssigneeNotificationTargets pattern with secretary support.
    /// </summary>
    public static async Task<IReadOnlyList<UserId>> GetAssigneeNotificationTargetsWithSecretariesAsync<TAssignee>(
        this Dp2DbContext dbContext,
        TAssignee assignee,
        CancellationToken ct)
        where TAssignee : IHasAssignee
    {
        var primaryTargets = assignee.GetAssigneeNotificationTargets().ToList();
        var targets = new HashSet<UserId>(primaryTargets);
        var allSecretaries = await Task.WhenAll(primaryTargets.Select(id => dbContext.GetSecretaryUserIdsAsync(id, ct)));
        targets.UnionWith(allSecretaries.SelectMany(s => s));
        return [.. targets];
    }

    /// <summary>
    /// Returns all assignee notification targets (original + delegation + secretaries) for a collection of assignees, deduplicated.
    /// Use this to avoid nested foreach loops at call sites.
    /// </summary>
    public static async Task<IReadOnlyList<UserId>> GetAssigneeNotificationTargetsWithSecretariesAsync<TAssignee>(
        this Dp2DbContext dbContext,
        IEnumerable<TAssignee> assignees,
        CancellationToken ct)
        where TAssignee : IHasAssignee
    {
        var allTargets = await Task.WhenAll(assignees.Select(a => dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(a, ct)));
        return [.. allTargets.SelectMany(t => t).ToHashSet()];
    }
}

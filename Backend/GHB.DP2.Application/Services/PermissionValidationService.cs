namespace GHB.DP2.Application.Services;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class PermissionValidationService : IPermissionValidationService
{
    private readonly Dp2DbContext dbContext;
    private readonly ILogger<PermissionValidationService> logger;

    public PermissionValidationService(
        Dp2DbContext dbContext,
        ILogger<PermissionValidationService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<bool> HasPermissionAsync(
        UserId userId, string path, Permission requiredPermission, CancellationToken ct = default)
    {
        try
        {
            var userPermission = await this.GetUserPermissionAsync(userId, path, ct);

            // Check if user has the required permission level
            var hasPermission = userPermission.HasFlag(requiredPermission);

            this.logger.LogDebug(
                "Permission check for user {UserId} on path {Path}: Required={RequiredPermission}, User={UserPermission}, HasAccess={HasAccess}",
                userId.Value,
                path,
                requiredPermission,
                userPermission,
                hasPermission);

            return hasPermission;
        }
        catch (Exception ex)
        {
            this.logger.LogError(
                ex,
                "Error checking permission for user {UserId} on path {Path}",
                userId.Value,
                path);
            return false; // Fail closed - deny access on errors
        }
    }

    public async Task<Permission> GetUserPermissionAsync(UserId userId, string path, CancellationToken ct = default)
    {
        var user = await this.dbContext.SuUsers
            .Include(u => u.Roles)
            .ThenInclude(r => r.RolePrograms)
            .ThenInclude(rp => rp.Program)
            .SingleOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            this.logger.LogWarning("User {UserId} not found", userId.Value);
            return Permission.None;
        }

        var userRoles = user.Roles?.ToList() ?? [];

        if (userRoles.Count == 0)
        {
            this.logger.LogWarning("User {UserId} has no roles assigned", userId.Value);
            return Permission.None;
        }

        var normalizedPath = NormalizePath(path);

        var rolePrograms = userRoles
            .SelectMany(r => r.RolePrograms)
            .Where(rp => rp.Program != null
                && (rp.IsManage is true || rp.IsView is true)
                && NormalizePath(rp.Program.Path) == normalizedPath)
            .ToList();

        if (rolePrograms.Count == 0)
        {
            this.logger.LogDebug(
                "User {UserId} has no permission for path {Path}",
                userId.Value,
                path);

            return Permission.None;
        }

        return rolePrograms
            .Select(rp => rp.Permission)
            .Aggregate(Permission.None, (current, p) => current | p);
    }

    internal static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Trim().ToLowerInvariant();

        if (normalized.StartsWith("/api/") || normalized == "/api")
        {
            normalized = normalized[4..];
        }

        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        normalized = normalized.TrimEnd('/');

        // Strip dynamic segments like /{id} so /st/st005/{id} matches /st/st005
        var segments = normalized.Split('/');
        var result = new List<string>();
        foreach (var segment in segments)
        {
            if (segment.StartsWith('{') && segment.EndsWith('}'))
            {
                break;
            }

            result.Add(segment);
        }

        return string.Join('/', result);
    }

    public async Task<bool> IsUserActiveAsync(UserId userId, CancellationToken ct = default)
    {
        var user = await this.dbContext.SuUsers
            .Include(u => u.Roles)
            .SingleOrDefaultAsync(u => u.Id == userId, ct);

        return user != null && user.IsActive && user.Roles.Any();
    }
}
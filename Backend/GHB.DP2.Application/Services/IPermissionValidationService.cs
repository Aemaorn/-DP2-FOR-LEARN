namespace GHB.DP2.Application.Services;

using GHB.DP2.Domain.SystemUtility;

public interface IPermissionValidationService
{
    /// <summary>
    /// Validates if the user has the required permission for a specific path
    /// </summary>
    /// <param name="userId">The user ID from JWT claims</param>
    /// <param name="path">The menu/endpoint path (e.g., "/settings", "/users")</param>
    /// <param name="requiredPermission">The minimum permission level required</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if user has permission, false otherwise</returns>
    Task<bool> HasPermissionAsync(UserId userId, string path, Permission requiredPermission, CancellationToken ct = default);

    /// <summary>
    /// Gets the user's actual permission for a specific path
    /// </summary>
    /// <param name="userId">The user ID from JWT claims</param>
    /// <param name="path">The menu/endpoint path</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The user's permission level for the path</returns>
    Task<Permission> GetUserPermissionAsync(UserId userId, string path, CancellationToken ct = default);

    /// <summary>
    /// Validates if the user exists and has any active roles
    /// </summary>
    /// <param name="userId">The user ID from JWT claims</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if user exists and is active, false otherwise</returns>
    Task<bool> IsUserActiveAsync(UserId userId, CancellationToken ct = default);
}
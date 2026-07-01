namespace GHB.DP2.Application.Authorization;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Authorization attribute that validates user permissions against program paths
/// This prevents parameter tampering by checking actual database permissions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly Permission requiredPermission;
    private readonly string? programPath;

    /// <summary>
    /// Requires specific permission for a program path
    /// </summary>
    /// <param name="requiredPermission">Minimum permission level required</param>
    /// <param name="programPath">Program path to check permissions for (optional - will use endpoint path if not specified)</param>
    public RequirePermissionAttribute(Permission requiredPermission, string? programPath = null)
    {
        this.requiredPermission = requiredPermission;
        this.programPath = programPath;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get required services
        var permissionService = context.HttpContext.RequestServices.GetService<IPermissionValidationService>();
        var logger = context.HttpContext.RequestServices.GetService<ILogger<RequirePermissionAttribute>>();

        if (permissionService == null || logger == null)
        {
            logger?.LogError("Required services not registered for permission validation");
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            logger.LogWarning("Unauthenticated access attempt to protected endpoint {Path}", context.HttpContext.Request.Path);
            context.Result = new UnauthorizedResult();
            return;
        }

        // Extract user ID from JWT claims
        var userIdClaim = context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userIdGuid))
        {
            logger.LogWarning("Invalid or missing user ID in JWT token");
            context.Result = new UnauthorizedResult();
            return;
        }

        var userId = UserId.From(userIdGuid);

        // Determine the path to check permissions for
        var pathToCheck = this.programPath ?? GetProgramPathFromEndpoint(context);
        if (string.IsNullOrEmpty(pathToCheck))
        {
            logger.LogError("Unable to determine program path for permission check");
            context.Result = new StatusCodeResult(500);
            return;
        }

        try
        {
            // Check if user is active
            if (!await permissionService.IsUserActiveAsync(userId))
            {
                logger.LogWarning("Inactive user {UserId} attempted to access {Path}", userId.Value, pathToCheck);
                context.Result = new ForbidResult();
                return;
            }

            // Validate permission
            if (!await permissionService.HasPermissionAsync(userId, pathToCheck, this.requiredPermission))
            {
                logger.LogWarning(
                    "Access denied: User {UserId} lacks {RequiredPermission} permission for {Path}",
                    userId.Value,
                    this.requiredPermission,
                    pathToCheck);

                context.Result = new ForbidResult(); // 403 Forbidden
                return;
            }

            logger.LogDebug(
                "Access granted: User {UserId} has {RequiredPermission} permission for {Path}",
                userId.Value,
                this.requiredPermission,
                pathToCheck);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error validating permission for user {UserId} on path {Path}",
                userId.Value,
                pathToCheck);
            context.Result = new StatusCodeResult(500);
        }
    }

    private static string? GetProgramPathFromEndpoint(AuthorizationFilterContext context)
    {
        // Try to extract path from route data or request path
        var routePath = context.ActionDescriptor.AttributeRouteInfo?.Template;
        if (!string.IsNullOrEmpty(routePath))
        {
            // Convert route template to program path format
            // e.g., "/api/settings" -> "/settings"
            var cleanedPath = routePath.Replace("/api", string.Empty).TrimStart('/');
            return $"/{cleanedPath}";
        }

        // Fallback to request path
        var requestPath = context.HttpContext.Request.Path.Value;
        if (!string.IsNullOrEmpty(requestPath) && requestPath.StartsWith("/api/"))
        {
            return requestPath.Replace("/api", string.Empty);
        }

        return requestPath;
    }
}
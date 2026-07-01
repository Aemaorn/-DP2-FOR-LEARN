namespace GHB.DP2.Application.Features;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Secure base endpoint that includes built-in permission validation
/// Use this for endpoints that need protection against parameter tampering
/// </summary>
public abstract class SecureEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly IPermissionValidationService permissionService;

    protected SecureEndpointBase(
        IPermissionValidationService permissionService,
        ILogger logger)
        : base(logger)
    {
        this.permissionService = permissionService;
    }

    /// <summary>
    /// Override this to specify a custom program path for this endpoint
    /// If not overridden, the program path will be automatically extracted from the endpoint route
    /// Permission is automatically determined based on HTTP method:
    /// GET -> View, POST/PUT/PATCH/DELETE -> Manage
    /// </summary>
    protected virtual string? GetProgramPath() => null;

    /// <summary>
    /// Gets permission requirements based on HTTP method and program path
    /// </summary>
    private (Permission RequiredPermission, string ProgramPath) GetPermissionRequirements()
    {
        var programPath = this.GetProgramPath() ?? this.ExtractProgramPathFromRoute();
        var httpMethod = this.HttpContext.Request.Method.ToUpperInvariant();

        var permission = httpMethod switch
        {
            "GET" => Permission.View,
            "POST" or "PUT" or "PATCH" or "DELETE" => Permission.Manage,
            _ => Permission.Manage, // Default to highest permission for unknown methods
        };

        return (permission, programPath);
    }

    /// <summary>
    /// Extracts program path from the configured endpoint route
    /// </summary>
    private string ExtractProgramPathFromRoute()
    {
        var routeTemplate = this.Definition.Routes?.FirstOrDefault();
        if (!string.IsNullOrEmpty(routeTemplate))
        {
            return routeTemplate;
        }

        var requestPath = this.HttpContext.Request.Path.Value;
        if (!string.IsNullOrEmpty(requestPath))
        {
            return requestPath;
        }

        throw new InvalidOperationException("Unable to determine program path for endpoint. Please override GetProgramPath() method.");
    }

    /// <summary>
    /// Validates permissions before handling the request
    /// </summary>
    public override async Task OnBeforeHandleAsync(TRequest req, CancellationToken ct)
    {
        await base.OnBeforeHandleAsync(req, ct);

        var (requiredPermission, programPath) = this.GetPermissionRequirements();

        // Get user ID from claims
        var userId = this.GetUserIdFromClaims();
        if (userId == null)
        {
            this.ThrowError("Unable to identify user from authentication token");
            return;
        }

        // Validate user permissions
        var hasPermission = await this.permissionService.HasPermissionAsync(
            (UserId)userId, programPath, requiredPermission, ct);

        if (!hasPermission)
        {
            // Log the access attempt for security monitoring
            var logger = this.HttpContext.RequestServices.GetRequiredService<ILogger<SecureEndpointBase<TRequest, TResponse>>>();
            logger.LogWarning(
                "ACCESS DENIED: User {UserId} attempted to access {Path} requiring {Permission}",
                userId.Value,
                programPath,
                requiredPermission);

            // Send 403 Forbidden response
            this.ThrowError("Access denied. Insufficient permissions.", 403);
            return;
        }
    }

    /// <summary>
    /// Helper method to get current user ID from JWT claims
    /// </summary>
    protected UserId? GetUserIdFromClaims()
    {
        var userIdClaim = this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userIdGuid))
        {
            return null;
        }

        return UserId.From(userIdGuid);
    }

    /// <summary>
    /// Convenience method to check if current user has specific permission for a path
    /// </summary>
    protected async Task<bool> CurrentUserHasPermissionAsync(string path, Permission permission, CancellationToken ct = default)
    {
        var userId = this.GetUserIdFromClaims();
        if (userId == null)
        {
            return false;
        }

        return await this.permissionService.HasPermissionAsync((UserId)userId, path, permission, ct);
    }

    /// <summary>
    /// Convenience method to get current user's permission for a path
    /// </summary>
    protected async Task<Permission> GetCurrentUserPermissionAsync(string path, CancellationToken ct = default)
    {
        var userId = this.GetUserIdFromClaims();
        if (userId == null)
        {
            return Permission.None;
        }

        return await this.permissionService.GetUserPermissionAsync((UserId)userId, path, ct);
    }
}

/// <summary>
/// Secure base endpoint without request that includes built-in permission validation
/// Use this for endpoints that need protection against parameter tampering and don't require request input
/// </summary>
public abstract class SecureEndpointBase<TResponse> : EndpointBase<TResponse>
    where TResponse : IResult
{
    private readonly IPermissionValidationService permissionService;

    protected SecureEndpointBase(
        IPermissionValidationService permissionService,
        ILogger logger)
        : base(logger)
    {
        this.permissionService = permissionService;
    }

    /// <summary>
    /// Override this to specify a custom program path for this endpoint
    /// If not overridden, the program path will be automatically extracted from the endpoint route
    /// Permission is automatically determined based on HTTP method:
    /// GET -> View, POST/PUT/PATCH/DELETE -> Manage
    /// </summary>
    protected virtual string? GetProgramPath() => null;

    /// <summary>
    /// Gets permission requirements based on HTTP method and program path
    /// </summary>
    private (Permission RequiredPermission, string ProgramPath) GetPermissionRequirements()
    {
        var programPath = this.GetProgramPath() ?? this.ExtractProgramPathFromRoute();
        var httpMethod = this.HttpContext.Request.Method.ToUpperInvariant();

        var permission = httpMethod switch
        {
            "GET" => Permission.View,
            "POST" or "PUT" or "PATCH" or "DELETE" => Permission.Manage,
            _ => Permission.Manage, // Default to highest permission for unknown methods
        };

        return (permission, programPath);
    }

    /// <summary>
    /// Extracts program path from the configured endpoint route
    /// </summary>
    private string ExtractProgramPathFromRoute()
    {
        var routeTemplate = this.Definition.Routes?.FirstOrDefault();
        if (!string.IsNullOrEmpty(routeTemplate))
        {
            return routeTemplate;
        }

        var requestPath = this.HttpContext.Request.Path.Value;
        if (!string.IsNullOrEmpty(requestPath))
        {
            return requestPath;
        }

        throw new InvalidOperationException("Unable to determine program path for endpoint. Please override GetProgramPath() method.");
    }

    /// <summary>
    /// Validates permissions before handling the request
    /// </summary>
    public override async Task OnBeforeHandleAsync(EmptyRequest req, CancellationToken ct)
    {
        await base.OnBeforeHandleAsync(req, ct);

        var (requiredPermission, programPath) = this.GetPermissionRequirements();

        // Get user ID from claims
        var userId = this.GetUserIdFromClaims();
        if (userId == null)
        {
            this.ThrowError("Unable to identify user from authentication token");
            return;
        }

        // Validate user permissions
        var hasPermission = await this.permissionService.HasPermissionAsync(
            (UserId)userId, programPath, requiredPermission, ct);

        if (!hasPermission)
        {
            // Log the access attempt for security monitoring
            var logger = this.HttpContext.RequestServices.GetRequiredService<ILogger<SecureEndpointBase<TResponse>>>();
            logger.LogWarning(
                "ACCESS DENIED: User {UserId} attempted to access {Path} requiring {Permission}",
                userId.Value,
                programPath,
                requiredPermission);

            // Send 403 Forbidden response
            this.ThrowError("Access denied. Insufficient permissions.", 403);
            return;
        }
    }

    /// <summary>
    /// Helper method to get current user ID from JWT claims
    /// </summary>
    protected UserId? GetUserIdFromClaims()
    {
        var userIdClaim = this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userIdGuid))
        {
            return null;
        }

        return UserId.From(userIdGuid);
    }

    /// <summary>
    /// Convenience method to check if current user has specific permission for a path
    /// </summary>
    protected async Task<bool> CurrentUserHasPermissionAsync(string path, Permission permission, CancellationToken ct = default)
    {
        var userId = this.GetUserIdFromClaims();
        if (userId == null)
        {
            return false;
        }

        return await this.permissionService.HasPermissionAsync((UserId)userId, path, permission, ct);
    }

    /// <summary>
    /// Convenience method to get current user's permission for a path
    /// </summary>
    protected async Task<Permission> GetCurrentUserPermissionAsync(string path, CancellationToken ct = default)
    {
        var userId = this.GetUserIdFromClaims();
        if (userId == null)
        {
            return Permission.None;
        }

        return await this.permissionService.GetUserPermissionAsync((UserId)userId, path, ct);
    }
}
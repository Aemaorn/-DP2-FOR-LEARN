namespace GHB.DP2.Application.Middleware;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Security audit middleware that logs all access attempts for monitoring
/// Helps detect parameter tampering and unauthorized access attempts
/// </summary>
public class SecurityAuditMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<SecurityAuditMiddleware> logger;

    public SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Log access attempts to protected endpoints
            if (IsProtectedEndpoint(context.Request.Path))
            {
                var userId = GetUserIdFromContext(context);
                var userAgent = context.Request.Headers.UserAgent.ToString();
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                this.logger.LogInformation(
                    "API_ACCESS: User={UserId} Path={Path} Method={Method} IP={IP} UserAgent={UserAgent}",
                    userId ?? "Anonymous",
                    context.Request.Path,
                    context.Request.Method,
                    ipAddress,
                    userAgent);
            }

            // Process the request
            await this.next(context);

            // Log authorization failures (403 responses) - only if response hasn't been modified
            if (context.Response.StatusCode == 403 && !context.Response.HasStarted)
            {
                var userId = GetUserIdFromContext(context);

                this.logger.LogWarning(
                    "ACCESS_DENIED: User={UserId} attempted to access {Path} - returned 403 Forbidden",
                    userId ?? "Anonymous",
                    context.Request.Path);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error in SecurityAuditMiddleware for path {Path}", context.Request.Path);

            // Re-throw to let the framework handle it properly
            throw;
        }
    }

    private static bool IsProtectedEndpoint(PathString path)
    {
        // Define which paths should be audited
        var protectedPaths = new[]
        {
            "/st/st001",
            "/st/st005",
            "/st/st004",
            "/st/st008",
        };

        return protectedPaths.Any(protectedPath =>
            path.StartsWithSegments(protectedPath, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetUserIdFromContext(HttpContext context)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        return context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    }
}
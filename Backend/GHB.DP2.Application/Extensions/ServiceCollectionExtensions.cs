namespace GHB.DP2.Application.Extensions;

using GHB.DP2.Application.Services;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers security services for API access control
    /// Call this in Program.cs to enable permission validation
    /// </summary>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        // Register permission validation service
        services.AddScoped<IPermissionValidationService, PermissionValidationService>();

        return services;
    }
}
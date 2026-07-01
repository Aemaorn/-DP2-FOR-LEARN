#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;

using GHB.DP2.Application.JopService;

using Microsoft.Extensions.Hosting;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using FastEndpoints.Security;
using GHB.DP2.Infrastructure.Configurations;

#pragma warning restore IDE0130

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFastEndpointsService(this IServiceCollection services)
    {
        services.AddFastEndpoints();

        return services;
    }

    public static IServiceCollection AddAuthenticationService(this IServiceCollection services, JwtConfiguration configuration)
    {
        services.AddAuthenticationJwtBearer(s => s.SigningKey = configuration.Secret);
        services.AddAuthorization();

        services.AddSingleton(configuration);

        return services;
    }

    public static IServiceCollection AddHangfireService(
        this IServiceCollection services,
        string connectionString,
        IConfigurationSection recurringJobsSection)
    {
        services.AddHangfire(cfg =>
        {
            cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
               .UseSimpleAssemblyNameTypeSerializer()
               .UseRecommendedSerializerSettings()
               .UsePostgreSqlStorage(options =>
                   options.UseNpgsqlConnection(connectionString));
        });

        services.AddHangfireServer(option =>
        {
            option.ServerName = Environment.GetEnvironmentVariable("HG_SERVER_NAME")
                                ?? "DP2-HANGFIRE-SERVER";
            option.Queues = ["default"];

            option.WorkerCount = Math.Max(2, Environment.ProcessorCount);

            option.HeartbeatInterval = TimeSpan.FromSeconds(15);
            option.ServerCheckInterval = TimeSpan.FromSeconds(15);
            option.ShutdownTimeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<MaintenanceJobs>();
        services.AddSingleton<TimeZoneHelper>();
        services.AddSingleton<RecurringRegistrar>();
        services.Configure<List<RecurringJobDef>>(recurringJobsSection);

        return services;
    }

    public static void UseHangfireRecurringRegistrar(this IHost app)
    {
        var service = app.Services.GetRequiredService<RecurringRegistrar>();
        service.SyncAtStartup();
    }
}
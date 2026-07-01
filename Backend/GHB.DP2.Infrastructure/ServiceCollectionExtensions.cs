#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;

using System.Text;
using Codehard.FileService.Client;
using EFCoreSecondLevelCacheInterceptor;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Configurations;
using GHB.DP2.Infrastructure.Interceptors;
using GHB.DP2.Infrastructure.Services.ActiveDirectory;
using GHB.DP2.Infrastructure.Services.ChEditor;
using GHB.DP2.Infrastructure.Services.Coi;
using GHB.DP2.Infrastructure.Services.Dp1Api;
using GHB.DP2.Infrastructure.Services.Email;
using GHB.DP2.Infrastructure.Services.ErmEmployee;
using GHB.DP2.Infrastructure.Services.ExcelImportAndExport;
using GHB.DP2.Infrastructure.Services.Watchlist;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#pragma warning restore IDE0130

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDb1ContextService(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<Dp1DbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        return services;
    }

    public static IServiceCollection AddDbContextService(this IServiceCollection services, string connectionString)
    {
        // Prevent ArgumentOutOfRangeException when DB contains DateTime.MinValue in timestamp columns.
        // Npgsql v6+ maps 'timestamp without timezone' to UTC DateTimeOffset by default;
        // DateTime.MinValue + UTC+7 offset = year 0000 UTC which is invalid.
        // Legacy mode keeps the original DateTime value without UTC conversion.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        // Configure EF Second Level Cache (memory-based)
        services.AddEFSecondLevelCache(options =>
        {
            options.UseMemoryCacheProvider()
                   .ConfigureLogging(enable: false)
                   .UseCacheKeyPrefix("EF_")
                   .CacheAllQueries(CacheExpirationMode.Sliding, TimeSpan.FromSeconds(30));
        });

        services.AddScoped<AuditInfoInterceptor>();

        // Existing DbContext for write operations (scoped)
        services.AddDbContext<Dp2DbContext>((provider, options) =>
        {
            var interceptor = provider.GetRequiredService<AuditInfoInterceptor>();

            options.UseNpgsql(connectionString)
                   .AddInterceptors(interceptor, provider.GetRequiredService<SecondLevelCacheInterceptor>())
                   .UseLazyLoadingProxies();
        });

        // Factory for parallel read operations - use pooled factory with separate configuration
        services.AddPooledDbContextFactory<Dp2ReadOnlyDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString)
                   .AddInterceptors(provider.GetRequiredService<SecondLevelCacheInterceptor>())
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        return services;
    }

    public static IServiceCollection AddFileService(this IServiceCollection services, FileServiceConfiguration configuration)
    {
        services.AddFileServiceClient(options =>
        {
            options.SetHostAddress(configuration.ServiceUrl.AbsoluteUri);
            options.SetTenantCredential(
                configuration.TenantId,
                configuration.ApiKey);
        });

        return services;
    }

    public static IServiceCollection AddActiveDirectoryService(this IServiceCollection services, ActiveDirectoryConfiguration configuration)
    {
        services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>(provider => new ActiveDirectoryService(
            configuration.Server,
            configuration.Port,
            configuration.DomainName,
            provider.GetRequiredService<ILogger<ActiveDirectoryService>>()));

        return services;
    }

    public static IServiceCollection AddTestingActiveDirectoryService(this IServiceCollection service)
    {
        service.AddScoped<IActiveDirectoryService, TestingActiveDirectoryService>();

        return service;
    }

    public static IServiceCollection AddPentestActiveDirectoryService(this IServiceCollection service)
    {
        service.AddScoped<IActiveDirectoryService, PentestActiveDirectoryService>();

        return service;
    }

    public static IServiceCollection AddEmailConfigurationService(this IServiceCollection service, Action<IMailKitEmailServiceBuilder> configure)
    {
        var emailServiceBuilder = new MailKitEmailServiceBuilder();
        configure(emailServiceBuilder);

        emailServiceBuilder.Configuration.Validate();

        var configuration = emailServiceBuilder.Configuration;

        service.AddSingleton(configuration);

        return service;
    }

    public static IServiceCollection AddCoiService(this IServiceCollection services, CoiConfiguration configuration)
    {
        services.AddHttpClient(CoiConfiguration.Key, client =>
        {
            client.BaseAddress = new Uri(configuration.BaseUrl);
        });

        services.AddSingleton<ITokenProvider, CoiPasswordTokenProvider>(provider => new CoiPasswordTokenProvider(
            provider.GetRequiredService<IHttpClientFactory>(),
            configuration.Username,
            configuration.Password,
            configuration.TokenEndpoint));

        services.AddHttpClient<ICoiService, CoiService>(client =>
                {
                    client.BaseAddress = new Uri(configuration.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(180); // Set to 30 seconds
                })
                .AddHttpMessageHandler<AuthCoiHandler>();

        services.AddTransient<AuthCoiHandler>();

        services.AddSingleton(configuration);

        return services;
    }

    public static IServiceCollection AddWatchlistService(this IServiceCollection services, WatchlistConfiguration configuration)
    {
        services.AddSingleton(configuration);

        services.AddHttpClient<IWatchlistService, WatchlistService>(client =>
        {
            client.BaseAddress = new Uri(configuration.BaseUrl);
            client.DefaultRequestHeaders.Add("x-api-key", configuration.ApiKey);

            // Let the resilience pipeline own all timeouts (attempt + total request).
            client.Timeout = Timeout.InfiniteTimeSpan;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
        }).AddStandardResilienceHandler(options =>
        {
            // Retry transient failures (5xx, 408, network errors, timeouts) with exponential backoff + jitter.
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.UseJitter = true;

            // Per-attempt timeout; a single slow call is abandoned and retried.
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);

            // Overall budget across all retries.
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(100);

            // Must be >= 2 * AttemptTimeout.
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IServiceCollection AddErmEmployeeService(this IServiceCollection services, ErmEmployeeConfiguration configuration)
    {
        services.AddHttpClient(ErmEmployeeConfiguration.Key, client =>
        {
            client.BaseAddress = new Uri(configuration.BaseUrl);
        });

        services.AddSingleton<IErmEmployeeTokenProvider, ErmEmployeePasswordTokenProvider>(provider => new ErmEmployeePasswordTokenProvider(
            provider.GetRequiredService<IHttpClientFactory>(),
            configuration.Username,
            configuration.Password));

        services.AddHttpClient<IErmEmployeeService, ErmEmployeeService>(client =>
                {
                    client.BaseAddress = new Uri(configuration.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(180); // Set to 30 seconds
                })
                .AddHttpMessageHandler<AuthErmEmployeeHandler>();

        services.AddTransient<AuthErmEmployeeHandler>();

        services.AddSingleton(configuration);

        return services;
    }

    public static IServiceCollection AddChEditorService(this IServiceCollection services, ChEditorConfiguration configuration)
    {
        services.AddHttpClient<IChEditorService, ChEditorService>(client =>
        {
            client.BaseAddress = new Uri(configuration.BaseUrl);
        });

        return services;
    }

    public static T GetConfiguration<T>(this IConfiguration configuration)
        where T : IServiceConfiguration
    {
        var sectionKey = T.Key;

        return configuration.GetSection(sectionKey)
                            .Get<T>()!;
    }

    /// <summary>
    /// Adds Excel import service to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExcelImportAndExportService(this IServiceCollection services)
    {
        // Register encoding provider required for ExcelDataReader with .NET Core
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Register the Excel import service
        services.AddScoped<IExcelImportService, ExcelImportService>();

        // Register the Excel export service
        services.AddScoped<IExcelExportService, ExcelExportService>();

        return services;
    }

    public static IServiceCollection AddDp1ApiService(this IServiceCollection services, Dp1ApiConfiguration configuration)
    {
        services.AddHttpClient<IDp1ApiService, Dp1ApiService>(client =>
        {
            client.BaseAddress = new Uri(configuration.GhbDp1);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
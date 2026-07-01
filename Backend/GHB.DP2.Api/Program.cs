using System.Text.Json;
using System.Text.Json.Serialization;
using Codehard.Common.JsonConverters;
using FastEndpoints;
using FastEndpoints.Swagger;
using GHB.DP2.Api.Configurations;
using GHB.DP2.Api.Middlewares;
using GHB.DP2.Application;
using GHB.DP2.Application.Services.Pdf;
using GHB.DP2.Application.Services.Token;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Middleware;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Configurations;
using Hangfire;
using HangfireBasicAuthenticationFilter;
using Microsoft.AspNetCore.Http.Json;
using System.Threading.RateLimiting;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var services = builder.Services;
var configuration = builder.Configuration;
var environment = builder.Environment;

Log.Logger = new LoggerConfiguration()
             .ReadFrom.Configuration(configuration)
             .CreateLogger();

builder.Host.UseSerilog();
services.AddControllers();

services.AddEmailConfigurationService(config =>
{
    var configBuilder = configuration.GetConfiguration<EmailServiceConfiguration>();

    config.SetHost(configBuilder.Host);
    config.SetPort(configBuilder.Port);
    config.SetUsername(configBuilder.Username);
    config.SetPassword(configBuilder.Password);
    config.SetFromMail(configBuilder.FromMail);
    config.SetDisplayName(configBuilder.DisplayName);
    config.SetEnableSsl(configBuilder.EnableSsl);
});

services.AddFastEndpointsService();

services.RegisterServicesFromGHBDP2Application()
        .RegisterServicesFromGHBDP2Infrastructure()
        .AddDb1ContextService(configuration.GetConnectionString("GHBDP1")!)
        .AddDbContextService(configuration.GetConnectionString("DefaultConnection")!)
        .AddFileService(
            configuration.GetConfiguration<FileServiceConfiguration>())
        .AddAuthenticationService(
            configuration.GetConfiguration<JwtConfiguration>())
        .AddSecurityServices()  // Add security services for parameter tampering protection
        .SwaggerDocument(o =>
        {
            o.AutoTagPathSegmentIndex = 0;
            o.UsePropertyNamingPolicy = false;
        })
        .AddOpenApi()
        .AddCors();

services.AddSingleton(
    configuration.GetSection(LoggingOptionsConfiguration.SectionName).Get<LoggingOptionsConfiguration>()!);

services.AddSingleton(configuration.GetConfiguration<EmailBaseUrlConfiguration>()!);

services.Configure<JsonOptions>(option =>
{
    option.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    option.SerializerOptions.Converters.Add(new DateOnlyJsonConverter());
    option.SerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
    option.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    option.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    option.SerializerOptions.WriteIndented = true;
});

var activeDirectoryConfig = configuration.GetConfiguration<ActiveDirectoryConfiguration>();

// Both stand-in validators are gated behind ASPNETCORE_ENVIRONMENT=Development so a
// stray flag on staging/production cannot turn into an open bypass.
//   PentestMode        → strict 2-account whitelist (wins over UseTestingService)
//   UseTestingService  → permissive: any username/password passes
// Anything else → real Active Directory.
if (environment.IsDevelopment() && activeDirectoryConfig.PentestMode)
{
    services.AddPentestActiveDirectoryService();
}
else if (environment.IsDevelopment() && activeDirectoryConfig.UseTestingService)
{
    services.AddTestingActiveDirectoryService();
}
else
{
    services.AddActiveDirectoryService(activeDirectoryConfig);
}

services.AddScoped<LogContextEnrichMiddleware>();
services.AddSingleton<IPdfService, PdfService>();
services.AddExcelImportAndExportService();
services.AddMemoryCache();

services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/api/user/signin", StringComparison.OrdinalIgnoreCase))
        {
            var ip = httpContext.TryGetIpAddress()?.ToString()
                     ?? httpContext.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ip,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                });
        }

        return RateLimitPartition.GetNoLimiter<string>(string.Empty);
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        var ip = context.HttpContext.TryGetIpAddress()?.ToString()
                 ?? context.HttpContext.Connection.RemoteIpAddress?.ToString()
                 ?? string.Empty;

        await new GHB.DP2.Application.EventHandlers.SuAuditLog.SaveAuditLogEvent(
                $"เข้าสู่ระบบล้มเหลว - rate limit exceeded: {ip}",
                "เข้าสู่ระบบ",
                LanguageExt.Option<Guid>.None,
                ip)
            .PublishAsync(FastEndpoints.Mode.WaitForNone, cancellation: cancellationToken);
    };
});

services.AddScoped<IJwtBlacklistService, JwtBlacklistService>();
services.AddScoped<ITokenCacheService, TokenCacheService>();

services.AddChEditorService(configuration.GetConfiguration<ChEditorConfiguration>());
services.AddCoiService(configuration.GetConfiguration<CoiConfiguration>());
services.AddWatchlistService(configuration.GetConfiguration<WatchlistConfiguration>());
services.AddErmEmployeeService(configuration.GetConfiguration<ErmEmployeeConfiguration>());
services.AddDp1ApiService(configuration.GetConfiguration<Dp1ApiConfiguration>());

services.AddHangfireService(
    configuration.GetConnectionString("DefaultConnection")!,
    configuration.GetSection("Hangfire:RecurringJobs"));

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwaggerGen();

    app.UseCors(e =>
    {
        e.AllowAnyHeader();
        e.AllowAnyOrigin();
        e.AllowAnyMethod();
    });
}
else
{
    var corsConfig = configuration.GetConfiguration<CorsConfiguration>();

    app.UseCors(policy =>
    {
        policy.WithOrigins(corsConfig.AllowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetPreflightMaxAge(TimeSpan.FromSeconds(corsConfig.MaxAge));
    });
}

var hangfireUser = app.Configuration["Hangfire:Dashboard:Username"];
var hangfirePassword = app.Configuration["Hangfire:Dashboard:Password"];

if (hangfireUser != null && hangfirePassword != null)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization =
        [
            new HangfireCustomBasicAuthenticationFilter
            {
                User = hangfireUser,
                Pass = hangfirePassword,
            }
        ],
    });
}

app.UseAuthentication();
app.UseMiddleware<JwtBlacklistMiddleware>();
app.UseAuthorization();

// Add security audit middleware for monitoring parameter tampering attempts
app.UseMiddleware<SecurityAuditMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<LogContextEnrichMiddleware>();
app.UseDefaultExceptionHandler();
app.MapControllers();
app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = FastEndpointsConstant.DefaultRoute;
    config.Errors.UseProblemDetails();
});

app.UseHangfireRecurringRegistrar();

#if DEBUG
{
    // See https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/Diagnostics.EntityFrameworkCore/src/MigrationsEndPointMiddleware.cs
    // to understand what format the migrations endpoint expects.
    app.UseMigrationsEndPoint(new MigrationsEndPointOptions
    {
        Path = "/migrations",
    });
}
# else
{
    await app.ApplyMigrationsAsync<Dp2DbContext>();
}
#endif

await app.RunAsync();
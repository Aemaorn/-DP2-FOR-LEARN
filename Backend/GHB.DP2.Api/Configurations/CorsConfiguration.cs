using GHB.DP2.Infrastructure.Configurations;

namespace GHB.DP2.Api.Configurations;

public record CorsConfiguration(
    string[] AllowedOrigins,
    int MaxAge) : IServiceConfiguration
{
    public static string Key => "Cors";
}
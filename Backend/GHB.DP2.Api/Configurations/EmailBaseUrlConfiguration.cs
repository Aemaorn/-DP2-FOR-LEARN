namespace GHB.DP2.Api.Configurations;

using GHB.DP2.Infrastructure.Configurations;

public record EmailBaseUrlConfiguration(
    string? BaseUrl)
    : IServiceConfiguration
{
    public static string Key => "EmailBaseUrl";
}

namespace GHB.DP2.Infrastructure.Configurations;

public record Dp1ApiConfiguration(
    string GhbDp1) : IServiceConfiguration
{
    public static string Key => "API";
}
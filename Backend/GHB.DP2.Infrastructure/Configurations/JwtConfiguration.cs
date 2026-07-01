namespace GHB.DP2.Infrastructure.Configurations;

public record JwtConfiguration(
    string Secret,
    uint AccessTokenValidity,
    uint RefreshTokenValidity) : IServiceConfiguration
{
    public static string Key => "Jwt";
}
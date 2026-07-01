namespace GHB.DP2.Infrastructure.Configurations;

public record CoiConfiguration(
    string BaseUrl,
    string Username,
    string Password,
    string TokenEndpoint,
    string GetAllEndpoint,
    string GetBySsnEndpoint,
    string GetByNameEndpoint,
    string GetByNameSsnEndpoint) : IServiceConfiguration
{
    public static string Key => "Coi";
}
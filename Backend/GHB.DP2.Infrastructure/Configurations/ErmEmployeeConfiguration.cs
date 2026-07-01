namespace GHB.DP2.Infrastructure.Configurations;

public record ErmEmployeeConfiguration(
    string BaseUrl,
    string Username,
    string Password) : IServiceConfiguration
{
    public static string Key => "ErmEmployee";
}
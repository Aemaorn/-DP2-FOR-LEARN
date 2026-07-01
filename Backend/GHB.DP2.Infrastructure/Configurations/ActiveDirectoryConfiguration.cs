namespace GHB.DP2.Infrastructure.Configurations;

public record ActiveDirectoryConfiguration(
    string Server,
    int Port,
    string DomainName,
    string BaseDn,
    bool UseTestingService,
    bool PentestMode = false) : IServiceConfiguration
{
    public static string Key => "ActiveDirectory";
}
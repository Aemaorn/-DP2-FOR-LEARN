namespace GHB.DP2.Infrastructure.Configurations;

public record FileServiceConfiguration(
    Uri ServiceUrl,
    string TenantId,
    string ApiKey) : IServiceConfiguration
{
    public static string Key => "FileService";
}
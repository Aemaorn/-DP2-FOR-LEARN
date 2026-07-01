namespace GHB.DP2.Infrastructure.Configurations;

public record ChEditorConfiguration(
    string BaseUrl) : IServiceConfiguration
{
    public static string Key => "ChEditor";
}
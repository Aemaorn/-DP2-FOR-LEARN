namespace GHB.DP2.Api.Configurations;

public sealed class LoggingOptionsConfiguration
{
    public const string SectionName = "LoggingOptions";

    public bool EnableAdvancedRequestLogging { get; set; }
}
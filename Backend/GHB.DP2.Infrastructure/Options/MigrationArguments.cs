namespace GHB.DP2.Infrastructure.Options;

using CommandLine;

public class MigrationArguments
{
    [Option('c', "connectionstring", Required = true, HelpText = "Connection string")]
    public string ConnectionString { get; set; } = string.Empty;
}
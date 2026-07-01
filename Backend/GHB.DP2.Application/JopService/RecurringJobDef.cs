namespace GHB.DP2.Application.JopService;

public class RecurringJobDef
{
    public string Id { get; init; }

    public string Method { get; init; }

    public string Cron { get; init; }

    public string? Queue { get; init; }

    public bool Enabled { get; init; }

    public object[]? Args { get; init; } // optional
}
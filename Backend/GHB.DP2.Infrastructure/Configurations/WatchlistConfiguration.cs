namespace GHB.DP2.Infrastructure.Configurations;

public record WatchlistConfiguration(
    string BaseUrl,
    string ApiKey,
    string SearchEndpoint) : IServiceConfiguration
{
    public static string Key => "Watchlist";
}
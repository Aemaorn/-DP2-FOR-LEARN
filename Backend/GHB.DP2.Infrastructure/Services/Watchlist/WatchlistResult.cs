namespace GHB.DP2.Infrastructure.Services.Watchlist;

using System.Text.Json.Serialization;

public class WatchlistResult
{
    [JsonPropertyName("message")]
    public string Message { get; init; }

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("data")]
    public IEnumerable<WatchlistInfo> Data { get; init; }
}

public class WatchlistDetail
{
    [JsonPropertyName("code")]
    public string Code { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; }
}

public class WatchlistInfo
{
    [JsonPropertyName("is_juristic")]
    public bool IsJuristic { get; init; }

    [JsonPropertyName("id_number")]
    public string IdNumber { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; }

    [JsonPropertyName("watchlist_found")]
    public bool WatchlistFound { get; init; }

    [JsonPropertyName("details")]
    public IEnumerable<WatchlistDetail>? Details { get; init; }
}
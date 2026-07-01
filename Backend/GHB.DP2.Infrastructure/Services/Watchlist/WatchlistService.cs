namespace GHB.DP2.Infrastructure.Services.Watchlist;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GHB.DP2.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;

public interface IWatchlistService
{
    Task<IEnumerable<WatchlistInfo>> SearchWatchlistAsync(
        bool isJuristic,
        string firstName,
        string lastName,
        string idNumber = "",
        CancellationToken cancellationToken = default);
}

public class WatchlistService : IWatchlistService
{
    private readonly HttpClient httpClient;
    private readonly WatchlistConfiguration configuration;
    private readonly ILogger<WatchlistService> logger;

    public WatchlistService(
        HttpClient httpClient,
        WatchlistConfiguration configuration,
        ILogger<WatchlistService> logger)
    {
        this.httpClient = httpClient;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task<IEnumerable<WatchlistInfo>> SearchWatchlistAsync(
        bool isJuristic,
        string firstName,
        string lastName,
        string idNumber = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new Dictionary<string, object>
            {
                { "is_juristic", isJuristic },
                { "first_name", firstName },
                { "last_name", lastName },
                { "id_number", idNumber },
            };

            var contentJson = JsonContent.Create(body);

            var response = await this.httpClient.PostAsync(
                this.configuration.SearchEndpoint,
                contentJson,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<WatchlistResult>(cancellationToken);

                // HTTP 2xx is treated as a successful call: return whatever data the body carries
                // (empty data => "not found"). A non-OK status inside the body is only logged.
                // Real failures are surfaced by the Watchlist system as HTTP error statuses,
                // which are handled in the !IsSuccessStatusCode branch below.
                if (result?.Status != (int)HttpStatusCode.OK)
                {
                    this.logger.LogError(
                        "Watchlist API returned non-OK status: {Status}, Message: {Message}",
                        result?.Status,
                        result?.Message);
                }

                return result?.Data ?? [];
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            this.logger.LogError(
                "Watchlist API returned error: {StatusCode} - {Content}",
                response.StatusCode,
                errorContent);

            // The Watchlist system may report the error as a real HTTP error status (non-2xx) while
            // still returning the standard { message, status, data } envelope in the body. Pull the
            // business message out so the caller can show the real reason instead of a generic one.
            throw new WatchlistException(
                $"API call failed with status {response.StatusCode}: {errorContent}",
                apiMessage: TryReadApiMessage(errorContent));
        }
        catch (HttpRequestException ex)
        {
            this.logger.LogError(ex, "HTTP request failed for watchlist search");

            throw new WatchlistException("Network error occurred while calling watchlist API", ex);
        }
        catch (TaskCanceledException ex)
        {
            this.logger.LogError(ex, "Watchlist API request timed out");

            throw new WatchlistException("Request timed out while calling watchlist API", ex);
        }
        catch (Exception ex) when (!(ex is WatchlistException))
        {
            this.logger.LogError(ex, "Unexpected error occurred during watchlist search");

            throw new WatchlistException("Unexpected error occurred while calling watchlist API", ex);
        }
    }

    /// <summary>
    /// Best-effort extraction of the business-level message from a Watchlist error body that
    /// follows the standard { message, status, data } envelope. Returns null when the body is
    /// empty or not in that shape (e.g. an HTML error page from a gateway).
    /// </summary>
    private static string? TryReadApiMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<WatchlistResult>(content)?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
namespace GHB.DP2.Infrastructure.Services.Coi;

using System.Text.Json;
using GHB.DP2.Infrastructure.Configurations;

public interface ITokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken);

    void Invalidate();
}

public class CoiPasswordTokenProvider : ITokenProvider
{
    private readonly HttpClient httpClient;
    private readonly string username;
    private readonly string password;
    private readonly string tokenEndpoint;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    private string? accessToken;
    private DateTimeOffset expiration;

    public CoiPasswordTokenProvider(
        IHttpClientFactory httpClientFactory,
        string username,
        string password,
        string tokenEndpoint)
    {
        this.username = username;
        this.password = password;
        this.tokenEndpoint = tokenEndpoint;
        this.httpClient = httpClientFactory.CreateClient(CoiConfiguration.Key);
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(this.accessToken) && DateTimeOffset.UtcNow < this.expiration)
        {
            return this.accessToken;
        }

        await this.semaphore.WaitAsync(cancellationToken);

        try
        {
            if (!string.IsNullOrEmpty(this.accessToken) && DateTimeOffset.UtcNow < this.expiration)
            {
                return this.accessToken;
            }

            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", this.username },
                { "password", this.password },
            };

            var response = await this.httpClient.PostAsync(
                this.tokenEndpoint,
                new FormUrlEncodedContent(parameters),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new CoiException("Failed to retrieve access token.");
            }

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDocument = JsonDocument.Parse(jsonString);

            if (!jsonDocument.RootElement.TryGetProperty("access_token", out var accessTokenElement) ||
                !jsonDocument.RootElement.TryGetProperty("expires_in", out var expiresInElement))
            {
                throw new CoiException("Invalid token response.");
            }

            var token = accessTokenElement.GetString() ?? throw new CoiException("Access token is null.");
            var expiresIn = expiresInElement.GetInt32();

            this.accessToken = token;
            this.expiration = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60); // Refresh 1 minute before expiration

            return token;
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public void Invalidate()
    {
        this.accessToken = null;
        this.expiration = DateTimeOffset.MinValue;
    }
}
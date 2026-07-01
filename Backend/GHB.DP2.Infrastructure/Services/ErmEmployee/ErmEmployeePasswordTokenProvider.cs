namespace GHB.DP2.Infrastructure.Services.ErmEmployee;

using System.Text.Json;
using GHB.DP2.Infrastructure.Configurations;
using GHB.DP2.Infrastructure.Services.Coi;

public interface IErmEmployeeTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
}

public class ErmEmployeePasswordTokenProvider : IErmEmployeeTokenProvider
{
    private readonly HttpClient httpClient;
    private readonly string username;
    private readonly string password;

    public ErmEmployeePasswordTokenProvider(
        IHttpClientFactory httpClientFactory,
        string username,
        string password)
    {
        this.username = username;
        this.password = password;
        this.httpClient = httpClientFactory.CreateClient(ErmEmployeeConfiguration.Key);
    }

    private string? accessToken;
    private DateTimeOffset expiration;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
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
            "GHBHRInternalAPI/token",
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

        this.accessToken = accessTokenElement.GetString() ?? throw new CoiException("Access token is null.");
        var expiresIn = expiresInElement.GetInt32();
        this.expiration = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60); // Refresh 1 minute before expiration

        return this.accessToken;
    }
}
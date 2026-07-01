namespace GHB.DP2.Infrastructure.Services.Dp1Api;

using System.Text.Json;

public class Dp1ApiService : IDp1ApiService
{
    private readonly HttpClient httpClient;

    public Dp1ApiService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<string> PostLoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
            });

            var response = await this.httpClient.PostAsync(
                "/account/signin",
                formData,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                try
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (jsonResponse.TryGetProperty("accessToken", out var tokenProperty))
                    {
                        return tokenProperty.GetString() ?? string.Empty;
                    }

                    if (jsonResponse.TryGetProperty("access_token", out var tokenProperty2))
                    {
                        return tokenProperty2.GetString() ?? string.Empty;
                    }
                }
                catch
                {
                    // If JSON parsing fails, return the raw response
                    return responseContent;
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
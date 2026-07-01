namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using GHB.DP2.Application.Services.Token;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

public record GetTokenCacheCommand(string UserName);

public class GetTokenCache : Endpoint<GetTokenCacheCommand, TokenCacheResponse>
{
    private readonly ITokenCacheService tokenCacheService;

    public GetTokenCache(ITokenCacheService tokenCacheService)
    {
        this.tokenCacheService = tokenCacheService;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("SuUser"));
        this.Get("/user/token-cache/{userName}");
        this.AllowAnonymous();
    }

    public override async Task HandleAsync(GetTokenCacheCommand req, CancellationToken ct)
    {
        var tokenJson = await this.tokenCacheService.GetTokenAsync(req.UserName, ct);

        if (string.IsNullOrEmpty(tokenJson))
        {
            this.Response = new TokenCacheResponse(null, null);

            return;
        }

        var tokenObj = JsonSerializer.Deserialize<TokenObject>(tokenJson);
        this.Response = new TokenCacheResponse(tokenObj?.AccessToken, tokenObj?.RefreshToken);
    }
}

public record TokenObject(
    [property: JsonPropertyName("access_token")]
    string? AccessToken,
    [property: JsonPropertyName("refresh_token")]
    string? RefreshToken);

public record TokenCacheResponse(string? AccessToken, string? RefreshToken);
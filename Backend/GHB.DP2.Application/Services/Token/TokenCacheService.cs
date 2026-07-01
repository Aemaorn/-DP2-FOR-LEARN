namespace GHB.DP2.Application.Services.Token;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public class TokenCacheService(
    IMemoryCache memoryCache,
    ILogger<TokenCacheService> logger) : ITokenCacheService
{
    private const string TokenCacheKeyPrefix = "access_token_";

    public Task SetTokenAsync(string username, string accessToken, DateTimeOffset expiry, CancellationToken ct = default)
    {
        var key = TokenCacheKeyPrefix + username.ToLowerInvariant();
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiry,
        };

        memoryCache.Set(key, accessToken, options);
        logger.LogInformation("Access token cached for user {Username}", username);

        return Task.CompletedTask;
    }

    public Task<string?> GetTokenAsync(string username, CancellationToken ct = default)
    {
        var key = TokenCacheKeyPrefix + username.ToLowerInvariant();
        var token = memoryCache.TryGetValue(key, out var cachedToken) ? cachedToken?.ToString() : null;

        if (token != null)
        {
            logger.LogDebug("Access token retrieved from cache for user {Username}", username);
        }

        return Task.FromResult(token);
    }

    public Task DeleteTokenAsync(string username, CancellationToken ct = default)
    {
        var key = TokenCacheKeyPrefix + username.ToLowerInvariant();
        memoryCache.Remove(key);
        logger.LogInformation("Access token removed from cache for user {Username}", username);

        return Task.CompletedTask;
    }

    public Task<bool> HasValidTokenAsync(string username, CancellationToken ct = default)
    {
        var key = TokenCacheKeyPrefix + username.ToLowerInvariant();
        var hasToken = memoryCache.TryGetValue(key, out _);

        return Task.FromResult(hasToken);
    }
}
namespace GHB.DP2.Application.Services.Token;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public class JwtBlacklistService(
    IMemoryCache memoryCache,
    ILogger<JwtBlacklistService> logger) : IJwtBlacklistService
{
    private const string BlacklistKeyPrefix = "jwt_blacklist_";

    public Task AddToBlacklistAsync(string jti, DateTimeOffset expiry, CancellationToken ct = default)
    {
        var key = BlacklistKeyPrefix + jti;
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiry,
        };

        memoryCache.Set(key, true, options);
        logger.LogInformation("JWT with JTI {Jti} has been blacklisted", jti);

        return Task.CompletedTask;
    }

    public Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default)
    {
        var key = BlacklistKeyPrefix + jti;
        var isBlacklisted = memoryCache.TryGetValue(key, out _);

        return Task.FromResult(isBlacklisted);
    }

    public Task CleanupExpiredTokensAsync(CancellationToken ct = default)
    {
        // Memory cache automatically removes expired entries
        logger.LogInformation("Memory cache automatically handles expired token cleanup");
        return Task.CompletedTask;
    }
}
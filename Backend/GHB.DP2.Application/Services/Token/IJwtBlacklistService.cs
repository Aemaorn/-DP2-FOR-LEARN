namespace GHB.DP2.Application.Services.Token;

public interface IJwtBlacklistService
{
    Task AddToBlacklistAsync(string jti, DateTimeOffset expiry, CancellationToken ct = default);

    Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default);

    Task CleanupExpiredTokensAsync(CancellationToken ct = default);
}
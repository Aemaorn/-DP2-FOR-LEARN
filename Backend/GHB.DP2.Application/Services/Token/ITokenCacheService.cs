namespace GHB.DP2.Application.Services.Token;

public interface ITokenCacheService
{
    Task SetTokenAsync(string username, string accessToken, DateTimeOffset expiry, CancellationToken ct = default);

    Task<string?> GetTokenAsync(string username, CancellationToken ct = default);

    Task DeleteTokenAsync(string username, CancellationToken ct = default);

    Task<bool> HasValidTokenAsync(string username, CancellationToken ct = default);
}
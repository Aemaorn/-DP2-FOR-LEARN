namespace GHB.DP2.Infrastructure.Services.Dp1Api;

public interface IDp1ApiService
{
    Task<string> PostLoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
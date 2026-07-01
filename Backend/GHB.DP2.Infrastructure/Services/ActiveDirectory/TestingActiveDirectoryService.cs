namespace GHB.DP2.Infrastructure.Services.ActiveDirectory;

// Permissive validator used only when ASPNETCORE_ENVIRONMENT=Development and the
// UseTestingService flag is on (gated in Program.cs). Any username/password is accepted
// so devs can sign in as any seeded employee — actual user lookup still happens against
// the database before this validator is called. For the strict 2-account pentest setup
// see PentestActiveDirectoryService.
public class TestingActiveDirectoryService : IActiveDirectoryService
{
    public Task<bool> ValidateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task PerformDummyValidationAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
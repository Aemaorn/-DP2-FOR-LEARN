namespace GHB.DP2.Infrastructure.Services.ActiveDirectory;

using LanguageExt;
using LanguageExt.Common;
using LdapForNet;
using LdapForNet.Native;
using Microsoft.Extensions.Logging;

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly string server;
    private readonly int port;
    private readonly string domainName;

    private readonly ILogger logger;

    public ActiveDirectoryService(
        string server,
        int port,
        string domainName,
        ILogger<ActiveDirectoryService> logger)
    {
        this.server = server;
        this.port = port;
        this.domainName = domainName;
        this.logger = logger;
    }

    public async Task<bool> ValidateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var usernameLdap = username.Contains("@") ? username : $"{username}@{this.domainName}";

        using var connection = new LdapConnection();

        var isLDAPS = this.port == (int)Native.LdapPort.LDAPS;

        connection.Connect(
            this.server,
            this.port,
            isLDAPS ? Native.LdapSchema.LDAPS : Native.LdapSchema.LDAP);

        if (isLDAPS)
        {
            connection.TrustAllCertificates();
        }

        var resultFin = await
            connection.BindAsync(
                          Native.LdapAuthType.Simple,
                          new LdapCredential
                          {
                              UserName = usernameLdap,
                              Password = password,
                          })
                      .ToUnit()
                      .ToAff()
                      .Run();

        return resultFin.Match(Success, Failure);

        bool Success(Unit unit)
        {
            this.logger.LogInformation("Active Directory authentication successful.");

            return true;
        }

        bool Failure(Error error)
        {
            this.logger.LogError("Active Directory authentication failed: {Error}", error);

            return false;
        }
    }

    public async Task PerformDummyValidationAsync(CancellationToken cancellationToken = default)
    {
        await this.ValidateAsync(
            $"nonexistent-{Guid.NewGuid():N}@{this.domainName}",
            Guid.NewGuid().ToString("N"),
            cancellationToken);
    }
}
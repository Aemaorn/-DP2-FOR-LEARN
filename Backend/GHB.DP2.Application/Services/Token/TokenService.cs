namespace GHB.DP2.Application.Services.Token;

using FastEndpoints.Security;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class TokenService : RefreshTokenService<TokenRequest, TokenResponse>
{
    private readonly Dp2DbContext dbContext;
    private readonly ILogger logger;

    public TokenService(JwtConfiguration config, Dp2DbContext dbContext, ILogger<TokenService> logger)
    {
        this.Setup(o =>
        {
            o.TokenSigningKey = config.Secret;

            o.AccessTokenValidity = TimeSpan.FromMilliseconds(config.AccessTokenValidity);
            o.RefreshTokenValidity = TimeSpan.FromMilliseconds(config.RefreshTokenValidity);

            o.Endpoint("/user/refresh", ep =>
            {
                ep.Options(b => b.WithTags("SuUser"));
                ep.Summary(s => s.Summary = "Refresh token endpoint");
            });
        });

        this.dbContext = dbContext;
        this.logger = logger;
    }

    public override async Task PersistTokenAsync(TokenResponse response)
    {
        var userId = Guid.Parse(response.UserId);

        var user = await this.dbContext.SuUsers
                             .SingleOrDefaultAsync(
                                 u => u.Id == UserId.From(userId),
                                 CancellationToken.None);

        if (user is null)
        {
            this.AddError("User not found");

            return;
        }

        user.AddRefreshToken(
            RefreshToken.Create(
                response.RefreshToken,
                response.RefreshExpiry));

        await this.dbContext.SaveChangesAsync(CancellationToken.None);
    }

    public override async Task RefreshRequestValidationAsync(TokenRequest req)
    {
        var userId = Guid.Parse(req.UserId);

        var user = await this.dbContext.SuUsers
                             .Include(u => u.RefreshTokens)
                             .SingleOrDefaultAsync(
                                 u => u.Id == UserId.From(userId),
                                 CancellationToken.None);

        if (user is null)
        {
            this.AddError("User not found");

            return;
        }

        var refreshToken = user.RefreshTokens
                               .FirstOrDefault(rt => rt.Token == req.RefreshToken);

        if (refreshToken is null)
        {
            this.AddError("Refresh token not found");

            return;
        }

        if (refreshToken.Expires < DateTimeOffset.UtcNow)
        {
            user.RemoveRefreshToken(req.RefreshToken);
            await this.dbContext.SaveChangesAsync(CancellationToken.None);

            this.AddError("Refresh token expired");

            return;
        }

        user.RemoveRefreshToken(req.RefreshToken);

        try
        {
            await this.dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // It's OK to have this exception
            this.logger.LogInformation("Refresh token already deleted! {Message}", ex.Message);
        }
    }

    public override async Task SetRenewalPrivilegesAsync(TokenRequest request, UserPrivileges privileges)
    {
        var userId = Guid.Parse(request.UserId);

        var user =
            await this.dbContext.SuUsers
                      .AsNoTracking()
                      .Include(suUser => suUser.Employee)
                      .SingleOrDefaultAsync(u => u.Id == UserId.From(userId));

        if (user is null)
        {
            this.AddError("User not found");

            return;
        }

        privileges.SetClaims(user.Id.Value, user.Employee.FullName);
    }
}
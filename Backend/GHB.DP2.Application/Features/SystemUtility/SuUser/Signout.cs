namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using FastEndpoints;
using GHB.DP2.Application.EventHandlers.SuAuditLog;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services.Token;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class SignOut(
    Dp2DbContext dbContext,
    IJwtBlacklistService jwtBlacklistService) : EndpointWithoutRequest
{
    public override void Configure()
    {
        this.Options(b => b.WithTags("SuUser"));
        this.Post("/user/signout");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var authorizationHeader = this.HttpContext.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
        {
            await this.SendResultAsync(TypedResults.BadRequest("Invalid authorization header"));
            return;
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            var jti = jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var exp = jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)?.Value;

            if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(exp))
            {
                await this.SendResultAsync(TypedResults.BadRequest("Invalid token structure"));
                return;
            }

            var expiryTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp));

            await jwtBlacklistService.AddToBlacklistAsync(jti, expiryTime, ct);

            var userIdClaim = this.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var user = await dbContext.SuUsers
                    .AsNoTracking()
                    .Include(u => u.RefreshTokens)
                    .SingleOrDefaultAsync(u => u.Id == UserId.From(userId), ct);

                if (user is not null)
                {
                    foreach (var refreshToken in user.RefreshTokens.ToList())
                    {
                        user.RemoveRefreshToken(refreshToken.Token);
                    }

                    await dbContext.SaveChangesAsync(ct);

                    var ipAddress = this.HttpContext.TryGetIpAddress();

                    await new SaveAuditLogEvent(
                            "Signout",
                            "Authentication",
                            userId,
                            ipAddress?.ToString() ?? string.Empty)
                        .PublishAsync(Mode.WaitForNone, cancellation: ct);
                }
            }

            await this.SendOkAsync(ct);
        }
        catch (Exception)
        {
            await this.SendResultAsync(TypedResults.BadRequest("Invalid token"));
        }
    }
}
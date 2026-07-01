namespace GHB.DP2.Api.Middlewares;

using GHB.DP2.Application.Services.Token;
using System.IdentityModel.Tokens.Jwt;

public class JwtBlacklistMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var authorizationHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
        {
            var token = authorizationHeader["Bearer ".Length..].Trim();

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);

                var jti = jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti))
                {
                    var jwtBlacklistService = context.RequestServices.GetRequiredService<IJwtBlacklistService>();
                    var isBlacklisted = await jwtBlacklistService.IsBlacklistedAsync(jti, context.RequestAborted);

                    if (isBlacklisted)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Token has been revoked");
                        return;
                    }
                }
            }
            catch
            {
                // If token is invalid, let the authentication middleware handle it
            }
        }

        await next(context);
    }
}
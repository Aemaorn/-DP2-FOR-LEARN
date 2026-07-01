namespace GHB.DP2.Application.Extensions;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public static class UserPrivilegesExtension
{
    public static void SetClaims(this UserPrivileges privileges, Guid userId, string fullName)
    {
        privileges.SetClaims(
            new Claim(
                JwtRegisteredClaimNames.Sub,
                userId.ToString()),
            new Claim(
                JwtRegisteredClaimNames.Name,
                fullName),
            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()));
    }

    public static void SetClaims(this UserPrivileges privileges, params IEnumerable<Claim> claims)
        => claims.Iter(privileges.Claims.Add);
}
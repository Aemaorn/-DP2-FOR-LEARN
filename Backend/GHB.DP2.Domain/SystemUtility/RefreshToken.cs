namespace GHB.DP2.Domain.SystemUtility;

using Codehard.Common.DomainModel;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter)]
public partial struct RefreshTokenId
{
    public static RefreshTokenId New() => From(Guid.CreateVersion7());
}

public class RefreshToken : Entity<RefreshTokenId>
{
    public override RefreshTokenId Id { get; init; }

    public string Token { get; init; }

    public virtual SuUser User { get; init; }

    public DateTimeOffset Expires { get; init; }

    public static RefreshToken Create(string token, DateTimeOffset expires)
    {
        return new RefreshToken
        {
            Id = RefreshTokenId.New(),
            Token = token,
            Expires = expires,
        };
    }
}
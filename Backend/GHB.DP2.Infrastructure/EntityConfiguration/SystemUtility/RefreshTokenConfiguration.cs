namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RefreshTokenConfiguration : EntityTypeConfigurationBase<RefreshToken, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(nameof(RefreshToken), nameof(SystemUtility));

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id)
               .HasVogenConversion();

        builder.Property(rt => rt.Token)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(rt => rt.Expires)
               .IsRequired();

        builder.HasOne(rt => rt.User)
               .WithMany(u => u.RefreshTokens)
               .IsRequired();
    }
}
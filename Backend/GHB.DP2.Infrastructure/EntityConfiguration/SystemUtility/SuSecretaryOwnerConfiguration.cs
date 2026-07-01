namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuSecretaryOwnerConfiguration : EntityTypeConfigurationBase<SuSecretaryOwner, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuSecretaryOwner> builder)
    {
        builder.ToTable(nameof(SuSecretaryOwner), nameof(SystemUtility));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.IsPositionType)
               .IsRequired();

        builder.Property(e => e.SuUserId)
               .HasConversion<UserId.EfCoreValueConverter, UserId.EfCoreValueComparer>();

        builder.Property(e => e.BusinessUnitId)
               .HasConversion<BusinessUnitId.EfCoreValueConverter, BusinessUnitId.EfCoreValueComparer>();

        builder.Property(e => e.PositionId)
               .HasConversion<PositionId.EfCoreValueConverter, PositionId.EfCoreValueComparer>();

        builder.HasMany(e => e.Secretaries)
               .WithOne(s => s.SecretaryOwner)
               .HasForeignKey(s => s.SecretaryOwnerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attachments)
               .WithOne(a => a.SecretaryOwner)
               .HasForeignKey(a => a.SecretaryOwnerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SuUser)
               .WithOne(u => u.SecretaryOwner)
               .HasForeignKey<SuSecretaryOwner>(e => e.SuUserId)
               .IsRequired(false);

        builder.OwnsAuditInfo();
        builder.HasActivityInfo();
    }
}

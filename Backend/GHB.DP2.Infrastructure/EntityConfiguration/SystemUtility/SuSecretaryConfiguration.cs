namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuSecretaryConfiguration : EntityTypeConfigurationBase<SuSecretary, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuSecretary> builder)
    {
        builder.ToTable(nameof(SuSecretary), nameof(SystemUtility));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.SecretaryOwnerId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(e => e.SuUserId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(e => e.Sequence)
               .IsRequired();

        builder.Property(e => e.PositionId)
               .HasConversion<PositionId.EfCoreValueConverter, PositionId.EfCoreValueComparer>();

        builder.HasOne(e => e.SuUser)
               .WithMany()
               .HasForeignKey(e => e.SuUserId);

        builder.OwnsAuditInfo();
    }
}

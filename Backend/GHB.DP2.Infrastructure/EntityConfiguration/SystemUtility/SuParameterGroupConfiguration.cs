namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuParameterGroupConfiguration : EntityTypeConfigurationBase<SuParameterGroup, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuParameterGroup> builder)
    {
        builder.ToTable(nameof(SuParameterGroup), nameof(SystemUtility));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.HasAlternateKey(p => p.Code);

        builder.Property(p => p.Code)
               .HasMaxLength(50)
               .IsRequired()
               .HasVogenConversion();

        builder.Property(p => p.Label)
               .HasMaxLength(1000)
               .IsRequired();

        builder.HasOne(p => p.Parent)
               .WithMany(p => p.Children)
               .HasForeignKey(p => p.ParentId)
               .IsRequired(false);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}
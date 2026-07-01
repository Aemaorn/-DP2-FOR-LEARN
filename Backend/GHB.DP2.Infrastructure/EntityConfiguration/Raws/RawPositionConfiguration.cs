namespace GHB.DP2.Infrastructure.EntityConfiguration.Raws;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RawPositionConfiguration : EntityTypeConfigurationBase<RawPosition, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RawPosition> builder)
    {
        builder.ToTable(nameof(RawPosition), nameof(Raws));

        builder.HasKey(e => e.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PositionCode)
               .IsRequired();

        builder.Property(p => p.Grade)
               .IsRequired();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.Name)
               .IsRequired();

        builder.Property(p => p.Remark);

        builder.Property(p => p.InRefCode)
               .IsRequired();

        builder.Property(p => p.InRefLevel)
               .IsRequired();

        builder.Property(p => p.CreatedAt)
               .IsRequired();

        builder.Property(p => p.UpdatedAt);
    }
}
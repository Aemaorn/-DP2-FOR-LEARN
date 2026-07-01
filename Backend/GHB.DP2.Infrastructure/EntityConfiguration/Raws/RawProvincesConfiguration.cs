namespace GHB.DP2.Infrastructure.EntityConfiguration.Raws;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RawProvincesConfiguration : EntityTypeConfigurationBase<RawProvinces, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RawProvinces> builder)
    {
        builder.ToTable(nameof(RawProvinces), nameof(Raws));

        builder.HasKey(e => e.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Code)
              .IsRequired();

        builder.Property(p => p.NameTh)
              .IsRequired();

        builder.Property(p => p.NameEn);

        builder.Property(p => p.IsActive);

        builder.Property(p => p.Sequence);

        builder.Property(p => p.CreatedAt)
              .IsRequired();

        builder.Property(p => p.UpdatedAt);
    }
}

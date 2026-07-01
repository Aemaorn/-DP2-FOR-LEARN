namespace GHB.DP2.Infrastructure.EntityConfiguration.Raws;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RawSubDistrictConfiguration : EntityTypeConfigurationBase<RawSubDistrict, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RawSubDistrict> builder)
    {
        builder.ToTable(nameof(RawSubDistrict), nameof(Raws));

        builder.HasKey(e => e.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.DistrictCode);

        builder.Property(p => p.Code)
              .IsRequired();

        builder.Property(p => p.NameTh)
              .IsRequired();

        builder.Property(p => p.NameEn);

        builder.Property(p => p.ZipCode);

        builder.Property(p => p.IsActive);

        builder.Property(p => p.Sequence);

        builder.Property(p => p.CreatedAt)
              .IsRequired();

        builder.Property(p => p.UpdatedAt);
    }
}

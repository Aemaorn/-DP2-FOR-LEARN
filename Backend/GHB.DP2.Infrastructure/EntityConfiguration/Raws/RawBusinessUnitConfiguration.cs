namespace GHB.DP2.Infrastructure.EntityConfiguration.Raws;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RawBusinessUnitConfiguration : EntityTypeConfigurationBase<RawBusinessUnit, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RawBusinessUnit> builder)
    {
        builder.ToTable(nameof(RawBusinessUnit), nameof(Raws));

        builder.HasKey(e => e.Id);

        builder.Property(b => b.Id)
               .HasVogenConversion();

        builder.Property(b => b.BusinessUnitCode)
               .IsRequired();

        builder.Property(b => b.Name)
               .IsRequired();

        builder.Property(b => b.ShortName)
               .IsRequired();

        builder.Property(b => b.OrganizationLevel)
               .IsRequired();

        builder.Property(b => b.Value)
               .IsRequired();

        builder.Property(b => b.Value2)
               .IsRequired();

        builder.Property(b => b.Value3)
               .IsRequired();

        builder.Property(b => b.Level)
               .IsRequired();

        builder.Property(b => b.Remark);

        builder.Property(b => b.CreatedAt)
               .IsRequired();

        builder.Property(b => b.UpdatedAt);

        builder.HasOne(b => b.Parent)
               .WithMany(b => b.Children)
               .HasForeignKey(b => b.ParentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
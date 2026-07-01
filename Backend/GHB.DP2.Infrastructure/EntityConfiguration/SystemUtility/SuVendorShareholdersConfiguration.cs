namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuVendorShareholdersConfiguration : EntityTypeConfigurationBase<SuVendorShareholders, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuVendorShareholders> builder)
    {
        builder.ToTable(nameof(SuVendorShareholders), nameof(SystemUtility));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.VendorId)
               .HasConversion<SuVendorId.EfCoreValueConverter, SuVendorId.EfCoreValueComparer>()
               .IsRequired(false);

        builder.Property(e => e.Sequence)
               .IsRequired(false);

        builder.Property(e => e.FirstName)
               .HasMaxLength(500)
               .IsRequired(false);

        builder.Property(e => e.LastName)
               .HasMaxLength(500)
               .IsRequired(false);

        builder.Property(e => e.IsDirector)
               .IsRequired(false);

        builder.Property(e => e.IsShareholder)
               .IsRequired(false);

        builder.Property(e => e.IsJuristic)
               .IsRequired(false);

        builder.HasOne(e => e.Vendor)
               .WithMany()
               .HasForeignKey(e => e.VendorId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}
